// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class StackWalkTests
{
    [Fact]
    public void GenericContextStorage_PreservesRegisterRepresentation()
    {
        Assert.Equal(string.Empty, default(GenericContextStorage).RegisterName);

        GenericContextStorage named = new(GenericContextStorageKind.RegisterRelative, "ebp", -4);
        Assert.Equal("ebp", named.RegisterName);
        Assert.Equal(0u, named.RegisterNumber);

        GenericContextStorage numbered = new(GenericContextStorageKind.Register, 5u, 0);
        Assert.Equal(string.Empty, numbered.RegisterName);
        Assert.Equal(5u, numbered.RegisterNumber);
    }

    [Theory]
    [InlineData(0u, false)]
    [InlineData(0x08000000u, true)]
    [InlineData(0x08000001u, true)]
    public void HasFaultedContext_UsesExceptionActiveFlag(uint contextFlags, bool expected)
    {
        var context = new Mock<IPlatformAgnosticContext>();
        context.SetupGet(c => c.RawContextFlags).Returns(contextFlags);

        Assert.Equal(expected, StackWalk_1.HasFaultedContext(context.Object));
    }

    private static TestPlaceholderTarget CreateTarget(
        MockTarget.Architecture arch,
        Action<MockThreadBuilder> configure,
        Action<MockFrameBuilder>? configureFrames = null,
        RuntimeInfoArchitecture? runtimeArchitecture = null)
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

        // Some paths (e.g. the interpreter virtual unwind's first-argument-register lookup)
        // consult IRuntimeInfo for the target architecture. Register a mock when the test needs it.
        if (runtimeArchitecture is RuntimeInfoArchitecture rtArch)
        {
            Mock<IRuntimeInfo> runtimeInfo = new();
            runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(rtArch);
            targetBuilder.AddMockContract(runtimeInfo.Object);
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
            [DataType.InterpMethodContextFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.InterpMethodContextFrameLayout),
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

    // WASM is a 32-bit little-endian target with no native register context; the initial
    // stack walk context is seeded from the Frame chain. This verifies that the degenerate
    // WasmContext is routed through WasmFrameHandler and that an active InlinedCallFrame at a
    // P/Invoke transition seeds the synthetic IP/SP/FP slots from CallSiteSP / CallerReturnAddress
    // / CalleeSavedFP -- the common context-seeding path on WASM.
    [Fact]
    public void UpdateContextFromFrame_WasmInlinedCallFrame_SeedsContextFromCallSiteSP()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong callSiteSP = 0x0004_1000;
        const ulong callerReturnAddress = 0x0004_2000;
        const ulong calleeSavedFP = 0x0004_3000;

        ulong icfAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                icfAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress, datum: 0, callSiteSP, calleeSavedFP).Address;
            });

        ContextHolder<WasmContext> context = new();
        FrameHelpers frameHelpers = new(target);
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(icfAddr);
        frameHelpers.UpdateContextFromFrame(frame, context);

        Assert.Equal(callSiteSP, context.StackPointer.Value);
        Assert.Equal(callerReturnAddress, context.InstructionPointer.Value);
        Assert.Equal(calleeSavedFP, context.FramePointer.Value);
    }

    // The WasmContext mirrors the native wasm T_CONTEXT (src/coreclr/pal/inc/pal.h): five
    // 32-bit slots (ContextFlags, InterpreterWalkFramePointer, InterpreterSP/FP/IP). Verify the
    // serialized size and that the synthetic first-argument register (InterpreterWalkFramePointer)
    // and context flags round-trip.
    [Fact]
    public void WasmContext_MirrorsNativeLayoutAndRoundTripsRegisters()
    {
        WasmContext context = default;

        Assert.Equal(5u * sizeof(uint), context.Size);

        Assert.True(context.TrySetRegister(WasmContext.InterpreterWalkFramePointerRegister, new TargetNUInt(0x0004_9000)));
        Assert.True(context.TryReadRegister(WasmContext.InterpreterWalkFramePointerRegister, out TargetNUInt walkFp));
        Assert.Equal(0x0004_9000ul, walkFp.Value);

        context.StackPointer = new TargetPointer(0x0004_1000);
        context.InstructionPointer = new TargetCodePointer(0x0004_2000);
        context.FramePointer = new TargetPointer(0x0004_3000);
        context.RawContextFlags = 0x8000000; // CONTEXT_EXCEPTION_ACTIVE

        Assert.Equal(0x0004_1000ul, context.StackPointer.Value);
        Assert.Equal(0x0004_2000ul, context.InstructionPointer.Value);
        Assert.Equal(0x0004_3000ul, context.FramePointer.Value);
        Assert.Equal(0x8000000u, context.RawContextFlags);
    }

    // When an active InlinedCallFrame is directly followed by an InterpreterFrame, WasmFrameHandler
    // stashes the InterpreterFrame address into the synthetic first-argument register
    // (InterpreterWalkFramePointer) so the subsequent interpreter virtual unwind can recover the
    // owning frame -- mirroring native SetFirstArgReg on the P/Invoke-into-interpreter transition.
    [Fact]
    public void UpdateContextFromFrame_WasmInlinedCallFrameOverInterpreterFrame_StashesInterpreterFrame()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        ulong icfAddr = 0;
        ulong interpAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                interpAddr = frameBuilder.AddFrame(MockFrameBuilder.InterpreterFrameIdentifierValue, "InterpreterFrame").Address;
                MockInlinedCallFrame icf = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 0x0004_2000, datum: 0, callSiteSP: 0x0004_1000);
                icf.Next = interpAddr;
                icfAddr = icf.Address;
            });

        ContextHolder<WasmContext> context = new();
        FrameHelpers frameHelpers = new(target);
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(icfAddr);
        frameHelpers.UpdateContextFromFrame(frame, context);

        Assert.True(context.TryReadRegister(WasmContext.InterpreterWalkFramePointerRegister, out TargetNUInt stashed));
        Assert.Equal(interpAddr, stashed.Value);
    }

    // Interpreter virtual unwind on WASM: with the WasmContext SP pointing at an
    // InterpMethodContextFrame, each InterpreterVirtualUnwind step follows pParent to the next
    // interpreted method, setting IP/SP/FP from the parent frame (matching native
    // VirtualUnwindInterpreterCallFrame). Walks a three-node chain to the point of exhaustion.
    [Fact]
    public void InterpreterVirtualUnwind_WasmChain_StepsThroughInterpMethodContextFrames()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong ip1 = 0x0005_1000, fp1 = 0x0006_1000;
        const ulong ip2 = 0x0005_2000, fp2 = 0x0006_2000;

        ulong frame0 = 0, frame1 = 0, frame2 = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                // Build leaf-to-root so parent addresses are known when linking children.
                frame2 = frameBuilder.AddInterpMethodContextFrame(parentPtr: 0, ip: ip2, stack: fp2).Address;
                frame1 = frameBuilder.AddInterpMethodContextFrame(parentPtr: frame2, ip: ip1, stack: fp1).Address;
                frame0 = frameBuilder.AddInterpMethodContextFrame(parentPtr: frame1, ip: 0, stack: 0).Address;
            });

        ContextHolder<WasmContext> context = new();
        context.StackPointer = new TargetPointer(frame0);
        FrameHelpers frameHelpers = new(target);

        // Step 1: frame0 -> parent frame1; context takes frame1's IP/SP/FP.
        frameHelpers.InterpreterVirtualUnwind(context);
        Assert.Equal(ip1, context.InstructionPointer.Value);
        Assert.Equal(frame1, context.StackPointer.Value);
        Assert.Equal(fp1, context.FramePointer.Value);

        // Step 2: frame1 -> parent frame2.
        frameHelpers.InterpreterVirtualUnwind(context);
        Assert.Equal(ip2, context.InstructionPointer.Value);
        Assert.Equal(frame2, context.StackPointer.Value);
        Assert.Equal(fp2, context.FramePointer.Value);
    }

    // When the InterpMethodContextFrame chain is exhausted (pParent == null) and no owning
    // InterpreterFrame is stashed in the synthetic first-argument register, the WASM interpreter
    // virtual unwind terminates gracefully without applying a transition. This also guards the
    // WASM first-argument-register wiring: before it was mapped to InterpreterWalkFramePointer,
    // this path threw NotSupportedException from GetFirstArgRegisterName.
    [Fact]
    public void InterpreterVirtualUnwind_WasmExhaustedChainNoOwningFrame_TerminatesGracefully()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        ulong frame0 = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                frame0 = frameBuilder.AddInterpMethodContextFrame(parentPtr: 0, ip: 0x0005_1000, stack: 0x0006_1000).Address;
            },
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm);

        ContextHolder<WasmContext> context = new();
        context.StackPointer = new TargetPointer(frame0);
        FrameHelpers frameHelpers = new(target);

        frameHelpers.InterpreterVirtualUnwind(context);

        // Chain exhausted with a null owning frame: context SP is left unchanged, no throw.
        Assert.Equal(frame0, context.StackPointer.Value);
    }
}
