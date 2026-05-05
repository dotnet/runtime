// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the InterpreterStackDoubleWalk debuggee.
/// This debuggee uses two threads:
///   - Worker thread: MethodA -> MethodB -> Bounce -> MethodC -> MethodD -> spin loop (interpreted)
///   - Main thread: waits for worker, then calls FailFast
///
/// The tests walk the <b>worker thread</b> (not the crashing thread) to verify
/// interpreter frame handling on a thread that has a fully populated InterpreterFrame
/// chain while spinning in interpreted code. Even though the worker executes interpreted
/// code, the CPU IP is inside the native interpreter engine at dump time, so the
/// walk starts from SW_FRAME state and encounters InterpreterFrames via the Frame chain.
/// </summary>
public class InterpreterStackDoubleWalkDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "InterpreterStackDoubleWalk";
    protected override string DumpType => "full";

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
        Assert.Null(f.FrameName);

        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IExecutionManager executionManager = Target.Contracts.ExecutionManager;

        MethodDescHandle md = rts.GetMethodDescHandle(f.MethodDescPtr);
        TargetCodePointer nativeCode = rts.GetNativeCode(md);
        TargetCodePointer resolvedCode = Target.Contracts.PrecodeStubs.GetInterpreterCodeFromInterpreterPrecodeIfPresent(nativeCode);
        Assert.NotEqual(TargetCodePointer.Null, resolvedCode);

        CodeBlockHandle? codeBlock = executionManager.GetCodeBlockHandle(resolvedCode);
        Assert.NotNull(codeBlock);
        Assert.Equal(JitType.Interpreter, executionManager.GetJITType(codeBlock.Value));
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
        Assert.Equal(JitType.Jit, executionManager.GetJITType(codeBlock.Value));
    }

    /// <summary>
    /// Walks the worker thread and verifies the interleaved JIT/interpreter frame layout
    /// matching the native DAC stack walk output.
    /// </summary>
    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void StackWalk_VerifyInterleavedStackLayout(TestConfiguration config)
    {
        InitializeDumpTest(config);
        SkipIfInterpreterNotAvailable();

        ThreadData workerThread = DumpTestHelpers.FindThreadWithMethod(Target, "MethodA");

        // Use adjacent assertions to verify no extra InterpreterFrame appears
        // after the interpreted region — this catches the "doubled frame" bug
        // where InterpreterFrame would appear both before AND after its methods.
        DumpTestStackWalker.Walk(Target, workerThread)
            .ExpectRuntimeFrame("InterpreterFrame")
            .ExpectAdjacentFrame("MethodD", AssertInterpreted)
            .ExpectAdjacentFrame("MethodC", AssertInterpreted)
            .ExpectAdjacentFrame("Bounce", AssertJitted)
            .ExpectAdjacentRuntimeFrame("InterpreterFrame")
            .ExpectAdjacentFrame("MethodB", AssertInterpreted)
            .ExpectAdjacentFrame("MethodA", AssertInterpreted)
            .Verify();
    }

    /// <summary>
    /// Walks the worker thread and verifies each interpreted method appears exactly once
    /// and that InterpreterFrame never appears consecutively (the "doubled frame" bug
    /// that native PR #126953 fixed via ResetRegDisp dedup).
    /// </summary>
    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void StackWalk_NoDoubledInterpreterFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);
        SkipIfInterpreterNotAvailable();

        ThreadData workerThread = DumpTestHelpers.FindThreadWithMethod(Target, "MethodA");
        DumpTestStackWalker walker = DumpTestStackWalker.Walk(Target, workerThread);

        string[] expectedMethods = ["MethodA", "MethodB", "MethodC", "MethodD"];
        foreach (string method in expectedMethods)
        {
            int count = walker.Frames.Count(f => string.Equals(f.Name, method, StringComparison.Ordinal));
            Assert.True(count == 1,
                $"Expected '{method}' to appear exactly once but found {count} occurrence(s). " +
                $"Full stack: [{string.Join(", ", walker.Frames.Select(f => $"{f.Name ?? "<null>"}({f.FrameName ?? "frameless"})"))}]");
        }

        // Verify no InterpreterFrame appears after the interpreted methods it
        // introduces. The original "doubled frame" bug (native PR #126953) was that
        // InterpreterFrame appeared both before AND after its interpreted region.
        // After the interpreter virtual unwind exhausts the chain, the frame
        // iterator must have advanced past the owning InterpreterFrame.
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
