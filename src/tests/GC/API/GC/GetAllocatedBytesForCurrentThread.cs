// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Tests GC.Collect()

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;

public class Test 
{
    static Random Rand = new Random();

    public static bool GetAllocatedBytesForCurrentThread(int size)
    {
        int startCount = GC.CollectionCount(0);
        long start = GC.GetAllocatedBytesForCurrentThread();

        GC.KeepAlive(new String('a', size));

        long end = GC.GetAllocatedBytesForCurrentThread();
        int endCount = GC.CollectionCount(0);

        if (start == end)
        {
            Console.WriteLine("GetAllocatedBytesForCurrentThread: start and end same!");
            return false;
        }
        return true;
    }

    static int Alloc(List<object> list, int size)
    {
        int toAlloc = Rand.Next(size / 2 , (int)((float)size * 1.5));
        Console.WriteLine("allocating {0} bytes", toAlloc);
        int allocated = 0;

        while (allocated < toAlloc)
        {
            int s = Rand.Next(100, 1000);
            allocated += s + 24;
            byte[] b = new byte[s];
            list.Add((object)b);
        }
        return allocated;
    }

    static bool TestWithAlloc()
    {
        int allocatedBytes = 0;
        for (int i = 0; i < 100; i++)
        {
            List<object> list = new List<object>();
            allocatedBytes = Alloc(list, 80*1024*1024);

            if (!GetAllocatedBytesForCurrentThread (100000)) 
            {
                return false;
            }
            Console.WriteLine("iter {0} allocated {1} bytes", i, allocatedBytes);
        }
        return true;
    }

    // In core 1.0 we didn't have the API exposed so needed to use reflection to get it.
    // This should be split into 2 tests, with and without GC.Collect.
    static bool TestCore1(bool testWithCollection)
    {
        const string name = "GetAllocatedBytesForCurrentThread";
        var typeInfo = typeof(GC).GetTypeInfo();
        var method = typeInfo.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        long nBytesBefore = 0;
        long nBytesAfter = 0;

        int countBefore = GC.CollectionCount(0);
        int bytesDiff = 0;


        for (int i = 0; i < 10000; ++i)
        {
            nBytesBefore = (long)method.Invoke(null, null);
            // Test with collection.
            if (testWithCollection)
            {
                GC.Collect();
            }

            nBytesAfter = (long)method.Invoke(null, null);

            if (nBytesAfter == nBytesBefore)  // Shouldn't be the same 
            {
                int countAfter = GC.CollectionCount(0);
                Console.WriteLine("b: {0}, a: {1}, iter {2}, {3}->{4}", nBytesBefore, nBytesAfter, i, countBefore, countAfter);
                return false;
            }
        }
        return true;
    }

    public static int Main() 
    {
        // First test with collection
        if (!TestCore1(true))
        {
            Console.WriteLine("Test for GetAllocatedBytesForCurrentThread() failed!");
            return 1;
        }

        // Test without collection
        if (!TestCore1(false))
        {
            Console.WriteLine("Test for GetAllocatedBytesForCurrentThread() failed!");
            return 1;
        }
        if (!TestWithAlloc())
        {
            Console.WriteLine("Test for GetAllocatedBytesForCurrentThread() failed!");
            return 1;
        }


        Console.WriteLine("Test for GetAllocatedBytesForCurrentThread() passed!");
        return 100;
    }
}
