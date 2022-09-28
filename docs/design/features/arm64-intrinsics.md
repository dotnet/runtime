# Arm64 Intrinsics

This document is intended to document proposed design decisions related to the introduction
of Arm64 Intrinsics

## Document Goals

+ Discuss design options
  + Document existing design pattern
  + Draft initial design decisions which are least likely to cause extensive rework
+ Decouple `X86`, `X64`, `ARM32` and `ARM64` development
  + Make some minimal decisions which encourage API similarity between platforms
  + Make some additional minimal decisions which allow `ARM32` and `ARM64` API's to be similar
+ Decouple CoreCLR implementation and testing from API design
+ Allow for best API design
+ Keep implementation simple

## Intrinsics in general

Use of intrinsics in general is a CoreCLR design decision to allow low level platform
specific optimizations.

At first glance, such a decision seems to violate the fundamental principles of .NET
code running on any platform.  However, the intent is not for the vast majority of
apps to use such optimizations.  The intended usage model is to allow library
developers access to low level functions which enable optimization of key
functions.  As such the use is expected to be limited, but performance critical.

## Intrinsic granularity

In general individual intrinsic will be chosen to be fine grained.  These will generally
correspond to a single assembly instruction.

## Logical Sets of Intrinsics

For various reasons, an individual CPU will have a specific set of supported instructions.  For `ARM64` the
set of supported instructions is identified by various `ID_* System registers`.
While these feature registers are only available for the OS to access, they provide
a logical grouping of instructions which are enabled/disabled together.

### API Logical Set grouping & `IsSupported`

The C# API must provide a mechanism to determine which sets of instructions are supported.
Existing design uses a separate `static class` to group the methods which correspond to each
logical set of instructions.  A single `IsSupported` property is included in each `static class`
to allow client code to alter control flow.  The `IsSupported` properties are designed so that JIT
can remove code on unused paths.  `ARM64` will use an identical approach.

### API `PlatformNotSupported` Exception

If client code calls an intrinsic which is not supported by the platform a `PlatformNotSupported`
exception must be thrown.

### JIT, VM, PAL & OS requirements

The JIT must use a set of flags corresponding to logical sets of instructions to alter code
generation.

The VM must query the OS to populate the set of JIT flags.  For the special altJit case, a
means must provide for setting the flags.

PAL must provide an OS abstraction layer.

Each OS must provide a mechanism for determining which sets of instructions are supported.

+ Linux provides the HWCAP detection mechanism which is able to detect current set of exposed
features
+ Arm64 MAC OS and Arm64 Windows OS must provide an equally capable detection mechanism.

In the event the OS fails to provides a means to detect a support for an instruction set extension
it must be treated as unsupported.

NOTE: Exceptions might be where:

+ CoreCLR is distributed as source and CMake build configuration test is used to detect these features
+ Installer detects features and sets appropriate configuration knobs
+ VM runs code inside safe try/catch blocks to test for instruction support
+ Platform requires a specific minimum set of instructions

### Intrinsics & Crossgen

For any intrinsic which may not be supported on all variants of a platform, crossgen method
compilation should be designed to allow optimal code generation.

Initial implementation will simply trap so that the JIT is forced to generate optimal platform dependent code at
runtime.  Subsequent implementations may use different approaches.

## Choice of Arm64 naming conventions

`x86`, `x64`, `ARM32` and `ARM64` will follow similar naming conventions.

### Namespaces

+ `System.Runtime.Intrinsics` is used for type definitions useful across multiple platforms
+ `System.Runtime.Intrinsics.Arm` is used type definitions shared across `ARM32` and `ARM64` platforms
+ `System.Runtime.Intrinsics.Arm.Arm64` is used for type definitions for the `ARM64` platform
  + The primary implementation of `ARM64` intrinsics will occur within this namespace
  + While `x86` and `x64` share a common namespace, this document is recommending a separate namespace
  for `ARM32` and `ARM64`.  This is because `AARCH64` is a separate `ISA` from the `AARCH32` `Arm` & `Thumb`
  instruction sets.  It is not an `ISA` extension, but rather a new `ISA`.  This is different from `x64`
  which could be viewed as a superset of `x86`.
  + The logical grouping of `ARM64` and `ARM32` instruction sets is different.  It is controlled by
  different sets of `System Registers`.

For the convenience of the end user, it may be useful to add convenience API's which expose functionality
which is common across platforms and sets of platforms.  These could be implemented in terms of the
platform specific functionality.  These API's are currently out of scope of this initial design document.

