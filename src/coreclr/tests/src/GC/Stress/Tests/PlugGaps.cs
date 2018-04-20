// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    public static int Main(System.String[] Args)
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


/*
00 0012df98 5e0f6282 ntdll!KiFastSystemCallRet
01 0012dfe8 5e0f580d mscorwks!CLREventWaitHelper+0x92 [f:\pd7\ndp\clr\src\vm\synch.cpp @ 647]
02 0012e168 5e0f53d7 mscorwks!CLREvent::WaitEx+0x42d [f:\pd7\ndp\clr\src\vm\synch.cpp @ 717]
03 0012e180 5de6eb3f mscorwks!CLREvent::Wait+0x27 [f:\pd7\ndp\clr\src\vm\synch.cpp @ 663]
04 0012e1a0 5de6a7a5 mscorwks!SVR::gc_heap::user_thread_wait+0x5f [f:\pd7\ndp\clr\src\vm\gcee.cpp @ 1876]
05 0012e1b0 5e031909 mscorwks!WKS::gc_heap::concurrent_gc_wait+0xa5 [f:\pd7\ndp\clr\src\vm\gcee.cpp @ 1890]
06 0012e1e8 5e03663d mscorwks!WKS::gc_heap::c_adjust_limits+0x119 [f:\pd7\ndp\clr\src\vm\gc.cpp @ 5834]
07 0012e200 5e04af14 mscorwks!WKS::gc_heap::garbage_collect+0xad [f:\pd7\ndp\clr\src\vm\gc.cpp @ 8357]
08 0012e230 5e032fa7 mscorwks!WKS::GCHeap::GarbageCollectGeneration+0x1e4 [f:\pd7\ndp\clr\src\vm\gc.cpp @ 18941]
09 0012e300 5e04a17b mscorwks!WKS::gc_heap::allocate_more_space+0x97 [f:\pd7\ndp\clr\src\vm\gc.cpp @ 6827]
0a 0012e320 5e04a9e8 mscorwks!WKS::gc_heap::allocate+0x8b [f:\pd7\ndp\clr\src\vm\gc.cpp @ 7185]
0b 0012e3d0 5de64ff6 mscorwks!WKS::GCHeap::Alloc+0x1f8 [f:\pd7\ndp\clr\src\vm\gc.cpp @ 18557]
0c 0012e4dc 5de65ab8 mscorwks!Alloc+0x256 [f:\pd7\ndp\clr\src\vm\gcscan.cpp @ 121]
0d 0012e5d4 5de39ead mscorwks!FastAllocatePrimitiveArray+0x3f8 [f:\pd7\ndp\clr\src\vm\gcscan.cpp @ 999]
0e 0012e798 02c80239 mscorwks!JIT_NewArr1+0x4dd [f:\pd7\ndp\clr\src\vm\jitinterface.cpp @ 15211]
0f 0012e7b0 5db8b98d 445488!Test.Main()+0xe1
10 0012ebe4 5db8b74e mscorwks!CallDescrWorker+0x10d [f:\pd7\ndp\clr\src\vm\class.cpp @ 13371]
11 0012ed44 5de59887 mscorwks!CallDescrWorkerWithHandler+0x22e [f:\pd7\ndp\clr\src\vm\class.cpp @ 13278]
12 0012f13c 5de58ba7 mscorwks!MethodDesc::CallDescr+0xc97 [f:\pd7\ndp\clr\src\vm\method.cpp @ 2046]
13 0012f268 5d993abd mscorwks!MethodDesc::CallTargetWorker+0x297 [f:\pd7\ndp\clr\src\vm\method.cpp @ 1717]

0:000> dt WKS::gc_heap::settings
   +0x000 condemned_generation : 2
   +0x004 promotion        : 1
   +0x008 compaction       : 1
   +0x00c heap_expansion   : 0
   +0x010 concurrent       : 1
   +0x014 concurrent_compaction : 1
   +0x018 demotion         : 0
   +0x01c card_bundles     : 1
   +0x020 gen0_reduction_count : 0
   +0x024 segment_allocation_failed_count : 0
   +0x028 elevation        : 3 ( el_locked )
   +0x02c reason           : 0 ( reason_alloc )
   +0x030 pause_mode       : 1 ( pause_interactive )

*/
