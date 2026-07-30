// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Base for the <see cref="ICorDebugDataTarget"/> proxies used on the DBI entry point. Same contract
/// as <see cref="DataTargetProxy"/>: reads pass through, mutations are recorded on one side and
/// replayed on the other.
/// </summary>
internal abstract unsafe class CorDebugDataTargetProxy : ICustomQueryInterface
{
    protected CorDebugDataTargetProxy(IntPtr callerTarget, object callerObject)
    {
        CallerTargetPointer = callerTarget;
        Target = callerObject as ICorDebugDataTarget
                 ?? throw new ArgumentException($"Data target does not implement {nameof(ICorDebugDataTarget)}", nameof(callerObject));
        MutableTarget = callerObject as ICorDebugMutableDataTarget;
    }

    protected IntPtr CallerTargetPointer { get; }

    protected ICorDebugDataTarget Target { get; }
    protected ICorDebugMutableDataTarget? MutableTarget { get; }

    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
    {
        ppv = IntPtr.Zero;

        if (CallerTargetPointer == IntPtr.Zero)
            return CustomQueryInterfaceResult.NotHandled;

        if (Marshal.QueryInterface(CallerTargetPointer, iid, out IntPtr supported) < 0)
            return CustomQueryInterfaceResult.Failed;

        if (iid == typeof(ICorDebugDataTarget).GUID || iid == typeof(ICorDebugMutableDataTarget).GUID)
        {
            Marshal.Release(supported);
            return CustomQueryInterfaceResult.NotHandled;
        }

        ppv = supported;
        return CustomQueryInterfaceResult.Handled;
    }
}

/// <summary>Data target handed to the production cDAC on the DBI entry point.</summary>
[GeneratedComClass]
internal sealed unsafe partial class RecordingCorDebugDataTarget
    : CorDebugDataTargetProxy, ICorDebugDataTarget, ICorDebugMutableDataTarget
{
    internal RecordingCorDebugDataTarget(IntPtr callerTarget, object callerObject)
        : base(callerTarget, callerObject)
    {
    }

    int ICorDebugDataTarget.GetPlatform(int* pTargetPlatform) => Target.GetPlatform(pTargetPlatform);

    int ICorDebugDataTarget.ReadVirtual(ulong address, byte* pBuffer, uint bytesRequested, uint* pBytesRead)
        => Target.ReadVirtual(address, pBuffer, bytesRequested, pBytesRead);

    int ICorDebugDataTarget.GetThreadContext(uint threadId, uint contextFlags, uint contextSize, byte* pContext)
        => Target.GetThreadContext(threadId, contextFlags, contextSize, pContext);

    int ICorDebugMutableDataTarget.WriteVirtual(ulong address, byte* pBuffer, uint bytesRequested)
    {
        if (MutableTarget is null)
            return HResults.E_NOTIMPL;

        int hr = MutableTarget.WriteVirtual(address, pBuffer, bytesRequested);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.WriteVirtual,
            Address = address,
            Data = pBuffer is null ? null : new ReadOnlySpan<byte>(pBuffer, checked((int)bytesRequested)).ToArray(),
            Count = bytesRequested,
            HResult = hr,
        });

        return hr;
    }

    int ICorDebugMutableDataTarget.SetThreadContext(uint threadId, uint contextSize, byte* pContext)
    {
        if (MutableTarget is null)
            return HResults.E_NOTIMPL;

        int hr = MutableTarget.SetThreadContext(threadId, contextSize, pContext);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.SetThreadContext,
            ThreadId = threadId,
            Data = pContext is null ? null : new ReadOnlySpan<byte>(pContext, checked((int)contextSize)).ToArray(),
            HResult = hr,
        });

        return hr;
    }

    int ICorDebugMutableDataTarget.ContinueStatusChanged(uint threadId, uint continueStatus)
    {
        if (MutableTarget is null)
            return HResults.E_NOTIMPL;

        int hr = MutableTarget.ContinueStatusChanged(threadId, continueStatus);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.SetTLSValue,
            ThreadId = threadId,
            Value = continueStatus,
            HResult = hr,
        });

        return hr;
    }
}

/// <summary>Data target handed to the legacy DBI: reads pass through, mutations replay.</summary>
[GeneratedComClass]
internal sealed unsafe partial class ReplayCorDebugDataTarget
    : CorDebugDataTargetProxy, ICorDebugDataTarget, ICorDebugMutableDataTarget
{
    internal ReplayCorDebugDataTarget(IntPtr callerTarget, object callerObject)
        : base(callerTarget, callerObject)
    {
    }

    private static Mutation? Expect(MutationKind kind)
    {
        CallState? call = ShimCall.Current;
        if (call is null)
        {
            ShimLog.Error($"Legacy DBI attempted a {kind} outside a proxied call; not executed.");
            return null;
        }

        Mutation? mutation = call.NextRecordedMutation();
        if (mutation is null)
        {
            Debug.Fail($"Legacy DBI performed an unmatched {kind} that the cDAC did not perform.");
            return null;
        }

        Debug.Assert(mutation.Kind == kind, $"cDAC: {mutation.Kind}, DAC: {kind}");
        return mutation;
    }

    int ICorDebugDataTarget.GetPlatform(int* pTargetPlatform) => Target.GetPlatform(pTargetPlatform);

    int ICorDebugDataTarget.ReadVirtual(ulong address, byte* pBuffer, uint bytesRequested, uint* pBytesRead)
        => Target.ReadVirtual(address, pBuffer, bytesRequested, pBytesRead);

    int ICorDebugDataTarget.GetThreadContext(uint threadId, uint contextFlags, uint contextSize, byte* pContext)
        => Target.GetThreadContext(threadId, contextFlags, contextSize, pContext);

    int ICorDebugMutableDataTarget.WriteVirtual(ulong address, byte* pBuffer, uint bytesRequested)
    {
        Mutation? recorded = Expect(MutationKind.WriteVirtual);
        if (recorded is null)
            return HResults.E_FAIL;

        Debug.Assert(recorded.Address == address, $"cDAC: 0x{recorded.Address:x}, DAC: 0x{address:x}");
        if (recorded.Data is not null && pBuffer is not null && recorded.Data.Length == (int)bytesRequested)
        {
            Debug.Assert(
                new ReadOnlySpan<byte>(pBuffer, (int)bytesRequested).SequenceEqual(recorded.Data),
                $"WriteVirtual payload mismatch at 0x{address:x}");
        }

        return recorded.HResult;
    }

    int ICorDebugMutableDataTarget.SetThreadContext(uint threadId, uint contextSize, byte* pContext)
    {
        Mutation? recorded = Expect(MutationKind.SetThreadContext);
        if (recorded is null)
            return HResults.E_FAIL;

        Debug.Assert(recorded.ThreadId == threadId, $"cDAC: {recorded.ThreadId}, DAC: {threadId}");
        return recorded.HResult;
    }

    int ICorDebugMutableDataTarget.ContinueStatusChanged(uint threadId, uint continueStatus)
    {
        Mutation? recorded = Expect(MutationKind.SetTLSValue);
        if (recorded is null)
            return HResults.E_FAIL;

        Debug.Assert(recorded.ThreadId == threadId, $"cDAC: {recorded.ThreadId}, DAC: {threadId}");
        Debug.Assert(recorded.Value == continueStatus, $"cDAC: {recorded.Value}, DAC: {continueStatus}");
        return recorded.HResult;
    }
}
