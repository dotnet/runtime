// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* RemoveMemoryPressureTest
 *
 * Tests GC.RemoveMemoryPressure by passing it values that are too small (<=0) and
 * values that are too large (>Int32.MaxValue on 32-bit).
 */


using System;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;

public class RemoveMemoryPressureTest
{
    public int TestCount = 0;

    private long[] _negValues = { 0, -1, Int32.MinValue - (long)1, Int64.MinValue / (long)2, Int64.MinValue };
    private long[] _largeValues = { Int32.MaxValue + (long)1, Int64.MaxValue };


    private RemoveMemoryPressureTest()
    {
    }


    public bool TooSmallTest()
    {
        TestCount++;
        bool retVal = true;

        foreach (long i in _negValues)
        {
            try
            {
                GC.RemoveMemoryPressure(i);
                Console.WriteLine("Failure at TooSmallTest: {0}", i);
                retVal = false;
                break;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Failure at TooSmallTest: {0}", i);
                retVal = false;
                break;
            }
        }

        if (retVal)
            Console.WriteLine("TooSmallTest Passed");
        return retVal;
    }


    public bool TooLargeTest()
    {
        TestCount++;

        bool retVal = true;

        foreach (long i in _largeValues)
        {
            try
            {
                GC.RemoveMemoryPressure(i);
                // this should throw exception on 32-bit
                if (IntPtr.Size == Marshal.SizeOf(new Int32()))
                {
                    Console.WriteLine("Failure at LargeValueTest: {0}", i);
                    retVal = false;
                    break;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // this should not throw exception on 64-bit
                if (IntPtr.Size == Marshal.SizeOf(new Int64()))
                {
                    Console.WriteLine("Failure at LargeValueTest: {0}", i);
                    retVal = false;
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                retVal = false;
                break;
            }
        }

        if (retVal)
            Console.WriteLine("TooLargeTest Passed");
        return retVal;
    }


    public bool RunTest()
    {
        int passCount = 0;

        if (TooSmallTest())
            passCount++;

        if (TooLargeTest())
            passCount++;

        return (passCount == TestCount);
    }


    public static int Main()
    {
        RemoveMemoryPressureTest test = new RemoveMemoryPressureTest();

        if (test.RunTest())
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;
    }
}
