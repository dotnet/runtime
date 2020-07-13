// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security;

public class Test
{

    public static void Usage()
    {
        Console.WriteLine("USAGE:");
        Console.WriteLine("plug.exe [numIterations]");
    }

    [SecuritySafeCritical]
    public static int Main(string[] args)
    {

        int size = 10000;
        int power = 20;
        int numIterations = 0;
        GCHandle[] list = new GCHandle[size];

        if (args.Length == 0)
        {
            //using defaults
            numIterations = 100;
        }
        else if (args.Length == 1)
        {
            if (!Int32.TryParse(args[0], out numIterations))
            {
                Usage();
                return 1;
            }
        }
        else
        {
            Usage();
            return 1;
        }

        Console.WriteLine("Running {0} iterations", numIterations);

        for (int j=0; j<numIterations; j++)
        {
            for (int i=0; i<size; i++)
            {
                GCHandleType type = GCHandleType.Normal;

                if (i%5==0)
                {
                    // pin every 5th handle
                    type = GCHandleType.Pinned;
                }

                if (!list[i].IsAllocated)
                {
                    try
                    {
                        byte[] b = new byte[(int)Math.Pow(2,(i%power))];
                        list[i] = (GCHandle.Alloc(b, type));
                    }
                    catch (OutOfMemoryException)
                    {
                        Console.WriteLine("OOM");
                        Console.WriteLine("Heap size: {0}", GC.GetTotalMemory(false));
                        Console.WriteLine("Trying to allocate array of size: {0}", Math.Pow(2,(i%power)));
                    }
                }
                else
                {
                    list[i].Free();
                }
            }

        }

        return 100;
    }
}

