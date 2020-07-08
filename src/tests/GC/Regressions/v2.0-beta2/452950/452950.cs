// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class b452950
{
    public static List<GCHandle> list = new List<GCHandle>();
    public static int DEFAULT = 1000;

    public static int Main(string[] args)
    {

        int numIterations = 0;
        if (args.Length >0)
        {
            Int32.TryParse(args[0], out numIterations);
            if (numIterations<0)
            {
                numIterations=DEFAULT;
            }
        }
        else
        {
            numIterations= DEFAULT;
        }

        // fragment the heap
        for (int i=0; i<numIterations; i++)
        {
            byte[] b = new byte[1024*50];
            list.Add(GCHandle.Alloc(b, GCHandleType.Pinned));
            byte[] b2 = new byte[1024*50];

        }

        int gcCount = GC.CollectionCount(GC.MaxGeneration);
        Console.WriteLine(gcCount);

        // if we do a full collection <= (5% of the interations) times, we pass
        if (gcCount <= (numIterations*0.05))
        {
            Console.WriteLine("Passed");
            return 100;
        }

        Console.WriteLine("Failed");
        return 1;
    }
}

