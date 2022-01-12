// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Unicode )]
public class Node
{
    GCHandle gch1;
    byte[] pinnedData1;
    public Node Next;


    public Node()
    {
        pinnedData1 = new byte[1024*50];
        gch1 = GCHandle.Alloc(pinnedData1, GCHandleType.Pinned);
    }
}

public class Test_445488
{
    //public static PerformanceCounter PC;


    public static int Main()
    {
        List<byte[]> list = new List<byte[]>();
        List<GCHandle> glist = new List<GCHandle>();
        //PC = new PerformanceCounter(".NET CLR Memory", "Gen 0 heap size", "445488", ".");
        long count =0;

        while (count <= 124979200)
        {
            //float gen0size = PC.NextValue();
            byte[] b = new byte[1024*50];
            count += (1024*50);

            if (count % (1024*2500)==0)
            {
                glist.Add(GCHandle.Alloc(b, GCHandleType.Pinned));
            }

            list.Add(b);
            //Console.WriteLine("{0} {1:F}",count, gen0size);
        }

        GC.KeepAlive(list);
        GC.KeepAlive(glist);
        return 100;

    }

}

/*
124979200 3075360.00
---------------------------
424916.exe - Assert Failure (PID 2336, Thread 1248/4e0)
---------------------------
mi1 >= 0

MSCORWKS! WKS::gc_heap::allocate_in_expanded_heap + 0x181 (0x5db32de9)
MSCORWKS! WKS::gc_heap::realloc_plug + 0x1B5 (0x5db36a16)
MSCORWKS! WKS::gc_heap::realloc_in_brick + 0x75 (0x5db36b3d)
MSCORWKS! WKS::gc_heap::realloc_plugs + 0xAE (0x5db36bff)
MSCORWKS! WKS::gc_heap::expand_heap + 0x478 (0x5db4049e)
MSCORWKS! WKS::gc_heap::plan_phase + 0x1167 (0x5db43383)
MSCORWKS! WKS::gc_heap::gc1 + 0x92 (0x5db43d85)
MSCORWKS! WKS::gc_heap::garbage_collect + 0x3FE (0x5db44ee7)
MSCORWKS! WKS::GCHeap::GarbageCollectGeneration + 0x23D (0x5db45162)
MSCORWKS! WKS::gc_heap::allocate_more_space + 0x45A (0x5db456ae)

f:\pd7\ndp\clr\src\vm\gc.cpp, Line: 7464

Abort - Kill program
Retry - Debug
Ignore - Keep running


Image:
D:\temp\424916.exe

---------------------------
Abort   Retry   Ignore
---------------------------

Ignoring the asserts generates the AV from the bug report:


 # ChildEBP RetAddr
00 0012d250 5e24645f mscorwks!DbgAssertDialog+0x394
01 0012d618 5dcedf7c mscorwks!CHECK::Trigger+0x2df
02 0012d800 5dcedb2d mscorwks!CLRVectoredExceptionHandlerPhase2+0x33c
03 0012d864 5d9cfd67 mscorwks!CLRVectoredExceptionHandler+0xcd
04 0012d890 5d9cfc4a mscorwks!CPFH_FirstPassHandler+0xc7
05 0012d8c4 7c9037bf mscorwks!COMPlusFrameHandler+0x14a
WARNING: Stack unwind information not available. Following frames may be wrong.
06 0012d8e8 7c90378b ntdll!RtlConvertUlongToLargeInteger+0x7a
07 0012d998 7c90eafa ntdll!RtlConvertUlongToLargeInteger+0x46
08 0012dca4 5e03ec96 ntdll!KiUserExceptionDispatcher+0xe
09 0012dcb8 5e03ea89 mscorwks!WKS::gc_heap::gcmemcopy+0x86
0a 0012dce4 5e03eda4 mscorwks!WKS::gc_heap::compact_plug+0xf9
0b 0012dd18 5e03f02f mscorwks!WKS::gc_heap::compact_in_brick+0xd4
0c 0012dd5c 5e03bce5 mscorwks!WKS::gc_heap::compact_phase+0x24f
0d 0012df5c 5e03618e mscorwks!WKS::gc_heap::plan_phase+0x19e5
0e 0012dfa0 5e036a6f mscorwks!WKS::gc_heap::gc1+0xae
0f 0012dfb4 5e04af14 mscorwks!WKS::gc_heap::garbage_collect+0x4df
10 0012dfe4 5e0333b8 mscorwks!WKS::GCHeap::GarbageCollectGeneration+0x1e4
11 0012e0b4 5e04a17b mscorwks!WKS::gc_heap::allocate_more_space+0x4a8
12 0012e0d4 5e04a9e8 mscorwks!WKS::gc_heap::allocate+0x8b
13 0012e184 5de64ff6 mscorwks!WKS::GCHeap::Alloc+0x1f8
14 0012e290 5de65ab8 mscorwks!Alloc+0x256
15 0012e388 5de39ead mscorwks!FastAllocatePrimitiveArray+0x3f8
*** WARNING: Unable to verify checksum for D:\WINDOWS\Microsoft.NET\Framework\v2.0.x86dbg\assembly\NativeImages_v2.0.x86dbg_32\mscorlib\ab6a82069375373ebc7e85bf2de124cb\mscorlib.ni.dll
*** ERROR: Module load completed but symbols could not be loaded for D:\WINDOWS\Microsoft.NET\Framework\v2.0.x86dbg\assembly\NativeImages_v2.0.x86dbg_32\mscorlib\ab6a82069375373ebc7e85bf2de124cb\mscorlib.ni.dll
16 0012e54c 5b69d907 mscorwks!JIT_NewArr1+0x4dd
17 0012e610 5b69d716 mscorlib_ni!Microsoft.Win32.RegistryKey.InternalGetValue(System.String, System.Object, Boolean, Boolean)+0x147
18 0012e610 5b69d716 mscorlib_ni!Microsoft.Win32.RegistryKey.InternalGetValue(System.String, System.Object, Boolean, Boolean)+0x147
19 00000000 7a7e6865 mscorlib_ni!Microsoft.Win32.RegistryKey.GetValue(System.String)+0x36
*** WARNING: Unable to verify checksum for D:\WINDOWS\Microsoft.NET\Framework\v2.0.x86dbg\assembly\NativeImages_v2.0.x86dbg_32\System\08fb29f559b89437a7fc3f4a7dbde9c1\System.ni.dll
*** ERROR: Module load completed but symbols could not be loaded for D:\WINDOWS\Microsoft.NET\Framework\v2.0.x86dbg\assembly\NativeImages_v2.0.x86dbg_32\System\08fb29f559b89437a7fc3f4a7dbde9c1\System.ni.dll
1a 0012e66c 7a7e617f System_ni!System.Diagnostics.PerformanceMonitor.GetData(System.String)+0x55
1b 0012e6a0 7a7e57fe System_ni!System.Diagnostics.PerformanceCounterLib.GetPerformanceData(System.String)+0x97
1c 00a855a4 7a7e5742 System_ni!System.Diagnostics.PerformanceCounterLib.GetCategorySample(System.String)+0x62
1d 0012e738 7a7e24e0 System_ni!System.Diagnostics.PerformanceCounterLib.GetCategorySample(System.String, System.String)+0x36
1e 0012e738 7a7e2651 System_ni!System.Diagnostics.PerformanceCounter.NextSample()+0x64
1f 04ffb000 02c800f0 System_ni!System.Diagnostics.PerformanceCounter.NextValue()+0x21
20 04ffb000 00000000 445488!Test_445488.Main()+0x80
*/
