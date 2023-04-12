// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct TwoBools
{
    public bool b1;
    public bool b2;

    public TwoBools(bool b1, bool b2)
    {
        this.b1 = b1;
        this.b2 = b2;
    }
}

public class Test_GitHub_37666
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;

        RunTest(Test1, "Test1", ref result);
        RunTest(Test2, "Test2", ref result);

        return result;
    }

    delegate TwoBools TestZeroInit();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunTest(TestZeroInit test, string testName, ref int result)
    {
        if (test().b2)
        {
            Console.WriteLine(testName + " failed");
            result = -1;
        }
        else
        {
            Console.WriteLine(testName + " passed");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TwoBools Test1()
    {
        TwoBools result = CreateTwoBools();
        result.b2 = false;
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TwoBools Test2()
    {
        TwoBools result = default(TwoBools);
        result.b2 = true;
        result = default(TwoBools);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TwoBools CreateTwoBools()
    {
        TwoBools result = new TwoBools(true, true);
        return result;
    }
}
