// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockHashMap : TypedView
{
    private const string BucketsFieldName = "Buckets";

    public static Layout<MockHashMap> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("HashMap", architecture)
            .AddPointerField(BucketsFieldName)
            .Build<MockHashMap>();

    public ulong Buckets
    {
        get => ReadPointerField(BucketsFieldName);
        set => WritePointerField(BucketsFieldName, value);
    }
}

internal sealed class MockHashMapBucket : TypedView
{
    private const string KeysFieldName = "Keys";
    private const string ValuesFieldName = "Values";

    public const int SlotsPerBucket = 4;

    public static Layout<MockHashMapBucket> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("Bucket", architecture)
            .AddField(KeysFieldName, checked(SlotsPerBucket * (architecture.Is64Bit ? sizeof(ulong) : sizeof(uint))))
            .AddField(ValuesFieldName, checked(SlotsPerBucket * (architecture.Is64Bit ? sizeof(ulong) : sizeof(uint))))
            .Build<MockHashMapBucket>();

    public ulong GetKey(int slot)
        => ReadPointer(GetSlotSlice(KeysFieldName, slot));

    public void SetKey(int slot, ulong value)
        => WritePointer(GetSlotSlice(KeysFieldName, slot), value);

    public ulong GetValue(int slot)
        => ReadPointer(GetSlotSlice(ValuesFieldName, slot));

    public void SetValue(int slot, ulong value)
        => WritePointer(GetSlotSlice(ValuesFieldName, slot), value);

    private Span<byte> GetSlotSlice(string fieldName, int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, SlotsPerBucket);

        int pointerSize = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        return GetFieldSlice(fieldName).Slice(slot * pointerSize, pointerSize);
    }
}

internal sealed class MockHashMapBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0003_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0004_0000;

    // See g_rgPrimes in hash.cpp
    private static readonly uint[] PossibleSizes = [5, 11, 17, 23, 29, 37];

    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockHashMap> HashMapLayout { get; }
    internal Layout<MockHashMapBucket> BucketLayout { get; }
    internal ulong HashMapSlotsPerBucket { get; }
    internal ulong HashMapValueMask { get; }

    private readonly MockMemorySpace.BumpAllocator _allocator;

    public MockHashMapBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    public MockHashMapBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Builder = builder;
        _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);
        HashMapLayout = MockHashMap.CreateLayout(builder.TargetTestHelpers.Arch);
        BucketLayout = MockHashMapBucket.CreateLayout(builder.TargetTestHelpers.Arch);
        HashMapSlotsPerBucket = MockHashMapBucket.SlotsPerBucket;
        HashMapValueMask = builder.TargetTestHelpers.MaxSignedTargetAddress;
    }

    public ulong CreateMap((ulong Key, ulong Value)[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        MockHashMap map = HashMapLayout.Create(AllocateAndAdd((ulong)HashMapLayout.Size, "HashMap"));
        PopulateMap(map.Address, entries);
        return map.Address;
    }

    public void PopulateMap(ulong mapAddress, (ulong Key, ulong Value)[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        // HashMap::NewSize
        int requiredSlots = entries.Length * 3 / 2;
        uint size = PossibleSizes.First(i => i > requiredSlots);

        uint bucketSize = checked((uint)BucketLayout.Size);

        // First bucket is the number of buckets
        uint numBuckets = size + 1;
        uint totalBucketsSize = checked(bucketSize * numBuckets);
        MockMemorySpace.HeapFragment buckets = AllocateAndAdd(totalBucketsSize, $"Buckets[{numBuckets}]");
        Builder.TargetTestHelpers.Write(buckets.Data.AsSpan().Slice(0, sizeof(uint)), size);

        const int MaxRetry = 8;
        foreach ((ulong key, ulong value) in entries)
        {
            ExecutionManagerHelpers.HashMapLookup.HashFunction(key, size, out uint seed, out uint increment);

            int tryCount = 0;
            while (tryCount < MaxRetry)
            {
                int bucketIndex = checked((int)((seed % size) + 1));
                MockHashMapBucket bucket = BucketLayout.Create(
                    buckets.Data.AsMemory(checked(bucketIndex * (int)bucketSize), BucketLayout.Size),
                    buckets.Address + (ulong)bucketIndex * bucketSize);

                if (TryAddEntryToBucket(bucket, key, value))
                {
                    break;
                }

                seed += increment;
                tryCount++;
            }

            if (tryCount >= MaxRetry)
            {
                throw new InvalidOperationException("HashMap test helper does not handle re-hashing");
            }
        }

        MockHashMap map = HashMapLayout.Create(Builder.BorrowAddressRangeMemory(mapAddress, HashMapLayout.Size), mapAddress);
        map.Buckets = buckets.Address;
    }

    public ulong CreatePtrMap((ulong Key, ulong Value)[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        (ulong Key, ulong Value)[] ptrMapEntries = entries
            .Select(static entry => (entry.Key, entry.Value >> 1))
            .ToArray();
        return CreateMap(ptrMapEntries);
    }

    public void PopulatePtrMap(ulong mapAddress, (ulong Key, ulong Value)[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        (ulong Key, ulong Value)[] ptrMapEntries = entries
            .Select(static entry => (entry.Key, entry.Value >> 1))
            .ToArray();
        PopulateMap(mapAddress, ptrMapEntries);
    }

    private bool TryAddEntryToBucket(MockHashMapBucket bucket, ulong key, ulong value)
    {
        for (int i = 0; i < MockHashMapBucket.SlotsPerBucket; i++)
        {
            if (bucket.GetKey(i) != (uint)ExecutionManagerHelpers.HashMapLookup.SpecialKeys.Empty)
            {
                continue;
            }

            bucket.SetKey(i, key);
            bucket.SetValue(i, value);
            return true;
        }

        bucket.SetValue(0, bucket.GetValue(0) | ~HashMapValueMask);
        bucket.SetValue(1, bucket.GetValue(1) & HashMapValueMask);
        return false;
    }

    private MockMemorySpace.HeapFragment AllocateAndAdd(ulong size, string name)
    {
        MockMemorySpace.HeapFragment fragment = _allocator.Allocate(size, name);
        Builder.AddHeapFragment(fragment);
        return fragment;
    }
}
