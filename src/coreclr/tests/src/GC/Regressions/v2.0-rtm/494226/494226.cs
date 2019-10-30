// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Test
{
    [System.Security.SecuritySafeCritical]
    public static int Main()
    {
        List<GCHandle> list = new List<GCHandle>();
        List<byte[]> blist = new List<byte[]>();

        try
        {
            for (int i = 0; i < 1024 * 1024 * 20; i++)
            {
                byte[] b = new byte[10];
                b[0] = 0xC;

                if (i % 1024 * 1024 * 10 == 0)
                {
                    list.Add(GCHandle.Alloc(b, GCHandleType.Pinned));
                }

                blist.Add(b);
            }
        }
        catch (OutOfMemoryException)
        {
            // we need to bail here
        }

        Console.WriteLine("Test passed");
        return 100;
    }
}

/*
Test passes if the following assert is not hit:

(*card_word)==0

MSCORWKS! WKS::gc_heap::find_card_dword + 0x1CC (0x5d9a2441)
MSCORWKS! WKS::gc_heap::find_card + 0x35 (0x5d9a2588)
MSCORWKS! WKS::gc_heap::mark_through_cards_for_segments + 0x33C (0x5d9a2a79)
MSCORWKS! WKS::gc_heap::relocate_phase + 0x1B4 (0x5d9ab25f)
MSCORWKS! WKS::gc_heap::plan_phase + 0x1AA4 (0x5d9b6ad8)
MSCORWKS! WKS::gc_heap::gc1 + 0x8E (0x5d9b75c1)
MSCORWKS! WKS::gc_heap::garbage_collect + 0x5BE (0x5d9b8d2c)
MSCORWKS! WKS::GCHeap::GarbageCollectGeneration + 0x2AD (0x5d9b9107)
MSCORWKS! WKS::gc_heap::try_allocate_more_space + 0x165 (0x5d9b934c)
MSCORWKS! WKS::gc_heap::allocate_more_space + 0x11 (0x5d9b9b82)

c:\vbl\ndp\clr\src\vm\gc.cpp, Line: 15024
*/
