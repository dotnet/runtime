// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Pinning of objects
// Cannot pin objects or array of objects

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        Object[] arr = new Object[100];
        Object obj = new Object();
        int exceptionCount = 0;

        Console.WriteLine("This test should throw 2 exceptions");

        try
        {
            GCHandle handle1 = GCUtil.Alloc(arr, GCHandleType.Pinned);
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught: {0}", e);
            exceptionCount++;
        }

        try
        {
            GCHandle handle2 = GCUtil.Alloc(obj, GCHandleType.Pinned);
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught: {0}", e);
            exceptionCount++;
        }

        if (exceptionCount == 2)
        {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 1;
    }
}
