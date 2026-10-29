---
name: sbom-create
description: Generate a CycloneDX Software Bill of Materials (SBOM) in JSON format for the active .NET solution or project using the `dotnet CycloneDX` global tool. Use whenever the user asks for an SBOM, a CycloneDX file, a "software bill of materials", a supply-chain inventory, or wants `dotnet CycloneDX` run against a .sln/.slnx/.csproj. Produces docs/sbom/sbom.json in CycloneDX JSON format covering all direct and transitive NuGet dependencies.
---

# Generate SBOM Skill

Generate a CycloneDX SBOM (JSON) for the active .NET solution or project and write it to `docs/sbom/sbom.json`.

## Arguments

- Optional path to a `.sln`, `.slnx`, or `.csproj`. If omitted, auto-detect in the current working directory in this order: `*.sln`, `*.slnx`, single `*.csproj`. If multiple solutions/projects exist and none was specified, pick the first `*.sln`/`*.slnx` alphabetically and note the choice in the report-back line.

## Steps

1. **Resolve the target.** Find the solution or project file per the rule above. Record its path; this is `<target>`.

2. **Ensure the CycloneDX tool is installed.** Probe with `dotnet tool list --global`. If `CycloneDX` is not listed, install it: `dotnet tool install --global CycloneDX`. (If the user later wants a pinned version, install with `--version <x.y.z>`.) If `dotnet` itself is unavailable, stop and tell the user.

3. **Restore packages.** Run `dotnet restore <target>`. CycloneDX reads the project's restored asset graph (`obj/project.assets.json`) — without a restore the generated SBOM will be empty or missing transitive nodes.

4. **Create the output directory.** Ensure `docs/sbom/` exists (create it if not). The skill writes only into `docs/sbom/`; existing files there with the same name are overwritten.

5. **Run CycloneDX.** Invoke:

   ```
   dotnet CycloneDX <target> --json --output docs/sbom --filename sbom.json
   ```

   - `--json` (alias `-j`) selects JSON output instead of the default XML.
   - `--output` (alias `-o`) is a **directory**, not a filename.
   - `--filename` (alias `-fn`) sets the output filename within that directory.
   - For a multi-project solution, CycloneDX walks every referenced `.csproj` automatically — no need to loop over projects yourself.

   Capture stdout+stderr. If the command exits non-zero, surface the full output to the user and stop — do not write a partial SBOM.

6. **Verify the file.** Confirm `docs/sbom/sbom.json` exists, is non-empty, and parses as JSON whose top-level `bomFormat` is `"CycloneDX"`. If verification fails, report the error rather than claiming success.

7. **Report back.** Print a one-line confirmation with the output path and a quick stat line (e.g., `Wrote docs/sbom/sbom.json — CycloneDX 1.x, <N> components, spec version <x.y>`). Pull `bomFormat`, `specVersion`, and the `components` array length from the generated JSON.

## Notes

- Use **PowerShell** for `dotnet` commands on Windows; Bash otherwise. The CycloneDX CLI is identical across shells.
- The `dotnet CycloneDX` global tool is community-maintained (`CycloneDX/cyclonedx-dotnet`); first-time install pulls it from NuGet.org and takes ~10–20s.
- CycloneDX includes transitive packages by default for SDK-style projects — no extra flag needed. Older `packages.config` projects may require `--enable-github-licenses` or other flags; if the target uses that style, stop and ask the user before adding flags.
- This skill produces **only** the JSON SBOM. If the user wants XML alongside JSON, run CycloneDX a second time without `--json` and with `--filename sbom.xml`.
- Do not commit the output or stage it for git — just write the file. `docs/sbom/sbom.json` is regenerable from `obj/project.assets.json` at any time.
- The SBOM contains package identifiers and versions but **no proprietary source code** — it's safe to share with security teams or upload to dependency-track / Dependency-Track style tools.
