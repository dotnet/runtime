// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// Cross-architecture tests for <see cref="ICallingConvention"/>. These
/// verify harness-level invariants (the contract decodes without throwing,
/// arg counts match) that should hold on every supported architecture.
/// Per-architecture offset assertions live in the platform-specific test
/// classes (e.g. <c>X86CallingConventionTests</c>).
/// </summary>
/// <remarks>
/// <para>Gaps NOT covered by Skip-tagged tests anywhere:</para>
/// <list type="bullet">
///   <item>#8 Base <c>ComputeSizeOfArgStack</c> byref adjustment — observable
///   via internal <c>CbStackPop()</c> / <c>SizeOfFrameArgumentArray()</c>
///   only, which are not exposed through <see cref="CallSiteLayout"/>.</item>
///   <item>#12 ARM64 / RV byref classification differences — the cDAC
///   heuristic agrees with native for all currently exercised struct shapes;
///   a divergent shape would need to be identified from native source.</item>
///   <item>Arm32 softfp detection — no detection mechanism in cDAC today.</item>
///   <item>SysV generic value-type TypeSpec resolution — needs generic
///   instantiation infrastructure in the mock RTS.</item>
/// </list>
/// </remarks>
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

    /// <summary>
    /// Verifies <see cref="CallSiteLayout.IsValueTypeThis"/> is true when the
    /// instance method's enclosing class is a value type. Not arch-specific —
    /// the bit is computed by <c>CallingConvention_1</c> directly from the
    /// enclosing MT's <c>IsValueType</c> flag.
    /// </summary>
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
