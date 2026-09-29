// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

internal static unsafe class MemoryRegionEnumerator
{
    private const uint MiniDumpWithPrivateReadWriteMemory = 0x200;
    private const uint MiniDumpWithFullAuxiliaryState = 0x8000;
    private const uint MiniDumpFilterTriage = 0x100000;

    public static int Enumerate(Target target, void* callback, uint miniDumpFlags)
    {
        if (target is not ContractDescriptorTarget descriptorTarget)
            return HResults.E_NOTIMPL;

        target.Flush(FlushScope.All);
        var emitter = new MemoryRegionEmitter((nint)callback, (uint)target.PointerSize);
        using IDisposable readScope = descriptorTarget.RegisterReadCallback((address, size) =>
        {
            if (emitter.ShouldEmitTargetRead(address, size))
                emitter.Add(address, size);
        });
        foreach (TargetSpan range in descriptorTarget.EnumerateDescriptorMemory())
            emitter.Add(range.Address.Value, range.Size);

        new DumpCreator(target, GetDumpFlags(miniDumpFlags), emitter).EnumerateMemoryRegions();
        return emitter.Result;
    }

    internal static CLRDataEnumMemoryFlags GetDumpFlags(uint miniDumpFlags)
    {
        if ((miniDumpFlags & MiniDumpWithPrivateReadWriteMemory) != 0)
            return CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_HEAP2;
        if ((miniDumpFlags & MiniDumpWithFullAuxiliaryState) != 0)
            return CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT;
        if ((miniDumpFlags & MiniDumpFilterTriage) != 0)
            return CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_TRIAGE;

        return CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_MINI;
    }
}

internal sealed unsafe class MemoryRegionEmitter(nint callback, uint pointerSize)
{
    private static readonly Guid s_ICLRDataEnumMemoryRegionsCallback2_Iid = new("3721A26F-8B91-4D98-A388-DB17B356FADB");

    private readonly delegate* unmanaged[MemberFunction]<nint, Guid*, nint*, int> _queryInterface =
        (delegate* unmanaged[MemberFunction]<nint, Guid*, nint*, int>)(*(nint**)callback)[0];
    // ICLRDataEnumMemoryRegionsCallback::EnumMemoryRegion follows the three IUnknown vtable slots.
    private readonly delegate* unmanaged[MemberFunction]<nint, ulong, uint, int> _enumMemoryRegion =
        (delegate* unmanaged[MemberFunction]<nint, ulong, uint, int>)(*(nint**)callback)[3];
    private readonly List<TargetSpan> _metadataRanges = [];

    public int Result { get; private set; }

    public void Add(ulong address, uint size) => Add(address, (ulong)size);

    public void Add(ulong address, ulong size)
    {
        if (address == 0 || size == 0)
            return;

        while (size != 0)
        {
            uint chunkSize = (uint)Math.Min(size, uint.MaxValue);
            int hr = _enumMemoryRegion(callback, ToClrDataAddress(address), chunkSize);
            if (hr == HResults.COR_E_OPERATIONCANCELED)
                Marshal.ThrowExceptionForHR(hr);

            if (hr < 0 && Result >= 0)
                Result = hr;

            address = checked(address + chunkSize);
            size -= chunkSize;
        }
    }

    public void RegisterMetadataRange(TargetSpan range)
    {
        if (range.Address != TargetPointer.Null && range.Size != 0)
            _metadataRanges.Add(range);
    }

    public bool ShouldEmitTargetRead(ulong address, ulong size)
    {
        foreach (TargetSpan range in _metadataRanges)
        {
            if (address < range.Address.Value)
                continue;

            ulong offset = address - range.Address.Value;
            if (offset <= range.Size && size <= range.Size - offset)
                return false;
        }

        return true;
    }

    public bool Update(ulong address, ReadOnlySpan<byte> buffer)
    {
        nint callback2 = 0;
        Guid iid = s_ICLRDataEnumMemoryRegionsCallback2_Iid;
        int hr = _queryInterface(callback, &iid, &callback2);

        if (hr < 0 || callback2 == 0)
            return false;

        try
        {
            delegate* unmanaged[MemberFunction]<nint, ulong, uint, byte*, int> updateMemoryRegion =
                (delegate* unmanaged[MemberFunction]<nint, ulong, uint, byte*, int>)(*(nint**)callback2)[4];
            fixed (byte* bufferPointer = buffer)
            {
                hr = updateMemoryRegion(
                    callback2,
                    ToClrDataAddress(address),
                    (uint)buffer.Length,
                    bufferPointer);
            }

            if (hr < 0)
                return false;

            return true;
        }
        finally
        {
            delegate* unmanaged[MemberFunction]<nint, uint> release =
                (delegate* unmanaged[MemberFunction]<nint, uint>)(*(nint**)callback2)[2];
            release(callback2);
        }
    }

    internal ulong ToClrDataAddress(ulong address) =>
        pointerSize == sizeof(uint) ? (ulong)(long)(int)address : address;
}
