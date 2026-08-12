// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class StackWalkTests
{
    [Fact]
    public void X86Unwind_EbpProlog_RestoresCallerContext()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        const uint MethodStart = 0x1000;
        const uint GcInfoAddress = 0x1800;
        const uint CurrentEbp = 0x2100;
        const uint CallerEbp = 0x3100;
        const uint ReturnAddress = 0x1234_5678;
        const uint SavedEdi = 0x1111_1111;
        const uint SavedEsi = 0x2222_2222;
        const uint CurrentEbx = 0x3333_3333;

        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = MethodStart,
            Data = [0x55, 0x8b, 0xec, 0x57, 0x56, 0x53],
            Name = "Method code",
        });

        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = GcInfoAddress,
            Data = [0x20, 0x4f],
            Name = "GC info",
        });

        byte[] stack = new byte[0x20];
        helpers.Write(stack.AsSpan(0x04, sizeof(uint)), CurrentEbx);
        helpers.Write(stack.AsSpan(0x08, sizeof(uint)), SavedEsi);
        helpers.Write(stack.AsSpan(0x0c, sizeof(uint)), SavedEdi);
        helpers.Write(stack.AsSpan(0x10, sizeof(uint)), CallerEbp);
        helpers.Write(stack.AsSpan(0x14, sizeof(uint)), ReturnAddress);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = CurrentEbp - 0x10,
            Data = stack,
            Name = "Stack",
        });

        CodeBlockHandle codeBlock = new(new TargetPointer(MethodStart));
        TargetPointer gcInfo = new(GcInfoAddress);
        uint gcInfoVersion = 4;
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(new TargetCodePointer(MethodStart + 5))).Returns(codeBlock);
        executionManager.Setup(e => e.GetGCInfo(codeBlock, out gcInfo, out gcInfoVersion));
        executionManager.Setup(e => e.GetRelativeOffset(codeBlock)).Returns(new TargetNUInt(5));
        executionManager.Setup(e => e.GetStartAddress(codeBlock)).Returns(new TargetPointer(MethodStart));
        executionManager.Setup(e => e.GetFuncletStartAddress(codeBlock)).Returns(new TargetPointer(MethodStart));
        executionManager.Setup(e => e.IsFunclet(codeBlock)).Returns(false);

        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(r => r.GetTargetOperatingSystem()).Returns(RuntimeInfoOperatingSystem.Windows);

        TestPlaceholderTarget target = targetBuilder
            .AddMockContract(executionManager.Object)
            .AddMockContract(runtimeInfo.Object)
            .Build();

        X86Context context = new()
        {
            ContextFlags = (uint)X86Context.ContextFlagsValues.CONTEXT_FULL,
            Eip = MethodStart + 5,
            Esp = CurrentEbp - 8,
            Ebp = CurrentEbp,
            Edi = uint.MaxValue,
            Esi = uint.MaxValue,
            Ebx = CurrentEbx,
        };

        Assert.True(new Contracts.StackWalkHelpers.X86.X86Unwinder(target).Unwind(ref context));
        Assert.Equal(SavedEdi, context.Edi);
        Assert.Equal(SavedEsi, context.Esi);
        Assert.Equal(CurrentEbx, context.Ebx);
        Assert.Equal(CallerEbp, context.Ebp);
        Assert.Equal(CurrentEbp + (2 * sizeof(uint)), context.Esp);
        Assert.Equal(ReturnAddress, context.Eip);
        Assert.NotEqual(0u, context.ContextFlags & (uint)X86Context.ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL);
    }

    [Fact]
    public void ARMUnwind_CompactEpilog_RestoresCallerContext()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        const uint ImageBase = 0x1000_0000;
        const uint FunctionStart = 0x1000;
        const uint RuntimeFunctionAddress = 0x1800;
        const uint CurrentSp = 0x2000;
        const uint SavedR4 = 0x4444_4444;
        const uint ReturnAddress = 0x1234_5679;

        Layout<MockRuntimeFunction> runtimeFunctionLayout = MockRuntimeFunction.CreateLayout(arch, includeEndAddress: false);
        byte[] runtimeFunctionData = new byte[runtimeFunctionLayout.Size];
        MockRuntimeFunction runtimeFunction = runtimeFunctionLayout.Create(runtimeFunctionData, RuntimeFunctionAddress);
        runtimeFunction.BeginAddress = FunctionStart;
        runtimeFunction.UnwindData =
            1u |           // Flag = packed unwind data
            (16u << 2) |   // FunctionLength = 16 halfwords
            (16u << 16) |  // L = 1, Reg = 0: save r4 and lr
            (1u << 22);    // StackAdjust = 1 word
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = RuntimeFunctionAddress,
            Data = runtimeFunctionData,
            Name = "Runtime function",
        });

        byte[] stack = new byte[2 * sizeof(uint)];
        helpers.Write(stack.AsSpan(0, sizeof(uint)), SavedR4);
        helpers.Write(stack.AsSpan(sizeof(uint), sizeof(uint)), ReturnAddress);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = CurrentSp,
            Data = stack,
            Name = "Stack",
        });

        TargetCodePointer controlPc = new(ImageBase + FunctionStart + (15 * 2));
        CodeBlockHandle codeBlock = new(new TargetPointer(controlPc.Value));
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(controlPc)).Returns(codeBlock);
        executionManager.Setup(e => e.GetUnwindInfoBaseAddress(codeBlock)).Returns(new TargetPointer(ImageBase));
        executionManager.Setup(e => e.GetUnwindInfo(codeBlock)).Returns(new TargetPointer(RuntimeFunctionAddress));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(runtimeFunctionLayout),
            })
            .AddMockContract(executionManager.Object)
            .Build();

        ARMContext context = new()
        {
            ContextFlags = (uint)ARMContext.ContextFlagsValues.CONTEXT_FULL,
            R4 = uint.MaxValue,
            Sp = CurrentSp,
            Lr = uint.MaxValue,
            Pc = (uint)controlPc.Value,
        };

        Assert.True(new Contracts.StackWalkHelpers.ARM.ARMUnwinder(target).Unwind(ref context));
        Assert.Equal(SavedR4, context.R4);
        Assert.Equal(CurrentSp + (2 * sizeof(uint)), context.Sp);
        Assert.Equal(ReturnAddress, context.Lr);
        Assert.Equal(ReturnAddress, context.Pc);
        Assert.NotEqual(0u, context.ContextFlags & (uint)ARMContext.ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL);
    }

    [Fact]
    public void ARM64Unwind_SaveAnyPair_RestoresCallerContext()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        const ulong ImageBase = 0x1000_0000;
        const uint FunctionStart = 0x1000;
        const uint XdataRva = 0x2000;
        const ulong RuntimeFunctionAddress = 0x1800;
        const ulong CurrentSp = 0x3000;
        const ulong SavedX19 = 0x1919_1919_1919_1919;
        const ulong SavedX20 = 0x2020_2020_2020_2020;
        const ulong ReturnAddress = 0x1234_5678_9abc_def0;

        Layout<MockRuntimeFunction> runtimeFunctionLayout = MockRuntimeFunction.CreateLayout(arch, includeEndAddress: false);
        byte[] runtimeFunctionData = new byte[runtimeFunctionLayout.Size];
        MockRuntimeFunction runtimeFunction = runtimeFunctionLayout.Create(runtimeFunctionData, RuntimeFunctionAddress);
        runtimeFunction.BeginAddress = FunctionStart;
        runtimeFunction.UnwindData = XdataRva;
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = RuntimeFunctionAddress,
            Data = runtimeFunctionData,
            Name = "Runtime function",
        });

        byte[] unwindData = new byte[2 * sizeof(uint)];
        helpers.Write(unwindData.AsSpan(0, sizeof(uint)), 16u | (1u << 27));
        unwindData[4] = 0xe7; // save_any
        unwindData[5] = 0x53; // p=1, x=0, r=19
        unwindData[6] = 0x00; // f=0, o=0
        unwindData[7] = 0xe4; // end
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = ImageBase + XdataRva,
            Data = unwindData,
            Name = "Unwind data",
        });

        byte[] stack = new byte[2 * sizeof(ulong)];
        helpers.Write(stack.AsSpan(0, sizeof(ulong)), SavedX19);
        helpers.Write(stack.AsSpan(sizeof(ulong), sizeof(ulong)), SavedX20);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = CurrentSp,
            Data = stack,
            Name = "Stack",
        });

        TargetCodePointer controlPc = new(ImageBase + FunctionStart + (8 * sizeof(uint)));
        CodeBlockHandle codeBlock = new(new TargetPointer(controlPc.Value));
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(controlPc)).Returns(codeBlock);
        executionManager.Setup(e => e.GetUnwindInfoBaseAddress(codeBlock)).Returns(new TargetPointer(ImageBase));
        executionManager.Setup(e => e.GetUnwindInfo(codeBlock)).Returns(new TargetPointer(RuntimeFunctionAddress));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(runtimeFunctionLayout),
            })
            .AddMockContract(executionManager.Object)
            .Build();

        ARM64Context context = new()
        {
            ContextFlags = (uint)ARM64Context.ContextFlagsValues.CONTEXT_FULL,
            X19 = ulong.MaxValue,
            X20 = ulong.MaxValue,
            Sp = CurrentSp,
            Lr = ReturnAddress,
            Pc = controlPc.Value,
        };

        Assert.True(new Contracts.StackWalkHelpers.ARM64.ARM64Unwinder(target).Unwind(ref context));
        Assert.Equal(SavedX19, context.X19);
        Assert.Equal(SavedX20, context.X20);
        Assert.Equal(CurrentSp, context.Sp);
        Assert.Equal(ReturnAddress, context.Pc);
        Assert.NotEqual(0u, context.ContextFlags & (uint)ARM64Context.ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL);
    }

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

    [Fact]
    public void GetStackSizeSkipped_ReturnsBytesSkippedByFiltering()
    {
        IXCLRDataStackWalk stackWalk = CreateClrDataStackWalk(
            new TestStackDataFrameHandle(StackWalkState.Frameless, 0x1000),
            new TestStackDataFrameHandle(StackWalkState.InitialNativeContext, 0x1100),
            new TestStackDataFrameHandle(StackWalkState.NativeMarker, 0x1200),
            new TestStackDataFrameHandle(StackWalkState.Frameless, 0x1500));

        ulong stackSizeSkipped = ulong.MaxValue;
        Assert.Equal(HResults.S_OK, stackWalk.GetStackSizeSkipped(&stackSizeSkipped));
        Assert.Equal(0ul, stackSizeSkipped);

        Assert.Equal(HResults.S_OK, stackWalk.Next());
        Assert.Equal(HResults.S_OK, stackWalk.GetStackSizeSkipped(&stackSizeSkipped));
        Assert.Equal(0x400ul, stackSizeSkipped);
    }

    private static IXCLRDataStackWalk CreateClrDataStackWalk(params TestStackDataFrameHandle[] frames)
    {
        TargetPointer threadAddress = new(0x1000);
        ThreadData threadData = new()
        {
            ThreadAddress = threadAddress,
        };

        var thread = new Mock<IThread>();
        thread.Setup(t => t.GetThreadData(threadAddress)).Returns(threadData);

        var stackWalk = new Mock<IStackWalk>();
        stackWalk.Setup(s => s.CreateStackWalk(threadData)).Returns(frames);
        stackWalk
            .Setup(s => s.GetStackPointer(It.IsAny<IStackDataFrameHandle>()))
            .Returns((IStackDataFrameHandle frame) => ((TestStackDataFrameHandle)frame).StackPointer);

        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .AddMockContract(thread)
            .AddMockContract(stackWalk)
            .Build();

        return new ClrDataStackWalk(threadAddress, CLRDataStackWalkFlag.CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE, target, legacyImpl: null);
    }

    private sealed record TestStackDataFrameHandle(StackWalkState State, ulong StackPointerValue) : IStackDataFrameHandle
    {
        public TargetPointer StackPointer => new(StackPointerValue);
        public bool IsInterrupted => false;
        public bool HasFaulted => false;
        public bool IsExceptionFrame => false;
        public bool IsActiveFrame => false;
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
            targetBuilder.AddGlobalStrings((Constants.Globals.Architecture, rtArch.ToString().ToLowerInvariant()));

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
        // Match enum class InlinedCallFrameMarker in src/coreclr/vm/exceptionhandling.h.
        const ulong ehMarker = 1;
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
        const ulong ehMarker = 1;

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
    [InlineData(RuntimeInfoArchitecture.X86, 0ul)]
    [InlineData(RuntimeInfoArchitecture.Arm, 0x1000ul)]
    public void GetMethodDescPtr_InlinedCallFrame_UsesX86StackSizeSentinel(RuntimeInfoArchitecture runtimeArchitecture, ulong expectedMethodDesc)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };

        ulong inlinedCallFrameAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                inlinedCallFrameAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 0xCAFE_BABE, datum: 0x1000).Address;
            },
            runtimeArchitecture: runtimeArchitecture);

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.Equal(expectedMethodDesc, contract.GetMethodDescPtr(new TargetPointer(inlinedCallFrameAddr)).Value);
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
