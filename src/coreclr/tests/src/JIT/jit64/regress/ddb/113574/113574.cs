// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Bug: JIT compiler generates incorrect native code for certain loops, resulting in incorrect behavior
//
// The 64bit JIT generates incorrect code for loops that have loop induction variables 
// that are close to overflowing (like for (int i = 1; i < int.MaxValue; i++)).  
// This can cause the loop to either become an infinite loop or to stop prematurely.

using System;
internal class LoopTests
{
    private static bool Test1()
    {
        long result = 0;
        for (int i = 1; i < Int32.MaxValue; ++i)
        {
            result += i;
        }

        if (result != 0x1FFFFFFF40000001)
        {
            Console.WriteLine("Expected 0x1FFFFFFF40000001 : Actual 0x{0:x}", result);
            return false;
        }

        return true;
    }

    private static bool Test2()
    {
        long result = 0;
        for (int i = 1; i < Int32.MaxValue; i += 3)
        {
            result += i - 1;
            result += i;
            result += i + 1;
        }

        if (result != 0x1FFFFFFEC0000003)
        {
            Console.WriteLine("Expected 0x1FFFFFFEC0000003 : Actual 0x{0:x}", result);
            return false;
        }

        return true;
    }

    private static bool Test3()
    {
        long result = 0;
        for (int i = 1; i < Int32.MaxValue; i += 5)
        {
            result += i - 1;
            result += i;
            result += i + 1;
            result += i + 2;
            result += i + 3;
        }

        if (result != 0x1FFFFFFCC0000003)
        {
            Console.WriteLine("Expected 0x1FFFFFFCC0000003 : Actual 0x{0:x}", result);
            return false;
        }

        return true;
    }

    private static int Main()
    {
        int ret = 100;

        if (!Test1()) { ret = 101; }
        if (!Test2()) { ret = 101; }
        if (!Test3()) { ret = 101; }

        if (ret == 101) Console.WriteLine("Test Failed");
        else Console.WriteLine("Test Passed");

        return ret;
    }
}
