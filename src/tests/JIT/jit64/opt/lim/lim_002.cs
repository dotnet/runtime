// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

/*********************************************

Notes on tests:

test_01:
Testing several assignments to see that they maintain order when hoisted.

test_02:
Like test_01, but with two loops.

test_03:
Like test_02, but nested.

test_04:
The increment is not invariant.

test_05:
Like test_04 but with two increments.

test_06:
Loop has one invariant and one not invariant.

test_07:
Like test_06, but with a different not invariant.

test_08:
Another not invariant assignemnt in a loop.

test_09:
An invariant addition.

test_10:
Like test_09 with a zero trip.

Test_11:
A nested invariant with a zero trip.

test_12:
Nested invariant with an add.

test_13:
Nested conflicting invariants.


Plus permutations:
int
long
uint


Notes:

It may be useful to run these tests with constant prop off.


*********************************************/







public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        int failed_tests = 0;

        // Test 01
        if (test_01() != 9)
        {
            Console.WriteLine("FAIL: test_01");
            failed_tests++;
        }

        // Test 02
        if (test_02() != 3)
        {
            Console.WriteLine("FAIL: test_02");
            failed_tests++;
        }

        // Test 03
        if (test_03() != 3)
        {
            Console.WriteLine("FAIL: test_03");
            failed_tests++;
        }

        // Test 04
        if (test_04() != 10)
        {
            Console.WriteLine("FAIL: test_04");
            failed_tests++;
        }

        // Test 05
        if (test_05() != 20)
        {
            Console.WriteLine("FAIL: test_05");
            failed_tests++;
        }

        // Test 06
        if (test_06() != 8)
        {
            Console.WriteLine("FAIL: test_06");
            failed_tests++;
        }

        // Test 07
        if (test_07() != 16)
        {
            Console.WriteLine("FAIL: test_07");
            failed_tests++;
        }

        // Test 08
        if (test_08() != 9)
        {
            Console.WriteLine("FAIL: test_08");
            failed_tests++;
        }

        // Test 09
        if (test_09() != 3)
        {
            Console.WriteLine("FAIL: test_09");
            failed_tests++;
        }

        // Test 10
        if (test_10() != 0)
        {
            Console.WriteLine("FAIL: test_10");
            failed_tests++;
        }

        // Test 11
        if (test_11() != 0)
        {
            Console.WriteLine("FAIL: test_11");
            failed_tests++;
        }

        // Test 12
        if (test_12() != 2)
        {
            Console.WriteLine("FAIL: test_12");
            failed_tests++;
        }

        // Test 13
        if (test_13() != 7)
        {
            Console.WriteLine("FAIL: test_13");
            failed_tests++;
        }

        // Test 01
        if (test_101() != 9)
        {
            Console.WriteLine("FAIL: test_101");
            failed_tests++;
        }

        // Test 02
        if (test_102() != 3)
        {
            Console.WriteLine("FAIL: test_102");
            failed_tests++;
        }

        // Test 03
        if (test_103() != 3)
        {
            Console.WriteLine("FAIL: test_103");
            failed_tests++;
        }

        // Test 04
        if (test_104() != 10)
        {
            Console.WriteLine("FAIL: test_104");
            failed_tests++;
        }

        // Test 05
        if (test_105() != 20)
        {
            Console.WriteLine("FAIL: test_105");
            failed_tests++;
        }

        // Test 06
        if (test_106() != 8)
        {
            Console.WriteLine("FAIL: test_106");
            failed_tests++;
        }

        // Test 07
        if (test_107() != 16)
        {
            Console.WriteLine("FAIL: test_107");
            failed_tests++;
        }

        // Test 08
        if (test_108() != 9)
        {
            Console.WriteLine("FAIL: test_108");
            failed_tests++;
        }

        // Test 09
        if (test_109() != 3)
        {
            Console.WriteLine("FAIL: test_109");
            failed_tests++;
        }

        // Test 10
        if (test_110() != 0)
        {
            Console.WriteLine("FAIL: test_110");
            failed_tests++;
        }

        // Test 11
        if (test_111() != 0)
        {
            Console.WriteLine("FAIL: test_111");
            failed_tests++;
        }

        // Test 12
        if (test_112() != 2)
        {
            Console.WriteLine("FAIL: test_112");
            failed_tests++;
        }

        // Test 13
        if (test_113() != 7)
        {
            Console.WriteLine("FAIL: test_113");
            failed_tests++;
        }

        // Test 01
        if (test_201() != 9)
        {
            Console.WriteLine("FAIL: test_201");
            failed_tests++;
        }

        // Test 02
        if (test_202() != 3)
        {
            Console.WriteLine("FAIL: test_202");
            failed_tests++;
        }

        // Test 03
        if (test_203() != 3)
        {
            Console.WriteLine("FAIL: test_203");
            failed_tests++;
        }

        // Test 04
        if (test_204() != 10)
        {
            Console.WriteLine("FAIL: test_204");
            failed_tests++;
        }

        // Test 05
        if (test_205() != 20)
        {
            Console.WriteLine("FAIL: test_205");
            failed_tests++;
        }

        // Test 06
        if (test_206() != 8)
        {
            Console.WriteLine("FAIL: test_206");
            failed_tests++;
        }

        // Test 07
        if (test_207() != 16)
        {
            Console.WriteLine("FAIL: test_207");
            failed_tests++;
        }

        // Test 08
        if (test_208() != 9)
        {
            Console.WriteLine("FAIL: test_208");
            failed_tests++;
        }

        // Test 09
        if (test_209() != 3)
        {
            Console.WriteLine("FAIL: test_209");
            failed_tests++;
        }

        // Test 10
        if (test_210() != 0)
        {
            Console.WriteLine("FAIL: test_210");
            failed_tests++;
        }

        // Test 11
        if (test_211() != 0)
        {
            Console.WriteLine("FAIL: test_211");
            failed_tests++;
        }

        // Test 12
        if (test_212() != 2)
        {
            Console.WriteLine("FAIL: test_212");
            failed_tests++;
        }

        // Test 13
        if (test_213() != 7)
        {
            Console.WriteLine("FAIL: test_213");
            failed_tests++;
        }



        return (failed_tests == 0) ? 100 : 1;
    }

    public static int test_13()
    {
        int a = 0;

        for (int i = 0; i < 10; i++)
        {
            a = 9;
            for (int j = 0; j < 10; j++)
            {
                a = 7;
            }
        }

        return a;
    }

    public static int test_12()
    {
        int a = 0; int b = 0;

        for (int i = 0; i < 10; i++)
        {
            b = 1;

            for (int j = 0; j < 10; j++)
            {
                a = 1 + b;
            }
        }

        return a;
    }


    public static int test_11()
    {
        int a = 0;

        for (int i = 0; i < 0; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                a = 1;
            }
        }

        return a;
    }


    public static int test_10()
    {
        int a = 0; int b = 0; int c = 0;

        int k = 0;

        for (int i = 0; i < k; i++)
        {
            a = 1; b = 2; c = a + b;
        }

        return c;
    }


    public static int test_09()
    {
        int a = 0; int b = 0; int c = 0;

        for (int i = 0; i < 10; i++)
        {
            a = 1; b = 2; c = a + b;
        }

        return c;
    }


    public static int test_08()
    {
        int a = 0;

        for (int i = 0; i < 10; i++)
        {
            a = i;
        }

        return a;
    }



    public static int test_07()
    {
        int a = 0;

        for (int i = 0; i < 10; i++)
        {
            a = 7; a += i;
        }

        return a;
    }


    public static int test_06()
    {
        int a = 0;

        for (int i = 0; i < 10; i++)
        {
            a = 7; a++;
        }

        return a;
    }





    public static int test_05()
    {
        int a = 0;

        for (int i = 0; i < 10; i++)
        {
            a++; a++;
        }

        return a;
    }




    public static int test_04()
    {
        int a = 0;

        for (int i = 0; i < 10; i++)
        {
            a++;
        }

        return a;
    }



    public static int test_01()
    {
        int a = 0;

        for (int i = 0; i < 10; i++)
        {
            a = 7; a = 8; a = 9;
        }

        return a;
    }


    public static int test_02()
    {
        int a = 0;

        for (int i = 0; i < 10; i++)
        {
            a = 7; a = 8; a = 9;
        }

        for (int j = 0; j < 10; j++)
        {
            a = 1; a = 2; a = 3;
        }


        return a;
    }

    public static int test_03()
    {
        int a = 0;

        for (int k = 0; k < 10; k++)
        {
            for (int i = 0; i < 10; i++)
            {
                a = 7; a = 8; a = 9;
            }

            for (int j = 0; j < 10; j++)
            {
                a = 1; a = 2; a = 3;
            }
        }

        return a;
    }



    public static long test_113()
    {
        long a = 0;

        for (long i = 0; i < 10; i++)
        {
            a = 9;
            for (long j = 0; j < 10; j++)
            {
                a = 7;
            }
        }

        return a;
    }

    public static long test_112()
    {
        long a = 0; long b = 0;

        for (long i = 0; i < 10; i++)
        {
            b = 1;

            for (long j = 0; j < 10; j++)
            {
                a = 1 + b;
            }
        }

        return a;
    }


    public static long test_111()
    {
        long a = 0;

        for (long i = 0; i < 0; i++)
        {
            for (long j = 0; j < 10; j++)
            {
                a = 1;
            }
        }

        return a;
    }


    public static long test_110()
    {
        long a = 0; long b = 0; long c = 0;

        long k = 0;

        for (long i = 0; i < k; i++)
        {
            a = 1; b = 2; c = a + b;
        }

        return c;
    }


    public static long test_109()
    {
        long a = 0; long b = 0; long c = 0;

        for (long i = 0; i < 10; i++)
        {
            a = 1; b = 2; c = a + b;
        }

        return c;
    }


    public static long test_108()
    {
        long a = 0;

        for (long i = 0; i < 10; i++)
        {
            a = i;
        }

        return a;
    }



    public static long test_107()
    {
        long a = 0;

        for (long i = 0; i < 10; i++)
        {
            a = 7; a += i;
        }

        return a;
    }


    public static long test_106()
    {
        long a = 0;

        for (long i = 0; i < 10; i++)
        {
            a = 7; a++;
        }

        return a;
    }





    public static long test_105()
    {
        long a = 0;

        for (long i = 0; i < 10; i++)
        {
            a++; a++;
        }

        return a;
    }




    public static long test_104()
    {
        long a = 0;

        for (long i = 0; i < 10; i++)
        {
            a++;
        }

        return a;
    }



    public static long test_101()
    {
        long a = 0;

        for (long i = 0; i < 10; i++)
        {
            a = 7; a = 8; a = 9;
        }

        return a;
    }


    public static long test_102()
    {
        long a = 0;

        for (long i = 0; i < 10; i++)
        {
            a = 7; a = 8; a = 9;
        }

        for (long j = 0; j < 10; j++)
        {
            a = 1; a = 2; a = 3;
        }


        return a;
    }

    public static long test_103()
    {
        long a = 0;

        for (long k = 0; k < 10; k++)
        {
            for (long i = 0; i < 10; i++)
            {
                a = 7; a = 8; a = 9;
            }

            for (long j = 0; j < 10; j++)
            {
                a = 1; a = 2; a = 3;
            }
        }

        return a;
    }


    public static uint test_213()
    {
        uint a = 0;

        for (uint i = 0; i < 10; i++)
        {
            a = 9;
            for (uint j = 0; j < 10; j++)
            {
                a = 7;
            }
        }

        return a;
    }

    public static uint test_212()
    {
        uint a = 0; uint b = 0;

        for (uint i = 0; i < 10; i++)
        {
            b = 1;

            for (uint j = 0; j < 10; j++)
            {
                a = 1 + b;
            }
        }

        return a;
    }


    public static uint test_211()
    {
        uint a = 0;

        for (uint i = 0; i < 0; i++)
        {
            for (uint j = 0; j < 10; j++)
            {
                a = 1;
            }
        }

        return a;
    }


    public static uint test_210()
    {
        uint a = 0; uint b = 0; uint c = 0;

        uint k = 0;

        for (uint i = 0; i < k; i++)
        {
            a = 1; b = 2; c = a + b;
        }

        return c;
    }


    public static uint test_209()
    {
        uint a = 0; uint b = 0; uint c = 0;

        for (uint i = 0; i < 10; i++)
        {
            a = 1; b = 2; c = a + b;
        }

        return c;
    }


    public static uint test_208()
    {
        uint a = 0;

        for (uint i = 0; i < 10; i++)
        {
            a = i;
        }

        return a;
    }



    public static uint test_207()
    {
        uint a = 0;

        for (uint i = 0; i < 10; i++)
        {
            a = 7; a += i;
        }

        return a;
    }


    public static uint test_206()
    {
        uint a = 0;

        for (uint i = 0; i < 10; i++)
        {
            a = 7; a++;
        }

        return a;
    }





    public static uint test_205()
    {
        uint a = 0;

        for (uint i = 0; i < 10; i++)
        {
            a++; a++;
        }

        return a;
    }




    public static uint test_204()
    {
        uint a = 0;

        for (uint i = 0; i < 10; i++)
        {
            a++;
        }

        return a;
    }



    public static uint test_201()
    {
        uint a = 0;

        for (uint i = 0; i < 10; i++)
        {
            a = 7; a = 8; a = 9;
        }

        return a;
    }


    public static uint test_202()
    {
        uint a = 0;

        for (uint i = 0; i < 10; i++)
        {
            a = 7; a = 8; a = 9;
        }

        for (uint j = 0; j < 10; j++)
        {
            a = 1; a = 2; a = 3;
        }


        return a;
    }

    public static uint test_203()
    {
        uint a = 0;

        for (uint k = 0; k < 10; k++)
        {
            for (uint i = 0; i < 10; i++)
            {
                a = 7; a = 8; a = 9;
            }

            for (uint j = 0; j < 10; j++)
            {
                a = 1; a = 2; a = 3;
            }
        }

        return a;
    }
}
