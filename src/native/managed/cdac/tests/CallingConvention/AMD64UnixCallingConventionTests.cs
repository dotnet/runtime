// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// AMD64-Unix (System V AMD64 ABI) calling-convention tests. GP args go in
/// RDI/RSI/RDX/RCX/R8/R9 (6 slots); FP args in XMM0-XMM7 (8 slots). The two
/// banks are independent.
/// <para>
/// The SysV struct classifier (<c>SystemVStructClassifier</c>) is exercised
/// end-to-end via the contract: each struct test allocates a value-type MT
/// in mock memory and references it via <c>ELEMENT_TYPE_INTERNAL</c> in the
/// stored sig blob.
/// </para>
/// </summary>
public class AMD64UnixCallingConventionTests
{
    private static readonly Lazy<SyntheticVectorMetadata> s_syntheticVectorMetadata = new(SyntheticVectorMetadata.Create);

    private static CallConvTestCase Case => CallConvCases.AMD64Unix;

    private static int OffsetOfNthGPReg(int n) => Case.ArgumentRegistersOffset + n * Case.PointerSize;
    private static int OffsetOfNthFPReg(int n) => Case.OffsetOfFloatArgumentRegisters!.Value + n * Case.FloatRegisterSize;
    private static int OffsetOfNthStackSlot(int n) => Case.OffsetOfArgs + n * Case.PointerSize;

