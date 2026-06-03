// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct ConditionalWeakTable_1 : IConditionalWeakTable
{
    private readonly Target _target;

    internal ConditionalWeakTable_1(Target target)
    {
        _target = target;
    }

    bool IConditionalWeakTable.TryGetValue(TargetPointer conditionalWeakTable, TargetPointer key, out TargetPointer value)
    {
        value = TargetPointer.Null;

        // Read _container from the CWT object and _buckets/_entries from the Container.
        Data.ConditionalWeakTable cwt = _target.ProcessedData.GetOrAdd<Data.ConditionalWeakTable>(conditionalWeakTable);
        Data.ConditionalWeakTableContainer container = _target.ProcessedData.GetOrAdd<Data.ConditionalWeakTableContainer>(cwt.Container);

        int hashCode = _target.Contracts.Object.TryGetHashCode(key);
        if (hashCode == 0)
            return false;

        hashCode &= int.MaxValue;

        Data.Array bucketsArray = _target.ProcessedData.GetOrAdd<Data.Array>(container.Buckets);
        uint bucketCount = bucketsArray.NumComponents;

        int bucket = hashCode & (int)(bucketCount - 1);
        int entriesIndex = _target.Read<int>(bucketsArray.DataPointer + (ulong)(bucket * sizeof(int)));

        Data.Array entriesArray = _target.ProcessedData.GetOrAdd<Data.Array>(container.Entries);
        TargetPointer entriesMT = _target.Contracts.Object.GetMethodTableAddress(container.Entries);
        TypeHandle entriesTypeHandle = _target.Contracts.RuntimeTypeSystem.GetTypeHandle(entriesMT);
        uint entrySize = _target.Contracts.RuntimeTypeSystem.GetComponentSize(entriesTypeHandle);

        while (entriesIndex != -1)
        {
            TargetPointer entryAddress = entriesArray.DataPointer + (ulong)((uint)entriesIndex * entrySize);
            Data.ConditionalWeakTableEntry entry = _target.ProcessedData.GetOrAdd<Data.ConditionalWeakTableEntry>(entryAddress);

            if (entry.HashCode == hashCode)
            {
                Data.ObjectHandle handle = _target.ProcessedData.GetOrAdd<Data.ObjectHandle>(entry.DepHndAddress);
                if (handle.Object == key)
                {
                    TargetNUInt extraInfo = _target.Contracts.GC.GetHandleExtraInfo(handle.Handle);
                    value = new TargetPointer(extraInfo.Value);

                    return true;
                }
            }

            entriesIndex = entry.Next;
        }

        return false;
    }
}
