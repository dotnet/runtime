// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct ConditionalWeakTable_1 : IConditionalWeakTable
{
    private const string CWTNamespace = "System.Runtime.CompilerServices";
    private const string CWTTypeName = "ConditionalWeakTable`2";
    private const string ContainerTypeName = "ConditionalWeakTable`2+Container";
    private const string EntryTypeName = "ConditionalWeakTable`2+Entry";
    private const string ContainerFieldName = "_container";
    private const string BucketsFieldName = "_buckets";
    private const string EntriesFieldName = "_entries";
    private const string HashCodeFieldName = "HashCode";
    private const string NextFieldName = "Next";
    private const string DepHndFieldName = "depHnd";
    private uint? _containerFieldOffset = null;
    private uint? _bucketsFieldOffset = null;
    private uint? _entriesFieldOffset = null;
    private uint? _hashCodeFieldOffset = null;
    private uint? _nextFieldOffset = null;
    private uint? _depHndFieldOffset = null;

    private readonly Target _target;

    internal ConditionalWeakTable_1(Target target)
    {
        _target = target;
    }

    bool IConditionalWeakTable.TryGetValue(TargetPointer conditionalWeakTable, TargetPointer key, out TargetPointer value)
    {
        value = TargetPointer.Null;
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

        // Read _container field from the ConditionalWeakTable object
        if (_containerFieldOffset is null)
        {
            rts.GetCoreLibFieldDescAndDef(CWTNamespace, CWTTypeName, ContainerFieldName, out TargetPointer containerFieldDescAddr, out FieldDefinition containerFieldDef);
            _containerFieldOffset = rts.GetFieldDescOffset(containerFieldDescAddr, containerFieldDef);
        }
        Data.Object cwtObj = _target.ProcessedData.GetOrAdd<Data.Object>(conditionalWeakTable);
        TargetPointer container = _target.ReadPointer(cwtObj.Data + _containerFieldOffset.Value);

        // Read _buckets and _entries fields from the Container object
        if (_bucketsFieldOffset is null)
        {
            rts.GetCoreLibFieldDescAndDef(CWTNamespace, ContainerTypeName, BucketsFieldName, out TargetPointer bucketsFieldDescAddr, out FieldDefinition bucketsFieldDef);
            _bucketsFieldOffset = rts.GetFieldDescOffset(bucketsFieldDescAddr, bucketsFieldDef);
        }
        if (_entriesFieldOffset is null)
        {
            rts.GetCoreLibFieldDescAndDef(CWTNamespace, ContainerTypeName, EntriesFieldName, out TargetPointer entriesFieldDescAddr, out FieldDefinition entriesFieldDef);
            _entriesFieldOffset = rts.GetFieldDescOffset(entriesFieldDescAddr, entriesFieldDef);
        }

        Data.Object containerObj = _target.ProcessedData.GetOrAdd<Data.Object>(container);
        TargetPointer bucketsPtr = _target.ReadPointer(containerObj.Data + _bucketsFieldOffset.Value);
        TargetPointer entriesPtr = _target.ReadPointer(containerObj.Data + _entriesFieldOffset.Value);

        int hashCode = _target.Contracts.Object.TryGetHashCode(key);
        if (hashCode == 0)
            return false;

        hashCode &= int.MaxValue;

        // Read the buckets array
        Data.Array bucketsArray = _target.ProcessedData.GetOrAdd<Data.Array>(bucketsPtr);
        uint bucketCount = bucketsArray.NumComponents;

        int bucket = hashCode & (int)(bucketCount - 1);
        int entriesIndex = _target.Read<int>(bucketsArray.DataPointer + (ulong)(bucket * sizeof(int)));

        // Resolve Entry field offsets via RuntimeTypeSystem
        if (_hashCodeFieldOffset is null)
        {
            rts.GetCoreLibFieldDescAndDef(CWTNamespace, EntryTypeName, HashCodeFieldName, out TargetPointer hashCodeFieldDescAddr, out FieldDefinition hashCodeFieldDef);
            _hashCodeFieldOffset = rts.GetFieldDescOffset(hashCodeFieldDescAddr, hashCodeFieldDef);
        }
        if (_nextFieldOffset is null)
        {
            rts.GetCoreLibFieldDescAndDef(CWTNamespace, EntryTypeName, NextFieldName, out TargetPointer nextFieldDescAddr, out FieldDefinition nextFieldDef);
            _nextFieldOffset = rts.GetFieldDescOffset(nextFieldDescAddr, nextFieldDef);
        }
        if (_depHndFieldOffset is null)
        {
            rts.GetCoreLibFieldDescAndDef(CWTNamespace, EntryTypeName, DepHndFieldName, out TargetPointer depHndFieldDescAddr, out FieldDefinition depHndFieldDef);
            _depHndFieldOffset = rts.GetFieldDescOffset(depHndFieldDescAddr, depHndFieldDef);
        }

        // Get entry size from the entries array's component size
        Data.Array entriesArray = _target.ProcessedData.GetOrAdd<Data.Array>(entriesPtr);
        TargetPointer entriesMT = _target.Contracts.Object.GetMethodTableAddress(entriesPtr);
        TypeHandle entriesTypeHandle = rts.GetTypeHandle(entriesMT);
        uint entrySize = rts.GetComponentSize(entriesTypeHandle);

        while (entriesIndex != -1)
        {
            TargetPointer entryAddress = entriesArray.DataPointer + (ulong)((uint)entriesIndex * entrySize);

            int entryHashCode = _target.Read<int>(entryAddress + _hashCodeFieldOffset.Value);
            if (entryHashCode == hashCode)
            {
                TargetPointer depHnd = _target.ReadPointer(entryAddress + _depHndFieldOffset.Value);
                Data.ObjectHandle handle = _target.ProcessedData.GetOrAdd<Data.ObjectHandle>(depHnd);
                if (handle.Object == key)
                {
                    TargetNUInt extraInfo = _target.Contracts.GC.GetHandleExtraInfo(depHnd);
                    value = new TargetPointer(extraInfo.Value);

                    return true;
                }
            }

            entriesIndex = _target.Read<int>(entryAddress + _nextFieldOffset.Value);
        }

        return false;
    }
}
