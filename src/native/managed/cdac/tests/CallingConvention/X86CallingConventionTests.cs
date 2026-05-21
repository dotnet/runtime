// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// x86-specific calling-convention tests. ECX/EDX register placement (in
/// REVERSE — first arg in ECX which is the higher slot), stack overflow, and
/// the audit-gap regression markers for x86 register placement / sig-walk
/// accounting.
/// </summary>
public class X86CallingConventionTests
{
    private static CallConvTestCase Case => CallConvCases.X86;

    private static int OffsetOfECX => Case.ArgumentRegistersOffset + Case.PointerSize;
    private static int OffsetOfEDX => Case.ArgumentRegistersOffset;

    [Fact]
    public void OneInt_GoesToFirstArgRegSlot()
    {
        // x86 places args in registers in REVERSE: first int -> (NumArgRegs - 1) * PtrSize = 4 (ECX-style high slot).
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(Case.ArgumentRegistersOffset + (Case.NumArgumentRegisters - 1) * Case.PointerSize, layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void TwoInts_FillBothArgRegs()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(CorElementType.I4).Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(2, layout.Arguments.Count);
        // First arg @ ECX (high slot)
        Assert.Equal(Case.ArgumentRegistersOffset + Case.PointerSize, layout.Arguments[0].Slots[0].Offset);
        // Second arg @ EDX (offset 0)
        Assert.Equal(Case.ArgumentRegistersOffset, layout.Arguments[1].Slots[0].Offset);
    }

    [Fact]
    public void InstanceMethod_ThisOffsetIsECX()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: true,
            sig => sig.Return(CorElementType.Void));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        // GetThisOffset = ArgRegsOffset + PointerSize (ECX slot, after EDX at 0)
        Assert.Equal(Case.ArgumentRegistersOffset + Case.PointerSize, layout.ThisOffset);
    }

    /// <summary>
    /// Audit gap #6 (closed): x86 ArgIterator used to exclude ValueType
    /// entirely from register placement. Native x86 enregisters value types
    /// of size 1, 2, or 4 bytes. This test verifies the fixed behavior.
    /// </summary>
    [Fact]
    public void SmallValueType_Enregisters_AuditGap6()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "OneInt", structSize: 4,
                    fields: [new(0, CorElementType.I4)]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(Case.ArgumentRegistersOffset + (Case.NumArgumentRegisters - 1) * Case.PointerSize, layout.Arguments[0].Slots[0].Offset);
    }

    /// <summary>
    /// Audit gap #7 (closed): X86ArgIterator.ComputeSizeOfArgStack used to
    /// assume ALL args go to stack, biasing the stack-arg offset upward.
    /// After the fix, the first stack arg lands at exactly OffsetOfArgs.
    /// </summary>
    [Fact]
    public void ThirdInt_LandsAtOffsetOfArgs_AuditGap7()
    {
        // Three ints: first two go in ECX/EDX, third spills to stack at OffsetOfArgs.
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig
                .Return(CorElementType.Void)
                .Param(CorElementType.I4)
                .Param(CorElementType.I4)
                .Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(3, layout.Arguments.Count);
        Assert.Equal(OffsetOfECX, layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfEDX, layout.Arguments[1].Slots[0].Offset);
        Assert.Equal(Case.OffsetOfArgs, layout.Arguments[2].Slots[0].Offset);
    }

    [Fact]
    public void StaticMethod_RetBuf_UserArgGoesToStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable bigMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "BigReturn", structSize: 12,
                    fields: [new(0, CorElementType.I4), new(4, CorElementType.I4), new(8, CorElementType.I4)]);
                sig.ReturnValueType(new TargetPointer(bigMT.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfEDX, layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void InstanceMethod_RetBuf_UserArgGoesToStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: true,
            (rts, sig) =>
            {
                MockMethodTable bigMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "BigReturn", structSize: 12,
                    fields: [new(0, CorElementType.I4), new(4, CorElementType.I4), new(8, CorElementType.I4)]);
                sig.ReturnValueType(new TargetPointer(bigMT.Address)).Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(Case.OffsetOfArgs, layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void StaticMethod_WithParamType_UserArgGoesToECX_ParamTypeInEDX()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) => sig.Return(CorElementType.Void).Param(CorElementType.I4),
            hasParamType: true);

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.Equal(OffsetOfECX, layout.Arguments[0].Slots[0].Offset);
    }

#pragma warning disable xUnit1004 // Test methods should not be skipped -- tracking an implementation gap.
    [Fact]
    public void VarArgs_CookieAtSizeOfTransitionBlock()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.VarArg().Return(CorElementType.Void).Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.NotNull(layout.VarArgCookieOffset);
        Assert.Equal(Case.TransitionBlockSize, layout.VarArgCookieOffset.Value);
        Assert.Single(layout.Arguments);
        // On x86 varargs, the cookie occupies the first stack slot (at OffsetOfArgs),
        // so the first user arg is one slot above it.
        Assert.Equal(Case.OffsetOfArgs + Case.PointerSize, layout.Arguments[0].Slots[0].Offset);
    }
#pragma warning restore xUnit1004

    [Fact]
    public void TypedReference_GoesToStack_NotEnregistered()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable typedRefMT = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "System.TypedReference", structSize: 8,
                    fields: [new(0, CorElementType.Byref), new(4, CorElementType.I)]);
                rts.SetTypedReferenceMethodTable(typedRefMT.Address);
                sig.Return(CorElementType.Void).Param(CorElementType.TypedByRef);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(Case.OffsetOfArgs, layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void LargeStruct_PassedByValueOnStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            Case, hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable mt = MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                    rts, "BigStruct", structSize: 24,
                    fields:
                    [
                        new(0, CorElementType.I4), new(4, CorElementType.I4), new(8, CorElementType.I4),
                        new(12, CorElementType.I4), new(16, CorElementType.I4), new(20, CorElementType.I4),
                    ]);
                sig.Return(CorElementType.Void).ParamValueType(new TargetPointer(mt.Address));
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(Case.OffsetOfArgs, layout.Arguments[0].Slots[0].Offset);
    }

    [Fact]
    public void TenArgs_TwoInRegs_EightOnStack()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig =>
            {
                sig.Return(CorElementType.Void);
                for (int i = 0; i < 10; i++) sig.Param(CorElementType.I4);
            });

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Equal(10, layout.Arguments.Count);
        Assert.Equal(OffsetOfECX, layout.Arguments[0].Slots[0].Offset);
        Assert.Equal(OffsetOfEDX, layout.Arguments[1].Slots[0].Offset);

        for (int i = 2; i < 10; i++)
        {
            int expectedStackOfs = Case.OffsetOfArgs + (10 - 1 - i) * Case.PointerSize;
            Assert.Equal(expectedStackOfs, layout.Arguments[i].Slots[0].Offset);
        }
    }

    [Theory]
    [InlineData(CorElementType.Object)]
    [InlineData(CorElementType.String)]
    public void GCReferenceArgs_Enregister(CorElementType refType)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            Case,
            sig => sig.Return(CorElementType.Void).Param(refType));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.Single(layout.Arguments);
        Assert.False(layout.Arguments[0].IsPassedByRef);
        Assert.Equal(OffsetOfECX, layout.Arguments[0].Slots[0].Offset);
    }
}
