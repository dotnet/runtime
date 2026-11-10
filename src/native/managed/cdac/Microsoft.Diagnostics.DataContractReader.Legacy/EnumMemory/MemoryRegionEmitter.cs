// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

internal sealed unsafe class MemoryRegionEmitter(nint callback, uint pointerSize, DumpType dumpType = DumpType.Mini)
{
    private const uint MaxMiniDumpRegion = 4 * 1024 * 1024 - 3;
    private static readonly Guid s_ICLRDataEnumMemoryRegionsCallback2_Iid = new("3721A26F-8B91-4D98-A388-DB17B356FADB");

    private readonly delegate* unmanaged[MemberFunction]<nint, Guid*, nint*, int> _queryInterface =
        (delegate* unmanaged[MemberFunction]<nint, Guid*, nint*, int>)(*(nint**)callback)[0];
    // ICLRDataEnumMemoryRegionsCallback::EnumMemoryRegion follows the three IUnknown vtable slots.
    private readonly delegate* unmanaged[MemberFunction]<nint, ulong, uint, int> _enumMemoryRegion =
        (delegate* unmanaged[MemberFunction]<nint, ulong, uint, int>)(*(nint**)callback)[3];
    // Both Start and End are inclusive.
    private readonly List<(ulong Start, ulong End)> _metadataRanges = [];

    public int Result { get; private set; }

    public void Add(ulong address, uint size) => Add(address, (ulong)size);

    public void Add(ulong address, ulong size)
    {
        if (address == 0 || size == 0)
            return;

        // Like the native DAC, treat unusually large mini/triage regions as likely target corruption
        // and truncate them to keep dumps small. The distinctive 4 MiB minus 3 size identifies truncation.
        if (dumpType is DumpType.Mini or DumpType.Triage)
            size = Math.Min(size, MaxMiniDumpRegion);

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
        ulong start = range.Address.Value;
        if (start == 0 || range.Size == 0)
            return;

        ulong end = checked(start + (range.Size - 1));
        int index = FindMetadataRangeIndex(start);
        // Subtract from nonzero starts to merge adjacent ranges without overflowing an end.
        if (index >= 0 && _metadataRanges[index].End >= start - 1)
        {
            if (_metadataRanges[index].End >= end)
                // The current range already covers the new range.
                return;

            // Merge with the current range by extending its end to cover the new range.
            start = _metadataRanges[index].Start;
        }
        else
        {
            index++;
        }

        int next = index;
        while (next < _metadataRanges.Count && _metadataRanges[next].Start - 1 <= end)
        {
            end = Math.Max(end, _metadataRanges[next].End);
            next++;
        }

        if (next == index)
            _metadataRanges.Insert(index, (start, end));
        else
        {
            _metadataRanges[index] = (start, end);
            _metadataRanges.RemoveRange(index + 1, next - index - 1);
        }
    }

    public bool ShouldEmitTargetRead(ulong address, ulong size)
    {
        int index = FindMetadataRangeIndex(address);
        if (index < 0)
            return true;

        ulong end = _metadataRanges[index].End;
        if (size == 0)
            return address > end && address - end > 1;

        return address > end || size - 1 > end - address;
    }

    // Returns the last range whose start is <= address, or -1 if none. The caller must still check its end.
    private int FindMetadataRangeIndex(ulong address)
    {
        int low = 0;
        int high = _metadataRanges.Count;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (_metadataRanges[middle].Start <= address)
                low = middle + 1;
            else
                high = middle;
        }

        return low - 1;
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
