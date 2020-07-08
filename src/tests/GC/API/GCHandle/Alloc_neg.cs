// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Negative Test for GCHandle.Alloc() .. trying to allocated a handle to a null object.

/*************************************************************************************
 This is allowed because Pinning happens whenever a GC occurs. So if a GC occurs and 
 there is an object in the handle, we will pin it. It is quite reasonable to create
 the handle now and fill in the object later.
**************************************************************************************/

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        int[] array = new int[25];
        array = null;

        GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);

        bool ans = handle.IsAllocated;
        if (ans)
            Console.WriteLine("GCHandle is allocated = ");

        if (ans == true)
        {
            Console.WriteLine("Negative test for GCHandle.Alloc() passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Negative test for GCHandle.Alloc() failed!");
            return 1;
        }
    }
}
