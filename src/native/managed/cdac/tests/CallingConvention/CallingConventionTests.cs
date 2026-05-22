// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class CallingConventionTests
{
    [Theory]
    [MemberData(nameof(CallConvCases.AllCases), MemberType = typeof(CallConvCases))]
    public void Harness_StaticMethod_OneInt_DecodesSuccessfully(CallConvTestCase testCase)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithStaticMethod(
            testCase,
            sig => sig.Return(CorElementType.Void).Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);

        Assert.Null(layout.ThisOffset);
        Assert.Null(layout.AsyncContinuationOffset);
        Assert.Single(layout.Arguments);
        Assert.Single(layout.Arguments[0].Slots);
        Assert.Equal(CorElementType.I4, layout.Arguments[0].Slots[0].ElementType);
    }

    [Theory]
    [MemberData(nameof(CallConvCases.AllCases), MemberType = typeof(CallConvCases))]
    public void Harness_InstanceMethod_ReportsThisOffset(CallConvTestCase testCase)
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            testCase, hasThis: true,
            sig => sig.Return(CorElementType.Void).Param(CorElementType.I4));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.NotNull(layout.ThisOffset);
    }

    [Fact]
    public void InstanceMethod_OnValueType_IsValueTypeThisShouldBeTrue()
    {
        var (target, mdh) = CallingConventionTestHelpers.CreateTargetWithMethod(
            CallConvCases.AMD64Windows, hasThis: true,
            (rts, sig) => sig.Return(CorElementType.Void),
            enclosingMTOverride: rts => MockDescriptors.CallingConvention.AddValueTypeMethodTable(
                rts, "ValueTypeWithMethod", structSize: 8, fields: [new(0, CorElementType.I4)]));

        CallSiteLayout layout = target.Contracts.CallingConvention.ComputeCallSiteLayout(mdh);
        Assert.NotNull(layout.ThisOffset);
        Assert.True(layout.IsValueTypeThis,
            "Instance method on a value type should report IsValueTypeThis == true so GcScanner emits GC_CALL_INTERIOR on the this slot");
    }
}
