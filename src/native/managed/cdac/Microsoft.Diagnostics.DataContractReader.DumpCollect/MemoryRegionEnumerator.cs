// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.DumpCollect;

[GeneratedComClass]
internal sealed unsafe partial class MemoryRegionEnumerator(
    ICLRDataTarget dataTarget,
    ulong contractDescriptor) : ICLRDataEnumMemoryRegions
{
    private const int ClrDataEnumMemHeap = 0x1;
    private const int ClrDataEnumMemHeap2 = 0x3;
    private const uint MiniDumpWithPrivateReadWriteMemory = 0x200;
    private const nuint ContextAlignment = 16;

    public int EnumMemoryRegions(ICLRDataEnumMemoryRegionsCallback callback, uint miniDumpFlags, int clrFlags)
    {
        if (callback is null)
            return HResults.E_INVALIDARG;

        try
        {
            var emitter = new MemoryRegionEmitter(callback);
            ContractDescriptorTarget target = CreateTarget(emitter);
            bool includeHeap =
                clrFlags is ClrDataEnumMemHeap or ClrDataEnumMemHeap2
                || (miniDumpFlags & MiniDumpWithPrivateReadWriteMemory) != 0;

            DumpCreator.EnumerateMemoryRegions(target, includeHeap, emitter);
            return emitter.Result;
        }
        catch (System.Exception ex)
        {
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

internal sealed class MemoryRegionEmitter(ICLRDataEnumMemoryRegionsCallback callback)
{
    public int Result { get; private set; }

    public void Add(ulong address, uint size) => Add(address, (ulong)size);

    public void Add(ulong address, ulong size)
    {
        if (address == 0 || size == 0)
            return;

        while (size != 0)
        {
            uint chunkSize = (uint)Math.Min(size, uint.MaxValue);
            int hr = callback.EnumMemoryRegion(address, chunkSize);
            if (hr == HResults.COR_E_OPERATIONCANCELED)
                Marshal.ThrowExceptionForHR(hr);

            if (hr < 0 && Result >= 0)
                Result = hr;

            address = checked(address + chunkSize);
            size -= chunkSize;
        }
    }
}
