# cDAC Scripts

Ad-hoc developer tools for inspecting .NET crash dumps using the cDAC (contract-based Data Access) reader.

## cdac-dump-inspect

A command-line tool that opens a .NET process dump with [ClrMD](https://github.com/microsoft/clrmd) and the cDAC, and runs diagnostic commands against it.

### Prerequisites

- A .NET SDK — the script prefers the repo-local `.dotnet` SDK (installed by `build.cmd`/`build.sh`) but falls back to any `dotnet` on your PATH
- A .NET crash dump (Windows minidump, Linux coredump, or macOS coredump) from a runtime that includes the `DotNetRuntimeContractDescriptor` export

### Quick Start

From the repo root:

```powershell
# Print the contract descriptor (contracts, types, globals)
./src/native/managed/cdac/scripts/cdac-dump-inspect.ps1 descriptor <dump-path>

# List managed threads
./src/native/managed/cdac/scripts/cdac-dump-inspect.ps1 threads <dump-path>

# Print managed stack traces
./src/native/managed/cdac/scripts/cdac-dump-inspect.ps1 stacks <dump-path>
```

Or run directly with `dotnet run`:

```powershell
.dotnet/dotnet run --project src/native/managed/cdac/scripts/cdac-dump-inspect.csproj -c Release -- descriptor <dump-path>
```

### Commands

| Command | Description |
|---------|-------------|
| `descriptor` | Print the full contract descriptor: version, baseline, contracts with versions, types with fields, globals, and sub-descriptors. Detects merge conflicts across descriptors. |
| `threads` | List all managed threads with OS ID, thread state, and address |
| `stacks` | Walk the managed stack for each thread, showing instruction pointers and method descriptors |

### Options (PowerShell wrapper)

| Option | Description |
|--------|-------------|
| `-Release` | Build in Release configuration (default: Debug) |

### Examples

```powershell
# Inspect contracts in a Linux coredump
./src/native/managed/cdac/scripts/cdac-dump-inspect.ps1 descriptor ~/dumps/crash.coredump

# List threads in a Windows minidump
./src/native/managed/cdac/scripts/cdac-dump-inspect.ps1 threads C:\dumps\app.dmp

# Get stack traces (Release build for no debug assertions)
./src/native/managed/cdac/scripts/cdac-dump-inspect.ps1 stacks C:\dumps\app.dmp -Release
```

### Notes

- The dump must be from a runtime that embeds the cDAC contract descriptor (the `DotNetRuntimeContractDescriptor` export in coreclr). Older runtimes or stripped builds may not include it.
- The tool reads dumps using ClrMD's `DataTarget.LoadDump`, which supports Windows minidumps, Linux ELF coredumps, and macOS Mach-O coredumps.
- Thread and stack commands require matching data descriptor versions between the dump's runtime and the locally-built cDAC contracts. Version mismatches may produce errors or empty results.
- For Release builds, debug assertions in the cDAC are disabled, which allows reading dumps with minor version mismatches.

### See Also

- [cDAC overview](../README.md)
- [cDAC tests](../tests/README.md)
- [Contract descriptor format](../../../../../docs/design/datacontracts/contract-descriptor.md)
