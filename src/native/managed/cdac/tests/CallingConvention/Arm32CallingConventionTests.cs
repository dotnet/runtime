// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// ARM32 (AAPCS, hard-float) calling-convention tests. Up to 4 GP args in
/// R0-R3, FP args in S0-S15 / D0-D7 via a bitmap allocator.
/// </summary>
public class Arm32CallingConventionTests
{
    private static CallConvTestCase Case => CallConvCases.Arm32;

    private static int OffsetOfNthGPReg(int n) => Case.ArgumentRegistersOffset + n * Case.PointerSize;
    private static int OffsetOfNthStackSlot(int n) => Case.OffsetOfArgs + n * Case.PointerSize;

    [Fact]
    public void FourInts_FillR0_R3()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < Case.NumArgumentRegisters; i++) sig.Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(Case.NumArgumentRegisters, layout.Arguments.Count);
        for (int i = 0; i < Case.NumArgumentRegisters; i++)
        {
            Assert.Equal(OffsetOfNthGPReg(i), layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void FifthInt_GoesToStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < Case.NumArgumentRegisters + 1; i++) sig.Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(Case.NumArgumentRegisters + 1, layout.Arguments.Count);
        Assert.Equal(OffsetOfNthStackSlot(0), layout.Arguments[4].Slots[0].Offset);
    }

    [Theory]
    [InlineData(CorElementType.I1, 4)]
    [InlineData(CorElementType.I2, 4)]
    [InlineData(CorElementType.I4, 4)]
    [InlineData(CorElementType.I4, 5)]
    public void IntArgs_FillR0_R3_AndSpillToStack(CorElementType elementType, int count)
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

    [Fact]
    public void InstanceMethod_ThisOffsetIsR0()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: true,
            sig => sig.Return(CorElementType.Void));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(OffsetOfNthGPReg(0), layout.ThisOffset);
    }

    [Theory]
    [InlineData(CorElementType.R4)]
    [InlineData(CorElementType.R8)]
    public void FloatArg_GoesToFirstFPRegister(CorElementType elementType)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(elementType));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(Case.OffsetOfFloatArgumentRegisters, layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void MixedIntAndFloat_UseSeparateBanks()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(CorElementType.I4).Param(CorElementType.R8));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(2, layout.Arguments.Count);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(Case.OffsetOfFloatArgumentRegisters, layout.Arguments[1].Slots[0].Offset);
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
    public void I8_AfterI4_AlignsToEvenRegisterPair()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig
                .Return(CorElementType.Void)
                .Param(CorElementType.I4)
                .Param(CorElementType.I8)
                .Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(3, layout.Arguments.Count);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfNthGPReg(2), layout.Arguments[1].Slots[0].Offset);
        Assert.Equal(OffsetOfNthStackSlot(0), layout.Arguments[2].Slots[0].Offset);
    }

    [Fact]
    public void LargeStruct_PassedByValueOnStack()
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
                        new(0, CorElementType.I4),
                        new(4, CorElementType.I4),
                        new(8, CorElementType.I4),
                        new(12, CorElementType.I4),
                        new(16, CorElementType.I4),
                        new(20, CorElementType.I4),
                    ]);
                sig.Return(CorElementType.Void);
                for (int i = 0; i < Case.NumArgumentRegisters; i++)
                {
                    sig.Param(CorElementType.I4);
                }
                sig.ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(Case.NumArgumentRegisters + 1, layout.Arguments.Count);
        Assert.False(layout.Arguments[4].IsPassedByRef);
        Assert.Equal(OffsetOfNthStackSlot(0), layout.Arguments[4].Slots[0].Offset);
    }

    [Fact]
    public void EightInts_FourInRegs_FourOnStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 8; i++)
                {
                    sig.Param(CorElementType.I4);
                }
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(8, layout.Arguments.Count);

        for (int i = 0; i < 8; i++)
        {
            int expectedOffset = i < Case.NumArgumentRegisters
                ? OffsetOfNthGPReg(i)
                : OffsetOfNthStackSlot(i - Case.NumArgumentRegisters);
            Assert.Equal(expectedOffset, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Fact]
    public void TypedReference_ConsumesR0AndR1()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable typedRefMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts,
                    "System.TypedReference",
                    structSize: 8,
                    fields: [new(0, CorElementType.Byref), new(4, CorElementType.I)]);
                rts.SetTypedReferenceMethodTable(typedRefMT.Address);
                sig.Return(CorElementType.Void).Param(CorElementType.TypedByRef).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(2, layout.Arguments.Count);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfNthGPReg(2), layout.Arguments[1].Slots[0].Offset);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void ReturnBuffer_ShiftsFirstUserArg(bool hasThis, int expectedUserArgRegister)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case,
            hasThis,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts,
                    "BigReturn",
                    structSize: 24,
                    fields:
                    [
                        new(0, CorElementType.I4),
                        new(4, CorElementType.I4),
                        new(8, CorElementType.I4),
                        new(12, CorElementType.I4),
                        new(16, CorElementType.I4),
                        new(20, CorElementType.I4),
                    ]);
                sig.ReturnValueType(new TargetPointer(mt.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfNthGPReg(expectedUserArgRegister), layout.Arguments[0].Slots[0].Offset);
    }

    // ---- Soft-float (armel) ----

    [Theory]
    [InlineData(CorElementType.R4)]
    [InlineData(CorElementType.R8)]
    public void SoftFloat_FloatArg_GoesToGPReg_NotFPReg(CorElementType elementType)
    {
        // On armel (soft-float), FeatureArmSoftFP is present and all arguments
        // -- including floats and doubles -- go through the GP register / stack
        // path. This mirrors the native #ifndef ARM_SOFTFP gate in
        // callingconvention.h:1546.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (_, sig) => sig.Return(CorElementType.Void).Param(elementType),
            isArmSoftFP: true);

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        // On soft-float the arg should be in R0 (first GP reg), not S0/D0.
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void SoftFloat_MixedIntAndFloat_BothInGPRegs()
    {
        // On armel, int and float args share the same GP register bank.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (_, sig) => sig.Return(CorElementType.Void).Param(CorElementType.I4).Param(CorElementType.R4),
            isArmSoftFP: true);

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(2, layout.Arguments.Count);
        Assert.Equal(OffsetOfNthGPReg(0), layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfNthGPReg(1), layout.Arguments[1].Slots[0].Offset);
    }
}