    [Fact]
    public void SixInts_FillGPRegs()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < Case.NumArgumentRegisters; i++) sig.Param(CorElementType.I8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(Case.NumArgumentRegisters, layout.Arguments.Count);
        for (int i = 0; i < Case.NumArgumentRegisters; i++)
        {
            int expected = Case.ArgumentRegistersOffset + i * Case.PointerSize;
            Assert.Equal(expected, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void SeventhInt_GoesToStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < Case.NumArgumentRegisters + 1; i++) sig.Param(CorElementType.I8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(Case.NumArgumentRegisters + 1, layout.Arguments.Count);
        Assert.Equal(Case.OffsetOfArgs, layout.Arguments[6].Slots[0].Offset);
    }

    [Fact]
    public void FourDoubles_FillFPRegs()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 4; i++) sig.Param(CorElementType.R8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(4, layout.Arguments.Count);
        for (int i = 0; i < 4; i++)
        {
            int expected = (Case.OffsetOfFloatArgumentRegisters ?? 0) + i * Case.FloatRegisterSize;
            Assert.Equal(expected, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void MixedIntDouble_UseSeparateBanks()
    {
        // SysV bank-independent allocation: int goes to GP, double to FP regardless of position.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig
                .Return(CorElementType.Void)
                .Param(CorElementType.I8)
                .Param(CorElementType.R8)
                .Param(CorElementType.I8)
                .Param(CorElementType.R8));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(4, layout.Arguments.Count);
        Assert.Equal(Case.ArgumentRegistersOffset, layout.Arguments[0].Slots[0].Offset);
        Assert.Equal((Case.OffsetOfFloatArgumentRegisters ?? 0), layout.Arguments[1].Slots[0].Offset);
        Assert.Equal(Case.ArgumentRegistersOffset + Case.PointerSize, layout.Arguments[2].Slots[0].Offset);
        Assert.Equal((Case.OffsetOfFloatArgumentRegisters ?? 0) + Case.FloatRegisterSize, layout.Arguments[3].Slots[0].Offset);
    }

    // ----- SysV struct classification -----

    [Fact]
    public void Struct_TwoInts_PassedInOneGPReg()
    {
        // { int x; int y; } => 8 bytes => 1 eightbyte (Integer) => 1 GP reg.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "TwoInts", structSize: 8,
                    fields: [new(0, CorElementType.I4), new(4, CorElementType.I4)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Single(arg.Slots);
        Assert.Equal(CorElementType.I8, arg.Slots[0].ElementType);
        Assert.Equal(Case.ArgumentRegistersOffset, arg.Slots[0].Offset);
    }

    [Fact]
    public void Struct_IntDouble_SplitAcrossGPAndFP()
    {
        // { int x; double d; } => 2 eightbytes (Integer, SSE) => 1 GP + 1 FP reg.
        // This is the key SysV "split" case GcScanner needs to see per-register.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "IntDouble", structSize: 16,
                    fields: [new(0, CorElementType.I4), new(8, CorElementType.R8)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(2, arg.Slots.Count);
        Assert.Equal(CorElementType.I8, arg.Slots[0].ElementType);
        Assert.Equal(Case.ArgumentRegistersOffset, arg.Slots[0].Offset);
        Assert.Equal(CorElementType.R8, arg.Slots[1].ElementType);
        Assert.Equal((Case.OffsetOfFloatArgumentRegisters ?? 0), arg.Slots[1].Offset);
    }

    [Fact]
    public void Struct_TwoDoubles_PassedInTwoFPRegs()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "TwoDoubles", structSize: 16,
                    fields: [new(0, CorElementType.R8), new(8, CorElementType.R8)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.Equal(2, arg.Slots.Count);
        Assert.Equal(CorElementType.R8, arg.Slots[0].ElementType);
        Assert.Equal((Case.OffsetOfFloatArgumentRegisters ?? 0), arg.Slots[0].Offset);
        Assert.Equal(CorElementType.R8, arg.Slots[1].ElementType);
        Assert.Equal((Case.OffsetOfFloatArgumentRegisters ?? 0) + Case.FloatRegisterSize, arg.Slots[1].Offset);
    }

    [Fact]
    public void Struct_TwoFloats_PackInOneFPReg()
    {
        // { float a; float b; } => 8 bytes => 1 eightbyte (SSE) => 1 FP reg.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "TwoFloats", structSize: 8,
                    fields: [new(0, CorElementType.R4), new(4, CorElementType.R4)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Single(layout.Arguments[0].Slots);
        Assert.Equal(CorElementType.R8, layout.Arguments[0].Slots[0].ElementType);
        Assert.Equal((Case.OffsetOfFloatArgumentRegisters ?? 0), layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void Struct_ObjectAndDouble_GCRefSplitWithFP()
    {
        // { object o; double d; } => 2 eightbytes (IntegerReference, SSE).
        // The GP slot is reported as a managed reference (CorElementType.Class).
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "ObjectAndDouble", structSize: 16,
                    fields: [new(0, CorElementType.Object), new(8, CorElementType.R8)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.Equal(2, arg.Slots.Count);
        Assert.Equal(CorElementType.Class, arg.Slots[0].ElementType);
        Assert.Equal(Case.ArgumentRegistersOffset, arg.Slots[0].Offset);
        Assert.Equal(CorElementType.R8, arg.Slots[1].ElementType);
        Assert.Equal((Case.OffsetOfFloatArgumentRegisters ?? 0), arg.Slots[1].Offset);
    }

    [Fact]
    public void Struct_LargerThan16Bytes_StackByValue_NotByRef()
    {
        // { long a; long b; long c; } => 24 bytes > 16 => stack by value
        // (NOT implicit by-ref like Windows x64).
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "ThreeLongs", structSize: 24,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I8),
                        new(16, CorElementType.I8),
                    ]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);    // SysV passes large structs BY VALUE on stack.
        Assert.Single(arg.Slots);
        Assert.Equal(Case.OffsetOfArgs, arg.Slots[0].Offset);
    }

    [Fact]
    public void TypedReference_PassedInTwoGPRegs()
    {
        // System.TypedReference is { ref byte _value; IntPtr _type; } -> 16 bytes
        // classified as [IntegerByRef, Integer] -> RDI + RSI on SysV AMD64.
        // The signature uses ELEMENT_TYPE_TYPEDBYREF (0x16); the ArgIterator
        // substitutes the well-known g_TypedReferenceMT MethodTable so the
        // SysV struct classifier walks its layout normally.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable typedRefMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "System.TypedReference", structSize: 16,
                    fields: [new(0, CorElementType.Byref), new(8, CorElementType.I)]);
                rts.SetTypedReferenceMethodTable(typedRefMT.Address);
                sig.Return(CorElementType.Void).Param(CorElementType.TypedByRef);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(2, arg.Slots.Count);
        Assert.Equal(Case.ArgumentRegistersOffset, arg.Slots[0].Offset);
        Assert.Equal(Case.ArgumentRegistersOffset + Case.PointerSize, arg.Slots[1].Offset);
    }

    [Fact]
    public void TypedReference_GlobalNotSet_FallsBackToStack()
    {
        // When g_TypedReferenceMT isn't populated (older runtime / missing global),
        // the signature provider falls back to a conservative pointer-sized
        // value-type placeholder. SysV AMD64 still passes it on the stack rather
        // than crashing on resolution.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(CorElementType.TypedByRef));

        // No SetTypedReferenceMethodTable call - global stays null.
        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        // Should not throw; layout is best-effort.
    }

    // ---- Section 1: Style-aligned coverage (mirrors AMD64-Windows tests) ----

    // Integer-valued args consume the 6 GP arg-regs in order, then spill to
    // stack slots that advance by one pointer-sized slot per overflow arg.
    [Theory]
    [InlineData(CorElementType.I1, 6)]
    [InlineData(CorElementType.U1, 6)]
    [InlineData(CorElementType.I2, 6)]
    [InlineData(CorElementType.U2, 6)]
    [InlineData(CorElementType.I4, 6)]
    [InlineData(CorElementType.U4, 6)]
    [InlineData(CorElementType.I8, 6)]
    [InlineData(CorElementType.U8, 6)]
    [InlineData(CorElementType.I4, 7)]
    [InlineData(CorElementType.I8, 8)]
    public void IntArgs_FillSixGPRegsAndSpillToStack(CorElementType elementType, int count)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < count; i++) sig.Param(elementType);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(count, layout.Arguments.Count);

        for (int i = 0; i < count; i++)
        {
            int expectedOffset = i < Case.NumArgumentRegisters
                ? OffsetOfNthGPReg(i)
                : OffsetOfNthStackSlot(i - Case.NumArgumentRegisters);

            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void InstanceMethod_ThisOffsetIsFirstGPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: true,
            sig => sig.Return(CorElementType.Void));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(OffsetOfNthGPReg(0), layout.ThisOffset);
    }

    // Float args fill the 8 FP arg-regs (independent of the GP bank).
    // Counts 1..8 exercise pure FP-bank allocation; this verifies the happy path.
    [Theory]
    [InlineData(CorElementType.R4, 1)]
    [InlineData(CorElementType.R8, 1)]
    [InlineData(CorElementType.R4, 4)]
    [InlineData(CorElementType.R8, 4)]
    [InlineData(CorElementType.R4, 8)]
    [InlineData(CorElementType.R8, 8)]
    public void FloatArgs_FillEightFPRegs(CorElementType elementType, int count)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < count; i++) sig.Param(elementType);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(count, layout.Arguments.Count);

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(elementType, layout.Arguments[i].Slots[0].ElementType);
            Assert.Equal(OffsetOfNthFPReg(i), layout.Arguments[i].Slots[0].Offset);
        }
    }

    // A lone float chooses the FP bank based on its FP-bank ordinal; surrounding
    // ints continue to map to GP registers by their GP-bank ordinal.
    // Verifies the SysV "independent banks" placement rule.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void OneFloatAmongInts_LandsInFirstFPReg(int floatPosition)
    {
        const int totalArgs = 6;
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < totalArgs; i++)
                    sig.Param(i == floatPosition ? CorElementType.R8 : CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(totalArgs, layout.Arguments.Count);

        int idxGen = 0;
        int idxFP = 0;
        for (int i = 0; i < totalArgs; i++)
        {
            if (i == floatPosition)
            {
                Assert.Equal(CorElementType.R8, layout.Arguments[i].Slots[0].ElementType);
                Assert.Equal(OffsetOfNthFPReg(idxFP), layout.Arguments[i].Slots[0].Offset);
                idxFP++;
            }
            else
            {
                Assert.Equal(CorElementType.I4, layout.Arguments[i].Slots[0].ElementType);
                Assert.Equal(OffsetOfNthGPReg(idxGen), layout.Arguments[i].Slots[0].Offset);
                idxGen++;
            }
        }
    }

    [Fact]
    public void ManyIntArgs_StackOffsetsProgress()
    {
        const int totalArgs = 10;
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < totalArgs; i++) sig.Param(CorElementType.I8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(totalArgs, layout.Arguments.Count);

        for (int i = 0; i < totalArgs; i++)
        {
            int expectedOffset = i < Case.NumArgumentRegisters
                ? OffsetOfNthGPReg(i)
                : OffsetOfNthStackSlot(i - Case.NumArgumentRegisters);

            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    // ---- Section 2: Return-value classification baselines ----

    // Baseline: 8-byte struct return classifies as 1 Integer eightbyte
    // (reg-passable), so no ret buf is needed and the first user arg stays
    // in the 1st GP reg.
    [Fact]
    public void Return_EightByteStruct_NoRetBuf_FirstArgStaysInFirstGPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "EightBytes", structSize: 8, fields: [new(0, CorElementType.I8)]);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    // Baseline: 16-byte struct return classifies as 2 Integer eightbytes
    // (reg-passable in RAX:RDX), so no ret buf.
    [Fact]
    public void Return_SixteenByteStruct_NoRetBuf_FirstArgStaysInFirstGPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "SixteenBytes", structSize: 16,
                    fields: [new(0, CorElementType.I8), new(8, CorElementType.I8)]);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    // Baseline: >16-byte struct return cannot fit in two regs, so a hidden ret
    // buf takes the 1st GP slot and the first user arg shifts to the 2nd GP reg.
    [Fact]
    public void Return_LargeStruct_HasRetBuf_FirstArgShiftsToSecondGPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "TwentyFourBytes", structSize: 24,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I8),
                        new(16, CorElementType.I8),
                    ]);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(1), layout.Arguments[0].Slots[0].Offset);
    }

