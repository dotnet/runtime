// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        // Heuristic parameters based on local testing
        double avg = numIterations * 0.1;
        double stdev = numIterations * 0.05;

        double zScore = (gcCount - avg) / stdev;

        // if we do a full collection <= (10% of the interations + stdev assuming normal distribution) times, we pass
        if (zScore >= -2 && zScore <= 2)
        {
            Console.WriteLine("Passed");
            return 100;
        }

        Console.WriteLine("Failed");
        return 1;
    }
}

