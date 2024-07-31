# System.Runtime.Intrinsics
Contains types used to create and convey register states in various sizes and formats for use with instruction-set extensions. Also exposes select instruction-set extensions for various architectures (`x86`, `Arm`, `Wasm`).

Documentation can be found here: https://learn.microsoft.com/dotnet/api/system.runtime.intrinsics.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](/src/libraries/README.md#primary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Runtime.Intrinsics+label%3A%22help+wanted%22+) issues.

## Source
* `Vector64/128/256/512`: [../System.Private.CoreLib/src/System/Runtime/Intrinsics](../System.Private.CoreLib/src/System/Runtime/Intrinsics)
* `Arm` intrinsics: [../System.Private.CoreLib/src/System/Runtime/Intrinsics/Arm](../System.Private.CoreLib/src/System/Runtime/Intrinsics/Arm)
* `Wasm` intrinsics: [../System.Private.CoreLib/src/System/Runtime/Intrinsics/Wasm](../System.Private.CoreLib/src/System/Runtime/Intrinsics/Wasm)
* `x86` intrinsics: [../System.Private.CoreLib/src/System/Runtime/Intrinsics/x86](../System.Private.CoreLib/src/System/Runtime/Intrinsics/x86)

## Tests
* `Vector64/128/256/512`: [./tests/Vectors](./tests/Vectors)
* JIT integration: [/src/tests/JIT/HardwareIntrinsics](/src/tests/JIT/HardwareIntrinsics)
* Everything else: [./tests](./tests)

## Deployment
`System.Runtime.Intrinsics` is included in the shared framework.