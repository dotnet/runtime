# System.Runtime.Intrinsics
Contains types used to create and convey register states in various sizes and formats for use with instruction-set extensions. Also exposes select instruction-set extensions for various architectures (`x86`, `Arm`, `Wasm`).

Documentation can be found here: https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Runtime.Intrinsics+label%3A%22help+wanted%22+) issues.

## Source
* `Vector64/128/256/512`: [../../coreclr/System.Private.CoreLib/src/System/Runtime/Intrinsics](../../coreclr/System.Private.CoreLib/src/System/Runtime/Intrinsics)
    * Tests live in [./tests/Vectors](./tests/Vectors)
* `Arm` intrinsics: [../../coreclr/System.Private.CoreLib/src/System/Runtime/Intrinsics/Arm](../../coreclr/System.Private.CoreLib/src/System/Runtime/Intrinsics/Arm)
* `Wasm` intrinsics: [../../coreclr/System.Private.CoreLib/src/System/Runtime/Intrinsics/Wasm](../../coreclr/System.Private.CoreLib/src/System/Runtime/Intrinsics/Wasm)
* `x86` intrinsics: [../../coreclr/System.Private.CoreLib/src/System/Runtime/Intrinsics/x86](../../coreclr/System.Private.CoreLib/src/System/Runtime/Intrinsics/x86)
* Tests for this library live in [./tests](./tests).
* JIT tests for this library live in [../../tests/JIT/HardwareIntrinsics](../../tests/JIT/HardwareIntrinsics)

## Deployment
`System.Runtime.Intrinsics` is included in the shared framework.