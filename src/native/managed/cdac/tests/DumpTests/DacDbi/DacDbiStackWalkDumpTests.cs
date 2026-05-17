// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl stack walk methods (IsLeafFrame, GetContext).
/// Uses the StackWalk debuggee (full dump).
/// </summary>
public class DacDbiStackWalkDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void GetContext_Succeeds_ForCrashingThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        uint contextSize = IPlatformAgnosticContext.GetContextForPlatform(Target).Size;
        byte[] contextBuffer = new byte[contextSize];

        fixed (byte* pContext = contextBuffer)
        {
            int hr = dbi.GetContext(crashingThread.ThreadAddress, pContext);
            Assert.Equal(System.HResults.S_OK, hr);
        }

        IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(Target);
        ctx.FillFromBuffer(contextBuffer);
        Assert.NotEqual(TargetPointer.Null, ctx.InstructionPointer);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void GetContext_MatchesContractGetContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        uint contextSize = IPlatformAgnosticContext.GetContextForPlatform(Target).Size;

        byte[] dbiContextBuffer = new byte[contextSize];
        fixed (byte* pContext = dbiContextBuffer)
        {
            int hr = dbi.GetContext(crashingThread.ThreadAddress, pContext);
            Assert.Equal(System.HResults.S_OK, hr);
        }

        uint allFlags = IPlatformAgnosticContext.GetContextForPlatform(Target).AllContextFlags;
        byte[] contractContext = Target.Contracts.Thread.GetContext(crashingThread.ThreadAddress, ThreadContextSource.Debugger, allFlags);

        IPlatformAgnosticContext dbiCtx = IPlatformAgnosticContext.GetContextForPlatform(Target);
        IPlatformAgnosticContext contractCtx = IPlatformAgnosticContext.GetContextForPlatform(Target);
        dbiCtx.FillFromBuffer(dbiContextBuffer);
        contractCtx.FillFromBuffer(contractContext);

        Assert.Equal(contractCtx.InstructionPointer, dbiCtx.InstructionPointer);
        Assert.Equal(contractCtx.StackPointer, dbiCtx.StackPointer);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void IsLeafFrame_TrueForLeafContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        uint allFlags = IPlatformAgnosticContext.GetContextForPlatform(Target).AllContextFlags;
        byte[] leafContext = Target.Contracts.Thread.GetContext(crashingThread.ThreadAddress, ThreadContextSource.None, allFlags);

        Interop.BOOL result;
        fixed (byte* pContext = leafContext)
        {
            int hr = dbi.IsLeafFrame(crashingThread.ThreadAddress, pContext, &result);
            Assert.Equal(System.HResults.S_OK, hr);
        }

        Assert.Equal(Interop.BOOL.TRUE, result);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void IsLeafFrame_FalseForNonLeafContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        uint allFlags = IPlatformAgnosticContext.GetContextForPlatform(Target).AllContextFlags;
        byte[] leafContext = Target.Contracts.Thread.GetContext(crashingThread.ThreadAddress, ThreadContextSource.None, allFlags);
        IPlatformAgnosticContext leafCtx = IPlatformAgnosticContext.GetContextForPlatform(Target);
        leafCtx.FillFromBuffer(leafContext);

        IStackWalk sw = Target.Contracts.StackWalk;

        // Find a frame whose SP+IP differs from the leaf context
        byte[]? nonLeafContext = sw.CreateStackWalk(crashingThread)
            .Select(sw.GetRawContext)
            .FirstOrDefault(ctx =>
            {
                IPlatformAgnosticContext frameCtx = IPlatformAgnosticContext.GetContextForPlatform(Target);
                frameCtx.FillFromBuffer(ctx);
                return frameCtx.StackPointer != leafCtx.StackPointer
                    || frameCtx.InstructionPointer != leafCtx.InstructionPointer;
            });

        Assert.NotNull(nonLeafContext);

        Interop.BOOL result;
        fixed (byte* pContext = nonLeafContext)
        {
            int hr = dbi.IsLeafFrame(crashingThread.ThreadAddress, pContext, &result);
            Assert.Equal(System.HResults.S_OK, hr);
        }

        Assert.Equal(Interop.BOOL.FALSE, result);
    }
}
