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
            DumpCreator.EnumerateMemoryRegions(target, includeHeap, emitter);
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
                    if (hr < 0)
                        return hr;
                    if (bytesRead != (uint)buffer.Length)
                        return HResults.E_FAIL;

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
    // ICLRDataEnumMemoryRegionsCallback::EnumMemoryRegion follows the three IUnknown vtable slots.
    private readonly delegate* unmanaged[MemberFunction]<nint, ulong, uint, int> _enumMemoryRegion =
        (delegate* unmanaged[MemberFunction]<nint, ulong, uint, int>)(*(nint**)callback)[3];

    public ulong RegionCount { get; private set; }
    public int Result { get; private set; }
    public ulong TotalBytes { get; private set; }

    public void Add(ulong address, uint size) => Add(address, (ulong)size);

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

            RegionCount++;
            TotalBytes = checked(TotalBytes + chunkSize);
            if (hr < 0 && Result >= 0)
                Result = hr;

            address = checked(address + chunkSize);
            size -= chunkSize;
        }
    }
}
