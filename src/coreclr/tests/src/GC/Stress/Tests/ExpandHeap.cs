// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
This test stimulates heap expansion on the finalizer thread
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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


    public static void FreePins()
    {
        foreach (GCHandle gch in list)
        {
            gch.Free();
        }
        list.Clear();
        blist.Clear();
    }


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



    public static void FreePins2()
    {
        foreach (GCHandle gch in list2)
        {
            gch.Free();
        }
        list2.Clear();
        blist2.Clear();
    }



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
    public static int Main(System.String[] Args)
    {
        Console.WriteLine("First Alloc");
        GCUtil.Alloc(1024 * 1024 * 4, 30);
        GCUtil.FreeNonPins();
        GC.Collect();

        Console.WriteLine("Second Alloc");
        GCUtil.Alloc(1024 * 1024 * 4, 50);
        GCUtil.FreeNonPins();
        GC.Collect();
        GCUtil.FreePins();

        Console.WriteLine("Test passed");
        return 100;
    }
}
