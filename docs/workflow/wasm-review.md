# Reviewing WebAssembly changes

The browser and WASI build infrastructure under `src/mono` serves both CoreCLR
and Mono. Follow the active import and call paths rather than inferring the
runtime or shipped behavior from a directory name.

For build, test and debugging commands, use the
[WebAssembly documentation index](wasm-documentation.md).
For integration-test conventions, see
[Wasm.Build.Tests](../../src/mono/wasm/Wasm.Build.Tests/README.md#reviewing-test-changes).

## Find the implementation that ships

| Area | Starting point | Review question |
| --- | --- | --- |
| SDK-side app processing | [WebAssembly SDK pack](../../src/mono/nuget/Microsoft.NET.Sdk.WebAssembly.Pack) and [tasks](../../src/tasks/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks) | Which SDK imports and packaged tasks does an ordinary application actually use? |
| Native workload build/relink | [Browser build](../../src/mono/browser/build/README.md) and [shared Wasm build](../../src/mono/wasm/build/README.md) | Does this operation need native tools, or should it also work without the workload? |
| Runtime repository build | [In-tree targets](../../src/mono/browser/build/WasmApp.InTree.targets) | Is this repository-only setup being kept out of shipped application targets? |
| Mirrored ReadyToRun logic | [Crossgen2Tasks README](../../src/tasks/Crossgen2Tasks/README.md) | Does the equivalent SDK implementation need the same change, or is the observed behavior confined to a test shim? |
| Test project setup | [WBT README](../../src/mono/wasm/Wasm.Build.Tests/README.md) | Is the intended runtime/pack selected by the existing helper, rather than an SDK default or duplicate asset-local override? |

Inspect package contents and imports, not just the source copy. Identify any
coordinated SDK change needed for the shipped path. Keep repository layout and
repository-only properties in in-tree setup. Put runtime-specific behavior in
the corresponding runtime-specific targets, but keep shared asset processing in
the shared pipeline. Managed trimming, ReadyToRun and native relinking have
different tool requirements; check the actual gates before coupling them.

For shared native inputs, trace each MSBuild-to-CMake bridge. For example,
[`eng/native.wasm.targets`](../../eng/native.wasm.targets) supplies
`NativeCMakeArg`, while application relinking is handled by the runtime-specific
Browser/WASI app targets. Reuse common wasm-opt feature flags without merging
runtime-specific optimization, debug or stripping policies.

## Check configuration at the point it is consumed

- Treat build/publish and Debug/Release as independent dimensions. A Debug
  publish is still a publish. For publish-default changes, check both CLI
  publishing and direct MSBuild `Publish`; do not assume a CLI-only property is
  set on every target path.
- Check defaults at the relevant import/evaluation or target-execution point,
  including nested publish when affected. Preserve explicit overrides; do not
  pass the property under test as a global property when testing its default.
- Gate new formats and behavior on the consuming target framework, not the
  runtime repository's current version. Reuse the grouped framework predicates
  where applicable and cover the affected compatibility boundary.
- Derive runtime/toolchain metadata from its owner, not an accidentally
  available application property such as `ProductVersion`. Verify shipped
  behavior outside the repository's inherited build properties.

Choose coverage for the changed contract and affected configurations, not an
unconditional Cartesian product.

## Review incremental and deployment contracts

For changes to generated outputs or command-line options, check that incremental
inputs account for command-affecting values and that outputs model every file
the consumer needs. Keep `FileWrites`/cleanup consistent. Exercise the affected
contract with an unchanged rebuild, an option toggle or a missing secondary
output as appropriate; inspecting the new command line alone cannot establish
that a previously built output will be regenerated.

Prefer modeling an additional compiler output at its producer to deleting the
primary output to force recompilation. If SDK compatibility requires a fallback,
explain that constraint and identify the follow-up/removal condition.

Trace browser assets through their actual consumers: ordinary
`ResolvedFileToPublish`, Wasm bundle/boot processing and Static Web Assets (SWA)
are not interchangeable item lists. Preserve the existing shared and
user-supplied fingerprint inputs when registering an asset. For incremental
deployment changes, compare successive publishes with an ordinary existing
asset before introducing feature-specific cleanup; check stale fingerprints and
compressed variants where affected. A CoreCLR-only feature does not by itself
justify CoreCLR-only handling of a shared SWA lifecycle.
