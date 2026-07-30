// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Base for the two data targets the shim interposes between a consumer's real data target and the
/// production cDAC / legacy DAC.
/// </summary>
/// <remarks>
/// Reads and every non-mutating query pass straight through to the caller's target. Mutations are
/// handled by the derived types: <see cref="RecordingDataTarget"/> (given to the production cDAC)
/// executes and records them, and <see cref="ReplayDataTarget"/> (given to the legacy DAC) replays
/// the recorded outcome and compares the requested mutation instead of performing it again. Running
/// both sides' writes against a live target would corrupt it, and letting both allocate would hand
/// the DAC a different address than the cDAC saw.
/// </remarks>
internal abstract unsafe class DataTargetProxy : ICustomQueryInterface
{
    protected DataTargetProxy(IntPtr callerTarget, object callerObject, ulong contractDescriptorOverride)
    {
        CallerTargetPointer = callerTarget;
        ContractDescriptorOverride = contractDescriptorOverride;
        Target = callerObject as ICLRDataTarget
                 ?? throw new ArgumentException($"Data target does not implement {nameof(ICLRDataTarget)}", nameof(callerObject));
        Target2 = callerObject as ICLRDataTarget2;
        Target3 = callerObject as ICLRDataTarget3;
        ContractLocator = callerObject as ICLRContractLocator;
        RuntimeLocator = callerObject as ICLRRuntimeLocator;
    }

    protected IntPtr CallerTargetPointer { get; }

    /// <summary>
    /// Contract descriptor address supplied by the caller of
    /// <c>DbgShimCreateInstanceFromContractDescriptor</c>, or 0 when the real target should answer.
    /// </summary>
    protected ulong ContractDescriptorOverride { get; }

    protected ICLRDataTarget Target { get; }
    protected ICLRDataTarget2? Target2 { get; }
    protected ICLRDataTarget3? Target3 { get; }
    protected ICLRContractLocator? ContractLocator { get; }
    protected ICLRRuntimeLocator? RuntimeLocator { get; }

    protected int GetContractDescriptorCore(ulong* contractAddress)
    {
        if (ContractDescriptorOverride != 0)
        {
            if (contractAddress is null)
                return HResults.E_POINTER;

            *contractAddress = ContractDescriptorOverride;
            return HResults.S_OK;
        }

        return ContractLocator is null ? HResults.E_NOTIMPL : ContractLocator.GetContractDescriptor(contractAddress);
    }

    /// <summary>
    /// Interface support is delegated to the caller's real target so that the consumers'
    /// <c>is</c>/<c>as</c> probes (for example for <see cref="ICLRDataTarget2"/>) observe exactly the
    /// same surface they would without the shim in the middle.
    /// </summary>
    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
    {
        ppv = IntPtr.Zero;

        if (CallerTargetPointer == IntPtr.Zero)
            return CustomQueryInterfaceResult.NotHandled;

        // A caller-supplied contract descriptor makes ICLRContractLocator available even when the
        // real target does not implement it.
        if (ContractDescriptorOverride != 0 && iid == typeof(ICLRContractLocator).GUID)
            return CustomQueryInterfaceResult.NotHandled;

        if (Marshal.QueryInterface(CallerTargetPointer, iid, out IntPtr supported) < 0)
            return CustomQueryInterfaceResult.Failed;

        if (iid == typeof(ICLRDataTarget).GUID
            || iid == typeof(ICLRDataTarget2).GUID
            || iid == typeof(ICLRDataTarget3).GUID
            || iid == typeof(ICLRContractLocator).GUID
            || iid == typeof(ICLRRuntimeLocator).GUID)
        {
            Marshal.Release(supported);
            return CustomQueryInterfaceResult.NotHandled;
        }

        ppv = supported;
        return CustomQueryInterfaceResult.Handled;
    }
}

