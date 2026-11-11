// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class GCUtil
{
    public static List<GCHandle> list = new List<GCHandle>();
    public static List<byte[]> blist = new List<byte[]>();
    public static List<GCHandle> list2 = new List<GCHandle>();
    public static List<byte[]> blist2 = new List<byte[]>();


    public static void Alloc(int numNodes, int percentPinned)
    {
        for (int i = 0; i < numNodes; i++)
        {
            byte[] b = new byte[10];
            b[0] = 0xC;

            if (i % ((int)(numNodes * (100 / percentPinned))) == 0)
            {
                list.Add(GCHandle.Alloc(b, GCHandleType.Pinned));
            }

            blist.Add(b);
        }
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void FreePins()
    {
        foreach (GCHandle gch in list)
        {
            gch.Free();
        }
        list.Clear();
        blist.Clear();
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void FreeNonPins()
    {
        blist.Clear();
    }



    public static void Alloc2(int numNodes, int percentPinned)
    {
        for (int i = 0; i < numNodes; i++)
        {
            byte[] b = new byte[10];
            b[0] = 0xC;

            if (i % ((int)(numNodes * (100 / percentPinned))) == 0)
            {
                list2.Add(GCHandle.Alloc(b, GCHandleType.Pinned));
            }

            blist2.Add(b);
        }
    }



    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void FreePins2()
    {
        foreach (GCHandle gch in list2)
        {
            gch.Free();
        }
        list2.Clear();
        blist2.Clear();
    }



    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void FreeNonPins2()
    {
        blist2.Clear();
    }


    public static void AllocWithGaps()
    {
        for (int i = 0; i < 1024 * 1024; i++)
        {
            byte[] unpinned = new byte[50];
            byte[] pinned = new byte[10];
            blist.Add(unpinned);
            list.Add(GCHandle.Alloc(pinned, GCHandleType.Pinned));
        }
    }
}

public class Test
{
    public static List<GCHandle> gchList = new List<GCHandle>();
    public static List<byte[]> bList = new List<byte[]>();

    public static int Main()
    {
        Console.WriteLine("Beginning phase 1");
        GCUtil.AllocWithGaps();

        Console.WriteLine("phase 1 complete");


        // losing all live references to the unpinned byte arrays
        // this will fragment the heap with ~50 byte holes
        GCUtil.FreeNonPins();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Console.WriteLine("Beginning phase 2");

        bList = new List<byte[]>();
        for (int i = 0; i < 1024 * 1024; i++)
        {
            byte[] unpinned = new byte[50];
            bList.Add(unpinned);
        }

        Console.WriteLine("phase 2 complete");

        GC.KeepAlive(gchList);
        GC.KeepAlive(bList);

        return 100;
    }
}