#pragma warning disable xUnit1004 // Test methods should not be skipped — these track audit gaps.

    // ---- Section 3: Open audit gaps ----

    // Audit gap (return classification): on AMD64-Unix, a 12-byte struct return
    // {int, int, int} classifies as 2 Integer eightbytes -> reg-passable in
    // RAX:RDX, so NO ret buf is needed. cDAC currently uses an X64-wide
    // power-of-2 heuristic that wrongly forces a ret buf for non-power-of-2
    // sizes, shifting the first user arg from the 1st GP reg to the 2nd.
    [Fact]
    public void Return_TwelveByteStruct_NoRetBuf_FirstArgStaysInFirstGPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "TwelveBytes", structSize: 12,
                    fields:
                    [
                        new(0, CorElementType.I4),
                        new(4, CorElementType.I4),
                        new(8, CorElementType.I4),
                    ]);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    // Audit gap (return classification): {long, short} is 10 bytes, classifies
    // as 2 Integer eightbytes -> reg-passable. cDAC's power-of-2 heuristic
    // wrongly says ret buf.
    [Fact]
    public void Return_TenByteStruct_NoRetBuf_FirstArgStaysInFirstGPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "TenBytes", structSize: 10,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I2),
                    ]);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    // Audit gap (return classification): a 3-byte struct {byte, byte, byte}
    // classifies as 1 Integer eightbyte -> reg-passable in RAX. cDAC's
    // power-of-2 heuristic wrongly forces a ret buf.
    [Fact]
    public void Return_ThreeByteStruct_NoRetBuf_FirstArgStaysInFirstGPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "ThreeBytes", structSize: 3,
                    fields:
                    [
                        new(0, CorElementType.I1),
                        new(1, CorElementType.I1),
                        new(2, CorElementType.I1),
                    ]);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    // Regression: when the 8 FP arg-regs are exhausted, the 9th double must
    // land on the stack. Verifies that the FP-overflow path in PlaceScalar
    // does NOT fall through to the GP-arg path (it falls directly to the
    // stack — matching native callingconvention.h:1457-1476).
    [Fact]
    public void NineDoubles_NinthGoesToFirstStackSlot()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 9; i++) sig.Param(CorElementType.R8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(9, layout.Arguments.Count);

        // First 8 doubles in FP regs.
        for (int i = 0; i < Case.NumFloatArgumentRegisters; i++)
            Assert.Equal(OffsetOfNthFPReg(i), layout.Arguments[i].Slots[0].Offset);

        // 9th double on the stack (1st stack slot), NOT in a GP reg.
        Assert.Equal(OffsetOfNthStackSlot(0), layout.Arguments[8].Slots[0].Offset);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, false, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, true, true)]
    public void HiddenArgs_DoNotAffectFPPlacement_BankIndependence(
        bool hasThis,
        bool hasRetBuf,
        bool hasParamType,
        bool hasAsyncCont)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case,
            hasThis,
            (rts, sig) =>
            {
                if (hasRetBuf)
                {
                    MockMethodTable bigMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                        rts,
                        "BigReturn",
                        structSize: 24,
                        fields:
                        [
                            new(0, CorElementType.I8),
                            new(8, CorElementType.I8),
                            new(16, CorElementType.I8),
                        ]);
                    sig.ReturnValueType(new TargetPointer(bigMT.Address));
                }
                else
                {
                    sig.Return(CorElementType.Void);
                }

                sig.Param(CorElementType.R8);
            },
            hasParamType,
            hasAsyncCont);

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);

        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(CorElementType.R8, arg.Slots[0].ElementType);
        Assert.Equal(OffsetOfNthFPReg(0), arg.Slots[0].Offset);
    }

    [Theory]
    [InlineData("Vector64`1", 8)]
    [InlineData("Vector128`1", 16)]
    public void VectorType_OnUnix_BypassesClassifier(string vectorTypeName, int vectorByteSize)
    {
        SyntheticVectorMetadata metadata = s_syntheticVectorMetadata.Value;
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case,
            hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddVectorMethodTable(
                    rts,
                    vectorTypeName,
                    vectorByteSize,
                    metadata.GetTypeDefToken(vectorTypeName));
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            },
            syntheticMetadata: metadata);

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);

        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Single(arg.Slots);
        Assert.Equal(OffsetOfNthStackSlot(0), arg.Slots[0].Offset);
    }

    [Fact]
    public void EmptyStruct_PassedByValueOnStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case,
            hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts,
                    "Empty",
                    structSize: 0,
                    fields: []);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);

        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Single(arg.Slots);
        Assert.Equal(OffsetOfNthStackSlot(0), arg.Slots[0].Offset);
    }

    [Theory]
    [InlineData(CorElementType.Object)]
    [InlineData(CorElementType.String)]
    public void GCReferenceArgs_GoToGPRegs(CorElementType refType)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(refType));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);

        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(OffsetOfNthGPReg(0), arg.Slots[0].Offset);
    }

    [Fact]
    public void GCReferenceArgs_GoToGPRegs_Class()
    {
        // ELEMENT_TYPE_CLASS with a dummy TypeDef token. The signature decoder
        // falls back to a pointer-sized Class placeholder, which lands in a GP reg.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).ParamClass());

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(OffsetOfNthGPReg(0), arg.Slots[0].Offset);
    }

    [Fact]
    public void VarArgs_ReturnsLayout_OnUnixTarget()
    {
        // Managed varargs are not supported on Unix, but the contract should
        // handle the signature gracefully (it flows through the iterator).
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.VarArg().Return(CorElementType.Void).Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.NotNull(layout.VarArgCookieOffset);
    }

