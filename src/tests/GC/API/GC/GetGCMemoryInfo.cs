// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Tests GC.Collect(0)

using System;
using System.Collections.Generic;
using System.Threading;

public class Test_GetGCMemoryInfo
{
    // Set this to false normally so the test doesn't have so much console output.
    static bool fPrintInfo = false;

    public static bool PrintGCMemoryInfo(GCKind kind)
    {
        if (!fPrintInfo)
            return true;

        GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo(kind);
        Console.WriteLine("last recorded {0} GC#{1}, collected gen{2}, concurrent: {3}, compact: {4}, promoted {5:N0} bytes",
            kind, memoryInfo.Index, memoryInfo.Generation, memoryInfo.Concurrent, memoryInfo.Compacted, memoryInfo.PromotedBytes);
        Console.WriteLine("GC pause: {0:N0}, {1:N0}",
            memoryInfo.PauseDurations[0].TotalMilliseconds,
            memoryInfo.PauseDurations[1].TotalMilliseconds);
        Console.WriteLine("Total committed {0:N0}, % Pause time in GC: {1}",
            memoryInfo.TotalCommittedBytes, memoryInfo.PauseTimePercentage);
        Console.WriteLine("This GC observed {0:N0} pinned objects and {1:N0} objects ready for finalization",
            memoryInfo.PinnedObjectsCount, memoryInfo.FinalizationPendingCount);
        int numGenerations = memoryInfo.GenerationInfo.Length;
        Console.WriteLine("there are {0} generations", numGenerations);
        for (int i = 0; i < numGenerations; i++)
        {
            Console.WriteLine("gen#{0}, size {1:N0}->{2:N0}, frag {3:N0}->{4:N0}", i,
                memoryInfo.GenerationInfo[i].SizeBeforeBytes, memoryInfo.GenerationInfo[i].SizeAfterBytes,
                memoryInfo.GenerationInfo[i].FragmentationBeforeBytes, memoryInfo.GenerationInfo[i].FragmentationAfterBytes);
        }

        if (kind == GCKind.Ephemeral)
        {
            if (memoryInfo.Generation == GC.MaxGeneration)
            {
                Console.WriteLine("FAILED: GC#{0} is supposed to be an ephemeral GC but condemned max gen", memoryInfo.Index);
                return false;
            }
        }
        else if (kind == GCKind.FullBlocking)
        {
            if ((memoryInfo.Generation != GC.MaxGeneration) || memoryInfo.Concurrent)
            {
                Console.WriteLine("FAILED: GC#{0} is supposed to be a full blocking GC but gen is {1}, concurrent {2}",
                    memoryInfo.Index, memoryInfo.Generation, memoryInfo.Concurrent);
                return false;
            }
        }
        else if (kind == GCKind.Background)
        {
            if ((memoryInfo.Generation != GC.MaxGeneration) || !(memoryInfo.Concurrent))
            {
                Console.WriteLine("FAILED: GC#{0} is supposed to be a BGC but gen is {1}, concurrent {2}",
                    memoryInfo.Index, memoryInfo.Generation, memoryInfo.Concurrent);
                return false;
            }
        }

        return true;
    }

    static void MakeTemporarySOHAllocations()
    {
        int totalTempAllocBytes = 32 * 1024 * 1024;
        int byteArraySize = 1000;
        for (int i = 0; i < (totalTempAllocBytes / byteArraySize); i++)
        {
            GC.KeepAlive(new byte[byteArraySize]);
        }
    }

    static object MakeLongLivedSOHAllocations()
    {
        List<byte[]> listByteArray = new List<byte[]>();
        int totalAllocBytes = 32 * 1024 * 1024;
        int byteArraySize = 1000;
        for (int i = 0; i < (totalAllocBytes / byteArraySize); i++)
        {
            listByteArray.Add(new byte[byteArraySize]);
        }

        Console.WriteLine("list has {0} elements, total mem {1:N0}",
            listByteArray.Count, GC.GetTotalMemory(false));
        return listByteArray;
    }

