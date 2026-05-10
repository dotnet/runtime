// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for <see cref="DacDbiImpl.GetVarArgSig"/> and the
/// underlying <see cref="ISignature"/> contract.
///
/// The dump tests focus on robustness of the calling convention and error handling on
/// invalid input. Unit tests in <c>SignatureTests</c> exercise the success path with a
/// synthetic VASigCookie laid out in mock memory; constructing one in a real dump
/// requires walking the IL stub's local variables for a vararg call site, which is
/// beyond the scope of these tests.
/// </summary>
public class DacDbiVarArgSigDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "VarargPInvoke";
    protected override string DumpType => "full";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public unsafe void GetVarArgSig_NullCookieAddr_ReturnsFailure(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong argBase;
        DacDbiTargetBuffer retVal;
        int hr = dbi.GetVarArgSig(0, &argBase, &retVal);

        Assert.True(hr < 0, $"Expected failing HRESULT for null VASigCookie address, got 0x{hr:X8}");
        Assert.Equal(0ul, argBase);
        Assert.Equal(0ul, retVal.pAddress);
        Assert.Equal(0u, retVal.cbSize);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public unsafe void GetVarArgSig_InvalidCookieAddr_ReturnsFailure(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        // An arbitrary address that does not point at a VASigCookie* slot in the dump.
        const ulong invalidAddr = 0xDEAD_BEEF_DEAD_BEEFul;

        ulong argBase;
        DacDbiTargetBuffer retVal;
        int hr = dbi.GetVarArgSig(invalidAddr, &argBase, &retVal);

        Assert.True(hr < 0, $"Expected failing HRESULT for invalid VASigCookie address, got 0x{hr:X8}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "VarargPInvoke debuggee uses msvcrt.dll (Windows only)")]
    public unsafe void GetVarArgSig_NullCookieAddr_OutputsCleared(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong argBase = 0xCAFEul;
        DacDbiTargetBuffer retVal = new() { pAddress = 0xCAFEul, cbSize = 0xCAFEu };
        int hr = dbi.GetVarArgSig(0, &argBase, &retVal);

        Assert.True(hr < 0);
        // On failure the API still zeroes the output parameters before throwing so callers
        // can rely on them being cleared.
        Assert.Equal(0ul, argBase);
        Assert.Equal(0ul, retVal.pAddress);
        Assert.Equal(0u, retVal.cbSize);
    }
}
