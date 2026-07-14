// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.DataContractReader.Tests.GCStress;

/// <summary>
/// Runs each debuggee under corerun with the cDAC stress framework enabled
/// and asserts the cross-checked verification produces no failures. The
/// <c>GCRefStress_*</c> theories run with DOTNET_CdacStress=0x101 (ALLOC +
/// GCREFS); the <c>ArgIterStress_*</c> theories run with 0x201 (ALLOC +
/// ARGITER). See <c>StressTests/README.md</c> for the flag layout and the
/// pass/fail semantics.
/// </summary>
public class CdacStressTests : CdacStressTestBase
{
    public CdacStressTests(ITestOutputHelper output) : base(output) { }

    public record Debuggee(string Name, bool WindowsOnly = false, bool SkipGCRefs = false);

    public static IEnumerable<object[]> Debuggees =>
    [
        [new Debuggee("BasicAlloc")],
        [new Debuggee("DeepStack")],
        [new Debuggee("Generics")],
        [new Debuggee("MultiThread")],
        [new Debuggee("Comprehensive")],
        [new Debuggee("ExceptionHandling")],
        [new Debuggee("StructScenarios")],
        [new Debuggee("DynamicMethods")],
        [new Debuggee("CallSignatures")],
        [new Debuggee("CrossModule")],
        [new Debuggee("PInvoke", WindowsOnly: true)],
        // VarArgs is intentionally excluded from GCREFS: the cDAC's
        // GetStackReferences does not yet walk the VASigCookie signature
        // blob to enumerate the variadic-tail GC refs, so GCREFS reports
        // false failures on vararg frames. ARGITER has no such gap (the
        // encoder emits GCRefMapToken.VASigCookie and stops, matching the
        // runtime's FakeGcScanRoots short-circuit).
        [new Debuggee("VarArgs", WindowsOnly: true, SkipGCRefs: true)],
    ];

    [ConditionalTheory]
    [MemberData(nameof(Debuggees))]
    public async Task GCRefStress_AllVerificationsPass(Debuggee debuggee)
    {
        GetTargetPlatform(out OSPlatform os, out _);

        if (debuggee.WindowsOnly && os != OSPlatform.Windows)
            throw new SkipTestException($"{debuggee.Name} debuggee is Windows-only.");

        if (debuggee.SkipGCRefs)
            throw new SkipTestException($"{debuggee.Name} is excluded from GCREFS pending follow-up work.");

        CdacStressResults results = await RunGCRefStressAsync(debuggee.Name);
        AssertAllPassed(results, debuggee.Name);
    }

    [ConditionalTheory]
    [MemberData(nameof(Debuggees))]
    public async Task ArgIterStress_AllVerificationsPass(Debuggee debuggee)
    {
        GetTargetPlatform(out OSPlatform os, out _);

        if (debuggee.WindowsOnly && os != OSPlatform.Windows)
            throw new SkipTestException($"{debuggee.Name} debuggee is Windows-only.");

        CdacStressResults results = await RunArgIterStressAsync(debuggee.Name);
        AssertAllArgIterPassed(results, debuggee.Name);
    }
}
