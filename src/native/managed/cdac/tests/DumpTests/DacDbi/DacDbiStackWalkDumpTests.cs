// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.X86;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.DotNet.XUnitExtensions;
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
        Assert.NotEqual(TargetCodePointer.Null, ctx.InstructionPointer);
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
        byte[] contractContext = Target.Contracts.StackWalk.GetContext(crashingThread, ThreadContextSource.Debugger, allFlags);

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
        byte[] leafContext = Target.Contracts.StackWalk.GetContext(crashingThread, ThreadContextSource.None, allFlags);

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
        byte[] leafContext = Target.Contracts.StackWalk.GetContext(crashingThread, ThreadContextSource.None, allFlags);
        IPlatformAgnosticContext leafCtx = IPlatformAgnosticContext.GetContextForPlatform(Target);
        leafCtx.FillFromBuffer(leafContext);

        IStackWalk sw = Target.Contracts.StackWalk;

        // Find a frame whose SP+IP differs from the leaf context
        byte[]? nonLeafContext = DumpTestStackWalker.LegacyVisibleFrames(sw, crashingThread)
            .Select(h => sw.GetRawContext(h))
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "x86 cDAC stack walking is not available in .NET 10")]
    public unsafe void GetStackWalkCurrentFrameInfo_X86HandlerFrame_IncludesSavedRegistersInAmbientSP(TestConfiguration config)
    {
        InitializeDumpTest(config, DebuggeeName, dumpType: "heap");

        if (Target.Contracts.RuntimeInfo.GetTargetArchitecture() != RuntimeInfoArchitecture.X86)
            throw new SkipTestException("This regression test applies only to x86 dumps.");

        DacDbiImpl dbi = CreateDacDbi();
        IExecutionManager executionManager = Target.Contracts.ExecutionManager;
        IGCInfo gcInfo = Target.Contracts.GCInfo;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        uint contextSize = IPlatformAgnosticContext.GetContextForPlatform(Target).Size;
        byte[] contextBuffer = new byte[contextSize];
        nuint stackWalkHandle = 0;

        fixed (byte* context = contextBuffer)
        {
            int hr = dbi.CreateStackWalk(crashingThread.ThreadAddress, context, &stackWalkHandle);
            Assert.Equal(System.HResults.S_OK, hr);
        }

        try
        {
            while (true)
            {
                Debugger_STRData data = default;
                FrameType frameType;
                fixed (byte* context = contextBuffer)
                {
                    data.ctx = (nuint)context;
                    int hr = dbi.GetStackWalkCurrentFrameInfo(stackWalkHandle, (nint)(&data), &frameType);
                    Assert.Equal(System.HResults.S_OK, hr);
                }

                if (frameType == FrameType.AtEndOfStack)
                    break;

                TargetPointer methodDesc = new(data.v.jitFuncData.vmNativeCodeMethodDescToken);
                if (frameType == FrameType.ManagedStackFrame &&
                    DumpTestHelpers.GetMethodName(Target, methodDesc) == "MethodB")
                {
                    IPlatformAgnosticContext frameContext = IPlatformAgnosticContext.GetContextForPlatform(Target);
                    frameContext.FillFromBuffer(contextBuffer);

                    CodeBlockHandle? codeBlockHandle = executionManager.GetCodeBlockHandle(frameContext.InstructionPointer);
                    Assert.NotNull(codeBlockHandle);
                    executionManager.GetGCInfo(codeBlockHandle.Value, out TargetPointer gcInfoAddress, out uint gcInfoVersion);

                    var decoder = Assert.IsType<X86GCInfo>(gcInfo.DecodePlatformSpecificGCInfo(gcInfoAddress, gcInfoVersion));
                    Assert.True(decoder.Header.Handlers);
                    Assert.False(decoder.Header.LocalAlloc);

                    uint expectedStackSize = decoder.RawStackSize
                        + uint.PopCount((uint)decoder.SavedRegsMask) * (uint)Target.PointerSize;
                    Assert.True(expectedStackSize > decoder.RawStackSize);

                    ulong expectedAmbientSP = (frameContext.FramePointer.Value - expectedStackSize + sizeof(int)) & ~3UL;
                    Assert.Equal(expectedAmbientSP, data.v.taAmbientESP);
                    return;
                }

                Interop.BOOL hasMoreFrames;
                int unwindHr = dbi.UnwindStackWalkFrame(stackWalkHandle, &hasMoreFrames);
                Assert.Equal(System.HResults.S_OK, unwindHr);
                if (hasMoreFrames == Interop.BOOL.FALSE)
                    break;
            }

            Assert.Fail("MethodB was not found on the crashing thread's stack.");
        }
        finally
        {
            Assert.Equal(System.HResults.S_OK, dbi.DeleteStackWalk(stackWalkHandle));
        }
    }
}
