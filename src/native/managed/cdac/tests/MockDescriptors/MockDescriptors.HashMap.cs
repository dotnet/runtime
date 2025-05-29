// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class HashMap
    {
        private const uint HashMapSlotsPerBucket = 4;

        internal static readonly TypeFields HashMapFields = new TypeFields()
        {
            DataType = DataType.HashMap,
            Fields =
            [
                new (nameof(Data.HashMap.Buckets), DataType.pointer),
            ]
        };

        internal static TypeFields BucketFields(TargetTestHelpers helpers) => new TypeFields()
        {
            DataType = DataType.Bucket,
            Fields =
            [
                new(nameof(Data.Bucket.Keys), DataType.Unknown, HashMapSlotsPerBucket * (uint)helpers.PointerSize),
                new(nameof(Data.Bucket.Values), DataType.Unknown, HashMapSlotsPerBucket * (uint)helpers.PointerSize),
            ]
        };

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        internal MockMemorySpace.Builder Builder { get; }

        private const ulong DefaultAllocationRangeStart = 0x0003_0000;
        private const ulong DefaultAllocationRangeEnd = 0x0004_0000;

        // See g_rgPrimes in hash.cpp
        private static readonly uint[] PossibleSizes = [5, 11, 17, 23, 29, 37];

        private readonly MockMemorySpace.BumpAllocator _allocator;

        public HashMap(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public HashMap(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            Builder = builder;
            _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);
            Types = GetTypes(builder.TargetTestHelpers);
            Globals = GetGlobals(builder.TargetTestHelpers);
        }

        internal static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
        {
            return GetTypesForTypeFields(
                helpers,
                [
                    HashMapFields,
                    BucketFields(helpers),
                ]);
        }

        internal static (string Name, ulong Value)[] GetGlobals(TargetTestHelpers helpers)
        {
            return [
                (nameof(Constants.Globals.HashMapSlotsPerBucket), HashMapSlotsPerBucket),
                (nameof(Constants.Globals.HashMapValueMask), helpers.MaxSignedTargetAddress),
            ];
        }

        public TargetPointer CreateMap((TargetPointer Key, TargetPointer Value)[] entries)
        {
            Target.TypeInfo hashMapType = Types[DataType.HashMap];
            MockMemorySpace.HeapFragment map = _allocator.Allocate(hashMapType.Size!.Value, "HashMap");
            Builder.AddHeapFragment(map);
            PopulateMap(map.Address, entries);
            return map.Address;
        }

        public void PopulateMap(TargetPointer mapAddress, (TargetPointer Key, TargetPointer Value)[] entries)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;

            // HashMap::NewSize
            int requiredSlots = entries.Length * 3 / 2;
            uint size = PossibleSizes.Where(i => i > requiredSlots).First();

            // Allocate the buckets
            Target.TypeInfo bucketType = Types[DataType.Bucket];
            uint bucketSize = bucketType.Size!.Value;

            // First bucket is the number of buckets
            uint numBuckets = size + 1;
            uint totalBucketsSize = bucketSize * numBuckets;
            MockMemorySpace.HeapFragment buckets = _allocator.Allocate(totalBucketsSize, $"Buckets[{numBuckets}]");
            Builder.AddHeapFragment(buckets);
            helpers.Write(buckets.Data.AsSpan().Slice(0, sizeof(uint)), size);

            const int maxRetry = 8;
            foreach ((TargetPointer key, TargetPointer value) in entries)
            {
                ExecutionManagerHelpers.HashMapLookup.HashFunction(key, size, out uint seed, out uint increment);

                int tryCount = 0;
                while (tryCount < maxRetry)
                {
                    Span<byte> bucket = buckets.Data.AsSpan().Slice((int)(bucketSize * ((seed % size) + 1)));
                    if (TryAddEntryToBucket(bucket, key, value))
                        break;

                    seed += increment;
                    tryCount++;
                }

                if (tryCount >= maxRetry)
                    throw new InvalidOperationException("HashMap test helper does not handle re-hashing");
            }

            // Update the map to point at the buckets
            Target.TypeInfo hashMapType = Types[DataType.HashMap];
            Span<byte> map = Builder.BorrowAddressRange(mapAddress, (int)hashMapType.Size!.Value);
            helpers.WritePointer(map.Slice(hashMapType.Fields[nameof(Data.HashMap.Buckets)].Offset, helpers.PointerSize), buckets.Address);
        }

        public TargetPointer CreatePtrMap((TargetPointer Key, TargetPointer Value)[] entries)
        {
            // PtrHashMap shifts values right by one bit
            (TargetPointer Key, TargetPointer Value)[] ptrMapEntries = entries
                .Select(e => (e.Key, new TargetPointer(e.Value >> 1)))
                .ToArray();
            return CreateMap(ptrMapEntries);
        }

        public void PopulatePtrMap(TargetPointer mapAddress, (TargetPointer Key, TargetPointer Value)[] entries)
        {
            // PtrHashMap shifts values right by one bit
            (TargetPointer Key, TargetPointer Value)[] ptrMapEntries = entries
                .Select(e => (e.Key, new TargetPointer(e.Value >> 1)))
                .ToArray();
            PopulateMap(mapAddress, ptrMapEntries);
        }

        private bool TryAddEntryToBucket(Span<byte> bucket, TargetPointer key, TargetPointer value)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;
            Target.TypeInfo bucketType = Types[DataType.Bucket];
            for (int i = 0; i < HashMapSlotsPerBucket; i++)
            {
                Span<byte> keySpan = bucket.Slice(bucketType.Fields[nameof(Data.Bucket.Keys)].Offset + i * helpers.PointerSize, helpers.PointerSize);
                if (helpers.ReadPointer(keySpan) != (uint)ExecutionManagerHelpers.HashMapLookup.SpecialKeys.Empty)
                    continue;

                helpers.WritePointer(keySpan, key);
                helpers.WritePointer(bucket.Slice(bucketType.Fields[nameof(Data.Bucket.Values)].Offset + i * helpers.PointerSize, helpers.PointerSize), value);
                return true;
            }

            // Bucket::SetCollision
            ulong valueMask = Globals.Where(g => g.Name == nameof(Constants.Globals.HashMapValueMask)).Select(g => g.Value).First();

            // Collision bit
            Span<byte> firstValueSpan = bucket.Slice(bucketType.Fields[nameof(Data.Bucket.Values)].Offset, helpers.PointerSize);
            TargetPointer firstValue = helpers.ReadPointer(firstValueSpan);
            helpers.WritePointer(firstValueSpan, firstValue | ~valueMask);

            // Has free slots bit
            Span<byte> secondValueSpan = bucket.Slice(bucketType.Fields[nameof(Data.Bucket.Values)].Offset + helpers.PointerSize, helpers.PointerSize);
            TargetPointer secondValue = helpers.ReadPointer(secondValueSpan);
            helpers.WritePointer(secondValueSpan, secondValue & valueMask);

            return false;
        }
    }
}
