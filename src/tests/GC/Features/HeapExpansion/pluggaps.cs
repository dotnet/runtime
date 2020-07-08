// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
This test fragments the heap with ~50 byte holes, then allocates ~50 byte objects to plug them
*/

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

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
        for (int i=0; i<1024*1024; i++)
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