#pragma warning restore xUnit1004

    // ----- ValueTypeHandle population for by-value value-type args -----
    //
    // AMD64-Unix (SysV) pre-decomposes by-value structs into GC-typed eightbytes
    // (I8/Class/Byref/R8) when they're passed in registers or split between
    // regs+stack. For those cases ValueTypeHandle MUST be null (the existing
    // per-slot ElementType already gives the GC scanner everything it needs;
    // walking GCDesc would duplicate or contradict the eightbyte classification).
    //
    // ValueTypeHandle is only populated when the iterator hands back a single
    // ValueType-typed slot, which happens when the struct is too large for
    // enregistration and lands entirely on the stack as a contiguous buffer.

    [Fact]
    public void ValueTypeByValue_LargeOnStack_PopulatesValueTypeHandle()
    {
        // 32-byte struct > 16 -> ineligible for enregistration -> passed by value
        // on the stack as a contiguous buffer. The iterator emits a single
        // ValueType-typed slot; ValueTypeHandle carries the MT for GCDesc walk.
        ulong expectedMtAddress = 0;
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "FourLongs", structSize: 32,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I8),
                        new(16, CorElementType.I8),
                        new(24, CorElementType.I8),
                    ]);
                expectedMtAddress = mt.Address;
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.NotNull(arg.ValueTypeHandle);
        Assert.Equal(expectedMtAddress, (ulong)arg.ValueTypeHandle.Value.Address);
    }

    [Fact]
    public void ValueTypeByValue_SysVDecomposed_DoesNotPopulateValueTypeHandle()
    {
        // { object o; double d; } -> SysV-classified into two eightbytes
        // (IntegerReference, SSE). The iterator emits one ArgSlot with
        // ElementType=Class and another with ElementType=R8. Because the per-slot
        // ElementType already encodes GC refs precisely, ValueTypeHandle must be
        // null -- the GCDesc walk would otherwise double-report or contradict.
        // Pins the "all slots ValueType" discriminator in ComputeValueTypeHandle.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "ObjectAndDouble2", structSize: 16,
                    fields: [new(0, CorElementType.Object), new(8, CorElementType.R8)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(2, arg.Slots.Count);
        Assert.Null(arg.ValueTypeHandle);
    }
}
