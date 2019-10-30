// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// seg 4MB, gen0 4MB: regression test for 424916
// seg 8MB, gen0 4MB regression test for 460373

using System;
using System.Runtime;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace b424916
{

    [ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Unicode )]
    public class Node
    {
        GCHandle gch1;
        byte[] unpinnedData1;
        byte[] pinnedData1;
        byte[] unpinnedData2;
        byte[] unpinnedData3;
        byte[] unpinnedData4;
        byte[] pinnedData2;
        byte[] unpinnedData5;
        GCHandle gch2;
        public Node Next;

	[System.Security.SecuritySafeCritical]
        public Node()
        {

            pinnedData1 = new byte[10];
            gch1 = GCHandle.Alloc(pinnedData1, GCHandleType.Pinned);
            pinnedData2 = new byte[10];
            gch2 = GCHandle.Alloc(pinnedData2, GCHandleType.Pinned);

            unpinnedData1 = new byte[1024*80];
            unpinnedData2 = new byte[1024*80];
            unpinnedData3 = new byte[1024*80];
            unpinnedData4 = new byte[1024*80];
            unpinnedData5 = new byte[1024*80];

        }
    }

    public class Test
    {

        public static int Main()
        {

            Node head = new Node();
            Node cur = head;

            for (int i=0; i<1250; i++)
            {
                cur.Next = new Node();
                cur = cur.Next;
                GC.KeepAlive(head);

            }

            return 100;
        }
    }
}

/*
PD7 asserts:

segment size: 4MB
gen0 initial size: 4MB
(at time of assert, gen0 is ~8MB)

Assert failure(PID 2560 [0x00000a00], Thread: 2488 [0x9b8]): (heap_segment_rw (g
eneration_start_segment (gen))!= ephemeral_heap_segment) || (gap_start > generat
ion_allocation_start (gen))

MSCORWKS! WKS::gc_heap::thread_gap + 0x5A (0x5db3bb55)
MSCORWKS! WKS::gc_heap::plan_phase + 0x17CF (0x5db439eb)
MSCORWKS! WKS::gc_heap::gc1 + 0x92 (0x5db43d85)
MSCORWKS! WKS::gc_heap::garbage_collect + 0x3FE (0x5db44ee7)
MSCORWKS! WKS::GCHeap::GarbageCollectGeneration + 0x23D (0x5db45162)
MSCORWKS! WKS::gc_heap::allocate_more_space + 0x124 (0x5db45378)
MSCORWKS! WKS::GCHeap::Alloc + 0x11D (0x5db4619d)
MSCORWKS! Alloc + 0x13A (0x5d9c90b8)
MSCORWKS! FastAllocatePrimitiveArray + 0x21B (0x5d9c9da2)
MSCORWKS! JIT_NewArr1 + 0x2CF (0x5d9d5155)
    File: f:\pd7\ndp\clr\src\vm\gc.cpp, Line: 12792 Image:
D:\temp\424916.exe



segment size: 8MB
gen0 initial size: 4MB
(at time of assert, gen0 is ~8KB)

Assert failure(PID 2172 [0x0000087c], Thread: 3668 [0xe54]): !"Can't allocate if
 no free space"

MSCORWKS! WKS::gc_heap::allocate_in_expanded_heap + 0x276 (0x5db32ede)
MSCORWKS! WKS::gc_heap::realloc_plug + 0x1B5 (0x5db36a16)
MSCORWKS! WKS::gc_heap::realloc_plugs + 0xE9 (0x5db36c3a)
MSCORWKS! WKS::gc_heap::expand_heap + 0x478 (0x5db4049e)
MSCORWKS! WKS::gc_heap::plan_phase + 0x1167 (0x5db43383)
MSCORWKS! WKS::gc_heap::gc1 + 0x92 (0x5db43d85)
MSCORWKS! WKS::gc_heap::garbage_collect + 0x3FE (0x5db44ee7)
MSCORWKS! WKS::GCHeap::GarbageCollectGeneration + 0x23D (0x5db45162)
MSCORWKS! WKS::GCHeap::GarbageCollectTry + 0x38 (0x5db4627e)
MSCORWKS! WKS::GCHeap::GarbageCollect + 0x3B (0x5db462bd)
    File: f:\pd7\ndp\clr\src\vm\gc.cpp, Line: 7490 Image:
D:\temp\424916.exe

*/