    public static int Main()
    {
        // We will keep executing the test in case of a failure to see if we have multiple failures.
        bool isTestSucceeded = true;

        try
        {
            GCMemoryInfo memoryInfoInvalid = GC.GetGCMemoryInfo((GCKind)(-1));
        }
        catch (Exception e)
        {
            if (e is ArgumentOutOfRangeException)
                Console.WriteLine("caught arg exception as expected: {0}", e);
            else
                isTestSucceeded = false;
        }

        while (GC.CollectionCount(0) == 0)
        {
            MakeTemporarySOHAllocations();
        }

        if (!PrintGCMemoryInfo(GCKind.Ephemeral)) isTestSucceeded = false;
        GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo(GCKind.Ephemeral);
        if (memoryInfo.GenerationInfo[0].SizeBeforeBytes < memoryInfo.GenerationInfo[0].SizeAfterBytes)
        {
            Console.WriteLine("Allocated only temp objects yet gen0 size didn't shrink! {0}->{1}",
                memoryInfo.GenerationInfo[0].SizeBeforeBytes, memoryInfo.GenerationInfo[0].SizeAfterBytes);

            isTestSucceeded = false;
        }

        List<byte[]> listByteArray = new List<byte[]>();
        listByteArray.Add(new byte[3 * 1024 * 1024]);
        listByteArray.Add(new byte[4 * 1024 * 1024]);

        GC.Collect();
        GC.Collect();

        GCMemoryInfo memoryInfoLastNGC2 = GC.GetGCMemoryInfo(GCKind.FullBlocking);
        GCMemoryInfo memoryInfoLastAnyGC = GC.GetGCMemoryInfo();

        if (memoryInfoLastNGC2.Index != memoryInfoLastAnyGC.Index)
        {
            Console.WriteLine("FAILED: last GC#{0} should be NGC2 but was not", memoryInfoLastAnyGC.Index);
            isTestSucceeded = false;
        }

        object obj = MakeLongLivedSOHAllocations();
        GC.Collect(1);
        GC.Collect();
        GC.Collect(2, GCCollectionMode.Default, false);
        if (!PrintGCMemoryInfo(GCKind.Any)) isTestSucceeded = false;
        long lastNGC2Index = GC.GetGCMemoryInfo(GCKind.FullBlocking).Index;
        long lastEphemeralIndex = GC.GetGCMemoryInfo(GCKind.Ephemeral).Index;

        GC.Collect();
        if (!PrintGCMemoryInfo(GCKind.Any)) isTestSucceeded = false;

        GC.Collect(2, GCCollectionMode.Default, false);
        if (!PrintGCMemoryInfo(GCKind.Any)) isTestSucceeded = false;
        if (!PrintGCMemoryInfo(GCKind.FullBlocking)) isTestSucceeded = false;
        if (!PrintGCMemoryInfo(GCKind.Background)) isTestSucceeded = false;
        if (!PrintGCMemoryInfo(GCKind.Ephemeral)) isTestSucceeded = false;

        long currentNGC2Index = GC.GetGCMemoryInfo(GCKind.FullBlocking).Index;
        long currentEphemeralIndex = GC.GetGCMemoryInfo(GCKind.Ephemeral).Index;

        if (lastNGC2Index >= currentNGC2Index)
        {
            Console.WriteLine("FAILED: We did an additional NGC2, yet last NGC2 index is {0} and current is {1}",
                lastNGC2Index, currentNGC2Index);
            isTestSucceeded = false;
        }

        if (lastEphemeralIndex != currentEphemeralIndex)
        {
            Console.WriteLine("FAILED: No ephemeral GCs happened so far, yet last eph index is {0} and current is {1}",
                lastEphemeralIndex, currentEphemeralIndex);
            isTestSucceeded = false;
        }

        Console.WriteLine("listByteArray has {0} elements, obj is of type {1}",
            listByteArray.Count, obj.GetType());
        Console.WriteLine("test {0}", (isTestSucceeded ? "succeeded" : "failed"));
        return isTestSucceeded ? 100 : 1;
    }
}
