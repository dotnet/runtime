# Calling Convention Test Inventory

This document tracks test coverage for each platform's managed calling
convention in the cDAC argument iterator. Each category represents a
behavioral aspect of the ABI that should have at least one test.

Legend:
- **Covered** -- test(s) exist and pass
- **Gap** -- no test exists yet
- **Skipped** -- test exists but is marked `[Skip]` due to a known implementation gap
- N/A -- category doesn't apply to this platform

## Test categories

| # | Category | Description |
|---|---|---|
| 1 | **Integer arg register filling** | N integer args fill the GP arg registers in order |
| 2 | **Integer arg stack spill** | Args beyond the register count land on the stack at sequential offsets |
| 3 | **Float/double register filling** | FP args fill FP registers (XMM on x64, V on ARM64, S/D on ARM32) |
| 4 | **Float/double stack spill** | FP args beyond the register count land on the stack |
| 5 | **Mixed int/float bank independence** | Int and float args consume from separate banks (or shared on Win x64) |
| 6 | **`this` pointer placement** | Instance method `this` lands in the first GP register |
| 7 | **Return buffer (retBuf)** | Large-return methods get a hidden retBuf arg that shifts user args |
| 8 | **Hidden arg shifts (generic context, async cont)** | Each hidden arg consumes a register slot, shifting user args |
| 9 | **Vararg cookie placement** | Vararg methods have a cookie after this/retBuf, before user args |
| 10 | **Implicit by-reference** | Structs above a size threshold are passed via hidden pointer |
| 11 | **Non-byref struct enregistration** | Structs at or below the threshold enregister as a value |
| 12 | **SysV eightbyte classification** | Struct fields classified per eightbyte merge rules (AMD64 Unix only) |
| 13 | **SysV struct split (GP + FP)** | Struct with mixed int/float fields splits across GP and FP banks |
| 14 | **HFA detection** | Homogeneous float aggregates placed in consecutive FP registers (ARM only) |
| 15 | **HFA not honored on non-ARM** | HFA-shaped structs do NOT get FP treatment on x64 |
| 16 | **TypedReference** | `System.TypedReference` placed correctly via `g_TypedReferenceMT` substitution |
| 17 | **Vector types (real intrinsic detection)** | Vector64/128 via synthetic metadata and `GetVectorSize` |
| 18 | **Large struct on stack by value** | Structs > 16 B passed by value on stack (SysV), not by pointer |
| 19 | **Many args (10+)** | Stack offsets progress correctly at scale |
| 20 | **Return value placement** | Return types in correct register or via retBuf |
| 21 | **Empty struct** | Zero-field struct behavior |
| 22 | **64-bit alignment (ARM32)** | I8/R8 args skip odd-numbered registers on ARM32 |
| 23 | **Apple ARM64 stack packing** | Natural-alignment packing on Darwin ARM64 stack |
| 24 | **Vararg FP -> GP demotion** | Variadic FP args go through GP path, not FP registers |
| 25 | **GC reference args** | Object/String args placed in GP regs (not byref, not FP) |

## Coverage matrix

### AMD64 Windows

| # | Category | Status | Test(s) |
|---|---|---|---|
| 1 | Int register filling | **Covered** | `IntArgs_FillRegsAndSpillToStack` (10 cases: I1-U8) |
| 2 | Int stack spill | **Covered** | `IntArgs_FillRegsAndSpillToStack` (I4x5, I8x5) |
| 3 | Float register filling | **Covered** | `FloatArgs_FillFPRegsAndSpillToStack` (R4x1, R8x1, R4x4, R8x4) |
| 4 | Float stack spill | **Covered** | `FloatArgs_FillFPRegsAndSpillToStack` (R4x6, R8x6) |
| 5 | Mixed int/float banks | **Covered** | `OneFloatAmongInts_LandsInXMM` (4 positions) |
| 6 | `this` placement | **Covered** | `InstanceMethod_ThisOffsetIsFirst` |
| 7 | Return buffer | **Covered** | `StaticMethod_RetBuf_*`, `InstanceMethod_RetBuf_*`, `HiddenArgs_ShiftFirstUserDouble` |
| 8 | Hidden arg shifts | **Covered** | `HiddenArgs_ShiftFirstUserDouble` (10 cases) |
| 9 | Vararg cookie | **Covered** | `VarArgs_CookieAndFirstUserArg_OnWindows` (4 cases), `NonVarArgs_HasNullVarArgCookieOffset` |
| 10 | Implicit byref | **Covered** | `NineByteStruct_*`, `ThreeByteStruct_*`, `TypedReference_ImplicitByref_*` |
| 11 | Non-byref enregistration | **Covered** | `EightByteStruct_Enregisters_NotByref` |
| 12 | SysV eightbyte | N/A | |
| 13 | SysV struct split | N/A | |
| 14 | HFA detection | N/A | |
| 15 | HFA not honored | **Covered** | `HFAShapedStruct_OnWindows_DoesNotEnregisterInFP` (2 cases) |
| 16 | TypedReference | **Covered** | `TypedReference_ImplicitByref_OneSlot` |
| 17 | Vector types | **Covered** | `VectorType_OnWindows_ClassifiedBySizeNotVectorness` (2 cases) |
| 18 | Large struct by value | N/A | (Windows uses byref, not by-value) |
| 19 | Many args (10+) | **Covered** | `TenArgs_StackOffsetsProgress` |
| 20 | Return value placement | **Gap** | No explicit return-register tests |
| 21 | Empty struct | **Gap** | No test (needs behavioral clarification) |
| 22 | 64-bit alignment | N/A | |
| 23 | Apple stack packing | N/A | |
| 24 | Vararg FP demotion | **Gap** | Not modeled in iterator; covered implicitly by position-based XMM |
| 25 | GC reference args | **Gap** | No explicit Object/String arg test |