/// <summary>
/// Data target handed to the production cDAC. Mutations execute against the real target and are
/// recorded on the current call so the legacy DAC's target can replay them.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class RecordingDataTarget
    : DataTargetProxy, ICLRDataTarget, ICLRDataTarget2, ICLRDataTarget3, ICLRContractLocator, ICLRRuntimeLocator
{
    internal RecordingDataTarget(IntPtr callerTarget, object callerObject, ulong contractDescriptorOverride)
        : base(callerTarget, callerObject, contractDescriptorOverride)
    {
    }

    int ICLRDataTarget.GetMachineType(uint* machineType) => Target.GetMachineType(machineType);

    int ICLRDataTarget.GetPointerSize(uint* pointerSize) => Target.GetPointerSize(pointerSize);

    int ICLRDataTarget.GetImageBase(string imagePath, ulong* baseAddress) => Target.GetImageBase(imagePath, baseAddress);

    int ICLRDataTarget.ReadVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesRead)
        => Target.ReadVirtual(address, buffer, bytesRequested, bytesRead);

    int ICLRDataTarget.WriteVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesWritten)
    {
        uint written = 0;
        int hr = Target.WriteVirtual(address, buffer, bytesRequested, &written);
        if (bytesWritten is not null)
            *bytesWritten = written;

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.WriteVirtual,
            Address = address,
            Data = buffer is null ? null : new ReadOnlySpan<byte>(buffer, checked((int)bytesRequested)).ToArray(),
            Count = written,
            HResult = hr,
        });

        return hr;
    }

    int ICLRDataTarget.GetTLSValue(uint threadID, uint index, ulong* value) => Target.GetTLSValue(threadID, index, value);

    int ICLRDataTarget.SetTLSValue(uint threadID, uint index, ulong value)
    {
        int hr = Target.SetTLSValue(threadID, index, value);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.SetTLSValue,
            ThreadId = threadID,
            Index = index,
            Value = value,
            HResult = hr,
        });

        return hr;
    }

    int ICLRDataTarget.GetCurrentThreadID(uint* threadID) => Target.GetCurrentThreadID(threadID);

    int ICLRDataTarget.GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte* context)
        => Target.GetThreadContext(threadID, contextFlags, contextSize, context);

    int ICLRDataTarget.SetThreadContext(uint threadID, uint contextSize, byte* context)
    {
        int hr = Target.SetThreadContext(threadID, contextSize, context);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.SetThreadContext,
            ThreadId = threadID,
            Data = context is null ? null : new ReadOnlySpan<byte>(context, checked((int)contextSize)).ToArray(),
            HResult = hr,
        });

        return hr;
    }

    int ICLRDataTarget.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => Target.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer);

    int ICLRDataTarget2.AllocVirtual(ClrDataAddress addr, uint size, uint typeFlags, uint protectFlags, ClrDataAddress* virt)
    {
        if (Target2 is null)
            return HResults.E_NOTIMPL;

        ClrDataAddress allocated = default;
        int hr = Target2.AllocVirtual(addr, size, typeFlags, protectFlags, &allocated);
        if (virt is not null)
            *virt = allocated;

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.AllocVirtual,
            Address = addr.Value,
            Count = size,
            Value = allocated.Value,
            HResult = hr,
        });

        return hr;
    }

    int ICLRDataTarget2.FreeVirtual(ClrDataAddress addr, uint size, uint typeFlags)
    {
        if (Target2 is null)
            return HResults.E_NOTIMPL;

        int hr = Target2.FreeVirtual(addr, size, typeFlags);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.FreeVirtual,
            Address = addr.Value,
            Count = size,
            HResult = hr,
        });

        return hr;
    }

    int ICLRDataTarget3.GetExceptionRecord(uint bufferSize, uint* bufferUsed, byte* buffer)
        => Target3 is null ? HResults.E_NOTIMPL : Target3.GetExceptionRecord(bufferSize, bufferUsed, buffer);

    int ICLRDataTarget3.GetExceptionContextRecord(uint bufferSize, uint* bufferUsed, byte* buffer)
        => Target3 is null ? HResults.E_NOTIMPL : Target3.GetExceptionContextRecord(bufferSize, bufferUsed, buffer);

    int ICLRDataTarget3.GetExceptionThreadID(uint* threadID)
        => Target3 is null ? HResults.E_NOTIMPL : Target3.GetExceptionThreadID(threadID);

    int ICLRContractLocator.GetContractDescriptor(ulong* contractAddress) => GetContractDescriptorCore(contractAddress);


    int ICLRRuntimeLocator.GetRuntimeBase(ulong* baseAddress)
        => RuntimeLocator is null ? HResults.E_NOTIMPL : RuntimeLocator.GetRuntimeBase(baseAddress);
}

