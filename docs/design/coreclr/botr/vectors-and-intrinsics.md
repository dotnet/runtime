Vectors and Hardware Intrinsics Support
===
---

# Introduction
The CoreCLR runtime has support for several varieties of hardware intrinsics, and various ways to compile code which uses them. This support varies by target processor, and the code produced depends on how the jit compiler is invoked. This document describes the various behaviors of intrinsics in the runtime, and concludes with implications  for developers working on the runtime and libraries portions of the runtime.

# Acronyms and definitions
| Acronym | Definition
| --- | --- |
| AOT | Ahead of time. In this document, it refers to compiling code before the process launches and saving it into a file for later use.

# Intrinsics apis
Most hardware intrinsics support is tied to the use of various Vector apis. There are 4 major api surfaces that are supported by the runtime

- The fixed length float vectors. `Vector2`, `Vector3`, and `Vector4`. These vector types represent a struct of floats of various lengths. For type layout, ABI and, interop purposes they are represented in exactly the same way as a structure with an appropriate number of floats in it. Operations on these vector types are supported on all architectures and platforms, although some architectures may optimize various operations.
- The variable length `Vector<T>`. This represents vector data of runtime-determined length. In any given process the length of a `Vector<T>` is the same in all methods, but this length may differ between various machines or environment variable settings read at startup of the process. The `T` type variable may be the following types (`System.Byte`, `System.SByte`, `System.Int16`, `System.UInt16`, `System.Int32`, `System.UInt32`, `System.Int64`, `System.UInt64`, `System.Single`, and `System.Double`), and allows use of integer or double data within a vector. The length and alignment of `Vector<T>` is unknown to the developer at compile time (although discoverable at runtime by using the `Vector<T>.Count` api), and `Vector<T>` may not exist in any interop signature. Operations on these vector types are supported on all architectures and platforms, although some architectures may optimize various operations if the `Vector<T>.IsHardwareAccelerated` api returns true.
- `Vector64<T>`, `Vector128<T>`, and `Vector256<T>` represent fixed-sized vectors that closely resemble the fixed- sized vectors available in C++. These structures can be used in any code that runs, but very few features are supported directly on these types other than creation. They are used primarily in the processor specific hardware intrinsics apis.
- Processor specific hardware intrinsics apis such as `System.Runtime.Intrinsics.X86.Ssse3`. These apis map directly to individual instructions or short instruction sequences that are specific to a particular hardware instruction. These apis are only usable on hardware that supports the particular instruction. See https://github.com/dotnet/designs/blob/master/accepted/2018/platform-intrinsics.md for the design of these.

# How to use intrinsics apis

There are 3 models for use of intrinsics apis.

1. Usage of `Vector2`, `Vector3`, `Vector4`, and `Vector<T>`. For these, its always safe to just use the types. The jit will generate code that is as optimal as it can for the logic, and will do so unconditionally.
2. Usage of `Vector64<T>`, `Vector128<T>`, and `Vector256<T>`. These types may be used unconditionally, but are only truly useful when also using the platform specific hardware intrinsics apis.
3. Usage of platform intrinsics apis. All usage of these apis should be wrapped in an `IsSupported` check of the appropriate kind. Then, within the `IsSupported` check the platform specific api may be used. If multiple instruction sets are used, then the application developer must have checks for the instruction sets as used on each one of them.

# Effect of usage of hardware intrinsics on how code is generated

Hardware intrinsics have dramatic impacts on codegen, and the codegen of these hardware intrinsics is dependent on the ISA available for the target machine when the code is compiled.

If the code is compiled at runtime by the JIT in a just-in-time manner, then the JIT will generate the best code it can based on the current processor's ISA. This use of hardware intrinsics is indendent of jit compilation tier. `MethodImplOptions.AggressiveOptimization` may be used to bypass compilation of tier 0 code and always produce tier 1 code for the method. In addition, the current policy of the runtime is that `MethodImplOptions.AggressiveOptimization` may also be used to bypass compilation of code as R2R code, although that may change in the future.

For AOT compilation, the situation is far more complex. This is due to the following principles of how our AOT compilation model works.

1. AOT compilation must never under any circumstance change the semantic behavior of code except for changes in performance.
2. If AOT code is generated, it should be used unless there is an overriding reason to avoid using it.
3. It must be exceedingly difficult to misuse the AOT compilation tool to violate principle 1.

