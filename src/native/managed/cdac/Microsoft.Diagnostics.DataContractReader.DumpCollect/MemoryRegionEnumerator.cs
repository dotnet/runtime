// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.DumpCollect;

// Callback implementation returns E_NOINTERFACE for IID_IUnknown. Accepting the callback as a
// generated COM interface would therefore fail in the generated stub before EnumMemoryRegions runs.
[GeneratedComInterface]
[Guid("471c35b4-7c2f-4ef0-a945-00f8c38056f1")]
internal unsafe partial interface ICLRDataEnumMemoryRegionsRaw
{
    [PreserveSig]
    int EnumMemoryRegions(nint callback, uint miniDumpFlags, int clrFlags);
}

[GeneratedComClass]
internal sealed unsafe partial class MemoryRegionEnumerator(
    ICLRDataTarget dataTarget,
    ulong contractDescriptor) : ICLRDataEnumMemoryRegionsRaw
{
    private const int ClrDataEnumMemHeap = 0x1;
    private const int ClrDataEnumMemHeap2 = 0x3;
    private const uint MiniDumpWithPrivateReadWriteMemory = 0x200;
    private const nuint ContextAlignment = 16;

    public int EnumMemoryRegions(nint callback, uint miniDumpFlags, int clrFlags)
    {
        if (callback == 0)
            return HResults.E_INVALIDARG;

        try
        {
            var emitter = new MemoryRegionEmitter(callback);
            ContractDescriptorTarget target = CreateTarget(emitter);
            bool includeHeap =
                clrFlags is ClrDataEnumMemHeap or ClrDataEnumMemHeap2
                || (miniDumpFlags & MiniDumpWithPrivateReadWriteMemory) != 0;

            DumpCollectLogger.Log(
                $"Starting memory enumeration: miniDumpFlags=0x{miniDumpFlags:x}, clrFlags=0x{clrFlags:x}, includeHeap={includeHeap}.");
            new DumpCreator(target, includeHeap, emitter).EnumerateMemoryRegions();
            DumpCollectLogger.Log(
                $"Completed memory enumeration: result=0x{emitter.Result:x8}, regions={emitter.RegionCount}, bytes={emitter.TotalBytes}.");
            return emitter.Result;
        }
        catch (System.Exception ex)
        {
            DumpCollectLogger.LogException(nameof(EnumMemoryRegions), ex);
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    private ContractDescriptorTarget CreateTarget(MemoryRegionEmitter emitter)
    {
        return ContractDescriptorTarget.Create(
            contractDescriptor,
            (address, buffer) =>
            {
                fixed (byte* bufferPointer = buffer)
                {
                    uint bytesRead;
                    int hr = dataTarget.ReadVirtual(address, bufferPointer, (uint)buffer.Length, &bytesRead);
                    emitter.RecordTargetRead(address, (uint)buffer.Length, hr, bytesRead);
                    if (hr < 0)
                        return hr;
                    if (bytesRead != (uint)buffer.Length)
                        return HResults.E_FAIL;

                    if (emitter.EmitTargetReads)
                        emitter.Add(address, bytesRead);
                    return hr;
                }
            },
            (address, buffer) => HResults.E_NOTIMPL,
            (threadId, contextFlags, buffer) =>
            {
                fixed (byte* bufferPointer = buffer)
                {
                    if (((nuint)bufferPointer & (ContextAlignment - 1)) == 0)
                        return dataTarget.GetThreadContext(threadId, contextFlags, (uint)buffer.Length, bufferPointer);

                    byte* alignedBuffer = (byte*)NativeMemory.AlignedAlloc((nuint)buffer.Length, ContextAlignment);
                    NativeMemory.Clear(alignedBuffer, (nuint)buffer.Length);
                    try
                    {
                        int hr = dataTarget.GetThreadContext(
                            threadId,
                            contextFlags,
                            (uint)buffer.Length,
                            alignedBuffer);
                        if (hr >= 0)
                            new ReadOnlySpan<byte>(alignedBuffer, buffer.Length).CopyTo(buffer);

                        return hr;
                    }
                    finally
                    {
                        NativeMemory.AlignedFree(alignedBuffer);
                    }
                }
            },
            (threadId, context) => HResults.E_NOTIMPL,
            (ulong size, out ulong allocatedAddress) =>
            {
                allocatedAddress = 0;
                return HResults.E_NOTIMPL;
            },
            [CoreCLRContracts.Register]);
    }
}

internal sealed unsafe class MemoryRegionEmitter(nint callback)
{
    private static readonly Guid s_callback2Iid = new("3721A26F-8B91-4D98-A388-DB17B356FADB");
    private const int LoggedReadsPerPhase = 32;

    private readonly delegate* unmanaged[MemberFunction]<nint, Guid*, nint*, int> _queryInterface =
        (delegate* unmanaged[MemberFunction]<nint, Guid*, nint*, int>)(*(nint**)callback)[0];
    // ICLRDataEnumMemoryRegionsCallback::EnumMemoryRegion follows the three IUnknown vtable slots.
    private readonly delegate* unmanaged[MemberFunction]<nint, ulong, uint, int> _enumMemoryRegion =
        (delegate* unmanaged[MemberFunction]<nint, ulong, uint, int>)(*(nint**)callback)[3];
    private string? _phase;
    private ulong _phaseReadBytes;
    private ulong _phaseReadCount;

    public ulong RegionCount { get; private set; }
    public int Result { get; private set; }
    public ulong TotalBytes { get; private set; }
    public bool EmitTargetReads { get; private set; } = true;

    public void Add(ulong address, uint size) => Add(address, (ulong)size);

    public void BeginPhase(string phase)
    {
        _phase = phase;
        _phaseReadBytes = 0;
        _phaseReadCount = 0;
    }

    public void EndPhase()
    {
        DumpCollectLogger.Log(
            $"{_phase} target reads: count={_phaseReadCount}, bytes={_phaseReadBytes}.");
        _phase = null;
    }

    public void RecordTargetRead(ulong address, uint requestedBytes, int hr, uint bytesRead)
    {
        _phaseReadCount++;
        _phaseReadBytes = checked(_phaseReadBytes + bytesRead);
        if (_phaseReadCount <= LoggedReadsPerPhase || hr < 0 || bytesRead != requestedBytes)
        {
            DumpCollectLogger.Log(
                $"{_phase} target read {_phaseReadCount}: address=0x{address:x}, requested={requestedBytes}, read={bytesRead}, hr=0x{hr:x8}.");
        }
    }

    public void Add(ulong address, ulong size)
    {
        if (address == 0 || size == 0)
            return;

        while (size != 0)
        {
            uint chunkSize = (uint)Math.Min(size, uint.MaxValue);
            int hr = _enumMemoryRegion(callback, address, chunkSize);
            if (hr == HResults.COR_E_OPERATIONCANCELED)
                Marshal.ThrowExceptionForHR(hr);
            if (hr < 0)
            {
                DumpCollectLogger.Log(
                    $"EnumMemoryRegion failed: address=0x{address:x}, size={chunkSize}, hr=0x{hr:x8}.");
            }

            RegionCount++;
            TotalBytes = checked(TotalBytes + chunkSize);
            if (hr < 0 && Result >= 0)
                Result = hr;

            address = checked(address + chunkSize);
            size -= chunkSize;
        }
    }

    public IDisposable SuppressTargetReadEmission()
    {
        EmitTargetReads = false;
        return new ReadEmissionScope(this);
    }

    public bool Update(ulong address, ReadOnlySpan<byte> buffer)
    {
        nint callback2 = 0;
        Guid iid = s_callback2Iid;
        int hr = _queryInterface(callback, &iid, &callback2);

        if (hr < 0 || callback2 == 0)
        {
            DumpCollectLogger.Log(
                $"ICLRDataEnumMemoryRegionsCallback2 unavailable: hr=0x{hr:x8}.");
            return false;
        }

        try
        {
            delegate* unmanaged[MemberFunction]<nint, ulong, uint, byte*, int> updateMemoryRegion =
                (delegate* unmanaged[MemberFunction]<nint, ulong, uint, byte*, int>)(*(nint**)callback2)[4];
            fixed (byte* bufferPointer = buffer)
            {
                hr = updateMemoryRegion(callback2, address, (uint)buffer.Length, bufferPointer);
            }

            if (hr < 0)
            {
                DumpCollectLogger.Log(
                    $"UpdateMemoryRegion failed: address=0x{address:x}, size={buffer.Length}, hr=0x{hr:x8}.");
                return false;
            }

            return true;
        }
        finally
        {
            delegate* unmanaged[MemberFunction]<nint, uint> release =
                (delegate* unmanaged[MemberFunction]<nint, uint>)(*(nint**)callback2)[2];
            release(callback2);
        }
    }

    private sealed class ReadEmissionScope(MemoryRegionEmitter emitter) : IDisposable
    {
        public void Dispose() => emitter.EmitTargetReads = true;
    }
}
