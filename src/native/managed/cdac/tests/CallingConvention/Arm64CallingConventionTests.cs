// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// ARM64 (AAPCS64) calling-convention tests. Up to 8 GP args in X0-X7, FP
/// args in V0-V7. HFAs (homogeneous aggregates of 1-4 floats/doubles) are
/// passed in consecutive FP registers; large value types via implicit byref.
/// </summary>
public class Arm64CallingConventionTests
{
    private static CallConvTestCase Case => CallConvCases.Arm64Windows;

    private static int OffsetOfNthGPReg(int n) => Case.ArgumentRegistersOffset + n * Case.PointerSize;
    private static int OffsetOfNthFPReg(int n) => Case.OffsetOfFloatArgumentRegisters!.Value + n * Case.FloatRegisterSize;
    private static int OffsetOfNthStackSlot(int n) => Case.OffsetOfArgs + n * Case.PointerSize;

    [Fact]
    public void EightInts_FillX0_X7()
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
            Assert.Equal(OffsetOfNthGPReg(i), layout.Arguments[i].Slots[0].Offset);
    }

    [Fact]
    public void EightDoubles_FillV0_V7()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 8; i++) sig.Param(CorElementType.R8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(8, layout.Arguments.Count);
        for (int i = 0; i < 8; i++)
            Assert.Equal(OffsetOfNthFPReg(i), layout.Arguments[i].Slots[0].Offset);
    }

    [Fact]
    public void InstanceMethod_ThisOffsetIsX0()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: true,
            sig => sig.Return(CorElementType.Void));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(OffsetOfNthGPReg(0), layout.ThisOffset);
    }

    [Theory]
    [InlineData(CorElementType.I1, 8)]
    [InlineData(CorElementType.I2, 8)]
    [InlineData(CorElementType.I4, 8)]
    [InlineData(CorElementType.I8, 8)]
    [InlineData(CorElementType.U1, 8)]
    [InlineData(CorElementType.U2, 8)]
    [InlineData(CorElementType.U4, 8)]
    [InlineData(CorElementType.U8, 8)]
    [InlineData(CorElementType.I4, 9)]
    public void IntArgs_FillX0_X7_AndSpillToStack(CorElementType elementType, int count)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < count; i++)
                {
                    sig.Param(elementType);
                }
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

    [Theory]
    [InlineData(CorElementType.R4, 1)]
    [InlineData(CorElementType.R8, 1)]
    [InlineData(CorElementType.R4, 8)]
    [InlineData(CorElementType.R8, 8)]
    [InlineData(CorElementType.R4, 10)]
    [InlineData(CorElementType.R8, 10)]
    public void FloatArgs_FillV0_V7_AndSpillToStack(CorElementType elementType, int count)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < count; i++)
                {
                    sig.Param(elementType);
                }
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(count, layout.Arguments.Count);

        for (int i = 0; i < count; i++)
        {
            int expectedOffset = i < Case.NumFloatArgumentRegisters
                ? OffsetOfNthFPReg(i)
                : OffsetOfNthStackSlot(i - Case.NumFloatArgumentRegisters);
            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void MixedIntAndFloat_UseSeparateBanks()
    {
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
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfNthFPReg(0), layout.Arguments[1].Slots[0].Offset);
        Assert.Equal(OffsetOfNthGPReg(1), layout.Arguments[2].Slots[0].Offset);
        Assert.Equal(OffsetOfNthFPReg(1), layout.Arguments[3].Slots[0].Offset);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, true, true)]
    public void HiddenArgs_DoNotAffectFirstUserDouble(
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
        Assert.Equal(OffsetOfNthFPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void LargeStruct_ImplicitByRef_ConsumesOneGPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts,
                    "BigStruct",
                    structSize: 24,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I8),
                        new(16, CorElementType.I8),
                    ]);
                sig.Return(CorElementType.Void)
                    .ParamValueType(new TargetPointer(mt.Address))
                    .Param(CorElementType.I8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(2, layout.Arguments.Count);
        Assert.True(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfNthGPReg(1), layout.Arguments[1].Slots[0].Offset);
    }

    [Fact]
    public void SixteenByteStruct_NotByRef_ConsumesTwoGPRegs()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts,
                    "TwoLongs",
                    structSize: 16,
                    fields: [new(0, CorElementType.I8), new(8, CorElementType.I8)]);
                sig.Return(CorElementType.Void)
                    .ParamValueType(new TargetPointer(mt.Address))
                    .Param(CorElementType.I8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(2, layout.Arguments.Count);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfNthGPReg(2), layout.Arguments[1].Slots[0].Offset);
    }

    [Fact]
    public void TypedReference_ConsumesTwoGPRegs()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable typedRefMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts,
                    "System.TypedReference",
                    structSize: 16,
                    fields: [new(0, CorElementType.Byref), new(8, CorElementType.I)]);
                rts.SetTypedReferenceMethodTable(typedRefMT.Address);
                sig.Return(CorElementType.Void)
                    .Param(CorElementType.TypedByRef)
                    .Param(CorElementType.I8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(2, layout.Arguments.Count);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfNthGPReg(2), layout.Arguments[1].Slots[0].Offset);
    }

    [Theory]
    [InlineData(CorElementType.Object)]
    [InlineData(CorElementType.String)]
    public void GCReferenceArgs_GoToGPRegs_NotByref(CorElementType refType)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(refType));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void TenArgs_EightInRegs_TwoOnStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 10; i++)
                {
                    sig.Param(CorElementType.I8);
                }
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(10, layout.Arguments.Count);

        for (int i = 0; i < 10; i++)
        {
            int expectedOffset = i < Case.NumArgumentRegisters
                ? OffsetOfNthGPReg(i)
                : OffsetOfNthStackSlot(i - Case.NumArgumentRegisters);
            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void VarArgs_DoubleUsesGPReg_NotFPReg()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.VarArg().Return(CorElementType.Void).Param(CorElementType.R8));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.NotNull(layout.VarArgCookieOffset);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(0), layout.VarArgCookieOffset);
        Assert.Equal(OffsetOfNthGPReg(1), layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void Return_LargeStruct_RetBufInX8_FirstUserArgStaysInX0()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts,
                    "BigReturn",
                    structSize: 24,
                    fields:
                    [
                        new(0, CorElementType.I8),
                        new(8, CorElementType.I8),
                        new(16, CorElementType.I8),
                    ]);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    // ---- Homogeneous Floating-point Aggregate (HFA) helpers ----

    private const uint WFlagsLow_IsHFA = 0x800;

    /// <summary>
    /// Allocates a value-type MethodTable representing an HFA of <paramref name="count"/>
    /// elements of <paramref name="elemType"/> (R4 or R8). Sets the IsHFA flag so the
    /// signature provider reports <c>IsHomogeneousAggregate=true</c> with the right
    /// element size.
    /// </summary>
    private static MockMethodTable AddHFA(MockDescriptors.RuntimeTypeSystem rts, CorElementType elemType, int count, string name)
    {
        int elemSize = elemType == CorElementType.R4 ? 4 : 8;
        MockDescriptors.CallingConvention.ValueTypeField[] fields = new MockDescriptors.CallingConvention.ValueTypeField[count];
        for (int i = 0; i < count; i++)
            fields[i] = new(i * elemSize, elemType);

        MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
            rts, name, structSize: count * elemSize, fields: fields);
        mt.MTFlags |= WFlagsLow_IsHFA;
        return mt;
    }

    // ----- Open audit gaps for ARM64 -----