There are 2 different implementations of AOT compilation under development at this time. The crossgen1 model (which is currently supported on all platforms and architectures), and the crossgen2 model, which is under active development. Any developer wishing to use hardware intrinsics in the runtime or libraries should be aware of the restrictions imposed by the crossgen1 model. Crossgen2, which we expect will replace crossgen1 at some point in the future, has strictly fewer restrictions.

## Crossgen1 model of hardware intrinsic usage

###Code written in System.Private.CoreLib.dll
#### Crossgen implementation rules
- Any code which uses `Vector<T>` will not be compiled AOT. (See code which throws a TypeLoadException using `IDS_EE_SIMD_NGEN_DISALLOWED`)
- Code which uses Sse and Sse2 platform hardware intrinsics is always generated as it would be at jit time.
- Code which uses Sse3, Ssse3, Sse41, Sse42, Popcnt, Pclmulqdq, and Lzcnt instruction sets will be generated, but the associated IsSupported check will be a runtime check. See `FilterNamedIntrinsicMethodAttribs` for details on how this is done.
- Code which uses other instruction sets will be generated as if the processor does not support that instruction set. (For instance, a usage of Avx2.IsSupported in CoreLib will generate native code where it unconditionally returns false, and then if and when tiered compilation occurs, the function may be rejitted and have code where the property returns true.)
- Non-platform intrinsics which require more hardware support than the minimum supported hardware capability will not take advantage of that capability. In particular the code generated for `Vector2/3/4.Dot`, and `Math.Round`, and `MathF.Round`. See `FilterNamedIntrinsicMethodAttribs` for details. MethodImplOptions.AggressiveOptimization may be used to disable precompilation compilation of this sub-par code.

#### Characteristics which result from rules
The rules here provide the following characteristics.
- Some platform specific hardware intrinsics can be used in CoreLib without encountering a startup time penalty
- Some uses of platform specific hardware intrinsics will force the compiler to be unable to AOT compile the code. However, if care is taken to only use intrinsics from the Sse, Sse2, Sse3, Ssse3, Sse41, Sse42, Popcnt, Pclmulqdq, or Lzcnt instruction sets, then the code may be AOT compiled. Preventing AOT compilation may cause a startup time penalty for important scenarios.
- Use of `Vector<T>` causes runtime jit and startup time concerns because it is never precompiled. Current analysis indicates this is acceptable, but it is a perennial concern for applications with tight startup time requirements.
- AOT generated code which could take advantage of more advanced hardware support experiences a performance penalty until rejitted. (If a customer chooses to disable tiered compilation, then customer code may always run slowly).

#### Code review rules for code written in System.Private.CoreLib.dll
- Any use of a platform intrinsic in the codebase MUST be wrapped with a call to the associated IsSupported property. This wrapping MUST be done within the same function that uses the hardware intrinsic, and MUST NOT be in a wrapper function unless it is one of the intrinsics that are enabled by default for crossgen compilation of System.Private.CoreLib (See list above in the implementation rules section).
- Within a single function that uses platform intrinsics, it must behave identically regardless of whether IsSupported returns true or not. This rule is required as code inside of an IsSupported check that calls a helper function cannot assume that the helper function will itself see its use of the same IsSupported check return true. This is due to the impact of tiered compilation on code execution within the process.
- Excessive use of intrinsics may cause startup performance problems due to additional jitting, or may not achieve desired performance characteristics due to suboptimal codegen.

ACCEPTABLE Code
```csharp
using System.Runtime.Intrinsics.X86;

public class BitOperations
{
    public static int PopCount(uint value)
    {
        if (Avx2.IsSupported)
        {
            Some series of Avx2 instructions that performs the popcount operation.
        }
        else
            return FallbackPath(input);
    }

    private static int FallbackPath(uint)
    {
        const uint c1 = 0x_55555555u;
        const uint c2 = 0x_33333333u;
        const uint c3 = 0x_0F0F0F0Fu;
        const uint c4 = 0x_01010101u;

        value -= (value >> 1) & c1;
        value = (value & c2) + ((value >> 2) & c2);
        value = (((value + (value >> 4)) & c3) * c4) >> 24;

        return (int)value;
    }
}
```

