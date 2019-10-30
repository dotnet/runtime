Implementation of Hardware Intrinsics in CoreCLR
================================================
This document describes the implementation of hardware intrinsics in CoreCLR.
For information about how the intrinsic APIs are designed, proposed and approved,
see https://github.com/dotnet/designs/blob/master/accepted/platform-intrinsics.md.

In discussing the hardware intrinsics, we refer to the target platform, such as X86 or Arm64, as the "platform" and each set of extensions that are implemented as a unit (e.g. AVX2 on X64 or Simd on Arm64) as an "ISA".

There is a design document for the Arm64 intrinsics: https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/arm64-intrinsics.md. It should be updated to reflect current (and ongoing) progress.

## Overview

The reference assemblies for the hardware intrinsics live in corefx, but all of the implementation is in the coreclr repo:

* The C# implementation lives in coreclr/src/System.Private.CoreLib/shared/System/Runtime/Intrinsics. These are little more than skeleton methods that are only compiled if needed for indirect invocation.

  * Note that they are mirrored to other repositories, including corefx, corert and mono.

## C# Implementation

The hardware intrinsics operate on and produce both primitive types (`int`, `float`, etc.) as well as vector types.

### Platform-agnostic vector types

The vector types supported by one or more target ISAs are supported across platforms, though they extent to which operations on them are available and accelerated is dependent on the target ISA. These are:

* `Vector64<T>` - A 64-bit vector of type `T`. For example, a `Vector64<int>` would hold two 32-bit integers.
  * Note that `Vector64<T>` intrinsics are currently supported only on Arm64, and these are not supported for `double`. Support could be added for this, but would require additional handling.
* `Vector128<T>` - A 128-bit vector of type `T`
* `Vector256<T>` - A 256-bit vector of type `T`
  * `Vector256<T>` intrinsics are supported only on x86 (and x64).

Note that these are generic types, which distinguishes these from native intrinsic vector types. It also somewhat complicates interop, as the runtime currently doesn't support interop for generic types. See https://github.com/dotnet/coreclr/issues/1685

Not all intrinsics defined on these types support all primitive type parameters. When not supported, they are expected to throw `NotSupportedException`. This is generally handled by the C# implementation code, though for the most part this is a non-issue, as the ISA-specific intrinsics are declared over all supported concrete types (e.g. `Vector128<float>` rather than `Vector128<T>`).

The C# declaration of a hardware intrinsic ISA class is marked with the `[Intrinsic]` attribute, and the implementations of the intrinsic methods on that class are recursive. When the VM encounters such a method, it will communicate to the JIT that this is an intrinsic method, and will also pass a `mustExpand` flag to indicate that the JIT must generate code. This allows these methods to be invoked indirectly to support the following scenarios:

* Debugging
* Reflection invocation
* Execution of an intrinsic with a non-constant operand when an immediate operand is required by the target instruction

This implementation approach has the downside that if the C# method bodies were not recursive, the IL for simple check and throw method bodies could be shared across intrinsics.


## JIT Implementation

The bulk of the implementation work for hardware intrinsics is in the JIT.

### Platform Target Information

The JIT depends on the VM and configuration settings to determine what target platform to generate code for. The VM settings are communicated in the `JitFlags` on `Compiler::opts` and the JIT checks the various `COMPlus_EnableXXX` configuration settings as well. See `Compiler::compSetProcessor()` and `jitconfigvalues.h`.

### Importation

Hardware intrinsics are built on RyuJIT's `NamedIntrinsic` mechanism to identify method calls that should be recognized as intrinsics (see https://github.com/dotnet/coreclr/blob/master/src/jit/namedintrinsiclist.h). In the incoming IL, intrinsic invocations are just method calls, so the JIT must distinguish intrinsic calls from ordinary call-sites and map them to its IR representation: the `GenTreeHWIntrinsic` node.

The [Intrinsic] attribute was added to eliminate the need to check each call-site. It [Intrinsic] attribute has a different meaning on each attribute target:

* Method: call targets marked with [Intrinsic] will be checked by the JIT when importing call-sites. If the method's (namespace, class name, method name) triple matches a record in the Hardware Intrinsics Table, it will be recognized as an intrinsic call.

* Struct: value types marked with [Intrinsic] are recognized by JIT as special types (e.g., TYP_SIMD16, TYP_SIMD32, etc.) in the IR. Currently, RyuJIT is using [Intrinsic] to distinguish Vector64<T>, Vector128<T>, Vector256<T> from other struct types. The VM also uses this to special-case the layout (packing) for these types

