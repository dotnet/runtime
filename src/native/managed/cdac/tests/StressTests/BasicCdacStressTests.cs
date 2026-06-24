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
///   <c>ComputeCallRefMap</c>. <c>[ARG_SKIP]</c> results (where either
///   side returned <c>E_NOTIMPL</c> / <c>S_FALSE</c>) are tolerated and
///   logged for triage visibility. <c>[ARG_FAIL]</c> (byte-for-byte
///   mismatch) and <c>[ARG_ERROR]</c> (unexpected failure HR from cDAC
///   or runtime) still fail the test.
///
/// Scope of this PR: ARGITER is validated on Windows x86 / x64 only.
/// Other targets (Linux, macOS, Windows ARM64, ARM32) hit known gaps
/// in the cDAC encoder or shared ArgIterator port and are explicitly
/// skipped pending follow-up work (see <see cref="IsArgIterValidatedTarget"/>).
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
        ["CallSignatures"],
    ];

    public static IEnumerable<object[]> WindowsOnlyDebuggees =>
    [
        ["PInvoke"],
    ];

    /// <summary>
    /// Debuggees that exercise the CLI native varargs calling convention
    /// (<c>__arglist</c>). The JIT only supports this convention on
    /// Windows x86 / x64 / ARM64 -- see
    /// <c>src/coreclr/jit/target.h::compFeatureVarArg</c>. Tests gate
    /// on both OS=Windows and architecture != ARM32. Additionally,
    /// these debuggees run under the ARGITER sub-check only: the cDAC
    /// <c>GetStackReferences</c> doesn't yet walk the VASigCookie
    /// signature blob, so GCREFS reports false failures on vararg
    /// frames.
    /// </summary>
    public static IEnumerable<object[]> VarArgsDebuggees =>
    [
        ["VarArgs"],
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

    [ConditionalTheory]
    [MemberData(nameof(Debuggees))]
    public async Task ArgIterStress_AllVerificationsPass(string debuggeeName)
    {
        // Scope of this PR: ARGITER is validated on Windows x86 / x64
        // only. Other architectures hit known gaps that need follow-up
        // work (SystemV-AMD64 / ARM64 struct-in-register classification,
        // arm32 ABI port). Skip there until those land.
        if (!IsArgIterValidatedTarget())
            throw new SkipTestException(ArgIterValidatedTargetReason);

        CdacStressResults results = await RunArgIterStressAsync(debuggeeName);
        AssertAllArgIterPassed(results, debuggeeName);
    }

    [ConditionalTheory]
    [MemberData(nameof(VarArgsDebuggees))]
    public async Task ArgIterStress_VarArgs_AllVerificationsPass(string debuggeeName)
    {
        // VarArgs additionally requires the CLI vararg / __arglist
        // calling convention, which compFeatureVarArg (target.h) gates
        // to Windows non-ARM32. Combined with the PR's overall scope
        // (windows-x86 / windows-x64 only), the effective matrix here
        // is the same as ArgIterStress_AllVerificationsPass.
        if (!IsArgIterValidatedTarget())
            throw new SkipTestException(ArgIterValidatedTargetReason);

        CdacStressResults results = await RunArgIterStressAsync(debuggeeName);
        AssertAllArgIterPassed(results, debuggeeName);
    }

    [ConditionalTheory]
    [MemberData(nameof(WindowsOnlyDebuggees))]
    public async Task ArgIterStress_WindowsOnly_AllVerificationsPass(string debuggeeName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new SkipTestException("P/Invoke debuggee uses kernel32.dll (Windows only)");
        if (!IsArgIterValidatedTarget())
            throw new SkipTestException(ArgIterValidatedTargetReason);

        CdacStressResults results = await RunArgIterStressAsync(debuggeeName);
        AssertAllArgIterPassed(results, debuggeeName);
    }

    /// <summary>
    /// The set of (OS, architecture) targets where the ARGITER sub-check
    /// is validated as part of this PR: Windows x86 and Windows x64.
    /// Other targets are intentionally out of scope and need follow-up
    /// work before they can be enabled:
    ///   * Linux / macOS: SystemV-AMD64 struct-in-register classification
    ///     (cDAC throws NotImplementedException for any method with a
    ///     small struct passed in registers, e.g. System.Guid).
    ///   * ARM64 (Windows or Linux): same struct-in-register gap plus
    ///     HFA/HVA handling.
    ///   * ARM32: shared ArgIterator port has unported paths that throw
    ///     mid-enumeration.
    /// </summary>
    private static bool IsArgIterValidatedTarget()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;
        Architecture arch = GetTargetArchitecture();
        return arch is Architecture.X86 or Architecture.X64;
    }

    private const string ArgIterValidatedTargetReason =
        "ARGITER stress is validated for windows-x86 / windows-x64 in this PR; " +
        "other targets need follow-up work (SystemV / ARM64 struct-in-registers, ARM32 ABI port).";
}
