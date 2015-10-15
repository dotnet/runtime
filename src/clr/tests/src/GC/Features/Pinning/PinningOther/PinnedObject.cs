// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
