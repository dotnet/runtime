// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Pinning of objects
// Cannot pin array of objects

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        Object[] arr = new Object[100];

        try
        {
            GCHandle handle1 = GCUtil.Alloc(arr, GCHandleType.Pinned);
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught: {0}", e);
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 1;
    }
}
