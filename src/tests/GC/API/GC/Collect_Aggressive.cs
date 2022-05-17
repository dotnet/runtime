// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class AggressiveCollect
{
    public static int Main(string[] args )
    {
        CreateGarbage();
        GC.Collect(2, GCCollectionMode.Aggressive);
        // At this point, we should have decommitted the unused regions back to the OS
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CreateGarbage()
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
    }
}
