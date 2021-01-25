// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
This test verifies GCHandle.Alloc's ability to validate bad GCHandleTypes, since any int can be cast as a GCHandleType
*/

using System;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main()
    {
        // The third element needs to be updated if Pinned is no longer the last value in the GCHandleType enum
        long[] invalidValues = { Int32.MinValue, -1, (long)(GCHandleType.Pinned + 1), Int32.MaxValue, UInt32.MaxValue, Int64.MaxValue };
        bool passed = true;

        for (int i = 0; i < invalidValues.Length; i++)
        {
            // GCHandle.Alloc internally casts the GCHandleType to a uint
            Console.WriteLine("Input: {0}, Converted to: {1}", invalidValues[i], (uint)invalidValues[i]);

            GCHandle gch = new GCHandle();
            try
            {
                gch = GCHandle.Alloc(new object(), (GCHandleType)(invalidValues[i]));
                Console.WriteLine("Failed");
                passed = false;
                gch.Free();
            }
            catch (ArgumentOutOfRangeException)
            {
                // caught the expected exception
                Console.WriteLine("Passed");
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught unexpected exception");
                Console.WriteLine(e);
                passed = false;
            }

            Console.WriteLine();
        }

        if (passed)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;
    }
}
