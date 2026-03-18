// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.DataContractReader.Tests.GCStress;

/// <summary>
/// Runs each debuggee app under corerun with DOTNET_GCStress=0x24 and asserts
/// that the cDAC stack reference verification achieves 100% pass rate.
/// </summary>
/// <remarks>
/// Prerequisites:
/// - Build CoreCLR native + cDAC: build.cmd -subset clr.native+tools.cdac -c Debug -rc Checked -lc Release
/// - Generate core_root: src\tests\build.cmd Checked generatelayoutonly /p:LibrariesConfiguration=Release
/// - Build debuggees: dotnet build this test project
///
/// The tests use CORE_ROOT env var if set, otherwise default to the standard artifacts path.
/// </remarks>
public class BasicGCStressTests : GCStressTestBase
{
    public BasicGCStressTests(ITestOutputHelper output) : base(output) { }

    public static IEnumerable<object[]> Debuggees =>
    [
        ["BasicAlloc"],
        ["DeepStack"],
        ["Generics"],
        ["MultiThread"],
        ["Comprehensive"],
        ["ExceptionHandling"],
    ];

    public static IEnumerable<object[]> WindowsOnlyDebuggees =>
    [
        ["PInvoke"],
    ];

    [Theory]
    [MemberData(nameof(Debuggees))]
    public void GCStress_AllVerificationsPass(string debuggeeName)
    {
        GCStressResults results = RunGCStress(debuggeeName);
        AssertAllPassed(results, debuggeeName);
    }

    [Theory]
    [MemberData(nameof(WindowsOnlyDebuggees))]
    public void GCStress_WindowsOnly_AllVerificationsPass(string debuggeeName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new SkipTestException("P/Invoke debuggee uses kernel32.dll (Windows only)");

        GCStressResults results = RunGCStress(debuggeeName);
        AssertAllPassed(results, debuggeeName);
    }
}
