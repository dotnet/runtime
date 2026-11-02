// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BackgroundArrayZeroing
{
    public static bool HasEnoughMemory => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes >= (16L << 30);

    [ConditionalFact(nameof(HasEnoughMemory))]
    public static void TestEntryPoint()
    {
        object[] live = new object[20_000_000];
        for (int i = 0; i < live.Length; i++)
        {
            live[i] = new int[2];
        }

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);

        // Keep a large object graph alive so background marking overlaps the allocations.
        for (int sizeInGiB = 4; sizeInGiB <= 8; sizeInGiB += 2)
        {
            CheckArray(sizeInGiB);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
        }

        GC.KeepAlive(live);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckArray(int sizeInGiB)
    {
        Console.WriteLine($"Checking zero initialization of a {sizeInGiB} GiB array during background GC.");
        GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: false);
        long[] array = new long[((long)sizeInGiB << 30) / sizeof(long)];
        int nonzeroIndex = array.AsSpan().IndexOfAnyExcept(0L);
        Assert.True(nonzeroIndex < 0,
            $"{sizeInGiB} GiB array contains a nonzero element at index {nonzeroIndex}.");
    }
}
