# Renamed WebAssembly MSBuild properties and environment variables for EventPipe diagnostics

## Scope
Major

## Version Introduced
.NET 9.0

## Source of Breaking Change
Renamed MSBuild properties and environment variables for consistency with .NET naming guidelines.

## Change Description
In .NET 9.0, several WebAssembly MSBuild properties and environment variables have been renamed to follow .NET naming guidelines, which recommend using full words instead of abbreviations in identifiers:

| Old Name | New Name |
|----------|----------|
| `WasmPerfTracing` | `WasmEnableEventPipe` |
| `WASM_PERFTRACING` | `WASM_ENABLE_EVENTPIPE` |

The new names better reflect the functionality as enabling EventPipe (diagnostic server) functionality, which is broader than just performance tracing.

## Reason for Change
This change was made to improve naming consistency in the .NET framework by following the established naming guidelines which recommend against using abbreviations in identifiers. The new names also more clearly describe the purpose of these flags, which is to enable the EventPipe functionality.

## Recommended Action
Update project files and build scripts to use the new property names:

1. Replace `<WasmPerfTracing>true</WasmPerfTracing>` with `<WasmEnableEventPipe>true</WasmEnableEventPipe>` in project files
2. Replace `WASM_PERFTRACING=1` with `WASM_ENABLE_EVENTPIPE=1` in environment variable settings or CI/CD scripts

## Affected APIs
No direct API changes, only MSBuild properties and environment variables used in project files and build scripts.

## Category
Browser Runtime, WebAssembly