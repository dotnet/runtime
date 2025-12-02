// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal sealed class HashMapLookup
{
    internal enum SpecialKeys : uint
    {
        Empty = 0,
        Deleted = 1,
        InvalidEntry = unchecked((uint)~0),
    }

    public static HashMapLookup Create(Target target)
        => new HashMapLookup(target);

    private readonly Target _target;
    private readonly ulong _valueMask;

    private HashMapLookup(Target target)
    {
        _target = target;
        _valueMask = target.ReadGlobal<ulong>(Constants.Globals.HashMapValueMask);
    }

    public TargetPointer GetValue(TargetPointer mapAddress, TargetPointer key)
    {
        Data.HashMap map = _target.ProcessedData.GetOrAdd<Data.HashMap>(mapAddress);

        // First pointer of Buckets is actually the number of buckets
        uint size = _target.Read<uint>(map.Buckets);
        HashFunction(key, size, out uint seed, out uint increment);

        // HashMap::LookupValue
        uint bucketSize = _target.GetTypeInfo(DataType.Bucket).Size!.Value;
        TargetPointer buckets = map.Buckets + bucketSize;
        for (int i = 0; i < size; i++)
        {
            Data.Bucket bucket = _target.ProcessedData.GetOrAdd<Data.Bucket>(buckets + bucketSize * (seed % size));
            for (int slotIdx = 0; slotIdx < bucket.Keys.Length; slotIdx++)
            {
                if (bucket.Keys[slotIdx] != key)
                    continue;

                return bucket.Values[slotIdx] & _valueMask;
            }

            seed += increment;

            // We didn't find a match and there is no collision
            if (!IsCollision(bucket))
                break;
        }

        return new TargetPointer((uint)SpecialKeys.InvalidEntry);
    }

    internal static void HashFunction(TargetPointer key, uint size, out uint seed, out uint increment)
    {
        // HashMap::HashFunction
        seed = (uint)(key >> 2);
        increment = (uint)(1 + (((uint)(key >> 5) + 1) % (size - 1)));
        Debug.Assert(increment > 0 && increment < size);
    }

    private bool IsCollision(Data.Bucket bucket)
    {
        return (bucket.Values[0] & ~_valueMask) != 0;
    }
}

internal sealed class PtrHashMapLookup
{
    public static PtrHashMapLookup Create(Target target)
        => new PtrHashMapLookup(target);

    private readonly HashMapLookup _lookup;
    private PtrHashMapLookup(Target target)
    {
        _lookup = HashMapLookup.Create(target);
    }

    public TargetPointer GetValue(TargetPointer mapAddress, TargetPointer key)
    {
        // See PtrHashMap::SanitizeKey in hash.h
        key = key > (uint)HashMapLookup.SpecialKeys.Deleted ? key : key + 100;

        TargetPointer value = _lookup.GetValue(mapAddress, key);

        // PtrHashMap shifts values right by one bit when storing. See PtrHashMap::LookupValue in hash.h
        return value != (uint)HashMapLookup.SpecialKeys.InvalidEntry
            ? value << 1
            : value;
    }
}