/// <summary>
/// Data target handed to the legacy DAC. Reads pass through; mutations are compared against what the
/// production cDAC did on this call and the recorded outcome (including the allocated address) is
/// replayed rather than re-executed.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ReplayDataTarget
    : DataTargetProxy, ICLRDataTarget, ICLRDataTarget2, ICLRDataTarget3, ICLRContractLocator, ICLRRuntimeLocator
{
    internal ReplayDataTarget(IntPtr callerTarget, object callerObject, ulong contractDescriptorOverride)
        : base(callerTarget, callerObject, contractDescriptorOverride)
    {
    }

    private static Mutation? Expect(MutationKind kind)
    {
        CallState? call = ShimCall.Current;
        if (call is null)
        {
            // Outside a proxied call there is nothing to replay against, so the mutation is not
            // executed: letting it through would change target state the cDAC never touched.
            ShimLog.Error($"Legacy DAC attempted a {kind} outside a proxied call; not executed.");
            return null;
        }

        Mutation? mutation = call.NextRecordedMutation();
        if (mutation is null)
        {
            Debug.Fail($"Legacy DAC performed an unmatched {kind} that the cDAC did not perform.");
            return null;
        }

        Debug.Assert(mutation.Kind == kind, $"cDAC: {mutation.Kind}, DAC: {kind}");
        return mutation;
    }

    int ICLRDataTarget.GetMachineType(uint* machineType) => Target.GetMachineType(machineType);

    int ICLRDataTarget.GetPointerSize(uint* pointerSize) => Target.GetPointerSize(pointerSize);

    int ICLRDataTarget.GetImageBase(string imagePath, ulong* baseAddress) => Target.GetImageBase(imagePath, baseAddress);

    int ICLRDataTarget.ReadVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesRead)
        => Target.ReadVirtual(address, buffer, bytesRequested, bytesRead);

    int ICLRDataTarget.WriteVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesWritten)
    {
        Mutation? recorded = Expect(MutationKind.WriteVirtual);
        if (recorded is null)
        {
            if (bytesWritten is not null)
                *bytesWritten = 0;
            return HResults.E_FAIL;
        }

        Debug.Assert(recorded.Address == address, $"cDAC: 0x{recorded.Address:x}, DAC: 0x{address:x}");
        Debug.Assert(
            (recorded.Data?.Length ?? 0) == (int)bytesRequested,
            $"cDAC: {recorded.Data?.Length ?? 0} bytes, DAC: {bytesRequested} bytes");

        if (recorded.Data is not null && buffer is not null && recorded.Data.Length == (int)bytesRequested)
        {
            Debug.Assert(
                new ReadOnlySpan<byte>(buffer, (int)bytesRequested).SequenceEqual(recorded.Data),
                $"WriteVirtual payload mismatch at 0x{address:x}");
        }

        if (bytesWritten is not null)
            *bytesWritten = recorded.Count;

        return recorded.HResult;
    }

    int ICLRDataTarget.GetTLSValue(uint threadID, uint index, ulong* value) => Target.GetTLSValue(threadID, index, value);

    int ICLRDataTarget.SetTLSValue(uint threadID, uint index, ulong value)
    {
        Mutation? recorded = Expect(MutationKind.SetTLSValue);
        if (recorded is null)
            return HResults.E_FAIL;

        Debug.Assert(recorded.ThreadId == threadID, $"cDAC: {recorded.ThreadId}, DAC: {threadID}");
        Debug.Assert(recorded.Index == index, $"cDAC: {recorded.Index}, DAC: {index}");
        Debug.Assert(recorded.Value == value, $"cDAC: 0x{recorded.Value:x}, DAC: 0x{value:x}");
        return recorded.HResult;
    }

    int ICLRDataTarget.GetCurrentThreadID(uint* threadID) => Target.GetCurrentThreadID(threadID);

    int ICLRDataTarget.GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte* context)
        => Target.GetThreadContext(threadID, contextFlags, contextSize, context);

    int ICLRDataTarget.SetThreadContext(uint threadID, uint contextSize, byte* context)
    {
        Mutation? recorded = Expect(MutationKind.SetThreadContext);
        if (recorded is null)
            return HResults.E_FAIL;

        Debug.Assert(recorded.ThreadId == threadID, $"cDAC: {recorded.ThreadId}, DAC: {threadID}");
        if (recorded.Data is not null && context is not null && recorded.Data.Length == (int)contextSize)
        {
            Debug.Assert(
                new ReadOnlySpan<byte>(context, (int)contextSize).SequenceEqual(recorded.Data),
                $"SetThreadContext payload mismatch for thread {threadID}");
        }

        return recorded.HResult;
    }

    int ICLRDataTarget.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => Target.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer);

    int ICLRDataTarget2.AllocVirtual(ClrDataAddress addr, uint size, uint typeFlags, uint protectFlags, ClrDataAddress* virt)
    {
        Mutation? recorded = Expect(MutationKind.AllocVirtual);
        if (recorded is null)
        {
            if (virt is not null)
                *virt = default;
            return HResults.E_FAIL;
        }

        Debug.Assert(recorded.Count == size, $"cDAC: {recorded.Count}, DAC: {size}");

        // Replay the allocation address so the legacy DAC writes into the same scratch buffer the
        // cDAC used, keeping the results comparable.
        if (virt is not null)
            *virt = new ClrDataAddress(recorded.Value);

        return recorded.HResult;
    }

    int ICLRDataTarget2.FreeVirtual(ClrDataAddress addr, uint size, uint typeFlags)
    {
        Mutation? recorded = Expect(MutationKind.FreeVirtual);
        if (recorded is null)
            return HResults.E_FAIL;

        Debug.Assert(recorded.Address == addr.Value, $"cDAC: 0x{recorded.Address:x}, DAC: 0x{addr.Value:x}");
        return recorded.HResult;
    }

    int ICLRDataTarget3.GetExceptionRecord(uint bufferSize, uint* bufferUsed, byte* buffer)
        => Target3 is null ? HResults.E_NOTIMPL : Target3.GetExceptionRecord(bufferSize, bufferUsed, buffer);

    int ICLRDataTarget3.GetExceptionContextRecord(uint bufferSize, uint* bufferUsed, byte* buffer)
        => Target3 is null ? HResults.E_NOTIMPL : Target3.GetExceptionContextRecord(bufferSize, bufferUsed, buffer);

    int ICLRDataTarget3.GetExceptionThreadID(uint* threadID)
        => Target3 is null ? HResults.E_NOTIMPL : Target3.GetExceptionThreadID(threadID);

    int ICLRContractLocator.GetContractDescriptor(ulong* contractAddress) => GetContractDescriptorCore(contractAddress);


    int ICLRRuntimeLocator.GetRuntimeBase(ulong* baseAddress)
        => RuntimeLocator is null ? HResults.E_NOTIMPL : RuntimeLocator.GetRuntimeBase(baseAddress);
}
