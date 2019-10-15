// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Security;
using System.Collections.Generic;
using System.Threading;

public class Test
{
    public static bool fail = false;

    [System.Security.SecuritySafeCritical]
    public static int Main(String[] args)
    {
        Thread[] threads = new Thread[Math.Max(Environment.ProcessorCount * 2, 64)];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(new ThreadStart(Allocate));
            threads[i].Name = i.ToString();
            threads[i].Start();
        }

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i].Join();
        }

        if (fail)
        {
            Console.WriteLine("Test Failed");
            return 0;
        }

        Console.WriteLine("Test Passed");
        return 100;
    }

    public static void Allocate()
    {
        try
        {
            List<byte[]> list = new List<byte[]>();
            for (int i = 0; i < 10000; i++)
            {
                if (fail)
                {
                    break;
                }

                byte[] b = new byte[8000];
                if (i % 10 == 0)
                {
                    list.Add(b);
                }
            }
        }
        catch (OutOfMemoryException)
        {
            Console.WriteLine("OOM");
            fail = true;
        }
    }
}
