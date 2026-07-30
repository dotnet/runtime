// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Raw callbacks handed to <c>cdac_reader_init</c>, together with their opaque context.
/// </summary>
internal unsafe struct ReaderCallbacks
{
    internal delegate* unmanaged<ulong, byte*, uint, void*, int> ReadFromTarget;
    internal delegate* unmanaged<ulong, byte*, uint, void*, int> WriteToTarget;
    internal delegate* unmanaged<uint, uint, uint, byte*, void*, int> ReadThreadContext;
    internal delegate* unmanaged<uint, uint, byte*, void*, int> WriteThreadContext;
    internal delegate* unmanaged<uint, ulong*, void*, int> AllocVirtual;
    internal void* Context;
}

/// <summary>
/// An <see cref="ICLRDataTarget"/> synthesized over the raw <c>cdac_reader_init</c> callback ABI so
/// the legacy DAC can be created for comparison on that entry point too.
/// </summary>
/// <remarks>
/// <para>
/// The callback ABI is deliberately narrower than <see cref="ICLRDataTarget"/>: it exposes memory
/// read/write, thread context read/write and virtual allocation, and nothing else. Everything that
/// can be derived is derived — the pointer size comes from the contract descriptor's flags word, and
/// the contract descriptor address is the one the caller passed to <c>cdac_reader_init</c>. The rest
/// (machine type, image bases, TLS slots, the current thread id and <c>Request</c>) has no
/// representation in the callback ABI and returns <c>E_NOTIMPL</c>.
/// </para>
/// <para>
/// The legacy DAC requires <c>GetMachineType</c>, so in practice creating a legacy DAC instance over
/// this target fails and the <c>cdac_reader_*</c> entry points degrade to pass-through. That is a
/// property of the callback ABI, not of the shim: consumers that want validation should use
/// <c>CLRDataCreateInstance</c>, which carries a full data target.
/// </para>
/// </remarks>
[GeneratedComClass]
internal sealed unsafe partial class SynthesizedReaderDataTarget
    : ICLRDataTarget, ICLRDataTarget2, ICLRContractLocator
{
    private readonly ReaderCallbacks _callbacks;
    private readonly ulong _contractDescriptor;
    private readonly bool _isRecording;

    private uint _pointerSize;
    private bool _pointerSizeResolved;

    internal SynthesizedReaderDataTarget(ReaderCallbacks callbacks, ulong contractDescriptor, bool isRecording)
    {
        _callbacks = callbacks;
        _contractDescriptor = contractDescriptor;
        _isRecording = isRecording;
    }

    int ICLRDataTarget.GetMachineType(uint* machineType)
    {
        // Not expressible in the reader callback ABI.
        if (machineType is not null)
            *machineType = 0;
        return HResults.E_NOTIMPL;
    }

    int ICLRDataTarget.GetPointerSize(uint* pointerSize)
    {
        if (pointerSize is null)
            return HResults.E_POINTER;

        if (!_pointerSizeResolved)
        {
            _pointerSize = ReadPointerSizeFromContractDescriptor();
            _pointerSizeResolved = true;
        }

        if (_pointerSize == 0)
            return HResults.E_FAIL;

        *pointerSize = _pointerSize;
        return HResults.S_OK;
    }

    // See docs/design/datacontracts/contract-descriptor.md: magic (8 bytes) then a flags word whose
    // bit 1 encodes the pointer size (0 = 64-bit, 1 = 32-bit).
    private uint ReadPointerSizeFromContractDescriptor()
    {
        if (_contractDescriptor == 0 || _callbacks.ReadFromTarget is null)
            return 0;

        byte* header = stackalloc byte[sizeof(ulong) + sizeof(uint)];
        if (_callbacks.ReadFromTarget(_contractDescriptor, header, sizeof(ulong) + sizeof(uint), _callbacks.Context) < 0)
            return 0;

        ReadOnlySpan<byte> magic = new(header, sizeof(ulong));
        bool isLittleEndian = magic.SequenceEqual("DNCCDAC\0"u8);
        if (!isLittleEndian && !magic.SequenceEqual("\0CADCCND"u8))
            return 0;

        ReadOnlySpan<byte> flagsBytes = new(header + sizeof(ulong), sizeof(uint));
        uint flags = isLittleEndian
            ? System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(flagsBytes)
            : System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(flagsBytes);

        return (flags & 0x2) == 0 ? (uint)sizeof(ulong) : (uint)sizeof(uint);
    }

    int ICLRDataTarget.GetImageBase(string imagePath, ulong* baseAddress)
    {
        // Not expressible in the reader callback ABI.
        if (baseAddress is not null)
            *baseAddress = 0;
        return HResults.E_NOTIMPL;
    }

    int ICLRDataTarget.ReadVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesRead)
    {
        if (_callbacks.ReadFromTarget is null)
            return HResults.E_NOTIMPL;

        int hr = _callbacks.ReadFromTarget(address, buffer, bytesRequested, _callbacks.Context);
        if (bytesRead is not null)
            *bytesRead = hr >= 0 ? bytesRequested : 0;
        return hr;
    }

    int ICLRDataTarget.WriteVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesWritten)
    {
        if (!_isRecording)
        {
            Mutation? recorded = ShimCall.Current?.NextRecordedMutation();
            if (recorded is null || recorded.Kind != MutationKind.WriteVirtual)
            {
                Debug.Fail("Legacy DAC performed an unmatched WriteVirtual that the cDAC did not perform.");
                if (bytesWritten is not null)
                    *bytesWritten = 0;
                return HResults.E_FAIL;
            }

            Debug.Assert(recorded.Address == address, $"cDAC: 0x{recorded.Address:x}, DAC: 0x{address:x}");
            if (bytesWritten is not null)
                *bytesWritten = recorded.Count;
            return recorded.HResult;
        }

        if (_callbacks.WriteToTarget is null)
            return HResults.E_NOTIMPL;

        int hr = _callbacks.WriteToTarget(address, buffer, bytesRequested, _callbacks.Context);
        if (bytesWritten is not null)
            *bytesWritten = hr >= 0 ? bytesRequested : 0;

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.WriteVirtual,
            Address = address,
            Data = buffer is null ? null : new ReadOnlySpan<byte>(buffer, checked((int)bytesRequested)).ToArray(),
            Count = hr >= 0 ? bytesRequested : 0,
            HResult = hr,
        });

        return hr;
    }

    int ICLRDataTarget.GetTLSValue(uint threadID, uint index, ulong* value) => HResults.E_NOTIMPL;

    int ICLRDataTarget.SetTLSValue(uint threadID, uint index, ulong value) => HResults.E_NOTIMPL;

    int ICLRDataTarget.GetCurrentThreadID(uint* threadID) => HResults.E_NOTIMPL;

    int ICLRDataTarget.GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte* context)
    {
        if (_callbacks.ReadThreadContext is null)
            return HResults.E_NOTIMPL;

        return _callbacks.ReadThreadContext(threadID, contextFlags, contextSize, context, _callbacks.Context);
    }

    int ICLRDataTarget.SetThreadContext(uint threadID, uint contextSize, byte* context)
    {
        if (!_isRecording)
        {
            Mutation? recorded = ShimCall.Current?.NextRecordedMutation();
            if (recorded is null || recorded.Kind != MutationKind.SetThreadContext)
            {
                Debug.Fail("Legacy DAC performed an unmatched SetThreadContext that the cDAC did not perform.");
                return HResults.E_FAIL;
            }

            Debug.Assert(recorded.ThreadId == threadID, $"cDAC: {recorded.ThreadId}, DAC: {threadID}");
            return recorded.HResult;
        }

        if (_callbacks.WriteThreadContext is null)
            return HResults.E_NOTIMPL;

        int hr = _callbacks.WriteThreadContext(threadID, contextSize, context, _callbacks.Context);

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
        => HResults.E_NOTIMPL;

    int ICLRDataTarget2.AllocVirtual(ClrDataAddress addr, uint size, uint typeFlags, uint protectFlags, ClrDataAddress* virt)
    {
        if (!_isRecording)
        {
            Mutation? recorded = ShimCall.Current?.NextRecordedMutation();
            if (recorded is null || recorded.Kind != MutationKind.AllocVirtual)
            {
                Debug.Fail("Legacy DAC performed an unmatched AllocVirtual that the cDAC did not perform.");
                if (virt is not null)
                    *virt = default;
                return HResults.E_FAIL;
            }

            Debug.Assert(recorded.Count == size, $"cDAC: {recorded.Count}, DAC: {size}");
            if (virt is not null)
                *virt = new ClrDataAddress(recorded.Value);
            return recorded.HResult;
        }

        if (_callbacks.AllocVirtual is null)
            return HResults.E_NOTIMPL;

        ulong allocated = 0;
        int hr = _callbacks.AllocVirtual(size, &allocated, _callbacks.Context);
        if (virt is not null)
            *virt = new ClrDataAddress(allocated);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.AllocVirtual,
            Address = addr.Value,
            Count = size,
            Value = allocated,
            HResult = hr,
        });

        return hr;
    }

    int ICLRDataTarget2.FreeVirtual(ClrDataAddress addr, uint size, uint typeFlags) => HResults.E_NOTIMPL;

    int ICLRContractLocator.GetContractDescriptor(ulong* contractAddress)
    {
        if (contractAddress is null)
            return HResults.E_POINTER;

        *contractAddress = _contractDescriptor;
        return HResults.S_OK;
    }
}