### AMD64 Unix (SysV)

| # | Category | Status | Test(s) |
|---|---|---|---|
| 1 | Int register filling | **Covered** | `SixInts_FillGPRegs`, `IntArgs_FillSixGPRegsAndSpillToStack` (10 cases) |
| 2 | Int stack spill | **Covered** | `SeventhInt_GoesToStack`, `IntArgs_*` (I4x7, I8x7) |
| 3 | Float register filling | **Covered** | `FourDoubles_FillFPRegs`, `FloatArgs_FillEightFPRegs` (6 cases) |
| 4 | Float stack spill | **Covered** | `NineDoubles_NinthGoesToFirstStackSlot` |
| 5 | Mixed int/float banks | **Covered** | `MixedIntDouble_UseSeparateBanks`, `OneFloatAmongInts_*` (4 cases) |
| 6 | `this` placement | **Covered** | `InstanceMethod_ThisOffsetIsFirstGPReg` |
| 7 | Return buffer | **Covered** | `Return_LargeStruct_HasRetBuf_*` |
| 8 | Hidden arg shifts | **Gap** | No generic-context or async-continuation test |
| 9 | Vararg cookie | **Gap** | No vararg test (SysV varargs are Linux/macOS only and "not supported" per clr-abi.md:84) |
| 10 | Implicit byref | N/A | SysV doesn't use implicit byref |
| 11 | Non-byref enregistration | N/A | (all structs <= 16 B are classified, not byref'd) |
| 12 | SysV eightbyte classification | **Covered** | `Struct_TwoInts_*`, `Struct_TwoFloats_*`, `Struct_TwoDoubles_*` |
| 13 | SysV struct split (GP+FP) | **Covered** | `Struct_IntDouble_SplitAcrossGPAndFP`, `Struct_ObjectAndDouble_*` |
| 14 | HFA detection | N/A | |
| 15 | HFA not honored | N/A | (SysV has no HFA concept) |
| 16 | TypedReference | **Covered** | `TypedReference_PassedInTwoGPRegs`, `TypedReference_GlobalNotSet_FallsBackToStack` |
| 17 | Vector types | **Gap** | No Vector64/128 test (SysV classifier bypass not exercised) |
| 18 | Large struct by value | **Covered** | `Struct_LargerThan16Bytes_StackByValue_NotByRef` |
| 19 | Many args (10+) | **Covered** | `ManyIntArgs_StackOffsetsProgress` |
| 20 | Return value placement | **Covered** | `Return_EightByteStruct_*`, `Return_SixteenByteStruct_*`, `Return_ThreeByteStruct_*`, etc. (6 tests) |
| 21 | Empty struct | **Gap** | No test |
| 22 | 64-bit alignment | N/A | |
| 23 | Apple stack packing | N/A | (Apple is ARM64, not x64) |
| 24 | Vararg FP demotion | N/A | (managed varargs not supported on Unix) |
| 25 | GC reference args | **Gap** | No explicit Object/String arg test |

### ARM32

| # | Category | Status | Test(s) |
|---|---|---|---|
| 1 | Int register filling | **Covered** | `FourInts_FillR0_R3` |
| 2 | Int stack spill | **Covered** | `FifthInt_GoesToStack` |
| 3 | Float register filling | **Gap** | No FP register test |
| 4 | Float stack spill | **Gap** | No FP spill test |
| 5 | Mixed int/float banks | **Gap** | No mixed test |
| 6 | `this` placement | **Gap** | No explicit test |
| 7 | Return buffer | **Gap** | No retBuf test |
| 8 | Hidden arg shifts | **Gap** | No test |
| 9 | Vararg cookie | **Gap** | No test |
| 10 | Implicit byref | N/A | (EnregisteredParamTypeMaxSize = 0) |
| 11 | Non-byref enregistration | N/A | |
| 12 | SysV eightbyte | N/A | |
| 13 | SysV struct split | N/A | |
| 14 | HFA detection | **Gap** | No HFA test |
| 15 | HFA not honored | N/A | |
| 16 | TypedReference | **Gap** | No test |
| 17 | Vector types | **Gap** | No test |
| 18 | Large struct by value | **Gap** | No test |
| 19 | Many args (10+) | **Gap** | No test |
| 20 | Return value placement | **Gap** | No test |
| 21 | Empty struct | **Gap** | No test |
| 22 | 64-bit alignment | **Gap** | No I8/R8 alignment-skip test |
| 23 | Apple stack packing | N/A | |
| 24 | Vararg FP demotion | **Gap** | No test |
| 25 | GC reference args | **Gap** | No test |

### ARM64

| # | Category | Status | Test(s) |
|---|---|---|---|
| 1 | Int register filling | **Covered** | `EightInts_FillX0_X7` |
| 2 | Int stack spill | **Gap** | No explicit test |
| 3 | Float register filling | **Covered** | `EightDoubles_FillV0_V7` |
| 4 | Float stack spill | **Gap** | No FP spill test |
| 5 | Mixed int/float banks | **Gap** | No mixed test |
| 6 | `this` placement | **Covered** | `InstanceMethod_ThisOffsetIsX0` |
| 7 | Return buffer | **Gap** | No retBuf test (X8 behavior) |
| 8 | Hidden arg shifts | **Gap** | No test |
| 9 | Vararg cookie | **Gap** | No test |
| 10 | Implicit byref | **Gap** | No > 16 B struct test |
| 11 | Non-byref enregistration | **Gap** | No <= 16 B non-HFA struct test |
| 12 | SysV eightbyte | N/A | |
| 13 | SysV struct split | N/A | |
| 14 | HFA detection | **Covered** | `HfaFloat2/3/4_*`, `HfaDouble2_*` |
| 15 | HFA not honored | N/A | |
| 16 | TypedReference | **Gap** | No test |
| 17 | Vector types | **Gap** | No Vector64/128 in V-register test |
| 18 | Large struct by value | N/A | (ARM64 uses implicit byref) |
| 19 | Many args (10+) | **Gap** | No test |
| 20 | Return value placement | **Gap** | No test |
| 21 | Empty struct | **Gap** | No test |
| 22 | 64-bit alignment | N/A | |
| 23 | Apple stack packing | **Gap** | No Apple-specific test |
| 24 | Vararg FP demotion | **Skipped** | `Windows_VarArgs_StructSpansX7AndStack_AuditGap4` |
| 25 | GC reference args | **Gap** | No test |

### RISC-V 64 / LoongArch 64

| # | Category | Status | Test(s) |
|---|---|---|---|
| 1 | Int register filling | **Covered** | `RiscV64_EightInts_FillA0_A7` |
| 2 | Int stack spill | **Gap** | No explicit test |
| 3 | Float register filling | **Covered** | `LoongArch64_OneFloat_GoesToFA0` |
| 4 | Float stack spill | **Gap** | No FP spill test |
| 5 | Mixed int/float banks | **Gap** | No mixed test |
| 6 | `this` placement | **Gap** | No test |
| 7 | Return buffer | **Gap** | No test |
| 8 | Hidden arg shifts | **Gap** | No test |
| 9 | Vararg cookie | **Gap** | No test |
| 10 | Implicit byref | **Gap** | No > 16 B struct test |
| 11 | Non-byref enregistration | **Gap** | No test |
| 12 | SysV eightbyte | N/A | |
| 13 | SysV struct split | N/A | |
| 14 | HFA detection | N/A | |
| 15 | HFA not honored | N/A | |
| 16 | TypedReference | **Gap** | No test |
| 17 | Vector types | **Gap** | No test |
| 18 | Large struct by value | N/A | |
| 19 | Many args (10+) | **Gap** | No test |
| 20 | Return value placement | **Gap** | No test |
| 21 | Empty struct | **Gap** | No test |
| 22 | 64-bit alignment | N/A | |
| 23 | Apple stack packing | N/A | |
| 24 | Vararg FP demotion | **Gap** | No test |
| 25 | GC reference args | **Gap** | No test |

### x86

| # | Category | Status | Test(s) |
|---|---|---|---|
| 1 | Int register filling | **Covered** | `OneInt_*`, `TwoInts_*` |
| 2 | Int stack spill | **Skipped** | `ThirdInt_LandsAtOffsetOfArgs_AuditGap7` |
| 3 | Float register filling | N/A | (x86 has no FP arg registers) |
| 4 | Float stack spill | N/A | |
| 5 | Mixed int/float banks | N/A | |
| 6 | `this` placement | **Covered** | `InstanceMethod_ThisOffsetIsECX` |
| 7 | Return buffer | **Gap** | No retBuf test |
| 8 | Hidden arg shifts | **Gap** | No test |
| 9 | Vararg cookie | **Gap** | No test |
| 10 | Implicit byref | N/A | (EnregisteredParamTypeMaxSize = 0) |
| 11 | Non-byref enregistration | **Skipped** | `SmallValueType_Enregisters_AuditGap6` |
| 12 | SysV eightbyte | N/A | |
| 13 | SysV struct split | N/A | |
| 14 | HFA detection | N/A | |
| 15 | HFA not honored | N/A | |
| 16 | TypedReference | **Gap** | No test |
| 17 | Vector types | N/A | |
| 18 | Large struct by value | **Gap** | No test |
| 19 | Many args (10+) | **Gap** | No test |
| 20 | Return value placement | **Gap** | No test |
| 21 | Empty struct | **Gap** | No test |
| 22 | 64-bit alignment | N/A | |
| 23 | Apple stack packing | N/A | |
| 24 | Vararg FP demotion | N/A | |
| 25 | GC reference args | **Gap** | No test |

### GetVectorSize (cross-platform, in `RuntimeTypeSystemGetVectorSizeTests.cs`)

| Test | Status |
|---|---|
| Known intrinsic returns size (Vector64, Vector128) | **Covered** |
| Non-intrinsic type returns 0 | **Covered** |
| No metadata returns 0 | **Covered** |
| Unhandled intrinsic name returns 0 | **Covered** |
| System.Numerics.Vector returns field bytes | **Covered** |

## AMD64 Unix: gap analysis and proposed tests

The following categories are **gaps** for AMD64 Unix that should be filled:

### Gap 8: Hidden arg shifts (generic context, async continuation)

AMD64 Unix uses the same `ComputeInitialNumRegistersUsed` as Windows, counting
`this`, retBuf, paramType, asyncCont in RDI, RSI, RDX, ... before user args.
The Phase 2 `HiddenArgs_ShiftFirstUserDouble` theory was added for Windows only.

**Proposed:** Port the same theory to `AMD64UnixCallingConventionTests.cs` with
Unix-specific offsets (RDI = slot 0, RSI = slot 1, etc.) and 6 GP regs instead
of 4. The same `hasParamType` / `hasAsyncContinuation` helper flags work.

### Gap 17: Vector types (SysV classifier bypass)

When `GetVectorSize` returns non-zero, `SystemVStructClassifier.ShouldClassify`
returns false, and the struct is not eightbyte-classified. The AMD64 Unix
iterator's behavior when classification is skipped should be verified:
- Vector64 (8 B) -> should go in a GP register (or a single XMM?)
- Vector128 (16 B) -> should go in a single XMM (not split into 2 eightbytes)

**Proposed:** Add `VectorType_OnUnix_BypassesEightbyteClassification` theory
using the synthetic metadata infrastructure from Phase 4. May surface a real
gap if the iterator currently mis-places unclassifiable structs.

### Gap 21: Empty struct

SysV AMD64 should pass empty structs by value on the stack per `clr-abi.md:569`.
Needs behavioral validation first (does the mock produce size 0? does
`IsArgPassedByRefBySize(0)` return false on SysV? etc.).

**Proposed:** Add `EmptyStruct_PassedByValue_OnStack` fact test.

### Gap 25: GC reference args

Object and String args should go in GP registers (RDI, RSI, ...) and not be
treated as implicit byref. Important for GC scanning correctness.

**Proposed:** Add `ObjectAndStringArgs_GoToGPRegs_NotByref` fact test.

### Gap 9: Vararg cookie

Per `clr-abi.md:84`: "Managed varargs are supported on Windows only." Unix
managed varargs are explicitly not supported. So this is correctly N/A, but
worth a negative test confirming the contract doesn't crash on a vararg sig
for a Unix target.

**Proposed:** Add `VarArgs_NotSupportedOnUnix_ReturnsEmptyOrThrows` fact test
(assert behavior matches the contract's current handling).
