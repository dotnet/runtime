// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Tests GC.Collect()

using System;

public class Test {


    public static int Main() {
        // allocate a bunch of SOH byte arrays and touch them.
        var r = new Random(1234);
        for (int i = 0; i < 10000; i++)
        {
            int size = r.Next(10000);
            var arr = GC.AllocateUninitializedArray<byte>(size);

            if (size > 1)
            {
                arr[0] = 5;
                arr[size - 1] = 17;
                if (arr[0] != 5 || arr[size - 1] != 17)
                {
                    Console.WriteLine("Scenario 1 for GC.AllocUninitialized() failed!");
                    return 1;
                }
            }
        }

        // allocate a bunch of LOH int arrays and touch them.
        for (int i = 0; i < 1000; i++)
        {
            int size = r.Next(100000, 1000000);
            var arr = GC.AllocateUninitializedArray<int>(size);

            arr[0] = 5;
            arr[size - 1] = 17;
            if (arr[0] != 5 || arr[size - 1] != 17)
            {
                Console.WriteLine("Scenario 2 for GC.AllocUninitialized() failed!");
                return 1;
            }
        }

        // allocate a string array
        {
            int i = 100;
            var arr = GC.AllocateUninitializedArray<string>(i);

            arr[0] = "5";
            arr[i - 1] = "17";
            if (arr[0] != "5" || arr[i - 1] != "17")
            {
                Console.WriteLine("Scenario 3 for GC.AllocUninitialized() failed!");
                return 1;
            }
        }

        // allocate max size byte array
        {
            if (IntPtr.Size == 8)
            {
                int i = 0x7FFFFFC7;
                var arr = GC.AllocateUninitializedArray<byte>(i);

                arr[0] = 5;
                arr[i - 1] = 17;
                if (arr[0] != 5 || arr[i - 1] != 17)
                {
                    Console.WriteLine("Scenario 4 for GC.AllocUninitialized() failed!");
                    return 1;
                }
            }
        }

        // negative size
        {
            int GetNegativeValue() => -1;
            int negativeSize = GetNegativeValue();
            Type expectedExceptionType = null;

            try
            {
                GC.KeepAlive(new byte[negativeSize]);

                Console.WriteLine("Scenario 5 Expected exception (new operator)!");
                return 1;
            }
            catch (Exception newOperatorEx)
            {
                expectedExceptionType = newOperatorEx.GetType();
            }

            try
            {
                var arr = GC.AllocateUninitializedArray<byte>(-1);

                Console.WriteLine("Scenario 5 Expected exception (GC.AllocateUninitializedArray)!");
                return 1;
            }
            catch (Exception allocUninitializedEx) when (allocUninitializedEx.GetType() == expectedExceptionType)
            {
                // OK
            }
            catch (Exception other)
            {
                Console.WriteLine($"Scenario 5 Expected exception type mismatch: expected {expectedExceptionType}, but got {other.GetType()}!");
                return 1;
            }
        }

        // too large
        {
            try
            {
                var arr = GC.AllocateUninitializedArray<double>(int.MaxValue);

                Console.WriteLine("Scenario 6 Expected exception!");
                return 1;
            }
            catch (OutOfMemoryException)
            {
            }
        }


        Console.WriteLine("Test for GC.Collect() passed!");
        return 100;
    }
}
