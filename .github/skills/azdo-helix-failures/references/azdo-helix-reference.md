# Azure DevOps and Helix Reference

## Build Definition IDs

Key Azure DevOps build definitions for dotnet/runtime:

| Definition ID | Name | Description |
|---------------|------|-------------|
| `129` | runtime | Main PR validation build |
| `133` | runtime-dev-innerloop | Fast innerloop validation |
| `139` | dotnet-linker-tests | ILLinker/trimming tests |

## Azure DevOps Organizations

**Public builds (default):**
- Organization: `dnceng-public`
- Project: `cbb18261-c48f-4abb-8651-8cdcb5474649`

**Internal/private builds:**
- Organization: `dnceng`
- Project GUID: Varies by pipeline

Override with:
```powershell
scripts/Get-HelixFailures.ps1 -BuildId 1276327 -Organization "dnceng" -Project "internal-project-guid"
```

## Common Pipeline Names

| Pipeline | Description |
|----------|-------------|
| `runtime` | Main PR validation build |
| `runtime-dev-innerloop` | Fast innerloop validation |
| `dotnet-linker-tests` | ILLinker/trimming tests |
| `runtime-wasm-perf` | WASM performance tests |
| `runtime-libraries enterprise-linux` | Enterprise Linux compatibility |

## Useful Links

- [Azure DevOps Build](https://dev.azure.com/dnceng-public/public/_build?definitionId=129): Main runtime build
- [Helix Portal](https://helix.dot.net/): View Helix jobs and work items
- [Helix API Documentation](https://helix.dot.net/swagger/): Swagger docs
- [Build Analysis](https://github.com/dotnet/arcade/blob/main/Documentation/Projects/Build%20Analysis/LandingPage.md): Known issues tracking
- [Triaging Failures Guide](https://github.com/dotnet/runtime/blob/main/docs/workflow/ci/triaging-failures.md): Official docs
- [Area Owners](https://github.com/dotnet/runtime/blob/main/docs/area-owners.md): Find the right person

## Test Execution Types

### Helix Tests
Tests run on Helix distributed test infrastructure. The script extracts console log URLs and can fetch detailed failure info with `-ShowLogs`.

### Local Tests (Non-Helix)
Some repositories (e.g., dotnet/sdk) run tests directly on the build agent. The script detects these and extracts Azure DevOps Test Run URLs.

## Known Issue Labels

- `Known Build Error` - Used by Build Analysis across dotnet repositories
- Search: `repo:dotnet/runtime is:issue is:open label:"Known Build Error" <test-name>`
