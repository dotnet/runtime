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
/// Runs each debuggee app under corerun with the cDAC stress framework
/// enabled and asserts that the cross-checked verification produces no
/// failures. Two parallel theories share the same Helix work item and
/// debuggee build but exercise independent sub-checks:
///
/// * <see cref="GCStress_AllVerificationsPass"/> -- DOTNET_CdacStress=0x101
///   (ALLOC + GCREFS). Compares cDAC <c>GetStackReferences</c> output
///   against the runtime's own GC root oracle. <c>[KNOWN_ISSUE]</c>
///   results (where the cDAC explicitly marks a frame as deferred via
///   <c>RecordDeferredFrame</c>) are tolerated.
///
/// * <see cref="ArgIterStress_AllVerificationsPass"/> -- DOTNET_CdacStress=0x201
///   (ALLOC + ARGITER). Compares cDAC-built GCRefMap blobs (via the
///   <see cref="ICallingConvention"/> contract) against the runtime's
///   <c>ComputeCallRefMap</c>. Any <c>[ARG_FAIL]</c> / <c>[ARG_ERROR]</c>
///   / <c>[ARG_SKIP]</c> fails the test -- there is no known-issue
///   mechanism for ARGITER today.
/// </summary>
/// <remarks>
/// Prerequisites:
/// - Build CoreCLR + cDAC (Checked): build.cmd -subset clr.runtime+tools.cdac -c Checked
/// - Generate core_root: src\tests\build.cmd Checked generatelayoutonly /p:LibrariesConfiguration=Release
/// - Build debuggees: dotnet build this test project
///
/// The tests use CORE_ROOT env var if set, otherwise default to the standard artifacts path.
/// </remarks>
public class BasicStressTests : CdacStressTestBase
{
    public BasicStressTests(ITestOutputHelper output) : base(output) { }

    public static IEnumerable<object[]> Debuggees =>
    [
        ["BasicAlloc"],
        ["DeepStack"],
        ["Generics"],
        ["MultiThread"],
        ["Comprehensive"],
        ["ExceptionHandling"],
        ["StructScenarios"],
        ["DynamicMethods"],
    ];

    public static IEnumerable<object[]> WindowsOnlyDebuggees =>
    [
        ["PInvoke"],
    ];

    /// <summary>
    /// Debuggees exercised only under the ARGITER sub-check. Today this is
    /// <c>CallSignatures</c>, which intentionally includes <c>__arglist</c>
    /// methods that hit a known cDAC GCREFS gap: <c>GetStackReferences</c>
    /// does not walk the <c>VASigCookie</c> signature blob to enumerate
    /// the variadic-tail GC refs, so GCREFS reports false failures on
    /// vararg frames. ARGITER has no such gap (the cdac encoder emits
    /// <c>GCRefMapToken.VASigCookie</c> and stops, matching the runtime's
    /// <c>FakeGcScanRoots</c> short-circuit).
    /// </summary>
    public static IEnumerable<object[]> ArgIterOnlyDebuggees =>
    [
        ["CallSignatures"],
    ];

    [ConditionalTheory]
    [MemberData(nameof(Debuggees))]
    public async Task GCStress_AllVerificationsPass(string debuggeeName)
    {
        // The GCREFS sub-check has only been validated on architectures where
        // the cDAC GC root enumeration is at parity with the runtime. x86 has
        // not been brought up yet (a separate effort); skip there until it is.
        if (GetTargetArchitecture() == Architecture.X86)
            throw new SkipTestException("GCREFS stress is not yet validated on x86 (ARGITER stress runs there instead)");

        CdacStressResults results = await RunGCStressAsync(debuggeeName);
        AssertAllPassed(results, debuggeeName);
    }

    [ConditionalTheory]
    [MemberData(nameof(WindowsOnlyDebuggees))]
    public async Task GCStress_WindowsOnly_AllVerificationsPass(string debuggeeName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new SkipTestException("P/Invoke debuggee uses kernel32.dll (Windows only)");
        if (GetTargetArchitecture() == Architecture.X86)
            throw new SkipTestException("GCREFS stress is not yet validated on x86");

        CdacStressResults results = await RunGCStressAsync(debuggeeName);
        AssertAllPassed(results, debuggeeName);
    }

    [Theory]
    [MemberData(nameof(Debuggees))]
    public async Task ArgIterStress_AllVerificationsPass(string debuggeeName)
    {
        CdacStressResults results = await RunArgIterStressAsync(debuggeeName);
        AssertAllArgIterPassed(results, debuggeeName);
    }

    [Theory]
    [MemberData(nameof(ArgIterOnlyDebuggees))]
    public async Task ArgIterStress_ArgIterOnly_AllVerificationsPass(string debuggeeName)
    {
        CdacStressResults results = await RunArgIterStressAsync(debuggeeName);
        AssertAllArgIterPassed(results, debuggeeName);
    }

    [ConditionalTheory]
    [MemberData(nameof(WindowsOnlyDebuggees))]
    public async Task ArgIterStress_WindowsOnly_AllVerificationsPass(string debuggeeName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new SkipTestException("P/Invoke debuggee uses kernel32.dll (Windows only)");

        CdacStressResults results = await RunArgIterStressAsync(debuggeeName);
        AssertAllArgIterPassed(results, debuggeeName);
    }
}
