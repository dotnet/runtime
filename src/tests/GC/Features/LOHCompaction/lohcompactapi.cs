// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Reflection;


namespace LOHCompactAPI
{
    class Program
    {
        static int ListSize = 500;
        static List<byte[]> shortLivedList = new List<byte[]>(ListSize);
        static List<byte[]> LongLivedList = new List<byte[]>(ListSize);

        public static int Main(string[] args)
        {
            int retVal=0;
            for (int i = 0; i < 3; i++)
            {
                retVal = Runtest(i);
                Console.WriteLine("Heap size=" + GC.GetTotalMemory(false));
                if (retVal != 100)
                    break;
            }
            if (retVal == 100)
            Console.WriteLine("Test passed");
            return retVal;
        }

        static int Runtest(int count)
        {
            //Create fragmentation in the Large Object Heap
            System.Random rnd = new Random(12345);
            for (int i = 0; i < ListSize; i++)
            {
                shortLivedList.Add(new byte[rnd.Next(85001, 100000)]);
                LongLivedList.Add(new byte[rnd.Next(85001, 100000)]);

            }
            shortLivedList.Clear();
            GC.Collect();  //when using perfview, LOH should be fragmented after this GC

            //Verify the initial compaction mode should be default
            if (GCSettings.LargeObjectHeapCompactionMode != GCLargeObjectHeapCompactionMode.Default)
            {
                Console.WriteLine("Initial GCLargeObjectHeapCompactionMode should be default; instead it is " + GCSettings.LargeObjectHeapCompactionMode);
                return 1;
            }
            //Set the compaction mode to compact the large object heap
            int initial_collectionCount = GetBlockingGen2Count();
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

            //verify the compaction mode is set correctly
            if (GCSettings.LargeObjectHeapCompactionMode != GCLargeObjectHeapCompactionMode.CompactOnce)
            {
                Console.WriteLine("GCLargeObjectHeapCompactionMode should be CompactOnce; instead it is " + GCSettings.LargeObjectHeapCompactionMode);
                return 2;
            }

            //CompactionMode should revert to default after a compaction has happened

            //The following byte array allocation has the purpose to try to trigger a blocking Gen2 collection. LOH should be compacted during the next blocking Gen2 GC.
            byte[] bArr;
            int listSize2 = 1000;
            List<byte[]> newList = new List<byte[]>();
            List<byte[]> tempList = new List<byte[]>();
            bool Gen2Happened = false;
            
            for (int k = 0; !Gen2Happened && (k < listSize2); k++)
            {
                newList.Add(new byte[rnd.Next(20, 5000)]);
                for (int i = 0; i < ListSize; i++)
                {
                    bArr = new byte[rnd.Next(20, 5000)];
                    tempList.Add(new byte[rnd.Next(20, 5000)]);

                    if (GetBlockingGen2Count() > initial_collectionCount)
                    {
                        Gen2Happened = true;
                        Console.WriteLine("Blocking Gen2 collection happened");
                        //when using perfview,LOH fragmentation should be zero after this GC
                        break;
                    }
                   
                }

                if(k>=10)
                {
                    newList[rnd.Next(0, newList.Count)] = new byte[rnd.Next(20, 5000)];
                    newList[rnd.Next(0, newList.Count)] = new byte[rnd.Next(20, 5000)];
                }
                if(k%10==0)
                    tempList.Clear();
          
                    
             }
            if (GetBlockingGen2Count() == initial_collectionCount) //a blocking Gen2 collection did not happen; trigger one.
                GC.Collect();

            if (GCSettings.LargeObjectHeapCompactionMode != GCLargeObjectHeapCompactionMode.Default)
            {
                Console.WriteLine("GCLargeObjectHeapCompactionMode should revert to default after compaction happened; instead it is " + GCSettings.LargeObjectHeapCompactionMode);
                return 3;
            }

            Console.WriteLine("Run " + count + " passed");
            return 100;
        }

        //Only count the blocking gen2 GC's. Concurrent GC's should be subtracted from the total GC count.
        public static int GetBlockingGen2Count()
        {
            //Get the number of concurrent collections (can use this method only through reflection):
            MethodInfo collectionCountmethod = null;
            Type GCType = Type.GetType("System.GC");
            foreach(MethodInfo m in GCType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (m.Name.Equals("_CollectionCount") && m.GetParameters().Length == 2) collectionCountmethod = m;
            }
            if (collectionCountmethod == null)
            {
                Console.WriteLine("collectionCount method is null");
                return 0;
            }
            object[] parameters = new object[2];
            parameters[0] = 2;
            parameters[1] = 1; // special gc count
            int backgroundCollections = (int)collectionCountmethod.Invoke(null, parameters);

            return (GC.CollectionCount(2) - backgroundCollections);
        }
    }
}