### Logical Set Class Names

Within the `System.Runtime.Intrinsics.Arm.Arm64` namespace there will be a separate `static class` for each
logical set of instructions

The sets will be chosen to match the granularity of the `ARM64` `ID_*` register fields.

#### Specific Class Names

The table below documents the set of known extensions, their identification, and their recommended intrinsic
class names.

| ID Register      | Field   | Values   | Intrinsic `static class` name |
| ---------------- | ------- | -------- | ----------------------------- |
| N/A              | N/A     | N/A      | Base                          |
| ID_AA64ISAR0_EL1 | AES     | (1b, 10b)| Aes                           |
| ID_AA64ISAR0_EL1 | Atomic  | (10b)    | Atomics                       |
| ID_AA64ISAR0_EL1 | CRC32   | (1b)     | Crc32                         |
| ID_AA64ISAR1_EL1 | DPB     | (1b)     | Dcpop                         |
| ID_AA64ISAR0_EL1 | DP      | (1b)     | Dp                            |
| ID_AA64ISAR1_EL1 | FCMA    | (1b)     | Fcma                          |
| ID_AA64PFR0_EL1  | FP      | (0b, 1b) | Fp                            |
| ID_AA64PFR0_EL1  | FP      | (1b)     | Fp16                          |
| ID_AA64ISAR1_EL1 | JSCVT   | (1b)     | Jscvt                         |
| ID_AA64ISAR1_EL1 | LRCPC   | (1b)     | Lrcpc                         |
| ID_AA64ISAR0_EL1 | AES     | (10b)    | Pmull                         |
| ID_AA64PFR0_EL1  | RAS     | (1b)     | Ras                           |
| ID_AA64ISAR0_EL1 | SHA1    | (1b)     | Sha1                          |
| ID_AA64ISAR0_EL1 | SHA2    | (1b, 10b)| Sha2                          |
| ID_AA64ISAR0_EL1 | SHA3    | (1b)     | Sha3                          |
| ID_AA64ISAR0_EL1 | SHA2    | (10b)    | Sha512                        |
| ID_AA64PFR0_EL1  | AdvSIMD | (0b, 1b) | Simd                          |
| ID_AA64PFR0_EL1  | AdvSIMD | (1b)     | SimdFp16                      |
| ID_AA64ISAR0_EL1 | RDM     | (1b)     | SimdV81                       |
| ID_AA64ISAR0_EL1 | SM3     | (1b)     | Sm3                           |
| ID_AA64ISAR0_EL1 | SM4     | (1b)     | Sm4                           |
| ID_AA64PFR0_EL1  | SVE     | (1b)     | Sve                           |

The `All`, `Simd`, and `Fp` classes will together contain the bulk of the `ARM64` intrinsics.  Most other extensions
will only add a few instruction so they should be simpler to review.

The `Base` `static class` is used to represent any intrinsic which is guaranteed to be implemented on all
`ARM64` platforms.  This set will include general purpose instructions.  For example, this would include intrinsics
such as `LeadingZeroCount` and `LeadingSignCount`.

As further extensions are released, this set of intrinsics will grow.

### Intrinsic Method Names

Intrinsics will be named to describe functionality.  Names will not correspond to specific named
assembly instructions.

Where precedent exists for common operations within the `System.Runtime.Intrinsics.X86` namespace, identical method
names will be chosen: `Add`, `Multiply`, `Load`, `Store` ...

Where `ARM` naming convention differs substantially from `XARCH`, `ARM` naming conventions will sometimes be preferred.
For instance

+ `ARM` uses `Replicate` or `Duplicate` rather than X86 `Broadcast`.
+ `ARM` uses `Across` rather than `X86` `Horizontal`.

These will need to reviewed on a case by case basis.

It is also worth noting `System.Runtime.Intrinsics.X86` naming conventions will include the suffix `Scalar` for
operations which take vector argument(s), but contain an implicit cast(s) to the base type and therefore operate only
on the first item of the argument vector(s).

### Intrinsic Method Argument and Return Types

Intrinsic methods will typically use a standard set of argument and return types:

+ Integer type: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`
+ Floating types: `double`, `single`, `System.Half`
+ Vector types: `Vector128<T>`, `Vector64<T>`
+ SVE will add new vector types: TBD
+ `ValueTuple<>` for return types returning multiple values

It is proposed to add the `Vector64<T>` type.  Most `ARM64` instructions support 8 byte and 16 byte forms.  8 byte
operations can execute faster with less power on some platforms. So adding `Vector64<T>` will allow exposing the full
flexibility of the instruction set and allow for optimal usage.

Some intrinsics will need to produce multiple results.  The most notable are the structured load operations `LD2`,
`LD3`, `LD4` ...  For these operations it is proposed that the intrinsic API return a `ValueTuple<>` of `Vector64<T>` or
`Vector128<T>`

#### Literal immediates

Some assembly instructions require an immediate encoded directly in the assembly instruction.  These need to be
constant at JIT time.

While the discussion is still on-going, consensus seems to be that any intrinsic must function correctly even when its
arguments are not constant.

## Intrinsic Interface Documentation

+ Namespace
+ Each `static class` will
  + Briefly document corresponding `System Register Field and Value` from ARM specification.
  + Document use of IsSupported property
  + Optionally summarize set of methods enabled by the extension
+ Each intrinsic method will
  + Document underlying `ARM64` assembly instruction
  + Optionally, briefly summarize operation performed
    + In many cases this may be unnecessary: `Add`, `Multiply`, `Load`, `Store`
    + In some cases this may be difficult to do correctly. (Crypto instructions)
  + Optionally mention corresponding compiler gcc, clang, and/or MSVC intrinsics
    + Review of existing documentation shows `ARM64` intrinsics are mostly absent or undocumented so
    initially this will not be necessary for `ARM64`
    + See gcc manual "AArch64 Built-in Functions"
    + MSVC ARM64 documentation has not been publicly released

## Phased Implementation

### Implementation Priorities

As rough guidelines for order of implementation:

+ Baseline functionality will be prioritized over architectural extensions
+ Architectural extensions will typically be prioritized in age order.  Earlier extensions will be added first
  + This is primarily driven by availability of hardware.  Features released in earlier will be prevalent in
  more hardware.
+ Priorities will be driven by optimization efforts and requests
  + Priority will be given to intrinsics which are equivalent/similar to those actively used in libraries for other
  platforms
  + Priority will be given to intrinsics which have already been implemented for other platforms

### API review

Intrinsics will extend the API of CoreCLR.  They will need to follow standard API review practices.

Initial XArch intrinsics are proposed to be added to the `netcoreapp2.1` Target Framework.  ARM64 intrinsics will
be in similar Target Frameworks as the XArch intrinsics.

Each review will identify the Target Framework API version where the API will be extended and released.

#### API review of an intrinsic `static class`

Given the need to add hundreds or thousands of intrinsics, it will be helpful to review incrementally.

A separate GitHub Issue will typically created for the review of each intrinsic `static class`.

When the `static class` exceeds a few dozen methods, it is desirable to break the review into smaller more manageable
pieces.

The extensive set of ARM64 assembly instructions make reviewing and implementing an exhaustive set a long process.
To facilitate incremental progress, initial intrinsic API for a given `static class` need not be exhaustive.

### Partial implementation of intrinsic `static class`

+ `IsSupported` must represent the state of an entire intrinsic `static class` for a given Target Framework.
+ Once API review is complete and approved, it is acceptable to implement approved methods in any order.
+ The approved API must be completed before the intrinsic `static class` is included in its Target Framework release

## Test coverage

As intrinsic support is added test coverage must be extended to provide basic testing.

Tests should be added as soon as practical.  CoreCLR Implementation and CoreFX API will need to be merged before tests
can be merged.

## LSRA changes to allocate contiguous register ranges

Some ARM64 instructions will require allocation of contiguous blocks of registers.  These are likely limited to load and
store multiple instructions.

It is not clear if this is a new LSRA feature and if it is how much complexity this will introduce into the LSRA.

## ARM ABI Vector64<T> and Vector128<T>

For intrinsic method calls, these vector types will implicitly be treated as pass by vector register.

For other calls, ARM64 ABI conventions must be followed.  For purposes of the ABI calling conventions, these vector
types will treated as composite struct type containing a contiguous array of `T`.  They will need to follow standard
struct argument and return passing rules.

## Half precision floating point

This document will refer to half precision floating point as `Half`.

+ Machine learning and Artificial intelligence often use `Half` type to simplify storage and improve processing time.
+ CoreCLR and `CIL` in general do not have general support for a `Half` type
+ There is an open request to expose `Half` intrinsics
+ There is an outstanding proposal to add `System.Half` to support this request
https://github.com/dotnet/runtime/issues/936
+ Implementation of `Half` features will be adjusted based on
  + Implementation of the `System.Half` proposal
  + Availability of supporting hardware (extensions)
  + General language extensions supporting `Half`

**`Half` support is currently outside the scope of the initial design proposal.  It is discussed below only for
introductory purposes.**

### ARM64 Half precision support

ARM64 supports two half precision floating point formats

+ IEEE-754 compliant.
+ ARM alternative format

The two formats are similar.  IEEE-754 has support for Inifinity and NAN and therefore has a somewhat smaller range.
IEEE-754 should be preferred.

ARM64 baseline support for `Half` is limited.  The following types of operations are supported

+ Loads and Stores
+ Conversion to/from `Float`
+ Widening from `Vector128<Half>` to two `Vector128<Float>`
+ Narrowing from two `Vector128<Float>` to `Vector128<Half>`

The optional ARMv8.2-FP16 extension adds support for

+ General operations on IEEE-754 `Half` types
+ Vector operations on IEEE-754 `Half` types

These correspond to the proposed `static class`es `Fp16` and `SimdFp16`

### `Half` and ARM64 ABI

Any complete `Half` implementation must conform to the `ARM64 ABI`.

The proposed `System.Half` type must be treated as a floating point type for purposes of the ARM64 ABI

As an argument it must be passed in a floating point register.

As a structure member, it must be treated as a floating point type and enter into the HFA determination logic.

Test cases must be written and conformance must be demonstrated.

## Scalable Vector Extension Support

`SVE`, the Scalable Vector Extension introduces its own complexity.

The extension

+ Creates a set of `Z0-Z31` scalable vector registers.  These overlay existing vector registers.  Each scalar vector
register has a platform specific length
  + Any multiple of 128 bits up to 2048 bits
+ Creates a new set of `P0-P15` predicate registers.  Each predicate register has a platform specific length which is
1/8th of the scalar vector length.
+ Add an extensive set of instructions including complex load and store operations.
+ Modifies the ARM64 ABI.

Therefore implementation will not be trivial.

+ Register allocator will need changes to support predicate allocation
+ SIMD support will face similar issues
+ Open issue: Should we use `Vector<T>`, `Vector128<t>, Vector256<t>, ... Vector2048<T>`, `SVE<T>` ... in user interface
design?
  + Use of `Vector128<t>, Vector256<t>, ... Vector2048<T>` is current default proposal.
Having 16 forms of every API may create issues for framework and client developers.
However generics may provide some/sufficient relief to make this acceptable.
  + Use of `Vector<T>` may be preferred if SVE will also be used for `FEATURE_SIMD`
  + Use of `SVE<T>` may be preferred if SVE will not be used for `FEATURE_SIMD`


Given lack of available hardware and a lack of thorough understanding of the specification:

+ SVE will require a separate design
+ **SVE is considered out of scope for this document.  It is discussed above only for
introductory purposes.**

## Miscellaneous
### Handling Instruction Deprecation

Deprecation of instructions should be relatively rare

+ Do not introduce an intrinsic for a feature that is currently deprecated
+ In event an assembly instruction is deprecated
  1. Prefer emulation using alternate instructions if practical
  2. Add `SetThrowOnDeprecated()` interface to allow developers to find these issues

## Approved APIs

The following sections document APIs which have completed the API review process.

Until each API is approved it shall be marked "TBD Not Approved"

### `All`

TBD Not approved

### `Aes`

TBD Not approved

### `Atomics`

TBD Not approved

### `Crc32`

TBD Not approved

### `Dcpop`

TBD Not approved

### `Dp`

TBD Not approved

### `Fcma`

TBD Not approved

### `Fp`

TBD Not approved

### `Fp16`

TBD Not approved

### `Jscvt`

TBD Not approved

### `Lrcpc`

TBD Not approved

### `Pmull`

TBD Not approved

### `Ras`

TBD Not approved

### `Sha1`

TBD Not approved

### `Sha2`

TBD Not approved

### `Sha3`

TBD Not approved

### `Sha512`

TBD Not approved

### `Simd`

TBD Not approved

### `SimdFp16`

TBD Not approved

### `SimdV81`

TBD Not approved

### `Sm3`

TBD Not approved

### `Sm4`

TBD Not approved

### `Sve`

TBD Not approved
