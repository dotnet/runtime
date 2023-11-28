// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_JTrueEqInt1
{
    const int Pass = 100;
    const int Fail = -1;

    // This test method returns:
    //   1 if the argument is equal to int.MinValue
    //   2 if the argument is equal to -1
    //   3 if the argument is equal to 0
    //   4 if the argument is equal to 1
    //   5 if the argument is equal to int.MaxValue
    //   0 for all other values

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int JTrueEqInt1(int x)
    {
        int returnValue = 0;

        if (x == int.MinValue) returnValue = 1;
        else if (x == -1) returnValue = 2;
        else if (x == 0) returnValue = 3;
        else if (x == 1) returnValue = 4;
        else if (x == int.MaxValue) returnValue = 5;

        return returnValue;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnValue = Pass;

        if (JTrueEqInt1(int.MinValue)   != 1) returnValue = Fail;
        if (JTrueEqInt1(int.MinValue+1) != 0) returnValue = Fail;
        if (JTrueEqInt1(-1)             != 2) returnValue = Fail;
        if (JTrueEqInt1(0)              != 3) returnValue = Fail;
        if (JTrueEqInt1(1)              != 4) returnValue = Fail;
        if (JTrueEqInt1(int.MaxValue-1) != 0) returnValue = Fail;
        if (JTrueEqInt1(int.MaxValue)   != 5) returnValue = Fail;

        return returnValue;
    }
}
