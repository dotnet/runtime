---
applyTo: "src/mono/wasm/**,src/mono/browser/**,src/mono/wasi/**,src/mono/nuget/Microsoft.NET.Sdk.WebAssembly.Pack/**,src/mono/nuget/Microsoft.NET.Runtime.WebAssembly.Sdk/**,src/mono/nuget/Microsoft.NET.Runtime.WebAssembly.Wasi.Sdk/**,src/mono/nuget/Microsoft.NET.Workload.Mono.Toolchain.Current.Manifest/**,src/native/libs/Common/JavaScript/**,src/tasks/WasmAppBuilder/**,src/tasks/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks/**,src/tasks/Microsoft.NET.WebAssembly.Webcil/**,eng/native.wasm.targets,src/tasks/Crossgen2Tasks/Microsoft.NET.CrossGen.targets"
---

# WebAssembly

For Wasm build, packaging, loader and test changes, read the
[Wasm review guide](/docs/workflow/wasm-review.md). For WBT or test-asset changes,
also read [WBT review conventions](/src/mono/wasm/Wasm.Build.Tests/README.md#reviewing-test-changes).
These supplement the applicable language and area instructions.

- Trace the shipped SDK/workload/in-tree implementation; a WBT shim is not proof
  of shipped behavior. For browser/WASI ReadyToRun changes in mirrored targets,
  follow the [Crossgen2Tasks equivalence guidance](/src/tasks/Crossgen2Tasks/README.md).
- Check build/publish defaults and framework boundaries at their actual consumer.
  For changed generation/deployment contracts, follow incremental outputs and
  SWA consumers rather than assuming ordinary publish copy lists cover them.
- In WBT, reuse scenarios, categories and output helpers. Prefer appropriately
  scoped binlog observations to failing assertion targets where they observe
  the contract; retain the runtime/artifact coverage that proves behavior.
