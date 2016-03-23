// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ForegroundGC
{
    class ForegroundGC
    {
        static bool done = false;
        static long maxAlloc = 1024 * 1024 * 1024;  //1GB max size
        static int size = 30;
        static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                if ((args[0].CompareTo("-?") == 0) || (args[0].CompareTo("/?") == 0))
                {
                    Console.WriteLine("Usage: ForegroundGC.exe [max allocation in MB] [object size in bytes]");
                    return 0;
                }
                else
                {
                    long maxAllocMB = Int32.Parse(args[0]);
                    maxAlloc = maxAllocMB * 1024 * 1024;
                }
            }
            if (args.Length > 1)
            {
                size = Int32.Parse(args[1]);
            }
            Console.WriteLine("Max allocation = {0} bytes; Objects size = {1}", maxAlloc, size);
            List<byte[]> List1 = new List<byte[]>();
            List<byte[]> List2 = new List<byte[]>();
            long AllocCount = 0;  //bytes allocated
            
            while (AllocCount < maxAlloc)
            {
                byte[] b = new byte[size];
                AllocCount += size;
                List1.Add(b);
               

                byte[] b2 = new byte[size];
                AllocCount += size;
                List2.Add(b2);
               
            }
            Thread t = new Thread(AllocateTemp);
            t.Start();
            List2.Clear();
            Console.WriteLine("Finished allocating big array");
            GC.Collect(2, GCCollectionMode.Optimized, false);

            for (int k = 0; k < 2; k++)
            {
                for (int i = List1.Count - 1; i >= 0; i--)
                {
                    List2.Add(List1[i]);
                    List1.RemoveAt(i);
                }
                for (int i = List2.Count - 1; i >= 0; i--)
                {
                    List1.Add(List2[i]);
                    List2.RemoveAt(i);
                }
            }
          
            done = true;
            t.Join();
            Console.WriteLine("List count=" + List1.Count);

            GC.KeepAlive(List1);
            GC.KeepAlive(List2);

            return 100;
        }

        static void AllocateTemp()
        {
            while (!done)
            {
                byte[] b = new byte[30];
                byte[] b2 = new byte[100];
            }
        }
    }
}
