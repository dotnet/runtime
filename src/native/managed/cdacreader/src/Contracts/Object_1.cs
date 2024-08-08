// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Object_1 : IObject
{
    private readonly Target _target;
    private readonly ulong _methodTableOffset;
    private readonly byte _objectToMethodTableUnmask;
    private readonly TargetPointer _stringMethodTable;
    private readonly TargetPointer _syncTableEntries;

    private static class SyncBlockValue
    {
        [Flags]
        public enum Bits
        {
            // Value represents either the hash code or sync block index (bits 0-25)
            // - IsHashCodeOrSyncBlockIndex and IsHashCode are set: rest of the value is the hash code.
            // - IsHashCodeOrSyncBlockIndex set, IsHashCode not set: rest of the value is the sync block index
            IsHashCodeOrSyncBlockIndex = 0x08000000,
            IsHashCode = 0x04000000,
        }

        public const uint SyncBlockIndexMask = (1 << 26) - 1;
    }

    internal Object_1(Target target, ulong methodTableOffset, byte objectToMethodTableUnmask, TargetPointer stringMethodTable, TargetPointer syncTableEntries)
    {
        _target = target;
        _methodTableOffset = methodTableOffset;
        _stringMethodTable = stringMethodTable;
        _objectToMethodTableUnmask = objectToMethodTableUnmask;
        _syncTableEntries = syncTableEntries;
    }

    public TargetPointer GetMethodTableAddress(TargetPointer address)
    {
        TargetPointer mt = _target.ReadPointer(address + _methodTableOffset);
        return mt.Value & (ulong)~_objectToMethodTableUnmask;
    }

    string IObject.GetStringValue(TargetPointer address)
    {
        TargetPointer mt = GetMethodTableAddress(address);
        if (mt != _stringMethodTable)
            throw new ArgumentException("Address does not represent a string object", nameof(address));

        Data.String str = _target.ProcessedData.GetOrAdd<Data.String>(address);
        Span<byte> span = stackalloc byte[(int)str.StringLength * sizeof(char)];
        _target.ReadBuffer(str.FirstChar, span);
        return new string(MemoryMarshal.Cast<byte, char>(span));
    }

    public TargetPointer GetArrayData(TargetPointer address, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds)
    {
        TargetPointer mt = GetMethodTableAddress(address);
        Contracts.IRuntimeTypeSystem typeSystemContract = _target.Contracts.RuntimeTypeSystem;
        TypeHandle typeHandle = typeSystemContract.GetTypeHandle(mt);
        uint rank;
        if (!typeSystemContract.IsArray(typeHandle, out rank))
            throw new ArgumentException("Address does not represent an array object", nameof(address));

        Data.Array array = _target.ProcessedData.GetOrAdd<Data.Array>(address);
        count = array.NumComponents;

        Target.TypeInfo arrayTypeInfo = _target.GetTypeInfo(DataType.Array);
        CorElementType corType = typeSystemContract.GetSignatureCorElementType(typeHandle);
        Debug.Assert(corType is CorElementType.Array or CorElementType.SzArray);
        if (corType == CorElementType.Array)
        {
            // Multi-dimensional - has bounds as part of the array object
            // The object is allocated with:
            //   << fields that are part of the array type info >>
            //   int32_t bounds[rank];
            //   int32_t lowerBounds[rank];
            boundsStart = address + (ulong)arrayTypeInfo.Size!;
            lowerBounds = boundsStart + (rank * sizeof(int));
        }
        else
        {
            // Single-dimensional, zero-based - doesn't have bounds
            boundsStart = address + (ulong)arrayTypeInfo.Fields["m_NumComponents"].Offset;
            lowerBounds = _target.ReadGlobalPointer(Constants.Globals.ArrayBoundsZero);
        }

        // Sync block is before `this` pointer, so substract the object header size
        ulong dataOffset = typeSystemContract.GetBaseSize(typeHandle) - _target.ReadGlobal<uint>(Constants.Globals.ObjectHeaderSize);
        return address + dataOffset;
    }

    public bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw)
    {
        rcw = TargetPointer.Null;
        ccw = TargetPointer.Null;

        Data.SyncBlock? syncBlock = GetSyncBlock(address);
        if (syncBlock == null)
            return false;

        Data.InteropSyncBlockInfo? interopInfo = syncBlock.InteropInfo;
        if (interopInfo == null)
            return false;

        rcw = interopInfo.RCW;
        ccw = interopInfo.CCW;
        return rcw != TargetPointer.Null || ccw != TargetPointer.Null;
    }

    private Data.SyncBlock? GetSyncBlock(TargetPointer address)
    {
        uint syncBlockValue = _target.Read<uint>(address - _target.ReadGlobal<ushort>(Constants.Globals.SyncBlockValueToObjectOffset));

        // Check if the sync block value represents a sync block index
        if ((syncBlockValue & (uint)(SyncBlockValue.Bits.IsHashCodeOrSyncBlockIndex | SyncBlockValue.Bits.IsHashCode)) != (uint)SyncBlockValue.Bits.IsHashCodeOrSyncBlockIndex)
            return null;

        // Get the offset into the sync table entries
        uint index = syncBlockValue & SyncBlockValue.SyncBlockIndexMask;
        ulong offsetInSyncTableEntries = index * (ulong)_target.GetTypeInfo(DataType.SyncTableEntry).Size!;
        Data.SyncTableEntry entry = _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + offsetInSyncTableEntries);
        return entry.SyncBlock;
    }
}
