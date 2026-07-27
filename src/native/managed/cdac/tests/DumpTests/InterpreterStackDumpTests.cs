// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for cDAC interpreter support.
/// Uses the InterpreterStack debuggee dump, which has a deterministic call stack:
/// Main -> MethodA -> MethodB -> JitTrampoline.Bounce -> MethodC -> MethodD -> FailFast.
/// Under DOTNET_Interpreter=MethodA, MethodA/B/C/D are interpreted while Main,
/// Bounce, and FailFast remain JIT'd. The trampoline is in a separate assembly
/// so it is NOT in g_interpModule, creating two distinct InterpreterFrame regions
/// on the stack with a JIT'd gap between them. Both InterpreterFrame regions have
/// multiple interpreted methods (pParent chain).
/// </summary>
public class InterpreterStackDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "InterpreterStack";

    private void SkipIfInterpreterNotAvailable()
    {
        try
        {
            Target.GetTypeInfo(DataType.InterpreterFrame);
        }
        catch (InvalidOperationException)
        {
            throw new SkipTestException("Interpreter support not available in this runtime build (FEATURE_INTERPRETER not enabled).");
        }
    }

    private void AssertInterpreted(ResolvedFrame f)
    {
        // In the DAC stack walk, interpreted methods appear as frameless frames
        // (via interpreter virtual unwind). Verify frameless and interpreter code.
        Assert.Null(f.FrameName);

        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IExecutionManager executionManager = Target.Contracts.ExecutionManager;

        MethodDescHandle md = rts.GetMethodDescHandle(f.MethodDescPtr);
        TargetCodePointer nativeCode = rts.GetNativeCode(md);
        TargetCodePointer resolvedCode = Target.Contracts.PrecodeStubs.GetInterpreterCodeFromInterpreterPrecodeIfPresent(nativeCode);
        Assert.NotEqual(TargetCodePointer.Null, resolvedCode);

        CodeBlockHandle? codeBlock = executionManager.GetCodeBlockHandle(resolvedCode);
        Assert.NotNull(codeBlock);
        Assert.Equal(CodeKind.Interpreter, executionManager.GetCodeKind(resolvedCode));
    }

    private void AssertJitted(ResolvedFrame f)
    {
        Assert.Null(f.FrameName);

        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IExecutionManager executionManager = Target.Contracts.ExecutionManager;

        MethodDescHandle md = rts.GetMethodDescHandle(f.MethodDescPtr);
        TargetCodePointer nativeCode = rts.GetNativeCode(md);
        Assert.NotEqual(TargetCodePointer.Null, nativeCode);
        CodeBlockHandle? codeBlock = executionManager.GetCodeBlockHandle(nativeCode);
        Assert.NotNull(codeBlock);
        Assert.Equal(CodeKind.Jitted, executionManager.GetCodeKind(nativeCode));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void StackWalk_VerifyInterleavedStackLayout(TestConfiguration config)
    {
        InitializeDumpTest(config);
        SkipIfInterpreterNotAvailable();

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        // Expected stack layout matching native DAC `!clrstack` output.
        // Use adjacent assertions to verify no extra InterpreterFrame appears
        // between the interpreter region and the JIT trampoline.
        DumpTestStackWalker.Walk(Target, crashingThread)
            .ExpectRuntimeFrame("InterpreterFrame")
            .ExpectAdjacentFrame("MethodD", AssertInterpreted)
            .ExpectAdjacentFrame("MethodC", AssertInterpreted)
            .ExpectAdjacentFrame("Bounce", AssertJitted)
            .ExpectAdjacentRuntimeFrame("InterpreterFrame")
            .ExpectAdjacentFrame("MethodB", AssertInterpreted)
            .ExpectAdjacentFrame("MethodA", AssertInterpreted)
            .ExpectAdjacentFrame("Main", AssertJitted)
            .Verify();
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void StackWalk_InterpreterMethodNativeCodeIsPrecode(TestConfiguration config)
    {
        InitializeDumpTest(config);
        SkipIfInterpreterNotAvailable();
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IExecutionManager executionManager = Target.Contracts.ExecutionManager;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        DumpTestStackWalker walker = DumpTestStackWalker.Walk(Target, crashingThread);

        // Find the first interpreter method (MethodA/B/C) on the stack.
        ResolvedFrame interpFrame = walker.Frames
            .First(f => f.Name is "MethodA" or "MethodB" or "MethodC" or "MethodD");

        MethodDescHandle mdHandle = rts.GetMethodDescHandle(interpFrame.MethodDescPtr);
        TargetCodePointer nativeCode = rts.GetNativeCode(mdHandle);
        Assert.NotEqual(TargetCodePointer.Null, nativeCode);

        // For interpreter methods, GetCodeBlockHandle returns null because the native code
        // slot points to a precode, not a managed code heap entry.
        CodeBlockHandle? codeBlock = executionManager.GetCodeBlockHandle(nativeCode);
        Assert.Null(codeBlock);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void Thread_CanEnumerateWithInterpreterFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);
        SkipIfInterpreterNotAvailable();
        IThread threadContract = Target.Contracts.Thread;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();
        Assert.True(storeData.ThreadCount >= 1,
            "Expected at least one thread in the thread store");

        int threadCount = 0;
        TargetPointer currentThreadPtr = storeData.FirstThread;
        while (currentThreadPtr != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThreadPtr);
            threadCount++;
            currentThreadPtr = threadData.NextThread;
        }

        Assert.True(threadCount >= 1, "Expected at least one thread when walking the list");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void StackWalk_NoDoubledInterpreterFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);
        SkipIfInterpreterNotAvailable();

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        DumpTestStackWalker walker = DumpTestStackWalker.Walk(Target, crashingThread);

        // Matching DAC behavior: each interpreted method appears exactly once as a
        // frameless frame. The InterpreterFrame entries have no method name (pMD=NULL).
        string[] expectedMethods = ["MethodA", "MethodB", "MethodC", "MethodD"];
        foreach (string method in expectedMethods)
        {
            int count = walker.Frames.Count(f => string.Equals(f.Name, method, StringComparison.Ordinal));
            Assert.True(count == 1,
                $"Expected '{method}' to appear exactly once but found {count} occurrence(s). " +
                $"Full stack: [{string.Join(", ", walker.Frames.Select(f => $"{f.Name ?? "<null>"}({f.FrameName ?? "frameless"})"))}]");
        }

        // Verify no InterpreterFrame appears consecutively — this is the "doubled
        // frame" bug that native PR #126953 fixed. After the interpreter virtual
        // unwind exhausts the chain, the frame iterator must have advanced past
        // the owning InterpreterFrame so it doesn't get yielded again.
        for (int i = 0; i < walker.Frames.Count - 1; i++)
        {
            if (walker.Frames[i].FrameName == "InterpreterFrame"
                && walker.Frames[i + 1].FrameName == "InterpreterFrame")
            {
                Assert.Fail(
                    $"Consecutive InterpreterFrame entries at indices {i} and {i + 1} — " +
                    $"this indicates the doubled InterpreterFrame bug. " +
                    $"Full stack: [{string.Join(", ", walker.Frames.Select(f => $"{f.Name ?? "<null>"}({f.FrameName ?? "frameless"})"))}]");
            }
        }
    }
}
