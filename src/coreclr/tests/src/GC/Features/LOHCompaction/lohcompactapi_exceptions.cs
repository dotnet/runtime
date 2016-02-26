// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Runtime;



namespace LOHCompactAPI
{
    class Program
    {

        public static int Main(string[] args)
        {
            for(int i = 0; i <= 5; i++)
            {
                Console.WriteLine(i);
                if ((GCLargeObjectHeapCompactionMode)(i) == GCLargeObjectHeapCompactionMode.Default)
                {
                    Console.WriteLine("Default");
                    continue;
                }
                if ((GCLargeObjectHeapCompactionMode)(i) == GCLargeObjectHeapCompactionMode.CompactOnce)
                {
                    Console.WriteLine("CompactOnce");
                    continue;
                }

                bool exc = false;
                try
                {
                    GCSettings.LargeObjectHeapCompactionMode = (GCLargeObjectHeapCompactionMode)(i);
                }
                catch (System.ArgumentOutOfRangeException e1)
                {
                    Console.WriteLine("Caught expected exception " + e1);
                    exc = true;
                }
                catch (System.Exception e2)
                {
                    Console.WriteLine("Wrong type of exception " + e2);
                    Console.WriteLine("Expected ArgumentOutOfrangeException");
                    return 1;
                }

                if (!exc)
                {
                    Console.WriteLine("Expected ArgumentOutOfrangeException for out of range input for LargeObjectHeapCompactionMode");
                    return 2;
                }
            }
           
            Console.WriteLine("Test passed");
            return 100;
        }


    }
}


