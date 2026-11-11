# Characterization Tests

## Running the tests

```bash
dotnet test
```

On the **first run**, Verify will write `.verified.txt` files and fail — this is expected.
Review the generated files, then accept them:

```bash
# Accept all pending snapshots (requires Verify.Tool)
dotnet tool install --global Verify.Tool
dotnet verify accept
```

Or manually rename `.received.txt` files to `.verified.txt`.

On **subsequent runs**, the tests will pass as long as the serializer output is identical.

## Try it: simulate a behavioral change

1. Open `LegacyNewtonsoftSerializer.cs` and change `NullValueHandling.Include` to `NullValueHandling.Ignore`.
2. Run `dotnet test` — `Serialize_NullFields_IncludedInOutput` will fail.
3. Verify shows you exactly which fields disappeared from the output.
4. This is the kind of silent change that would reach production without these tests.

## Commit the .verified.txt files

The `.verified.txt` files are part of the test suite — commit them to source control.
When a dependency update changes the output, Verify fails and shows a diff,
giving you an explicit decision point rather than a silent regression.