* Class: marking reference types with [Intrinsic] causes any member methods to be considered as possible intrinsics. For example, although the methods in the `Avx` class do not have [Intrinsic] but the `Avx` class itself does, which causes all of its methods to be treated as if they also have the attribute.

Currently, the JIT determines in the importer whether it will:

* Generate code for the intrinsic (i.e. it is recognized and supported on the current platform)
* Generate a call (e.g. if it is a recognized intrinsic but an operand is not immediate as it is expected to be). The `mustExpand` option, which is returned by the VM as an "out" parameter to the `getIntrinsicID` method, must be false in this case.
* Throw `PlatformNotSupportedException` if it is not a recognized and supported intrinsic for the current platform.

There is some room for improvement here. For example, it may be that an argument that appears to be non-constant could later be determined to be a constant value (https://github.com/dotnet/coreclr/issues/17108).

### Hardware Intrinsics Table

There is a hardware intrinsics table for each platform that supports hardware intrinsics: currently `_TARGET_XARCH_` and `TARGET_ARM64_`. They live in hwintrinsiclistxarch.h and hwintrinsiclistarm64.h respectively.

These tables are intended to capture information that can assist in making the implementation as data-driven as possible.

Note that the x86/x64 implementation is shared, while currently the Arm64 intrinsics are not shared with Arm. Where there is overlap between Arm and Arm64, it may be reasonable to implement those intrinsics in a shared space, to leverage both API and JIT implementation.

### IR

The hardware intrinsics nodes are generally imported as `GenTreeHWIntrinsic` nodes, with the `GT_HWIntrinsic` operator. On these nodes:
* The `gtHWIntrinsicId` field contains the intrinsic ID, as declared in the hardware intrinsics table
* The `gtSIMDBaseType` field indicates the "base type" (generic type argument).
* The `gtSIMDSize` field indicates the full byte width of the vector (e.g. 16 bytes for `Vector128<T>`).

### Lowering

As described here: https://github.com/dotnet/coreclr/blob/master/Documentation/botr/ryujit-overview.md#lowering, Lowering is responsible for transforming the IR in such a way that the control flow, and any register requirements, are fully exposed. This includes determining what instructions can be "contained" in another, such as immediates or addressing modes. For the hardware intrinsics, these are done in the target-specific methods `Lowering::LowerHWIntrinsic()` and `Lowering::ContainCheckHWIntrinsic()`.

The main consideration here is whether there are child nodes that are folded into the generated instruction. These may be:
* An immediate operand
* A memory operand

It is the job of `Lowering` to perform the necessary legality checks, and then to mark them as contained (`GenTree::SetContained()`) as appropriate.

### Register Allocation

The register allocator has three main passes.

The `LinearScan::buildNode` method is responsible for identifying all register references in the IR, and constructing the `RefPosition`s that represent those references, for each node. For hardware intrinsics it delegates this function to `LinearScan::buildHWIntrinsic()` and the `LinearScan::getKillSetForHWIntrinsic()` method is responsible for generating kill `RefPositions` for these nodes.

The other thing to be aware of is that the calling convention for large vectors (256-bit vectors on x86, and 128-bit vectors on Arm64) does not preserve the upper half of the callee-save vector registers. As a result, this require some special modeling in the register allocator. See the places where `FEATURE_PARTIAL_SIMD_CALLEE_SAVE` appears in the code. This code, fortunately, requires little differentiation between the two platforms.

## Code Generation

By design, the actual code generation is fairly straightforward, since the hardware intrinsics are intended to each map to a specific target instructions. Much of the implementation of the x86 intrinsics is table-driven. 

## Encoding

The only thing that makes the hardware intrinsics different in the area of instruction encodings is that they depend on many instructions (and their encodings) that are not used in any context other than the implementation of the associated hardware intrinsic.

The encodings are largely specified by `coreclr\src\jit\instrs{arch}.h`, and most of the target-specific code is in the `emit{arch}.*` files.

This is an area of the JIT that could use some redesign and refactoring (https://github.com/dotnet/coreclr/issues/23006 and https://github.com/dotnet/coreclr/issues/21441 among others).

## Testing

The tests for the hardware intrinsics reside in the coreclr/tests/src/JIT/HardwareIntrinsics directory.

Many of the tests are generated programmatically from templates. See `coreclr\tests\src\JIT\HardwareIntrinsics\General\Shared\GenerateTests.csx`. We would like to see most, if not all, of the remaining tests converted to use this mechanism.

