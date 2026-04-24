// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the ExecutionManager EH clause enumeration.
/// Uses the ExceptionHandlingInfo debuggee, which has a method with:
///   - A filter clause (catch-when)
///   - A typed catch clause
///   - A catch-all handler (catch without a type)
///   - A finally clause
/// The debuggee crashes via FailFast inside the finally block.
/// </summary>
public class ExceptionHandlingInfoDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "ExceptionHandlingInfo";
    protected override string DumpType => "full";

    /// <summary>
    /// Finds the CodeBlockHandle for the CrashInExceptionHandler method by walking
    /// the crashing thread's stack, resolving method names, then using the method's
    /// native code entry point to obtain a CodeBlockHandle.
    /// </summary>
    private CodeBlockHandle FindCrashMethodCodeBlock()
    {
        IExecutionManager executionManager = Target.Contracts.ExecutionManager;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            string? name = DumpTestHelpers.GetMethodName(Target, methodDescPtr);
            if (name is "CrashInExceptionHandler")
            {
                MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
                TargetCodePointer nativeCode = rts.GetNativeCode(mdHandle);
                CodeBlockHandle? handle = executionManager.GetCodeBlockHandle(new TargetCodePointer(nativeCode.Value + 1UL)); // add 1 to test method-start-finding
                Assert.NotNull(handle);

                return handle.Value;
            }
        }

        Assert.Fail("Could not find CrashInExceptionHandler on the crashing thread's stack");
        return default;
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetJITType_ReturnsCorrectValue(TestConfiguration config)
    {
        InitializeDumpTest(config);
        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        JitType jitType = Target.Contracts.ExecutionManager.GetJITType(codeBlock);
        if (config.R2RMode == "jit")
            Assert.Equal(JitType.Jit, jitType);
        else if (config.R2RMode == "r2r")
            Assert.Equal(JitType.R2R, jitType);
        else
            Assert.Fail($"Unexpected R2RMode value: {config.R2RMode}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetExceptionClauses_ReturnsNonEmptyList(TestConfiguration config)
    {
        InitializeDumpTest(config);

        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        List<ExceptionClauseInfo> clauses = Target.Contracts.ExecutionManager.GetExceptionClauses(codeBlock);

        Assert.True(clauses.Count > 0, "Expected at least one exception clause");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetExceptionClauses_ContainsFilterClause(TestConfiguration config)
    {
        InitializeDumpTest(config);

        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        List<ExceptionClauseInfo> clauses = Target.Contracts.ExecutionManager.GetExceptionClauses(codeBlock);

        Assert.Contains(clauses, c => c.ClauseType == ExceptionClauseInfo.ExceptionClauseFlags.Filter);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetExceptionClauses_ContainsTypedClause(TestConfiguration config)
    {
        InitializeDumpTest(config);

        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        List<ExceptionClauseInfo> clauses = Target.Contracts.ExecutionManager.GetExceptionClauses(codeBlock);

        Assert.Contains(clauses, c => c.ClauseType == ExceptionClauseInfo.ExceptionClauseFlags.Typed);
    }

    // The JIT may optimize a finally into a fault. See Compiler::fgCloneFinally().
    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetExceptionClauses_ContainsFinallyOrFaultClause(TestConfiguration config)
    {
        InitializeDumpTest(config);

        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        List<ExceptionClauseInfo> clauses = Target.Contracts.ExecutionManager.GetExceptionClauses(codeBlock);

        Assert.Contains(clauses, c => c.ClauseType == ExceptionClauseInfo.ExceptionClauseFlags.Finally ||
                                      c.ClauseType == ExceptionClauseInfo.ExceptionClauseFlags.Fault);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetExceptionClauses_ContainsCatchAllClause(TestConfiguration config)
    {
        InitializeDumpTest(config);

        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        List<ExceptionClauseInfo> clauses = Target.Contracts.ExecutionManager.GetExceptionClauses(codeBlock);

        Assert.Contains(clauses, c => c.IsCatchAllHandler == true);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetExceptionClauses_AllClausesHaveValidOffsets(TestConfiguration config)
    {
        InitializeDumpTest(config);

        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        List<ExceptionClauseInfo> clauses = Target.Contracts.ExecutionManager.GetExceptionClauses(codeBlock);

        foreach (ExceptionClauseInfo clause in clauses)
        {
            Assert.True(clause.TryStartPC < clause.TryEndPC,
                $"TryStartPC (0x{clause.TryStartPC:x}) should be less than TryEndPC (0x{clause.TryEndPC:x})");
            Assert.True(clause.HandlerStartPC < clause.HandlerEndPC,
                $"HandlerStartPC (0x{clause.HandlerStartPC:x}) should be less than HandlerEndPC (0x{clause.HandlerEndPC:x})");
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetExceptionClauses_FilterClauseHasFilterOffset(TestConfiguration config)
    {
        InitializeDumpTest(config);

        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        List<ExceptionClauseInfo> clauses = Target.Contracts.ExecutionManager.GetExceptionClauses(codeBlock);

        ExceptionClauseInfo filterClause = clauses.First(c => c.ClauseType == ExceptionClauseInfo.ExceptionClauseFlags.Filter);
        Assert.NotNull(filterClause.FilterOffset);
        Assert.Null(filterClause.ClassToken);
        Assert.Null(filterClause.TypeHandle);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "EH clause enumeration was added after net10.0")]
    public void GetExceptionClauses_TypedClauseHasModuleAddr(TestConfiguration config)
    {
        InitializeDumpTest(config);

        CodeBlockHandle codeBlock = FindCrashMethodCodeBlock();
        List<ExceptionClauseInfo> clauses = Target.Contracts.ExecutionManager.GetExceptionClauses(codeBlock);

        ExceptionClauseInfo typedClause = clauses.First(c => c.ClauseType == ExceptionClauseInfo.ExceptionClauseFlags.Typed);
        Assert.NotNull(typedClause.ModuleAddr);
        Assert.NotEqual(TargetPointer.Null, typedClause.ModuleAddr.Value);
        Assert.NotNull(typedClause.ClassToken);
    }
}
