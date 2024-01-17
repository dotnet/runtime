// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* TEST:        delete_next_card_table
 * SDET:        clyon
 * DESCRIPTION: gains 14 blocks in gc.cpp
                mscorwks!WKS::delete_next_card_table: (7 blocks, 11 arcs)
                mscorwks!SVR::delete_next_card_table: (7 blocks, 11 arcs)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public class delete_next_card_table
{
    [Fact]
    public static void TestEntryPoint()
    {
        new delete_next_card_table().DoMemoryChurn();
    }

    // this function attempts to allocate & free large amounts
    // of memory to ensure our objects remain pinned, don't get
    // relocated, etc...
    void DoMemoryChurn()
    {

        Random r = new Random();
        for (int j=0; j<10; j++)
        {
            Console.Write("Churn loop {0}", j);

            try
            {
                // arraylist keeps everything rooted until we run out of memory
                List<object> al = new List<object>();
                int len = 1;

                for (int i = 0; i < 32; i++)        // todo: this should be based upon size of IntPtr (32 bits on 32 bit platforms, 64 on 64 bit platforms)
                {
                    Console.Write(".");

                    if (i < 30)
                    {
                        // Random.Next cannot handle negative (0x80000000) numbers
                        len *= 2;
                    }
                    al.Add(new Guid[len + r.Next(len)]);
                }
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("OOM while Churning");
                GC.Collect();
            }

            Console.WriteLine();
        }
    }
}

