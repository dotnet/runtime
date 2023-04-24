// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace GCTest_selfref_cs
{
    public class Test
    {
        [Fact]
        public static int TestEntryPoint()
        {
            object aref = null;
            object[] arr = new object[16];
            for (int i = arr.GetLowerBound(0); i <= arr.GetUpperBound(0); i++)
                arr[i] = arr;
            aref = arr[11];
            arr = null; //but keep reference to element

            GC.Collect();

            Array a2 = (Array)aref;
            for (int i = a2.GetLowerBound(0); i <= a2.GetUpperBound(0); i++)
            {
                if (((Array)a2.GetValue(i)).GetLowerBound(0) != 0 ||
                    ((Array)a2.GetValue(i)).GetUpperBound(0) != 15)
                {
                    Console.WriteLine("TEST FAILED!");
                    return 1;
                }
            }
            Console.WriteLine("TEST PASSED!");
            return 100;
        }
    }
}
