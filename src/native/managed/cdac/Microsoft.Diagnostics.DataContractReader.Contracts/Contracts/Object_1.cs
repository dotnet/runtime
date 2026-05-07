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
    private readonly uint _syncBlockIsHashOrSyncBlockIndex;
    private readonly uint _syncBlockIsHashCode;
    private readonly uint _syncBlockHashCodeMask;
    private readonly uint _syncBlockIndexMask;

    internal Object_1(Target target)
    {
        _target = target;
        _methodTableOffset = (ulong)target.GetTypeInfo(DataType.Object).Fields["m_pMethTab"].Offset;
        _objectToMethodTableUnmask = target.ReadGlobal<byte>(Constants.Globals.ObjectToMethodTableUnmask);
        _stringMethodTable = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.StringMethodTable));
        _syncBlockIsHashOrSyncBlockIndex = target.ReadGlobal<uint>(Constants.Globals.SyncBlockIsHashOrSyncBlockIndex);
        _syncBlockIsHashCode = target.ReadGlobal<uint>(Constants.Globals.SyncBlockIsHashCode);
        _syncBlockHashCodeMask = target.ReadGlobal<uint>(Constants.Globals.SyncBlockHashCodeMask);
        _syncBlockIndexMask = target.ReadGlobal<uint>(Constants.Globals.SyncBlockIndexMask);
    }

    public TargetPointer GetMethodTableAddress(TargetPointer address)
    {
        TargetPointer mt = _target.ReadPointer(address + _methodTableOffset);
        return mt.Value & (ulong)~_objectToMethodTableUnmask;
    }

    string IObject.GetStringValue(TargetPointer address)
    {
        TargetPointer mt = GetMethodTableAddress(address);
        if (mt == TargetPointer.Null)
            throw new ArgumentException("Address represents a set-free object");
        if (mt != _stringMethodTable)
            throw new ArgumentException("Address does not represent a string object", nameof(address));

        Data.String str = _target.ProcessedData.GetOrAdd<Data.String>(address);
        if (str.StringLength == 0)
            return string.Empty;

        Span<byte> span = stackalloc byte[(int)str.StringLength * sizeof(char)];
        _target.ReadBuffer(str.FirstChar, span);
        return new string(MemoryMarshal.Cast<byte, char>(span));
    }

    public TargetPointer GetArrayData(TargetPointer address, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds)
    {
        TargetPointer mt = GetMethodTableAddress(address);
        if (mt == TargetPointer.Null)
            throw new ArgumentException("Address represents a set-free object");
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
            boundsStart = address + (ulong)arrayTypeInfo.Fields[Constants.FieldNames.Array.NumComponents].Offset;
            lowerBounds = _target.ReadGlobalPointer(Constants.Globals.ArrayBoundsZero);
        }

        // Sync block is before `this` pointer, so substract the object header size
        ulong dataOffset = typeSystemContract.GetBaseSize(typeHandle) - _target.GetTypeInfo(DataType.ObjectHeader).Size!.Value;
        return address + dataOffset;
    }

    public bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf)
    {
        rcw = TargetPointer.Null;
        ccw = TargetPointer.Null;
        ccf = TargetPointer.Null;

        TargetPointer syncBlockPtr = GetSyncBlockAddress(address);
        if (syncBlockPtr == TargetPointer.Null)
            return false;

        return _target.Contracts.SyncBlock.GetBuiltInComData(syncBlockPtr, out rcw, out ccw, out ccf);
    }

    int IObject.TryGetHashCode(TargetPointer address)
    {
        ulong objectHeaderSize = _target.GetTypeInfo(DataType.ObjectHeader).Size!.Value;
        Data.ObjectHeader header = _target.ProcessedData.GetOrAdd<Data.ObjectHeader>(address - objectHeaderSize);
        uint syncBlockValue = header.SyncBlockValue;

        if ((syncBlockValue & _syncBlockIsHashOrSyncBlockIndex) != 0)
        {
            if ((syncBlockValue & _syncBlockIsHashCode) != 0)
            {
                return (int)(syncBlockValue & _syncBlockHashCodeMask);
            }
            else
            {
                TargetPointer syncBlockPtr = GetSyncBlockAddress(address);
                if (syncBlockPtr != TargetPointer.Null)
                {
                    Data.SyncBlock syncBlock = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlockPtr);
                    return (int)syncBlock.HashCode;
                }
            }
        }

        return 0;
    }

    public TargetPointer GetSyncBlockAddress(TargetPointer address)
    {
        ulong objectHeaderSize = _target.GetTypeInfo(DataType.ObjectHeader).Size!.Value;
        Data.ObjectHeader header = _target.ProcessedData.GetOrAdd<Data.ObjectHeader>(address - objectHeaderSize);
        uint syncBlockValue = header.SyncBlockValue;

        // Check if the sync block value represents a sync block index (not a hash code)
        if ((syncBlockValue & (_syncBlockIsHashCode | _syncBlockIsHashOrSyncBlockIndex))
                != _syncBlockIsHashOrSyncBlockIndex)
            return TargetPointer.Null;

        uint index = syncBlockValue & _syncBlockIndexMask;
        return _target.Contracts.SyncBlock.GetSyncBlock(index);
    }
}
