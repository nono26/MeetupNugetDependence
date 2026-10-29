---
name: sbom-enrich
description: Enrich an existing CycloneDX SBOM (docs/sbom/sbom.json) with vulnerability data by scanning it with grype. Use whenever the user asks to "enrich an SBOM", "add vulnerabilities to an SBOM", "scan an SBOM for CVEs", or wants a vulnerability-augmented copy of their CycloneDX file. Reads docs/sbom/sbom.json (never modified) and writes a new file docs/sbom/sbom-enriched.json — a CycloneDX JSON document with the original components plus a populated `vulnerabilities[]` array from grype's vulnerability database.
---

# SBOM Enrich Skill

Run `grype` against `docs/sbom/sbom.json` to look up CVEs for every component, and emit an enriched copy at `docs/sbom/sbom-enriched.json` in CycloneDX JSON format. The input file is read-only — the enriched output is a brand-new file beside it.

## Arguments

None. The skill always reads `docs/sbom/sbom.json` and writes `docs/sbom/sbom-enriched.json`. To enrich an SBOM elsewhere, copy it to `docs/sbom/sbom.json` first, or invoke `grype` directly.

## Why grype and not dependency-check

OWASP Dependency-Check is a *project scanner*: its `--scan` flag wants source artifacts (jars, exes, `package-lock.json`, `pom.xml`, …), not a finished SBOM. Pointing it at `sbom.json` produces a report with **zero dependencies** because it tries to analyze the JSON file as if it were an artifact. `grype` is purpose-built for the opposite direction — it reads an SBOM as input, looks each component up in its vulnerability DB, and emits CycloneDX JSON with `vulnerabilities[]` populated. If a future request specifically asks for dependency-check, write a separate skill that scans the project (`*.csproj`/`*.sln`) rather than re-pointing this one.

## Prerequisites

- **grype** on `PATH`. Probe with `grype version`. If absent, stop and tell the user to install it — do not silently fall back to a different scanner. Installation paths:
  - Windows (Chocolatey): `choco install grype`
  - Windows (Scoop):     `scoop install grype`
  - macOS:               `brew install grype`
  - Linux / manual:      `curl -sSfL https://raw.githubusercontent.com/anchore/grype/main/install.sh | sh -s -- -b /usr/local/bin`
- The input file `docs/sbom/sbom.json` must already exist (generate it via the `generate-sbom` skill if it doesn't).

No Java, no API key, no NVD download — grype ships its own vulnerability DB and refreshes it (~150 MB) on first use.

## Steps

1. **Validate the input.** Confirm `docs/sbom/sbom.json` exists and parses as JSON with `bomFormat == "CycloneDX"`. If missing, stop and tell the user to run `generate-sbom` first. If malformed, stop and report the JSON error — do not try to repair the file.

2. **Probe the toolchain.** Run `grype version`. If it fails, stop and surface the install instructions above. Do not attempt to install grype automatically — the manual-install script touches `/usr/local/bin` and the user should choose the install method.

3. **Stage a copy of the input and downgrade `specVersion` if needed.** Copy `docs/sbom/sbom.json` to a temp path (`$env:TEMP\sbom.input.json` on Windows, `/tmp/sbom.input.json` elsewhere) and record its SHA-256 hash. Then check the staged copy's `specVersion`: **grype 0.110.x rejects CycloneDX 1.7 with `failed to catalog: unable to decode sbom: sbom format not recognized`**, but accepts 1.6. The `dotnet CycloneDX` tool emits 1.7 by default. If the staged copy is 1.7, rewrite its `specVersion` to `"1.6"` in place (`(Get-Content … -Raw) -replace '"specVersion": "1\.7"', '"specVersion": "1.6"' | Out-File …`). The `components` and `dependencies` sections are schema-identical between 1.6 and 1.7 — the downgrade is purely a parser-compatibility fib, not a data loss. The canonical `docs/sbom/sbom.json` stays at 1.7; only the TEMP copy is modified.

4. **Run grype against the staged copy.** Invoke:

   ```
   grype sbom:<staged-temp-path> -o cyclonedx-json --file docs/sbom/sbom-enriched.json
   ```

   - The `sbom:` prefix tells grype the argument is an SBOM file, not a directory to scan.
   - `-o cyclonedx-json` selects CycloneDX JSON output. (Other useful formats: `json` for grype's native schema, `table` for human-readable.)
   - `--file` writes directly to the target path; without it, grype writes to stdout.
   - Note that the enriched output's `specVersion` will be 1.6 (echoing the staged input), not 1.7 like the canonical SBOM. That's fine — downstream CycloneDX consumers accept any 1.x.

   Capture stdout+stderr. First-run DB download takes ~30 seconds; subsequent runs take a second or two. If the command exits non-zero, surface the full output and stop — do not write a partial enriched file.

5. **Verify the enriched file.** Open `docs/sbom/sbom-enriched.json`, confirm it parses as JSON, and pull sanity values: `bomFormat == "CycloneDX"`, `specVersion`, the `components[]` length (should match the input SBOM's component count), and the `vulnerabilities[]` length. If `bomFormat` is wrong or the file failed to parse, surface the error rather than claiming success.

6. **Confirm the original is unchanged.** Recompute SHA-256 on `docs/sbom/sbom.json` and compare against the hash from step 3. If they differ, restore from the staged copy and report the discrepancy — that would mean grype mutated its input, which is unexpected and worth flagging.

7. **Report back.** Print a one-line confirmation with the output path and a stat line: e.g., `Wrote docs/sbom/sbom-enriched.json — CycloneDX 1.x, 103 components, 27 vulnerabilities (3 Critical / 9 High / 12 Medium / 3 Low)`. Pull `specVersion`, `components` count, and the severity breakdown from `vulnerabilities[].ratings[].severity` in the enriched JSON.

## Notes

- Grype's vulnerability DB lives in `~/.cache/grype/db/` (Linux/macOS) or `%LOCALAPPDATA%\grype\db\` (Windows). It auto-updates roughly every 24 hours; the skill does not force-refresh on every run. To force a refresh, run `grype db update` before step 4.
- **CycloneDX 1.7 / grype 0.110.x parser gap (Step 3).** This was discovered the hard way: grype 0.110.0 emits `sbom format not recognized` against a 1.7 SBOM but accepts an otherwise-identical 1.6 SBOM. When grype catches up to 1.7, the downgrade hack in step 3 becomes a no-op and can stay in place. Check release notes if the skill ever stops needing it.
- Grype uses Anchore's vulnerability feeds (NVD, GitHub Security Advisories, vendor advisories) — the package coverage is broader than `dotnet list package --vulnerable` (which uses GitHub's advisory database only). Expect more findings than the NuGet audit reports, including some Anchore may flag that NuGet doesn't.
- Use **PowerShell** for the commands on Windows; Bash otherwise. The grype CLI is identical across shells.
- The enriched JSON is **valid CycloneDX 1.x** — downstream tools that consume CycloneDX (Dependency-Track, GitHub Dependency Submission API, OWASP CycloneDX viewer) will accept it directly.
- Do not commit the enriched output or stage it for git — it's regenerable from `sbom.json` and includes large free-text CVE descriptions. Consider adding `docs/sbom/sbom-enriched.json` to `.gitignore` if it isn't already.
- If the user previously generated a dependency-check-based `sbom-enriched.json` (empty, dependency-check schema), this skill overwrites it with the correct CycloneDX-format output on the next run.
