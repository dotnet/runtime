// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_JTrueLtInt1
{
    const int Pass = 100;
    const int Fail = -1;

    // This test method returns:
    //   1 if x == int.MinValue
    //   2 if int.MinValue < x < -1
    //   3 if x == -1
    //   4 if x == 0
    //   5 if x == 1
    //   6 if 1 < x < int.MaxValue
    //   7 if x == int.MaxValue

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int JTrueLtInt1(int x)
    {
        int returnValue = -1;

        if (x < int.MinValue)          returnValue = 0;    // Never true
        else if (x < (int.MinValue+1)) returnValue = 1;
        else if (x < -1)               returnValue = 2;
        else if (x < 0)                returnValue = 3;
        else if (x < 1)                returnValue = 4;
        else if (x < (int.MaxValue-1)) returnValue = 5;
        else if (x < int.MaxValue)     returnValue = 6;
        else                           returnValue = 7;

        return returnValue;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnValue = Pass;

        if (JTrueLtInt1(int.MinValue)   != 1) returnValue = Fail;
        if (JTrueLtInt1(int.MinValue+1) != 2) returnValue = Fail;
        if (JTrueLtInt1(-1)             != 3) returnValue = Fail;
        if (JTrueLtInt1(0)              != 4) returnValue = Fail;
        if (JTrueLtInt1(1)              != 5) returnValue = Fail;
        if (JTrueLtInt1(int.MaxValue-1) != 6) returnValue = Fail;
        if (JTrueLtInt1(int.MaxValue)   != 7) returnValue = Fail;

        return returnValue;
    }
}
