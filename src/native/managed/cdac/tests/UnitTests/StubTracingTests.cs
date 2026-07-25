// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class StubTracingTests
{
    public enum ManagedRuntimeStubKind
    {
        ILStub,
        PInvoke,
        AsyncThunk,
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ManagedCode(MockTarget.Architecture arch)
    {
        TargetCodePointer address = new(0x7000);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(address)).Returns(CodeKind.Jitted);
        executionManager.Setup(e => e.GetCodeBlockHandle(address)).Returns((CodeBlockHandle?)null);
        IStubTracing stubTracing = CreateTarget(arch, executionManager.Object)
            .Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(address, default, TargetPointer.Null);

        Assert.Equal(StubTraceKind.Managed, step.Kind);
        Assert.Equal(address, step.Address);
        Assert.Equal(default, step.Continuation);
    }

    [Theory]
    [MemberData(nameof(ManagedRuntimeStubData))]
    public void ManagedRuntimeStubFails(
        MockTarget.Architecture arch,
        ManagedRuntimeStubKind stubKind)
    {
        TargetCodePointer address = new(0x7000);
        CodeBlockHandle codeBlock = new(new TargetPointer(0x8000));
        TargetPointer methodDesc = new(0x9000);
        MethodDescHandle method = new(methodDesc);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(address)).Returns(CodeKind.Jitted);
        executionManager.Setup(e => e.GetCodeBlockHandle(address)).Returns(codeBlock);
        executionManager
            .Setup(e => e.GetMethodDesc(It.Is<CodeBlockHandle>(h => h.Address == codeBlock.Address)))
            .Returns(methodDesc);
        Mock<IRuntimeTypeSystem> runtimeTypeSystem = new(MockBehavior.Strict);
        runtimeTypeSystem.Setup(r => r.GetMethodDescHandle(methodDesc)).Returns(method);
        runtimeTypeSystem
            .Setup(r => r.IsILStub(method))
            .Returns(stubKind == ManagedRuntimeStubKind.ILStub);
        runtimeTypeSystem
            .Setup(r => r.IsPInvoke(method))
            .Returns(stubKind == ManagedRuntimeStubKind.PInvoke);
        runtimeTypeSystem
            .Setup(r => r.GetAsyncMethodFlags(method))
            .Returns(stubKind == ManagedRuntimeStubKind.AsyncThunk
                ? AsyncMethodFlags.Thunk
                : AsyncMethodFlags.None);
        IStubTracing stubTracing = CreateTarget(
            arch,
            executionManager.Object,
            runtimeTypeSystem: runtimeTypeSystem.Object).Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(
            address,
            default,
            TargetPointer.Null);

        Assert.Equal(StubTraceKind.Failed, step.Kind);
    }

    public static IEnumerable<object[]> ManagedRuntimeStubData()
    {
        foreach (object[] architecture in new MockTarget.StdArch())
        {
            foreach (ManagedRuntimeStubKind stubKind in Enum.GetValues<ManagedRuntimeStubKind>())
                yield return [architecture[0], stubKind];
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnjittedPrecode(MockTarget.Architecture arch)
    {
        TargetCodePointer address = new(0x7000);
        TargetPointer methodDesc = new(0x8000);
        TargetCodePointer notification = new(0x9000);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(address)).Returns(CodeKind.StubPrecode);
        TestPrecodeStubs precodes = new()
        {
            PrecodeType = PrecodeType.Stub,
            EntryPoint = address.AsTargetPointer,
            MethodDesc = methodDesc,
        };
        Mock<IRuntimeTypeSystem> runtimeTypeSystem = new(MockBehavior.Strict);
        runtimeTypeSystem
            .Setup(r => r.GetMethodDescHandle(methodDesc))
            .Returns(new MethodDescHandle(methodDesc));
        runtimeTypeSystem
            .Setup(r => r.GetNativeCode(It.Is<MethodDescHandle>(m => m.Address == methodDesc)))
            .Returns(TargetCodePointer.Null);
        runtimeTypeSystem
            .Setup(r => r.IsIL(It.Is<MethodDescHandle>(m => m.Address == methodDesc)))
            .Returns(true);
        IStubTracing stubTracing = CreateTarget(
            arch,
            executionManager.Object,
            precodes,
            runtimeTypeSystem.Object,
            notification).Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(address, default, TargetPointer.Null);

        Assert.Equal(StubTraceKind.UnjittedMethod, step.Kind);
        Assert.Equal(notification, step.Address);
        Assert.Equal(
            new StubContinuation(
                StubContinuationKind.MethodJitted,
                methodDesc,
                TargetCodePointer.Null),
            step.Continuation);
    }

    [Theory]
    [InlineData((int)PrecodeType.PInvokeImport)]
    [InlineData((int)PrecodeType.UMEntry)]
    [InlineData((int)PrecodeType.Interpreter)]
    [InlineData((int)PrecodeType.DynamicHelper)]
    public void UnsupportedPrecodeFails(int precodeType)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TargetCodePointer address = new(0x7000);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(address)).Returns(CodeKind.StubPrecode);
        TestPrecodeStubs precodes = new()
        {
            PrecodeType = (PrecodeType)precodeType,
            EntryPoint = address.AsTargetPointer,
        };
        IStubTracing stubTracing = CreateTarget(
            arch,
            executionManager.Object,
            precodes).Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(
            address,
            default,
            TargetPointer.Null);

        Assert.Equal(StubTraceKind.Failed, step.Kind);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void JittedMethodContinuationUnwrapsInterpreterPrecode(MockTarget.Architecture arch)
    {
        TargetCodePointer notification = new(0x7000);
        TargetPointer methodDesc = new(0x8000);
        TargetCodePointer interpreterPrecode = new(0x9000);
        TargetCodePointer interpreterCode = new(0xa000);
        StubContinuation continuation = new(
            StubContinuationKind.MethodJitted,
            methodDesc,
            TargetCodePointer.Null);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        TestPrecodeStubs precodes = new() { InterpreterCode = interpreterCode };
        Mock<IRuntimeTypeSystem> runtimeTypeSystem = new(MockBehavior.Strict);
        runtimeTypeSystem
            .Setup(r => r.GetMethodDescHandle(methodDesc))
            .Returns(new MethodDescHandle(methodDesc));
        runtimeTypeSystem
            .Setup(r => r.GetNativeCode(It.Is<MethodDescHandle>(m => m.Address == methodDesc)))
            .Returns(interpreterPrecode);
        IStubTracing stubTracing = CreateTarget(
            arch,
            executionManager.Object,
            precodes,
            runtimeTypeSystem.Object,
            notification).Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(
            notification,
            continuation,
            TargetPointer.Null);

        Assert.Equal(StubTraceKind.Managed, step.Kind);
        Assert.Equal(interpreterCode, step.Address);
        Assert.Equal(default, step.Continuation);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void PrecodeUnwrapsInterpreterPrecode(MockTarget.Architecture arch)
    {
        TargetCodePointer address = new(0x7000);
        TargetPointer methodDesc = new(0x8000);
        TargetCodePointer interpreterPrecode = new(0x9000);
        TargetCodePointer interpreterCode = new(0xa000);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(address)).Returns(CodeKind.StubPrecode);
        executionManager.Setup(e => e.GetCodeKind(interpreterCode)).Returns(CodeKind.Interpreter);
        executionManager.Setup(e => e.GetCodeBlockHandle(interpreterCode)).Returns((CodeBlockHandle?)null);
        TestPrecodeStubs precodes = new()
        {
            PrecodeType = PrecodeType.Stub,
            EntryPoint = address.AsTargetPointer,
            MethodDesc = methodDesc,
            InterpreterCode = interpreterCode,
        };
        Mock<IRuntimeTypeSystem> runtimeTypeSystem = new(MockBehavior.Strict);
        runtimeTypeSystem
            .Setup(r => r.GetMethodDescHandle(methodDesc))
            .Returns(new MethodDescHandle(methodDesc));
        runtimeTypeSystem
            .Setup(r => r.GetNativeCode(It.Is<MethodDescHandle>(m => m.Address == methodDesc)))
            .Returns(interpreterPrecode);
        IStubTracing stubTracing = CreateTarget(
            arch,
            executionManager.Object,
            precodes,
            runtimeTypeSystem.Object).Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(address, default, TargetPointer.Null);

        Assert.Equal(StubTraceKind.Managed, step.Kind);
        Assert.Equal(interpreterCode, step.Address);
        Assert.Equal(default, step.Continuation);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void FrameContinuation(MockTarget.Architecture arch)
    {
        TargetCodePointer patchLabel = new(0x7000);
        TargetPointer threadAddress = new(0x8000);
        TargetPointer frameAddress = new(0x9000);
        TargetPointer methodDesc = new(0xa000);
        TargetCodePointer entryPoint = new(0xb000);
        StubContinuation continuation = new(
            StubContinuationKind.FramePush,
            TargetPointer.Null,
            patchLabel);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(entryPoint)).Returns(CodeKind.Jitted);
        executionManager.Setup(e => e.GetCodeBlockHandle(entryPoint)).Returns((CodeBlockHandle?)null);
        Mock<IThread> thread = new(MockBehavior.Strict);
        thread
            .Setup(t => t.GetThreadData(threadAddress))
            .Returns(default(ThreadData) with { Frame = frameAddress });
        TestStackWalk stackWalk = new()
        {
            MethodDesc = methodDesc,
        };
        Mock<IRuntimeTypeSystem> runtimeTypeSystem = new(MockBehavior.Strict);
        runtimeTypeSystem
            .Setup(r => r.GetMethodDescHandle(methodDesc))
            .Returns(new MethodDescHandle(methodDesc));
        runtimeTypeSystem
            .Setup(r => r.GetMethodEntryPointIfExists(
                It.Is<MethodDescHandle>(m => m.Address == methodDesc)))
            .Returns(entryPoint);
        IStubTracing stubTracing = CreateTarget(
            arch,
            executionManager.Object,
            runtimeTypeSystem: runtimeTypeSystem.Object,
            thread: thread.Object,
            stackWalk: stackWalk).Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(
            patchLabel,
            continuation,
            threadAddress);

        Assert.Equal(StubTraceKind.Managed, step.Kind);
        Assert.Equal(entryPoint, step.Address);
        Assert.Equal(default, step.Continuation);
    }

    [Fact]
    public void PreStubFailsOnAppleArm64()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TargetCodePointer address = new(0x7000);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(address)).Returns(CodeKind.ThePreStub);
        Mock<IRuntimeInfo> runtimeInfo = new(MockBehavior.Strict);
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(RuntimeInfoArchitecture.Arm64);
        runtimeInfo.Setup(r => r.GetTargetOperatingSystem()).Returns(RuntimeInfoOperatingSystem.Apple);
        IStubTracing stubTracing = CreateTarget(
            arch,
            executionManager.Object,
            runtimeInfo: runtimeInfo.Object).Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(
            address,
            default,
            new TargetPointer(0x8000));

        Assert.Equal(StubTraceKind.Failed, step.Kind);
    }

    [Fact]
    public void ArmCallCountingStubUsesInstructionPointer()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        TargetTestHelpers helpers = new(arch);
        const ulong DescriptorAddress = 0x1000;
        const ulong StubAddress = 0x7000;
        const uint StubCodePageSize = 0x1000;
        TargetCodePointer codePointer = new(StubAddress | 1);
        TargetCodePointer targetAddress = new(0x9001);
        TargetTestHelpers.LayoutResult descriptorLayout = helpers.LayoutFields(
        [
            new(nameof(Data.PrecodeMachineDescriptor.InvalidPrecodeType), DataType.uint8),
            new(nameof(Data.PrecodeMachineDescriptor.StubPrecodeType), DataType.uint8),
            new(nameof(Data.PrecodeMachineDescriptor.StubCodePageSize), DataType.uint32),
        ]);
        TargetTestHelpers.LayoutResult stubDataLayout = helpers.LayoutFields(
        [
            new(
                nameof(Data.CallCountingStubData.TargetForMethod),
                DataType.CodePointer,
                (uint)helpers.PointerSize),
        ]);
        byte[] descriptor = new byte[descriptorLayout.Stride];
        helpers.Write(
            descriptor.AsSpan(
                descriptorLayout.Fields[nameof(Data.PrecodeMachineDescriptor.StubCodePageSize)].Offset,
                sizeof(uint)),
            StubCodePageSize);
        byte[] stubData = new byte[stubDataLayout.Stride];
        helpers.WritePointer(
            stubData.AsSpan(
                stubDataLayout.Fields[nameof(Data.CallCountingStubData.TargetForMethod)].Offset,
                helpers.PointerSize),
            targetAddress.Value);
        TestPlaceholderTarget.Builder builder = new TestPlaceholderTarget.Builder(arch)
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.PrecodeMachineDescriptor] = new Target.TypeInfo
                {
                    Fields = descriptorLayout.Fields,
                    Size = descriptorLayout.Stride,
                },
                [DataType.CallCountingStubData] = new Target.TypeInfo
                {
                    Fields = stubDataLayout.Fields,
                    Size = stubDataLayout.Stride,
                },
            })
            .AddGlobals(
                (Constants.Globals.DACNotifyCompilationFinished, 0ul),
                (Constants.Globals.ThePreStubPatchLabel, 0ul));
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = DescriptorAddress,
            Data = descriptor,
        });
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = StubAddress + StubCodePageSize,
            Data = stubData,
        });
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(codePointer)).Returns(CodeKind.CallCountingStub);
        executionManager.Setup(e => e.GetCodeKind(targetAddress)).Returns(CodeKind.Jitted);
        executionManager.Setup(e => e.GetCodeBlockHandle(targetAddress)).Returns((CodeBlockHandle?)null);
        Mock<IPlatformMetadata> platformMetadata = new(MockBehavior.Strict);
        platformMetadata
            .Setup(p => p.GetPrecodeMachineDescriptor())
            .Returns(new TargetPointer(DescriptorAddress));
        platformMetadata
            .Setup(p => p.GetCodePointerFlags())
            .Returns(CodePointerFlags.HasArm32ThumbBit);
        IStubTracing stubTracing = builder
            .AddMockContract(executionManager)
            .AddMockContract(platformMetadata)
            .AddContract<IStubTracing>("c1")
            .Build()
            .Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(
            codePointer,
            default,
            TargetPointer.Null);

        Assert.Equal(StubTraceKind.Managed, step.Kind);
        Assert.Equal(targetAddress, step.Address);
    }

    [Theory]
    [InlineData(CodeKind.VSD_LookupStub)]
    [InlineData(CodeKind.VSD_DispatchStub)]
    [InlineData(CodeKind.VSD_ResolveStub)]
    [InlineData(CodeKind.VSD_VTableStub)]
    public void VirtualDispatchFails(CodeKind kind)
    {
        TargetCodePointer address = new(0x7100);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeKind(address)).Returns(kind);
        IStubTracing stubTracing = CreateTarget(
            new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true },
            executionManager.Object).Contracts.StubTracing;

        StubTraceStep step = stubTracing.TraceStubStep(
            address,
            default,
            TargetPointer.Null);

        Assert.Equal(StubTraceKind.Failed, step.Kind);
    }

    private sealed class TestPrecodeStubs : IPrecodeStubs
    {
        public TargetPointer EntryPoint { get; init; }
        public TargetPointer MethodDesc { get; init; }
        public TargetCodePointer InterpreterCode { get; init; }
        public PrecodeType? PrecodeType { get; init; }

        public TargetPointer GetPrecodeEntryPointFromInteriorAddress(
            TargetCodePointer interiorAddress,
            bool isFixupPrecode)
            => EntryPoint;

        public TargetPointer GetMethodDescFromStubAddress(TargetCodePointer entryPoint)
            => MethodDesc;

        public TargetCodePointer GetInterpreterCodeFromInterpreterPrecodeIfPresent(
            TargetCodePointer entryPoint)
            => InterpreterCode;

        public PrecodeType? GetPrecodeType(TargetCodePointer entryPoint)
            => PrecodeType;
    }

    private sealed class TestStackWalk : IStackWalk
    {
        public TargetPointer MethodDesc { get; init; }
        public TargetPointer GetMethodDescPtr(TargetPointer framePtr)
            => MethodDesc;

        public byte[] GetContext(
            ThreadData threadData,
            ThreadContextSource contextSource,
            uint contextFlags)
            => [];
    }

    private static Target CreateTarget(
        MockTarget.Architecture arch,
        IExecutionManager executionManager,
        IPrecodeStubs? precodes = null,
        IRuntimeTypeSystem? runtimeTypeSystem = null,
        TargetCodePointer notification = default,
        IThread? thread = null,
        IStackWalk? stackWalk = null,
        IRuntimeInfo? runtimeInfo = null)
    {
        TestPlaceholderTarget.Builder builder = new TestPlaceholderTarget.Builder(arch)
            .AddGlobals(
                (Constants.Globals.DACNotifyCompilationFinished, notification.Value),
                (Constants.Globals.ThePreStubPatchLabel, 0xa000))
            .AddMockContract(executionManager)
            .AddContract<IStubTracing>("c1");
        if (precodes is not null)
            builder.AddMockContract(precodes);
        if (runtimeTypeSystem is not null)
            builder.AddMockContract(runtimeTypeSystem);
        if (thread is not null)
            builder.AddMockContract(thread);
        if (stackWalk is not null)
            builder.AddMockContract(stackWalk);
        if (runtimeInfo is not null)
            builder.AddMockContract(runtimeInfo);

        return builder.Build();
    }
}
