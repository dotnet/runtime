// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
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
    public void GetContext_BufferTooSmall_ReturnsInvalidArg(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        IStackDataFrameHandle firstFrame = stackWalk.CreateStackWalk(crashingThread).First();

        ClrDataFrame frame = new ClrDataFrame(Target, firstFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        byte[] tinyBuf = new byte[1];
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

        void* appDomainPtr;
        int hr = xclrFrame.GetAppDomain(&appDomainPtr);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(appDomainPtr is not null, "Expected non-null AppDomain pointer");

        Marshal.Release((nint)appDomainPtr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetAppDomain_HasExpectedUniqueID(TestConfiguration config)
    {
        InitializeDumpTest(config, "StackWalk", "full");

        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        IStackDataFrameHandle firstFrame = stackWalk.CreateStackWalk(crashingThread).First();

        ClrDataFrame frame = new ClrDataFrame(Target, firstFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        void* appDomainPtr;
        int hr = xclrFrame.GetAppDomain(&appDomainPtr);
        Assert.Equal(System.HResults.S_OK, hr);

        try
        {
            StrategyBasedComWrappers cw = new();
            IXCLRDataAppDomain appDomain = (IXCLRDataAppDomain)cw.GetOrCreateObjectForComInstance(
                (nint)appDomainPtr, CreateObjectFlags.UniqueInstance);

            ulong uniqueId;
            int hrId = appDomain.GetUniqueID(&uniqueId);
            Assert.Equal(System.HResults.S_OK, hrId);
            Assert.Equal(1ul, uniqueId);
        }
        finally
        {
            Marshal.Release((nint)appDomainPtr);
        }
    }
}
