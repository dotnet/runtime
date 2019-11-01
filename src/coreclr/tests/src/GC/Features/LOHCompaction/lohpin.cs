// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime;

namespace LOHPin
{
    class LOHPin
    {
        //Pin an object on the Large Object Heap and verify it does not move during a LOH compaction
        //Also verify that most of the large objects not pinned have moved

        /* What the test does:
         *   - create high fragmentation in the LOH
         *   - pin some of the large objects
         *   - compact LOH then check the address of the objects
         * */
 
        static int Main(string[] args)
        {
            List<GCHandle> GCHandleList = new List<GCHandle>();
            int ListSize = 300;
            List<byte[]> shortLivedList = new List<byte[]>(ListSize);
            List<byte[]> LongLivedList = new List<byte[]>(ListSize-ListSize/10);
             List<IntPtr> LongLivedAddress = new List<IntPtr>(ListSize-ListSize/10);  //addresses of objects in LongLivedList
            List<byte[]> PinList = new List<byte[]>(ListSize/10);
            List<IntPtr> PinAddress = new List<IntPtr>(ListSize/10); //addresses of objects in PinList
            //Create fragmentation in the Large Object Heap
            System.Random rnd = new Random(12345);
            for (int i = 0; i < ListSize; i++)
            {
                shortLivedList.Add(new byte[rnd.Next(85001, 100000)]);

                byte[] bt = new byte[rnd.Next(85001, 100000)];
                if (i % 10 == 0)  //object pinned
                {
                    PinList.Add(bt);
                    GCHandle gch = GCHandle.Alloc(bt,GCHandleType.Pinned);
                    GCHandleList.Add(gch);
                    PinAddress.Add(gch.AddrOfPinnedObject());

                }
                else  //object not pinned
                {
                    LongLivedList.Add(bt);
                    GCHandle gch = GCHandle.Alloc(bt, GCHandleType.Pinned);
                    LongLivedAddress.Add(gch.AddrOfPinnedObject());
                    gch.Free();
                }

            }
            shortLivedList.Clear();
            GC.Collect();  //LOH should be fragmented 40-50% after this GC - can observe this with perfview
            GC.WaitForPendingFinalizers();
            GC.Collect();

           GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        

            //check the addresses of objects; pinned and not pinned
            Console.WriteLine("Check the pinned list");
            for (int i = 0; i < PinList.Count; i++)
            {
                GCHandle gch = GCHandle.Alloc(PinList[i], GCHandleType.Pinned);
                IntPtr newAddress = gch.AddrOfPinnedObject();
                Console.WriteLine("OldAddress={0}, NewAddress={1}", PinAddress[i], newAddress);
                gch.Free();
                if (!(PinAddress[i] == newAddress))
                {
                    Console.WriteLine("OldAddress={0}, NewAddress={1}", PinAddress[i], newAddress);
                    Console.WriteLine("Test failed");
                    return 2;
                }
            }

            int moved = 0;
            Console.WriteLine("Check the non pinned list");
            for (int i = 0; i < LongLivedList.Count; i++)
            {
                GCHandle gch = GCHandle.Alloc(LongLivedList[i], GCHandleType.Pinned);
                IntPtr newAddress = gch.AddrOfPinnedObject();
                Console.WriteLine("OldAddress={0}, NewAddress={1}", LongLivedAddress[i], newAddress);
                gch.Free();
                if (!(LongLivedAddress[i] == newAddress))
                {
                    moved++;
                }
            }
            Console.WriteLine(moved + " objects have moved out of " + LongLivedList.Count);
            if (moved < LongLivedList.Count/2)
            {
                Console.WriteLine("Test failed. Too few objects have moved during compaction");
                return 2;
            }

            Console.WriteLine("Test passed");
            return 100;
         
        }

      
      
    }

   
}
