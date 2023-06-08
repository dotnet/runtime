// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class AggressiveCollect_MultipleParameters
{
    public static int Main()
    {
        long before = CreateGarbage();
        GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);

        // Positive Test Case. 
        long after = GC.GetGCMemoryInfo().TotalCommittedBytes;
        long reclaimed = before - after;
        long reclaimedAtLeast = 2000 * 4000;
        if (reclaimed < reclaimedAtLeast)
        {
            // If we reach this case, the aggressive GC is not releasing as much memory as
            // we wished, something is wrong.
            throw new ArgumentException("Test for Collect_Aggressive_MultipleParameters failed: ArgumentException.");
        }
        else
        {
            // Doing some extra allocation (and also trigger GC indirectly) here
            // should be just fine.
            for (int i = 0; i < 10; i++)
            {
                CreateGarbage();
            }
        }

        before = CreateGarbage();

        // Negative Test Cases.
        try
        {
            for (int i = 0; i < 10; i++)
            {
                CreateGarbage();
            }

            GC.Collect(2, GCCollectionMode.Aggressive, blocking: false, compacting: false);
        }
        catch (ArgumentException ex)
        {
            // Should throw an exception.
        }

        catch (Exception ex)
        {
            throw new Exception("Test for Collect_Aggressive_MultipleParameters failed.");
        }

        // If we got this far, we have successfully executed all the tests. 
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CreateGarbage()
    {
        byte[][] smallGarbage = new byte[2000][];

        // This will force us to use more than one region in the small object heap
        for (int i = 0; i < 2000; i++)
        {
            // It will roughly span one page
            smallGarbage[i] = new byte[4000];
        }

        // This will force us to use more than one region in the large object heap
        byte[] largeGarbage = new byte[33 * 1024 * 1024];

        // This will force us to use more than one region in the pin object heap
        byte[] pinnedGarbage = GC.AllocateArray<byte>(33 * 1024 * 1024, /* pinned = */true);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        long committed = GC.GetGCMemoryInfo().TotalCommittedBytes;

        GC.KeepAlive(smallGarbage);
        GC.KeepAlive(largeGarbage);
        GC.KeepAlive(pinnedGarbage);

        return committed;
    }
}
