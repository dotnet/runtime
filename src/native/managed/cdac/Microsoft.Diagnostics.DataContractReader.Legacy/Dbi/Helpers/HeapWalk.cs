// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal sealed class HeapWalk : IEnum<COR_HEAPOBJECT>
{
    private readonly IGC _gc;
    private readonly IRuntimeTypeSystem _rts;
    private readonly TargetPointer _freeObjectMT;
    private readonly LinearReadCache _cache;
    private readonly uint _numComponentsOffsetArray;
    private readonly uint _numComponentsOffsetString;
    private readonly uint _methodTableOffset;
    private readonly ulong _methodTableMask;

    public IEnumerator<COR_HEAPOBJECT> Enumerator { get; }
    public nuint LegacyHandle { get; set; } = 0;

    public HeapWalk(Target target)
    {
        _gc = target.Contracts.GC;
        _rts = target.Contracts.RuntimeTypeSystem;
        _freeObjectMT = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.FreeObjectMethodTable));
        _cache = new LinearReadCache(target);
        // use these fields directly instead of through RuntimeTypeSystem so that we can use our cache that we really only need for heap walking
        _numComponentsOffsetArray = (uint)target.GetTypeInfo(DataType.Array).Fields[Constants.FieldNames.Array.NumComponents].Offset;
        _numComponentsOffsetString = (uint)target.GetTypeInfo(DataType.String).Fields["m_StringLength"].Offset;
        _methodTableOffset = (uint)target.GetTypeInfo(DataType.Object).Fields["m_pMethTab"].Offset;
        _methodTableMask = (ulong)~target.ReadGlobal<byte>(Constants.Globals.ObjectToMethodTableUnmask);

        Enumerator = Walk().GetEnumerator();
    }

    private IEnumerable<COR_HEAPOBJECT> Walk()
    {
        bool pendingFailure = false;
        foreach ((GCHeapSegmentInfo seg, GCHeapData _) in EnumerateAllSegments())
        {
            if (seg.Start.Value >= seg.End.Value)
                continue;

            TargetPointer currentObj = _gc.GetPotentialNextObjectAddress(seg.Start, 0, seg);
            while (currentObj.Value < seg.End.Value)
            {
                if (!_cache.TryReadPointer(currentObj.Value + _methodTableOffset, out TargetPointer mt))
                {
                    pendingFailure = true;
                    break;
                }
                mt = new TargetPointer(mt.Value & _methodTableMask);

                if (!TryGetObjectSize(currentObj, mt, out ulong size) || size == 0)
                {
                    pendingFailure = true;
                    break;
                }

                size = _gc.AlignObjectSize(size, seg.Generation);
                if (currentObj.Value + size > seg.End.Value)
                {
                    pendingFailure = true;
                    break;
                }

                // Advance past this object (and any allocation-context tail) in a single call.
                TargetPointer nextObj = _gc.GetPotentialNextObjectAddress(currentObj, size, seg);
                if (nextObj.Value > seg.End.Value)
                {
                    break;
                }

                TargetPointer reportedAddr = currentObj;
                bool isFree = mt == _freeObjectMT;

                currentObj = nextObj;

                if (isFree)
                    continue;

                if (pendingFailure)
                {
                    // sentinel, show that we have a read failure and then yield the next valid object
                    yield return default;
                    pendingFailure = false;
                }

                yield return new COR_HEAPOBJECT
                {
                    address = reportedAddr.Value,
                    size = size,
                    type = new COR_TYPEID { token1 = mt.Value, token2 = 0 },
                };
            }
        }

        // Trailing corruption: if validation failed after the last successful yield (or
        // before any yield at all), show a sentinel now.
        if (pendingFailure)
            yield return default;
    }

    private IEnumerable<(GCHeapSegmentInfo Segment, GCHeapData Heap)> EnumerateAllSegments()
    {
        string[] gcIdentifiers = _gc.GetGCIdentifiers();
        bool isWorkstation = gcIdentifiers.Contains(GCIdentifiers.Workstation);
        foreach (GCHeapData heap in EnumerateHeaps(_gc, isWorkstation))
        {
            foreach (GCHeapSegmentInfo seg in _gc.EnumerateHeapSegments(heap))
            {
                yield return (seg, heap);
            }
        }
    }

    private bool TryGetObjectSize(TargetPointer objAddr, TargetPointer mt, out ulong size)
    {
        size = 0;
        try
        {
            TypeHandle handle = _rts.GetTypeHandle(mt);
            ulong baseSize = _rts.GetBaseSize(handle);
            uint componentSize = _rts.GetComponentSize(handle);
            uint numComponentsOffset = 0;
            if (componentSize != 0)
            {
                if (_rts.IsArray(handle, out _) || _rts.IsFreeObjectMethodTable(handle))
                    numComponentsOffset = _numComponentsOffsetArray;
                else if (_rts.IsString(handle))
                    numComponentsOffset = _numComponentsOffsetString;
                else
                    return false; // unrecognized component type
                if (!_cache.TryReadUInt32(objAddr.Value + numComponentsOffset, out uint numComponents))
                    return false;
                baseSize += (ulong)componentSize * numComponents;
            }
            size = baseSize;
            return true;
        }
        catch
        {
            // The MT may be corrupt — surface as a read failure.
            return false;
        }
    }

    private static IEnumerable<GCHeapData> EnumerateHeaps(IGC gc, bool isWorkstation)
    {
        if (isWorkstation)
        {
            yield return gc.GetHeapData();
        }
        else
        {
            foreach (TargetPointer heapAddress in gc.GetGCHeaps())
                yield return gc.GetHeapData(heapAddress);
        }
    }

    // Linear page cache used by the per-object heap walk.
    private sealed class LinearReadCache
    {
        // Typical page size
        private const uint PageSize = 0x1000;

        private readonly Target _target;
        private readonly byte[] _page = new byte[PageSize];
        private ulong _currPageStart;
        private uint _currPageSize;

        public LinearReadCache(Target target)
        {
            _target = target;
        }

        public bool TryReadPointer(ulong addr, out TargetPointer value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            buffer = buffer.Slice(0, _target.PointerSize);
            if (!TryRead(addr, buffer))
            {
                value = TargetPointer.Null;
                return false;
            }
            value = _target.ReadPointerFromSpan(buffer);
            return true;
        }

        public bool TryReadUInt32(ulong addr, out uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            if (!TryRead(addr, buffer))
            {
                value = 0;
                return false;
            }
            value = _target.IsLittleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(buffer)
                : BinaryPrimitives.ReadUInt32BigEndian(buffer);
            return true;
        }

        private bool TryRead(ulong addr, Span<byte> dest)
        {
            // If the request misses the currently-cached page, try to load the page
            // containing it. If that fails (e.g. the page is unmapped), or the request
            // straddles the end of the cached page, fall back to a direct read.
            if (addr < _currPageStart || addr - _currPageStart >= _currPageSize)
            {
                if (!MoveToPage(addr))
                    return DirectRead(addr, dest);
            }

            ulong offset = addr - _currPageStart;
            if (offset + (ulong)dest.Length > _currPageSize)
                return DirectRead(addr, dest);

            _page.AsSpan((int)offset, dest.Length).CopyTo(dest);
            return true;
        }

        private bool MoveToPage(ulong addr)
        {
            ulong pageStart = addr - (addr % PageSize);
            try
            {
                _target.ReadBuffer(pageStart, _page.AsSpan(0, (int)PageSize));
                _currPageStart = pageStart;
                _currPageSize = PageSize;
                return true;
            }
            catch
            {
                _currPageStart = 0;
                _currPageSize = 0;
                return false;
            }
        }

        private bool DirectRead(ulong addr, Span<byte> dest)
        {
            try
            {
                _target.ReadBuffer(addr, dest);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
