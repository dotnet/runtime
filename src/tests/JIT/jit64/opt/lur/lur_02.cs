// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

/*********************************************

Notes on tests:


loops:
0,1,2,3,4,5,7,8,9,12,13

Values:
uint, int, long

inc, assignment, adding, double inc.



*********************************************/







public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        int failed_tests = 0;


        // Test 01
        if (test_01(3) != 67)
        {
            Console.WriteLine("FAIL: test_01(3)");
            failed_tests++;
        }

        // Test 02
        if (test_02(3) != 131)
        {
            Console.WriteLine("FAIL: test_02(3)");
            failed_tests++;
        }

        // Test 03
        if (test_03(3) != 582636163)
        {
            Console.WriteLine("FAIL: test_03(3)");
            failed_tests++;
        }

        // Test 04
        if (test_04(3) != 643)
        {
            Console.WriteLine("FAIL: test_04(3)");
            failed_tests++;
        }

        // Test 05
        if (test_05(3) != 67)
        {
            Console.WriteLine("FAIL: test_05(3)");
            failed_tests++;
        }

        // Test 06
        if (test_06(3) != 131)
        {
            Console.WriteLine("FAIL: test_06(3)");
            failed_tests++;
        }

        // Test 07
        if (test_07(3) != 582636163)
        {
            Console.WriteLine("FAIL: test_07(3)");
            failed_tests++;
        }

        // Test 08
        if (test_08(3) != 643)
        {
            Console.WriteLine("FAIL: test_08(3)");
            failed_tests++;
        }

        // Test 09
        if (test_09(3) != 67)
        {
            Console.WriteLine("FAIL: test_09(3)");
            failed_tests++;
        }

        // Test 10
        if (test_10(3) != 131)
        {
            Console.WriteLine("FAIL: test_10(3)");
            failed_tests++;
        }

        // Test 11
        if (test_11(3) != -6817972681569578365)
        {
            Console.WriteLine("FAIL: test_11(3)");
            failed_tests++;
        }

        // Test 12
        if (test_12(3) != 643)
        {
            Console.WriteLine("FAIL: test_12(3)");
            failed_tests++;
        }



        // Test 01
        if (test_01(5) != 69)
        {
            Console.WriteLine("FAIL: test_01(5)");
            failed_tests++;
        }

        // Test 02
        if (test_02(5) != 133)
        {
            Console.WriteLine("FAIL: test_02(5)");
            failed_tests++;
        }

        // Test 03
        if (test_03(5) != -1403567675)
        {
            Console.WriteLine("FAIL: test_03(5)");
            failed_tests++;
        }

        // Test 04
        if (test_04(5) != 1029)
        {
            Console.WriteLine("FAIL: test_04(5)");
            failed_tests++;
        }

        // Test 05
        if (test_05(5) != 69)
        {
            Console.WriteLine("FAIL: test_05(5)");
            failed_tests++;
        }

        // Test 06
        if (test_06(5) != 133)
        {
            Console.WriteLine("FAIL: test_06(5)");
            failed_tests++;
        }

        // Test 07
        if (test_07(5) != 2891399621)
        {
            Console.WriteLine("FAIL: test_07(5)");
            failed_tests++;
        }

        // Test 08
        if (test_08(5) != 1029)
        {
            Console.WriteLine("FAIL: test_08(5)");
            failed_tests++;
        }

        // Test 09
        if (test_09(5) != 69)
        {
            Console.WriteLine("FAIL: test_09(5)");
            failed_tests++;
        }

        // Test 10
        if (test_10(5) != 133)
        {
            Console.WriteLine("FAIL: test_10(5)");
            failed_tests++;
        }

        // Test 11
        if (test_11(5) != -1088802703752609339)
        {
            Console.WriteLine("FAIL: test_11(5)");
            failed_tests++;
        }

        // Test 12
        if (test_12(5) != 1029)
        {
            Console.WriteLine("FAIL: test_12(5)");
            failed_tests++;
        }



        return (failed_tests == 0) ? 100 : 1;
    }

    public static int test_01(int a)
    {
        int b = a;

        for (int i = 0; i < 0; i++)
        {
            b++;
        }

        for (int i = 0; i < 1; i++)
        {
            b++;
        }

        for (int i = 0; i < 2; i++)
        {
            b++;
        }

        for (int i = 0; i < 3; i++)
        {
            b++;
        }

        for (int i = 0; i < 4; i++)
        {
            b++;
        }

        for (int i = 0; i < 5; i++)
        {
            b++;
        }

        for (int i = 0; i < 7; i++)
        {
            b++;
        }

        for (int i = 0; i < 8; i++)
        {
            b++;
        }

        for (int i = 0; i < 9; i++)
        {
            b++;
        }

        for (int i = 0; i < 12; i++)
        {
            b++;
        }

        for (int i = 0; i < 13; i++)
        {
            b++;
        }


        return b;
    }

    public static int test_02(int a)
    {
        int b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b++;
        }


        return b;
    }


    public static int test_03(int a)
    {
        int b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b = b * a;
        }


        return b;
    }

    public static int test_04(int a)
    {
        int b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b = b + a * 3;
        }


        return b;
    }






    public static uint test_05(uint a)
    {
        uint b = a;

        for (int i = 0; i < 0; i++)
        {
            b++;
        }

        for (int i = 0; i < 1; i++)
        {
            b++;
        }

        for (int i = 0; i < 2; i++)
        {
            b++;
        }

        for (int i = 0; i < 3; i++)
        {
            b++;
        }

        for (int i = 0; i < 4; i++)
        {
            b++;
        }

        for (int i = 0; i < 5; i++)
        {
            b++;
        }

        for (int i = 0; i < 7; i++)
        {
            b++;
        }

        for (int i = 0; i < 8; i++)
        {
            b++;
        }

        for (int i = 0; i < 9; i++)
        {
            b++;
        }

        for (int i = 0; i < 12; i++)
        {
            b++;
        }

        for (int i = 0; i < 13; i++)
        {
            b++;
        }


        return b;
    }

    public static uint test_06(uint a)
    {
        uint b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b++;
        }


        return b;
    }


    public static uint test_07(uint a)
    {
        uint b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b = b * a;
        }


        return b;
    }

    public static uint test_08(uint a)
    {
        uint b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b = b + a * 3;
        }


        return b;
    }




    public static long test_09(long a)
    {
        long b = a;

        for (int i = 0; i < 0; i++)
        {
            b++;
        }

        for (int i = 0; i < 1; i++)
        {
            b++;
        }

        for (int i = 0; i < 2; i++)
        {
            b++;
        }

        for (int i = 0; i < 3; i++)
        {
            b++;
        }

        for (int i = 0; i < 4; i++)
        {
            b++;
        }

        for (int i = 0; i < 5; i++)
        {
            b++;
        }

        for (int i = 0; i < 7; i++)
        {
            b++;
        }

        for (int i = 0; i < 8; i++)
        {
            b++;
        }

        for (int i = 0; i < 9; i++)
        {
            b++;
        }

        for (int i = 0; i < 12; i++)
        {
            b++;
        }

        for (int i = 0; i < 13; i++)
        {
            b++;
        }


        return b;
    }

    public static long test_10(long a)
    {
        long b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b++;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b++;
        }


        return b;
    }


    public static long test_11(long a)
    {
        long b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b = b * a;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b = b * a;
        }


        return b;
    }

    public static long test_12(long a)
    {
        long b = a;

        for (int i = 0; i < 0; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 1; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 2; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 3; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 4; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 5; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 7; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 8; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 9; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 12; i++)
        {
            b++; b = b + a * 3;
        }

        for (int i = 0; i < 13; i++)
        {
            b++; b = b + a * 3;
        }


        return b;
    }
}

