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
/// Runs each debuggee app under corerun with DOTNET_CdacStress=0x101 (ALLOC + GCREFS)
/// and asserts that the cDAC stack reference verification produces no
/// `[FAIL]` results. `[KNOWN_ISSUE]` verifications (where the cDAC explicitly
/// marks a frame as deferred via `RecordDeferredFrame`) are tolerated.
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

    [Theory]
    [MemberData(nameof(Debuggees))]
    public async Task GCStress_AllVerificationsPass(string debuggeeName)
    {
        CdacStressResults results = await RunGCStressAsync(debuggeeName);
        AssertAllPassed(results, debuggeeName);
    }

    [ConditionalTheory]
    [MemberData(nameof(WindowsOnlyDebuggees))]
    public async Task GCStress_WindowsOnly_AllVerificationsPass(string debuggeeName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new SkipTestException("P/Invoke debuggee uses kernel32.dll (Windows only)");

        CdacStressResults results = await RunGCStressAsync(debuggeeName);
        AssertAllPassed(results, debuggeeName);
    }
}
