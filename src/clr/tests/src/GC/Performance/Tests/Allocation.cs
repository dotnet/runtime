// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

//Measure allocation time when allocating objects on a single thread.
//Should be run with server GC

class Allocation
{     
    static void Main(string[] args)
    {
        if ((args.Length > 0) && (args.Length < 2))
        {
            Console.WriteLine("Usage: Allocation.exe <maxbytes> <byteArraySize> ");
            return;
        }

        string processor = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        Console.WriteLine("Running on {0}", processor);

        UInt64 MaxBytes = 1500000000;
        if (processor.ToLower() == "amd64")
        {
            MaxBytes = 5000000000;
        }

        int byteArraySize = 95000;

        if (args.Length >= 2)
        {
            if (!UInt64.TryParse(args[0], out MaxBytes))
            {
                Console.WriteLine("Usage: Allocation.exe <maxbytes> <byteArraySize> ");
                return;
            }

            if (!Int32.TryParse(args[1], out byteArraySize))
            {
                Console.WriteLine("Usage: Allocation.exe <maxbytes> <byteArraySize> ");
                return;
            }
        }


        //check if running on server GC:
        if (!System.Runtime.GCSettings.IsServerGC)
        {
            Console.WriteLine("GCSettings is not server GC!");
            return;
        }

        //Allocate memory

        UInt64 objCount = MaxBytes / (UInt64)byteArraySize;

        Console.WriteLine("Creating a list of {0} objects", objCount);
        if (objCount > 0X7FEFFFFF)
        {
            Console.WriteLine("Exceeded the max number of objects in a list");
            Console.WriteLine("Creating a list with {0} objects", 0X7FEFFFFF);
            objCount = 0X7FEFFFFF;
        }

        Console.WriteLine("Byte array size is " + byteArraySize);

        Object[] objList = new Object[objCount];
        long timerStart = Environment.TickCount;
        int count = (int)objCount;
        for (int i = 0; i < count; i++)
        {
            objList[i] = new byte[byteArraySize];
        }
        long timerEnd = Environment.TickCount;
        long allocTime = timerEnd - timerStart;
        Console.WriteLine("Allocation time= {0} ms", allocTime);

        Console.WriteLine("GC count: ");
        Console.WriteLine("gen0: " + GC.CollectionCount(0));
        Console.WriteLine("gen1: " + GC.CollectionCount(1));
        Console.WriteLine("gen2: " + GC.CollectionCount(2));

    }

}