UNACCEPTABLE code
```csharp
using System.Runtime.Intrinsics.X86;

public class BitOperations
{
    public static int PopCount(uint value)
    {
        if (Avx2.IsSupported)
            return UseAvx2(value);
        else
            return FallbackPath(input);
    }

    private static int FallbackPath(uint)
    {
        const uint c1 = 0x_55555555u;
        const uint c2 = 0x_33333333u;
        const uint c3 = 0x_0F0F0F0Fu;
        const uint c4 = 0x_01010101u;

        value -= (value >> 1) & c1;
        value = (value & c2) + ((value >> 2) & c2);
        value = (((value + (value >> 4)) & c3) * c4) >> 24;

        return (int)value;
    }

    private static int UseAvx2(uint value)
    {
        // THIS IS A BUG!!!!!
        Some series of Avx2 instructions that performs the popcount operation.
        The bug here is triggered by the presence of tiered compilation and R2R. The R2R version
        of this method may be compiled as if the Avx2 feature is not available, and is not reliably rejitted
        at the same time as the PopCount function.

        As a special note, on the x86 and x64 platforms, this generally unsafe pattern may be used
        with the Sse, Sse2, Sse3, Sssse3, Ssse41 and Sse42 instruction sets as those instruction sets
        are treated specially by both crossgen1 and crossgen2 when compiling System.Private.CoreLib.dll.
    }
}
```

### Code written in other assemblies (both first and third party)

#### Crossgen implementation rules
- Any code which uses an intrinsic from the `System.Runtime.Intrinsics.Arm` or `System.Runtime.Intrinsics.X86` namespace will not be compiled AOT. (See code which throws a TypeLoadException using `IDS_EE_HWINTRINSIC_NGEN_DISALLOWED`)
- Any code which uses `Vector<T>` will not be compiled AOT. (See code which throws a TypeLoadException using `IDS_EE_SIMD_NGEN_DISALLOWED`)
- Any code which uses `Vector64<T>`, `Vector128<T>` or `Vector256<T>` will not be compiled AOT. (See code which throws a TypeLoadException using `IDS_EE_HWINTRINSIC_NGEN_DISALLOWED`)
- Non-platform intrinsics which require more hardware support than the minimum supported hardware capability will not take advantage of that capability. In particular the code generated for Vector2/3/4 is sub-optimal. MethodImplOptions.AggressiveOptimization may be used to disable compilation of this sub-par code.

#### Characteristics which result from rules
The rules here provide the following characteristics.
- Use of platform specific hardware intrinsics causes runtime jit and startup time concerns.
- Use of `Vector<T>` causes runtime jit and startup time concerns
- AOT generated code which could take advantage of more advanced hardware support experiences a performance penalty until rejitted. (If a customer chooses to disable tiered compilation, then customer code may always run slowly).

#### Code review rules for use of platform intrinsics
- Any use of a platform intrinsic in the codebase SHOULD be wrapped with a call to the associated IsSupported property. This wrapping may be done within the same function that uses the hardware intrinsic, but this is not required as long as the programmer can control all entrypoints to a function that uses the hardware intrinsic.
- If an application developer is highly concerned about startup performance, developers should avoid use of all platform specific hardware intrinsics on startup paths.

## Crossgen2 model of hardware intrinsic usage
There are 2 sets of instruction sets known to the compiler.
- The baseline instruction set which defaults to (Sse, Sse2), but may be adjusted via compiler option.
- The optimistic instruction set which defaults to (Sse3, Ssse3, Sse41, Sse42, Popcnt, Pclmulqdq, and Lzcnt).

Code will be compiled using the optimistic instruction set to drive compilation, but any use of an instruction set beyond the baseline instruction set will be recorded, as will any attempt to use an instruction set beyond the optimistic set if that attempted use has a semantic effect. If the baseline instruction set includes `Avx2` then the size and characteristics of of `Vector<T>` is known. Any other decisions about ABI may also be encoded. For instance, it is likely that the ABI of `Vector256<T>` will vary based on the presence/absence of `Avx` support.

- Any code which uses `Vector<T>` will not be compiled AOT unless the size of `Vector<T>` is known.
- Any code which passes a `Vector256<T>` as a parameter on a Linux or Mac machine will not be compiled AOT unless the support for the `Avx` instruction set is known.
- Non-platform intrinsics which require more hardware support than the optimistic supported hardware capability will not take advantage of that capability. MethodImplOptions.AggressiveOptimization may be used to disable compilation of this sub-par code.
- Code which takes advantage of instructions sets in the optimistic set will not be used on a machine which only supports the baseline instruction set.
- Code which attempts to use instruction sets outside of the optimistic set will generate code that will not be used on machines with support for the instruction set.

