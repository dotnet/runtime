// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class ArrayOOM
{
    private static int s_meg = 1024 * 1024; // 16 MB: 16777216

    public bool RunTest()
    {
        byte[] arr;
        int failed_i = 0;

        bool failed = false;
        for (int i = (int)(s_meg * 16); i >= (int)(s_meg * 15.9); i -= 10)
        {
            try
            {
                if (i % 1024 == 0)
                {
                    Console.Write(".");
                }
                arr = new byte[i];
                arr = null;
            }
            catch (OutOfMemoryException)
            {
                failed = true;
                failed_i = i;
            }
        }

        if (failed)
        {
            Console.Write("OOM while allocating a byte array of size ");
            Console.WriteLine(failed_i);
            return false;
        }

        return true;
    }
}

public class ByteArrayOOM
{
    public static int Main()
    {
        ArrayOOM byteTest = new ArrayOOM();
        if (byteTest.RunTest())
        {
            Console.WriteLine("Test Passed!");
            return 100;
        }

        Console.WriteLine("Test Failed!");
        return 1;
    }
}