#pragma warning disable xUnit1004 // Test methods should not be skipped — these track audit gaps.

    [Fact]
    public void Windows_VarArgs_StructSpansX7AndStack()
    {
        // Cookie consumes one GP slot (X0). 6 user longs fill X1-X6.
        // The 16-byte struct's first 8 bytes fit in X7; second 8 bytes spill to stack.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "TwoLongs", structSize: 16,
                    fields: [new(0, CorElementType.I8), new(8, CorElementType.I8)]);
                sig.VarArg().Return(CorElementType.Void);
                for (int i = 0; i < 6; i++) sig.Param(CorElementType.I8);
                sig.ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(7, layout.Arguments.Count);
        ArgLayout structArg = layout.Arguments[6];
        Assert.Equal(2, structArg.Slots.Count);
        Assert.Equal(OffsetOfNthGPReg(7), structArg.Slots[0].Offset);
        Assert.Equal(OffsetOfNthStackSlot(0), structArg.Slots[1].Offset);
    }

    // Under varargs, HFAs lose their HFA treatment and go through the GP path.
    // They should NOT split across X7/stack -- they should either fit entirely
    // in GP regs or go entirely to stack (no split for HFA-shaped composites).
    [Fact]
    public void Windows_VarArgs_HFA_DoesNotSplit_GoesToStack()
    {
        // Cookie consumes X0. 7 user longs fill X1-X7 (cookie + 7 = 8 total).
        // The 4-float HFA (16 bytes, treated as composite under varargs) can't fit
        // in GP regs (all exhausted). Under varargs, the FP path is skipped, so the
        // HFA goes entirely to stack -- no split.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable hfa = AddHFA(rts, CorElementType.R4, 4, "HFA_4F_VarArg");
                sig.VarArg().Return(CorElementType.Void);
                for (int i = 0; i < 7; i++) sig.Param(CorElementType.I8);
                sig.ParamValueType(new TargetPointer(hfa.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(8, layout.Arguments.Count);
        ArgLayout hfaArg = layout.Arguments[7];
        // Under varargs the HFA is NOT passed in FP regs -- it goes through GP path.
        // All GP regs are consumed, so the entire arg goes to stack (no split).
        Assert.Single(hfaArg.Slots);
        Assert.Equal(OffsetOfNthStackSlot(0), hfaArg.Slots[0].Offset);
    }

    // ---- HFA per-slot reporting ----
    //
    // ARM64 HFAs (homogeneous aggregates of 1-4 floats/doubles) are passed
    // in consecutive FP registers. The iterator emits one ArgLocation per FP
    // register with ElementType set to the HFA element type (R4 or R8) --
    // analogous to how SystemV per-eightbyte emission works on AMD64-Unix.

    [Theory]
    [InlineData(CorElementType.R4, 2)]
    [InlineData(CorElementType.R4, 3)]
    [InlineData(CorElementType.R4, 4)]
    [InlineData(CorElementType.R8, 2)]
    [InlineData(CorElementType.R8, 3)]
    [InlineData(CorElementType.R8, 4)]
    public void HFA_OccupiesNConsecutiveFPRegs(CorElementType elemType, int count)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = AddHFA(rts, elemType, count, $"HFA_{elemType}_{count}");
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef);
        Assert.Equal(count, arg.Slots.Count);

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(elemType, arg.Slots[i].ElementType);
            Assert.Equal(OffsetOfNthFPReg(i), arg.Slots[i].Offset);
        }
    }

    // HFA after some FP args: the HFA's first FP register is at the current
    // FP-allocation index, not back at V0. Verifies the iterator correctly
    // advances past the HFA so subsequent FP args don't overlap.
    [Fact]
    public void HFA_AfterTwoDoubles_StartsAtV2()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable hfa = AddHFA(rts, CorElementType.R8, 3, "HFA_3D");
                sig.Return(CorElementType.Void)
                    .Param(CorElementType.R8)
                    .Param(CorElementType.R8)
                    .ParamValueType(new TargetPointer(hfa.Address))
                    .Param(CorElementType.R8);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(4, layout.Arguments.Count);

        // 2 doubles in V0, V1
        Assert.Equal(OffsetOfNthFPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfNthFPReg(1), layout.Arguments[1].Slots[0].Offset);

        // 3-double HFA in V2, V3, V4
        ArgLayout hfaArg = layout.Arguments[2];
        Assert.Equal(3, hfaArg.Slots.Count);
        for (int i = 0; i < 3; i++)
            Assert.Equal(OffsetOfNthFPReg(2 + i), hfaArg.Slots[i].Offset);

        // Trailing double picks up at V5
        Assert.Equal(OffsetOfNthFPReg(5), layout.Arguments[3].Slots[0].Offset);
    }

    // HFA that exactly fills the remaining FP regs (boundary case): 6 doubles
    // before a 2-double HFA → HFA fits in V6 + V7 with no overflow.
    [Fact]
    public void HFA_FitsExactlyInRemainingFPRegs()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable hfa = AddHFA(rts, CorElementType.R8, 2, "HFA_2D");
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 6; i++) sig.Param(CorElementType.R8);
                sig.ParamValueType(new TargetPointer(hfa.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(7, layout.Arguments.Count);

        ArgLayout hfaArg = layout.Arguments[6];
        Assert.Equal(2, hfaArg.Slots.Count);
        Assert.Equal(OffsetOfNthFPReg(6), hfaArg.Slots[0].Offset);
        Assert.Equal(OffsetOfNthFPReg(7), hfaArg.Slots[1].Offset);
    }

    // HFA that doesn't fit in the remaining FP regs: 5 doubles + 4-double HFA
    // (needs 4 slots, only 3 remain) → ENTIRE HFA spills to stack, FP regs are
    // marked exhausted, and no further FP enregistration happens.
    [Fact]
    public void HFA_DoesNotFit_EntireHFASpillsToStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable hfa = AddHFA(rts, CorElementType.R8, 4, "HFA_4D");
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 5; i++) sig.Param(CorElementType.R8);
                sig.ParamValueType(new TargetPointer(hfa.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(6, layout.Arguments.Count);

        ArgLayout hfaArg = layout.Arguments[5];
        // Single stack slot covering the full 32-byte struct payload
        Assert.Single(hfaArg.Slots);
        Assert.Equal(OffsetOfNthStackSlot(0), hfaArg.Slots[0].Offset);
    }

    // HFA placement is byref-free: a 4-double HFA (32 bytes > 16) is normally
    // passed by implicit byref for ordinary value types, but the HFA path
    // overrides that and keeps it as a multi-FP-reg pass-by-value.
    [Fact]
    public void HFA_FourDoubles_NotByRef()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable hfa = AddHFA(rts, CorElementType.R8, 4, "HFA_4D");
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(hfa.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(4, layout.Arguments[0].Slots.Count);
        for (int i = 0; i < 4; i++)
            Assert.Equal(OffsetOfNthFPReg(i), layout.Arguments[0].Slots[i].Offset);
    }

    // Original 4-float HFA test (kept for continuity with the earlier audit-gap marker).
    [Fact]
    public void HFA_FourFloats_ShouldReportFourFPSlots()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = AddHFA(rts, CorElementType.R4, 4, "HFA_4F");
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(4, layout.Arguments[0].Slots.Count);
        for (int i = 0; i < 4; i++)
            Assert.Equal(OffsetOfNthFPReg(i), layout.Arguments[0].Slots[i].Offset);
    }

#pragma warning restore xUnit1004
}