#### Characteristics which result from rules
- Code which uses platform intrinsics within the optimistic instruction set will generate good code.
- Code which relies on platform intrinsics not within the baseline or optimistic set will cause runtime jit and startup time concerns if used on hardware which does support the instruction set.
- `Vector<T>` code has runtime jit and startup time concerns unless the baseline is raised to include `Avx2`.

#### Code review rules for use of platform intrinsics
-  Any use of a platform intrinsic in the codebase SHOULD be wrapped with a call to the associated IsSupported property. This wrapping may be done within the same function that uses the hardware intrinsic, but this is not required as long as the programmer can control all entrypoints to a function that uses the hardware intrinsic.
-  If an application developer is highly concerned about startup performance, developers should avoid use intrinsics beyond Sse42, or should use Crossgen with an updated baseline instruction set support.

### Crossgen2 adjustment to rules for System.Private.CoreLib.dll
Since System.Private.CoreLib.dll is known to be code reviewed with the code review rules as written above for crossgen1 with System.Private.CoreLib.dll, it is possible to relax rule "Code which attempts to use instruction sets outside of the optimistic set will generate code that will not be used on machines with support for the instruction set." What this will do is allow the generation of non-optimal code for these situations, but through the magic of code review, the generated logic will still work correctly.


# Mechanisms in the JIT to generate correct code to handle varied instruction set support

The JIT receives flags which instruct it on what instruction sets are valid to use, and has access to a new jit interface api `notifyInstructionSetUsage(isa, bool supportBehaviorRequired)`.

The notifyInstructionSetUsage api is used to notify the AOT compiler infrastructure that the code may only execute if the runtime environment of the code is exactly the same as the boolean parameter indicates it should be. For instance, if `notifyInstructionSetUsage(Avx, false)` is used, then the code generated must not be used if the `Avx` instruction set is usable. Similarly `notifyInstructionSetUsage(Avx, true)` will indicate that the code may only be used if the `Avx` instruction set is available.

While the above api exists, it is not expected that general purpose code within the JIT will use it. In general jitted code is expected to use a number of different apis to understand the available hardware instruction support available.

| Api | Description of use | Exact behavior
| --- | --- | --- |
|`compExactlyDependsOn(isa)`| Use when making a decision to use or not use an instruction set when the decision will affect the semantics of the generated code. Should never be used in an assert. | Return whether or not an instruction set is supported. Calls notifyInstructionSetUsage with the result of that computation.
|`compOpportunisticallyDependsOn(isa)`| Use when making an opportunistic decision to use or not use an instruction set. Use when the instruction set usage is a "nice to have optimization opportunity", but do not use when a false result may change the semantics of the program. Should never be used in an assert. | Return whether or not an instruction set is supported. Calls notifyInstructionSetUsage if the instruction set is supported.
|`compIsaSupportedDebugOnly(isa)` | Use to assert whether or not an instruction set is supported | Return whether or not an instruction set is supported. Does not report anything. Only available in debug builds.
|`getSIMDSupportLevel()`| Use when determining what codegen to generate for code that operates on `Vector<T>`, `Vector2`, `Vector3` or `Vector4`.| Queries the instruction sets supported using `compOpportunisticallyDependsOn`, and finds a set of instructions available to use for working with the platform agnostic vector types.
|`getSIMDVectorType()`| Use to get the TYP of a the `Vector<T>` type. | Determine the TYP of the `Vector<T>` type. If on the architecture the TYP may vary depending on whatever rules, this function will make sufficient use of the `notifyInstructionSetUsage` api to ensure that the TYP is consistent between compile time and runtime.
|`getSIMDVectorRegisterByteLength()` | Use to get the size of a `Vector<T>` value. | Determine the size of the `Vector<T>` type. If on the architecture the size may vary depending on whatever rules, this function will make sufficient use of the `notifyInstructionSetUsage` api to ensure that the size is consistent between compile time and runtime.
|`maxSIMDStructBytes()`| Get the maximum number of bytes that might be used in a SIMD type during this compilation. | Query the set of instruction sets supported, and determine the largest simd type supported. Use `compOpportunisticallyDependsOn` to perform the queries so that the maximum size needed is the only one recorded.
|`largestEnregisterableStructSize()`| Get the maximum number of bytes that might be represented by a single register in this compilation. Use only as an optimization to avoid calling `impNormStructType` or `getBaseTypeAndSizeOfSIMDType`. | Query the set of instruction sets supported, and determine the largest simd type supported in this compilation, report that size.
