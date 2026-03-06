// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for IXCLRDataFrame methods.
/// Each test selects its own debuggee via the overloaded InitializeDumpTest.
/// </summary>
public unsafe class ClrDataFrameDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    // GetContext tests

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_ReturnsNonEmptyContext(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        IStackDataFrameHandle firstFrame = stackWalk.CreateStackWalk(crashingThread).First();

        ClrDataFrame frame = new ClrDataFrame(Target, firstFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        byte[] contextBuf = new byte[4096];
        uint contextSize;
        int hr = xclrFrame.GetContext(0, (uint)contextBuf.Length, &contextSize, contextBuf);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(contextSize > 0, "Expected non-zero context size");
        Assert.True(contextBuf.Take((int)contextSize).Any(b => b != 0), "Expected non-zero context bytes");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_SetsContextSize(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        IStackDataFrameHandle firstFrame = stackWalk.CreateStackWalk(crashingThread).First();

        ClrDataFrame frame = new ClrDataFrame(Target, firstFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        byte[] contextBuf = new byte[4096];
        uint contextSize;
        int hr = xclrFrame.GetContext(0, (uint)contextBuf.Length, &contextSize, contextBuf);

        Assert.Equal(System.HResults.S_OK, hr);

        byte[] rawContext = stackWalk.GetRawContext(firstFrame);
        Assert.Equal((uint)rawContext.Length, contextSize);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_ExactSizeBuffer_Succeeds(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        IStackDataFrameHandle firstFrame = stackWalk.CreateStackWalk(crashingThread).First();

        ClrDataFrame frame = new ClrDataFrame(Target, firstFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        byte[] rawContext = stackWalk.GetRawContext(firstFrame);
        byte[] contextBuf = new byte[rawContext.Length];
        uint contextSize;
        int hr = xclrFrame.GetContext(0, (uint)contextBuf.Length, &contextSize, contextBuf);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal((uint)rawContext.Length, contextSize);
        Assert.True(contextBuf.SequenceEqual(rawContext), "Context bytes should match raw context");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_BufferTooSmall_ReturnsInvalidArg(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        IStackDataFrameHandle firstFrame = stackWalk.CreateStackWalk(crashingThread).First();

        ClrDataFrame frame = new ClrDataFrame(Target, firstFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        byte[] rawContext = stackWalk.GetRawContext(firstFrame);
        Assert.True(rawContext.Length > 0, "Raw context should not be empty for this test.");
        byte[] tinyBuf = new byte[rawContext.Length - 1];
        int hr = xclrFrame.GetContext(0, (uint)tinyBuf.Length, null, tinyBuf);

        Assert.Equal(System.HResults.E_INVALIDARG, hr);
    }

    // GetAppDomain tests

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetAppDomain_ReturnsValidAppDomain(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        IStackDataFrameHandle firstFrame = stackWalk.CreateStackWalk(crashingThread).First();

        ClrDataFrame frame = new ClrDataFrame(Target, firstFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
        int hr = xclrFrame.GetAppDomain(appDomainOut);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.NotNull(appDomainOut.Interface);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetAppDomain_HasExpectedUniqueID(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IXCLRDataAppDomain appDomain = GetAppDomainFromFirstFrame(config);

        ulong uniqueId;
        int hr = appDomain.GetUniqueID(&uniqueId);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(1ul, uniqueId);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetAppDomain_GetName_ReturnsNonEmptyName(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IXCLRDataAppDomain appDomain = GetAppDomainFromFirstFrame(config);

        uint nameLen;
        int hr = appDomain.GetName(0, &nameLen, null);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(nameLen > 1, "Expected non-empty AppDomain name");
        Assert.True(nameLen <= 1024, "AppDomain name unexpectedly long");

        char[] nameBuf = new char[nameLen];
        fixed (char* nameBufPtr = nameBuf)
        {
            hr = appDomain.GetName(nameLen, &nameLen, nameBufPtr);
            Assert.Equal(System.HResults.S_OK, hr);
        }

        string name = new string(nameBuf, 0, (int)nameLen - 1);
        Assert.False(string.IsNullOrEmpty(name), "Expected non-empty AppDomain name string");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetAppDomain_GetFlags_ReturnsDefault(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IXCLRDataAppDomain appDomain = GetAppDomainFromFirstFrame(config);

        uint flags;
        int hr = appDomain.GetFlags(&flags);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0u, flags); // CLRDATA_DOMAIN_DEFAULT
    }

    private IXCLRDataAppDomain GetAppDomainFromFirstFrame(TestConfiguration config)
    {
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        IStackDataFrameHandle firstFrame = stackWalk.CreateStackWalk(crashingThread).First();

        ClrDataFrame frame = new ClrDataFrame(Target, firstFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
        int hr = xclrFrame.GetAppDomain(appDomainOut);
        Assert.Equal(System.HResults.S_OK, hr);

        return appDomainOut.Interface!;
    }
}
