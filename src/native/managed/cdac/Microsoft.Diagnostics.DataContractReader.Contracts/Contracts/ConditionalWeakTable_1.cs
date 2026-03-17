// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ConditionalWeakTable_1 : IConditionalWeakTable
{
    private readonly Target _target;

    internal ConditionalWeakTable_1(Target target)
    {
        _target = target;
    }

    bool IConditionalWeakTable.TryGetValue(TargetPointer conditionalWeakTable, TargetPointer key, out TargetPointer value)
    {
        value = TargetPointer.Null;
        Data.ConditionalWeakTableObject cwt = _target.ProcessedData.GetOrAdd<Data.ConditionalWeakTableObject>(conditionalWeakTable);
        TargetPointer container = cwt.Container;
        Data.ConditionalWeakTableContainerObject cwtContainer = _target.ProcessedData.GetOrAdd<Data.ConditionalWeakTableContainerObject>(container);
        int hashCode = _target.Contracts.Object.TryGetHashCode(key);
        if (hashCode == 0)
            return false;

        hashCode &= int.MaxValue;

        // Read the buckets array
        Data.Array bucketsArray = _target.ProcessedData.GetOrAdd<Data.Array>(cwtContainer.Buckets);
        uint bucketCount = bucketsArray.NumComponents;

        int bucket = hashCode & (int)(bucketCount - 1);
        int entriesIndex = _target.Read<int>(bucketsArray.DataPointer + (ulong)(bucket * sizeof(int)));

        // Read the entries array
        Data.Array entriesArray = _target.ProcessedData.GetOrAdd<Data.Array>(cwtContainer.Entries);
        Target.TypeInfo entryTypeInfo = _target.GetTypeInfo(DataType.ConditionalWeakTableEntry);
        uint entrySize = entryTypeInfo.Size!.Value;

        while (entriesIndex != -1)
        {
            TargetPointer entryAddress = entriesArray.DataPointer + (ulong)((uint)entriesIndex * entrySize);
            Data.ConditionalWeakTableEntry entry = _target.ProcessedData.GetOrAdd<Data.ConditionalWeakTableEntry>(entryAddress);

            if (entry.HashCode == hashCode)
            {
                Data.ObjectHandle handle = _target.ProcessedData.GetOrAdd<Data.ObjectHandle>(entry.DepHnd);
                if (handle.Object == key)
                {
                    TargetNUInt extraInfo = _target.Contracts.GC.GetHandleExtraInfo(entry.DepHnd);

                    value = new TargetPointer(extraInfo.Value);
                    return true;
                }
            }

            entriesIndex = entry.Next;
        }

        return false;
    }
}
