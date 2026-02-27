// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;
using Xunit;

namespace JIT.jit64.opt.cse.VolatileTest_op_and;

public class Program
{
    [Fact]
    [OuterLoop]
    [SkipOnCoreClr("", RuntimeTestModes.AnyGCStress)]
    public static int TestEntryPoint()
    {
        Console.WriteLine("this test is designed to hang if jit cse doesnt honor volatile");
        if (TestCSE.Test()) return 100;
        return 1;
    }
}

public class TestCSE
{
    private const int VAL1 = 0x404;
    private const int VAL2 = 0x03;
    private static volatile bool s_timeUp = false;

    private volatile int _a;
    private volatile int _b;

    private static int[] s_objs;

    static TestCSE()
    {
        s_objs = new int[3];
        s_objs[0] = VAL1;
        s_objs[1] = VAL1;
        s_objs[2] = VAL2;
    }

    public TestCSE()
    {
        _a = s_objs[0];
        _b = s_objs[1];
    }

    public static bool Equal(int val1, int val2)
    {
        if (val1 == val2)
            return true;
        return false;
    }

    public static bool TestFailed(int result, int expected1, int expected2, string tname)
    {
        if (result == expected1)
            return false;
        if (result == expected2)
            return false;
        Console.WriteLine("this failure may not repro everytime");
        Console.WriteLine("ERROR FAILED:" + tname + ",got val1=" + result + " expected value is, either " + expected1 + " or " + expected2);
        throw new Exception("check failed for " + tname);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public bool TestOp()
    {
        long i;

        Thread.Sleep(0);


        _a = VAL1;
        _b = VAL1;
        for (i = 0; ; i++)
        {
            if (!Equal(_a & _b, _a & _b)) break;
            if (!Equal(_a & _b, _a & _b)) break;
            i++;
        }
        Console.WriteLine("Test 1 passed after " + i + " tries");


        _a = VAL1;
        _b = VAL1;
        for (i = 0; ; i++)
        {
            if (!Equal(_a & _b, VAL1 & VAL2)) break;
            if (!Equal(_a & _b, VAL1 & VAL2)) break;
        }
        Console.WriteLine("Test 2 passed after " + i + " tries");


        bool passed = false;
        _a = VAL1;
        _b = VAL1;
        for (i = 0; ; i++)
        {
            int ans1 = _a & _b;
            int ans2 = _a & _b;
            if (TestFailed(ans1, VAL1 & VAL1, VAL1 & VAL2, "Test 3") || TestFailed(ans2, VAL1 & VAL1, VAL1 & VAL2, "Test 3"))
            {
                passed = false;
                break;
            }

            if (ans1 != ans2)
            {
                passed = true;
                break;
            }
        }
        Console.WriteLine("Test 3 " + (passed ? "passed" : "failed") + " after " + i + " tries");

        return passed;
    }


    private void Flip()
    {
        for (uint i = 0; !s_timeUp; i++)
        {
            _a = s_objs[i % 2];

            _b = s_objs[(i % 2) + 1];
        }
    }


    public static bool Test()
    {
        TestCSE o = new TestCSE();
        Thread t = new Thread(new ThreadStart(o.Flip));

        t.Start();
        bool ans = o.TestOp();
        s_timeUp = true;
        t.Join();

        return ans;
    }
}
