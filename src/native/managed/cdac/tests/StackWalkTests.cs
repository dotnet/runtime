// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class StackWalkTests
{
    private static TestPlaceholderTarget CreateTarget(
        MockTarget.Architecture arch,
        Action<MockThreadBuilder> configure,
        Action<MockFrameBuilder>? configureFrames = null)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockThreadBuilder threadBuilder = new(targetBuilder.MemoryBuilder);
        configure(threadBuilder);

        MockFrameBuilder? frameBuilder = null;
        if (configureFrames is not null)
        {
            frameBuilder = new MockFrameBuilder(targetBuilder.MemoryBuilder);
            configureFrames(frameBuilder);
        }

        targetBuilder
            .AddTypes(CreateThreadTypes(threadBuilder))
            .AddGlobals(
                (nameof(Constants.Globals.ThreadStore), threadBuilder.ThreadStoreGlobalAddress),
                (nameof(Constants.Globals.FinalizerThread), threadBuilder.FinalizerThreadGlobalAddress),
                (nameof(Constants.Globals.GCThread), threadBuilder.GCThreadGlobalAddress));

        if (frameBuilder is not null)
        {
            targetBuilder
                .AddTypes(CreateFrameTypes(frameBuilder))
                .AddGlobals(
                    ("InlinedCallFrameIdentifier", MockFrameBuilder.InlinedCallFrameIdentifierValue),
                    ("FramedMethodFrameIdentifier", MockFrameBuilder.FramedMethodFrameIdentifierValue),
                    ("FuncEvalFrameIdentifier", MockFrameBuilder.FuncEvalFrameIdentifierValue),
                    ("DebuggerExitFrameIdentifier", MockFrameBuilder.DebuggerExitFrameIdentifierValue),
                    ("PrestubMethodFrameIdentifier", MockFrameBuilder.PrestubMethodFrameIdentifierValue),
                    ("DebuggerClassInitMarkFrameIdentifier", MockFrameBuilder.DebuggerClassInitMarkFrameIdentifierValue),
                    ("SoftwareExceptionFrameIdentifier", MockFrameBuilder.SoftwareExceptionFrameIdentifierValue),
                    ("DebuggerU2MCatchHandlerFrameIdentifier", MockFrameBuilder.DebuggerU2MCatchHandlerFrameIdentifierValue),
                    ("InterpreterFrameIdentifier", MockFrameBuilder.InterpreterFrameIdentifierValue),
                    ("HijackFrameIdentifier", MockFrameBuilder.HijackFrameIdentifierValue));
        }

        return targetBuilder
            .AddContract<IThread>(version: "c1")
            .AddContract<IStackWalk>(version: "c1")
            // StackWalk_1's constructor reads these contracts via target.Contracts.{ExecutionManager,GCInfo}
            // when constructing its GcScanner. Our tests only exercise GetFrames /
            // IsExceptionHandlingHelperInlinedCallFrame / GetDebuggerEvalData, none of which
            // invoke ExecutionManager or GCInfo, so empty mocks satisfy construction.
            .AddMockContract(Mock.Of<IExecutionManager>())
            .AddMockContract(Mock.Of<IGCInfo>())
            .Build();
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateThreadTypes(MockThreadBuilder threadBuilder)
        => new()
        {
            [DataType.ExceptionInfo] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ExceptionInfoLayout),
            [DataType.Thread] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ThreadLayout),
            [DataType.ThreadStore] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ThreadStoreLayout),
            [DataType.GCAllocContext] = TargetTestHelpers.CreateTypeInfo(threadBuilder.GCAllocContextLayout),
            [DataType.EEAllocContext] = TargetTestHelpers.CreateTypeInfo(threadBuilder.EEAllocContextLayout),
            [DataType.RuntimeThreadLocals] = TargetTestHelpers.CreateTypeInfo(threadBuilder.RuntimeThreadLocalsLayout),
        };

    private static Dictionary<DataType, Target.TypeInfo> CreateFrameTypes(MockFrameBuilder frameBuilder)
        => new()
        {
            [DataType.Frame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.FrameLayout),
            [DataType.InlinedCallFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.InlinedCallFrameLayout),
            [DataType.FramedMethodFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.FramedMethodFrameLayout),
            [DataType.FuncEvalFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.FuncEvalFrameLayout),
            [DataType.DebuggerEval] = TargetTestHelpers.CreateTypeInfo(frameBuilder.DebuggerEvalLayout),
        };

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFrames_EmptyChain_ReturnsNothing(MockTarget.Architecture arch)
    {
        MockThread? thread = null;
        ulong terminator = arch.Is64Bit ? ulong.MaxValue : uint.MaxValue;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                thread = threadBuilder.AddThread(1, 1234);
                thread.Frame = terminator;
            },
            frameBuilder => { /* register layouts and identifiers, no frames */ });

        IStackWalk contract = target.Contracts.StackWalk;
        StackFrameData[] frames = contract.GetFrames(new TargetPointer(thread!.Address)).ToArray();
        Assert.Empty(frames);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFrames_ClassifiesInternalFrameTypes(MockTarget.Architecture arch)
    {
        // Builds a chain whose frames exercise every InternalFrameType branch reachable
        // without the StubDispatchFrame layout. PrestubMethodFrame is a subclass of
        // FramedMethodFrame so it is allocated with the FramedMethodFrame layout but
        // overridden with the PrestubMethodFrameIdentifier.
        MockThread? thread = null;

        ulong framedMethodAddr = 0;
        ulong prestubAddr = 0;
        ulong funcEvalAddr = 0;
        ulong debuggerExitAddr = 0;
        ulong classInitAddr = 0;
        ulong softwareExAddr = 0;
        ulong u2mAddr = 0;
        ulong interpAddr = 0;
        ulong hijackAddr = 0;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => thread = threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                framedMethodAddr = frameBuilder.AddFramedMethodFrame(0x12345000).Address;

                MockFramedMethodFrame prestubFmf = frameBuilder.AddFramedMethodFrame(0);
                prestubFmf.Identifier = MockFrameBuilder.PrestubMethodFrameIdentifierValue;
                prestubAddr = prestubFmf.Address;

                funcEvalAddr = frameBuilder.AddFrame(MockFrameBuilder.FuncEvalFrameIdentifierValue, "FuncEvalFrame").Address;
                debuggerExitAddr = frameBuilder.AddFrame(MockFrameBuilder.DebuggerExitFrameIdentifierValue, "DebuggerExitFrame").Address;
                classInitAddr = frameBuilder.AddFrame(MockFrameBuilder.DebuggerClassInitMarkFrameIdentifierValue, "DebuggerClassInitMarkFrame").Address;
                softwareExAddr = frameBuilder.AddFrame(MockFrameBuilder.SoftwareExceptionFrameIdentifierValue, "SoftwareExceptionFrame").Address;
                u2mAddr = frameBuilder.AddFrame(MockFrameBuilder.DebuggerU2MCatchHandlerFrameIdentifierValue, "DebuggerU2MCatchHandlerFrame").Address;
                interpAddr = frameBuilder.AddFrame(MockFrameBuilder.InterpreterFrameIdentifierValue, "InterpreterFrame").Address;
                hijackAddr = frameBuilder.AddFrame(MockFrameBuilder.HijackFrameIdentifierValue, "HijackFrame").Address;

                thread!.Frame = frameBuilder.LinkChain(
                    framedMethodAddr, prestubAddr, funcEvalAddr, debuggerExitAddr,
                    classInitAddr, softwareExAddr, u2mAddr, interpAddr, hijackAddr);
            });

        IStackWalk contract = target.Contracts.StackWalk;
        StackFrameData[] frames = contract.GetFrames(new TargetPointer(thread!.Address)).ToArray();
        Assert.Equal(9, frames.Length);

        Assert.Equal(framedMethodAddr, frames[0].FrameAddress.Value);
        Assert.Equal(InternalFrameType.M2U, frames[0].InternalFrameType);

        Assert.Equal(prestubAddr, frames[1].FrameAddress.Value);
        Assert.Equal(InternalFrameType.JitCompilation, frames[1].InternalFrameType);

        Assert.Equal(funcEvalAddr, frames[2].FrameAddress.Value);
        Assert.Equal(InternalFrameType.FuncEval, frames[2].InternalFrameType);

        Assert.Equal(debuggerExitAddr, frames[3].FrameAddress.Value);
        Assert.Equal(InternalFrameType.M2U, frames[3].InternalFrameType);

        Assert.Equal(classInitAddr, frames[4].FrameAddress.Value);
        Assert.Equal(InternalFrameType.ClassInit, frames[4].InternalFrameType);

        Assert.Equal(softwareExAddr, frames[5].FrameAddress.Value);
        Assert.Equal(InternalFrameType.Exception, frames[5].InternalFrameType);

        Assert.Equal(u2mAddr, frames[6].FrameAddress.Value);
        Assert.Equal(InternalFrameType.U2M, frames[6].InternalFrameType);

        // InterpreterFrame classifies as M2U at the StackWalk layer; the
        // debugger-internal-frames consumer filters it out separately.
        Assert.Equal(interpAddr, frames[7].FrameAddress.Value);
        Assert.Equal(InternalFrameType.M2U, frames[7].InternalFrameType);

        Assert.Equal(hijackAddr, frames[8].FrameAddress.Value);
        Assert.Equal(InternalFrameType.None, frames[8].InternalFrameType);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsExceptionHandlingHelperInlinedCallFrame_DetectsMarkedActiveIcf(MockTarget.Architecture arch)
    {
        // Match enum class InlinedCallFrameMarker in src/coreclr/vm/exceptionhandling.h:
        // ExceptionHandlingHelper == 2 on 64-bit, 1 on 32-bit. The Mask is the same value.
        ulong ehMarker = arch.Is64Bit ? 2u : 1u;
        ulong activeReturnAddr = 0xCAFE_BABE;

        ulong ehHelperAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                ehHelperAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress: activeReturnAddr, datum: ehMarker).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.True(contract.IsExceptionHandlingHelperInlinedCallFrame(new TargetPointer(ehHelperAddr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsExceptionHandlingHelperInlinedCallFrame_ReturnsFalseForPlainActiveIcf(MockTarget.Architecture arch)
    {
        ulong activeReturnAddr = 0xCAFE_BABE;

        ulong plainIcfAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                plainIcfAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress: activeReturnAddr, datum: 0).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.False(contract.IsExceptionHandlingHelperInlinedCallFrame(new TargetPointer(plainIcfAddr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsExceptionHandlingHelperInlinedCallFrame_ReturnsFalseForInactiveIcf(MockTarget.Architecture arch)
    {
        ulong ehMarker = arch.Is64Bit ? 2u : 1u;

        ulong inactiveAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                // The marker is set but CallerReturnAddress == 0, so the frame is not active.
                inactiveAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 0, datum: ehMarker).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.False(contract.IsExceptionHandlingHelperInlinedCallFrame(new TargetPointer(inactiveAddr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsExceptionHandlingHelperInlinedCallFrame_ReturnsFalseForNonIcf(MockTarget.Architecture arch)
    {
        ulong framedAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                framedAddr = frameBuilder.AddFramedMethodFrame(0x9000).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.False(contract.IsExceptionHandlingHelperInlinedCallFrame(new TargetPointer(framedAddr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDebuggerEvalData_ReturnsTokenAndAssemblyFromDebuggerEval(MockTarget.Architecture arch)
    {
        const uint expectedToken = 0x0600_0042;
        // Use a pointer-sized-safe value: the mock allocator writes _helpers.PointerSize
        // bytes for AssemblyPtr, so values must fit in 32 bits to remain consistent
        // across 32- and 64-bit architectures.
        const ulong expectedAssembly = 0x5678_9000;

        ulong funcEvalFrameAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                MockDebuggerEval eval = frameBuilder.AddDebuggerEval(expectedToken, expectedAssembly);
                funcEvalFrameAddr = frameBuilder.AddFuncEvalFrame(eval.Address).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        DebuggerEvalData data = contract.GetDebuggerEvalData(new TargetPointer(funcEvalFrameAddr));

        Assert.Equal(expectedToken, data.MethodToken);
        Assert.Equal(expectedAssembly, data.AssemblyPtr.Value);
    }
}
