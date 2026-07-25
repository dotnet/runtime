// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class StubTracing_1 : IStubTracing
{
    private static readonly StubTraceStep s_failed = new(
        StubTraceKind.Failed,
        TargetCodePointer.Null,
        default);

    private readonly Target _target;
    private readonly TargetCodePointer _notifyCompilationFinished;
    private readonly TargetCodePointer _preStub;
    private readonly TargetCodePointer _preStubPatchLabel;

    public StubTracing_1(Target target)
    {
        _target = target;
        _notifyCompilationFinished = target.ReadCodePointer(
            target.ReadGlobalPointer(Constants.Globals.DACNotifyCompilationFinished).Value);
        _preStub = target.TryReadGlobalPointer(
            Constants.Globals.ThePreStub,
            out TargetPointer? preStub)
                ? target.ReadCodePointer(preStub.Value.Value)
                : TargetCodePointer.Null;
        _preStubPatchLabel = target.TryReadGlobalPointer(
            Constants.Globals.ThePreStubPatchLabel,
            out TargetPointer? patchLabel)
                ? target.ReadCodePointer(patchLabel.Value.Value)
                : TargetCodePointer.Null;
    }

    public StubTraceStep TraceStubStep(
        TargetCodePointer address,
        StubContinuation continuation,
        TargetPointer thread)
    {
        return continuation.Kind switch
        {
            StubContinuationKind.None => TraceAddress(
                address,
                hasThread: thread != TargetPointer.Null),
            StubContinuationKind.MethodJitted => TraceJittedMethod(address, continuation),
            StubContinuationKind.FramePush => TraceFrame(address, continuation, thread),
            _ => throw new ArgumentException(),
        };
    }

    private StubTraceStep TraceJittedMethod(
        TargetCodePointer address,
        StubContinuation continuation)
    {
        if (address != _notifyCompilationFinished ||
            continuation.MethodDesc == TargetPointer.Null)
        {
            throw new ArgumentException();
        }

        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle method = runtimeTypeSystem.GetMethodDescHandle(continuation.MethodDesc);
        TargetCodePointer nativeCode = runtimeTypeSystem.GetNativeCode(method);
        if (nativeCode == TargetCodePointer.Null)
            return UnjittedMethod(continuation.MethodDesc);

        nativeCode = _target.Contracts.PrecodeStubs
            .GetInterpreterCodeFromInterpreterPrecodeIfPresent(nativeCode);
        return new StubTraceStep(StubTraceKind.Managed, nativeCode, default);
    }

    private StubTraceStep TraceFrame(
        TargetCodePointer address,
        StubContinuation continuation,
        TargetPointer thread)
    {
        if (thread == TargetPointer.Null || address != continuation.Address)
            throw new ArgumentException();

        ThreadData threadData = _target.Contracts.Thread.GetThreadData(thread);
        TargetPointer methodDesc = _target.Contracts.StackWalk.GetMethodDescPtr(threadData.Frame);
        if (methodDesc == TargetPointer.Null)
            return s_failed;

        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle method = runtimeTypeSystem.GetMethodDescHandle(methodDesc);
        TargetCodePointer entryPoint = runtimeTypeSystem.GetMethodEntryPointIfExists(method);
        return TraceAddress(entryPoint, hasThread: true);
    }

    private StubTraceStep TraceAddress(TargetCodePointer address, bool hasThread)
    {
        while (true)
        {
            CodeKind kind = _target.Contracts.ExecutionManager.GetCodeKind(address);
            switch (kind)
            {
                case CodeKind.Jitted:
                case CodeKind.ReadyToRun:
                case CodeKind.Interpreter:
                    if (IsRuntimeManagedStub(address)) // we don't support these yet
                        return s_failed;

                    return new StubTraceStep(
                        StubTraceKind.Managed,
                        address,
                        default);

                case CodeKind.JumpStub:
                    address = DecodeJumpStub(address);
                    break;

                case CodeKind.CallCountingStub:
                    address = GetCallCountingTarget(address);
                    break;

                case CodeKind.StubPrecode:
                case CodeKind.FixupPrecode:
                    return TracePrecode(
                        address,
                        kind == CodeKind.FixupPrecode,
                        hasThread);

                case CodeKind.ThePreStub:
                    if (_target.Contracts.RuntimeInfo.GetTargetArchitecture() == RuntimeInfoArchitecture.Arm64 &&
                        _target.Contracts.RuntimeInfo.GetTargetOperatingSystem() == RuntimeInfoOperatingSystem.Apple)
                    {
                        return s_failed;
                    }

                    if (!hasThread || _preStubPatchLabel == TargetCodePointer.Null)
                        return s_failed;

                    return new StubTraceStep(
                        StubTraceKind.FramePush,
                        _preStubPatchLabel,
                        new StubContinuation(
                            StubContinuationKind.FramePush,
                            TargetPointer.Null,
                            _preStubPatchLabel));

                case CodeKind.VSD_DispatchStub:
                case CodeKind.VSD_ResolveStub:
                case CodeKind.VSD_LookupStub:
                case CodeKind.VSD_VTableStub:
                    return s_failed; // we don't support these yet

                default:
                    return s_failed;
            }
        }
    }

    private bool IsRuntimeManagedStub(TargetCodePointer address)
    {
        IExecutionManager executionManager = _target.Contracts.ExecutionManager;
        if (executionManager.GetCodeBlockHandle(address) is not CodeBlockHandle codeBlock)
            return false;

        TargetPointer methodDesc = executionManager.GetMethodDesc(codeBlock);
        if (methodDesc == TargetPointer.Null)
            return false;

        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle method = runtimeTypeSystem.GetMethodDescHandle(methodDesc);
        return runtimeTypeSystem.IsILStub(method)
            || runtimeTypeSystem.IsPInvoke(method)
            || (runtimeTypeSystem.GetAsyncMethodFlags(method) & AsyncMethodFlags.Thunk) != 0;
    }

    private StubTraceStep TracePrecode(
        TargetCodePointer address,
        bool isFixupPrecode,
        bool hasThread)
    {
        IPrecodeStubs precodes = _target.Contracts.PrecodeStubs;
        TargetPointer entryPoint = precodes.GetPrecodeEntryPointFromInteriorAddress(
            address,
            isFixupPrecode);
        if (precodes.GetPrecodeType(new TargetCodePointer(entryPoint)) is not
                (PrecodeType.Stub or PrecodeType.Fixup or PrecodeType.ThisPtrRetBuf))
        {
            return s_failed;
        }

        TargetPointer methodDesc = precodes.GetMethodDescFromStubAddress(
            new TargetCodePointer(entryPoint.Value));
        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle method = runtimeTypeSystem.GetMethodDescHandle(methodDesc);
        TargetCodePointer nativeCode = runtimeTypeSystem.GetNativeCode(method);
        if (nativeCode != TargetCodePointer.Null)
        {
            nativeCode = precodes.GetInterpreterCodeFromInterpreterPrecodeIfPresent(nativeCode);
            return TraceAddress(nativeCode, hasThread);
        }

        return runtimeTypeSystem.IsIL(method) || runtimeTypeSystem.IsILStub(method)
            ? UnjittedMethod(methodDesc)
            : TraceAddress(_preStub, hasThread);
    }

    private StubTraceStep UnjittedMethod(TargetPointer methodDesc)
    {
        return new StubTraceStep(
            StubTraceKind.UnjittedMethod,
            _notifyCompilationFinished,
            new StubContinuation(
                StubContinuationKind.MethodJitted,
                methodDesc,
                TargetCodePointer.Null));
    }

    private TargetCodePointer GetCallCountingTarget(TargetCodePointer address)
    {
        TargetPointer descriptorAddress = _target.Contracts.PlatformMetadata.GetPrecodeMachineDescriptor();
        PrecodeMachineDescriptor descriptor = _target.ProcessedData.GetOrAdd<PrecodeMachineDescriptor>(
            descriptorAddress);
        CallCountingStubData data = _target.ProcessedData.GetOrAdd<CallCountingStubData>(
            CodePointerUtils.AddressFromCodePointer(address, _target) + descriptor.StubCodePageSize);
        return data.TargetForMethod;
    }

    private TargetCodePointer DecodeJumpStub(TargetCodePointer address)
    {
        // The offsets below match emitBackToBackJump/decodeBackToBackJump in
        // src/coreclr/vm/<arch>/cgencpu.h for each architecture.
        return _target.Contracts.RuntimeInfo.GetTargetArchitecture() switch
        {
            RuntimeInfoArchitecture.X64 => _target.ReadCodePointer(address.Value + 2),
            RuntimeInfoArchitecture.X86 => new TargetCodePointer(
                unchecked((uint)address.Value + 5u + (uint)_target.Read<int>(address.Value + 1))),
            RuntimeInfoArchitecture.Arm => _target.ReadCodePointer((address.Value & ~1ul) + 4),
            RuntimeInfoArchitecture.Arm64 => _target.ReadCodePointer(address.Value + 8),
            RuntimeInfoArchitecture.LoongArch64 => _target.ReadCodePointer(address.Value + 16),
            RuntimeInfoArchitecture.RiscV64 => _target.ReadCodePointer(address.Value + 16),
            _ => throw new PlatformNotSupportedException(),
        };
    }
}
