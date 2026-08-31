// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_JTrueEqDbl
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int JTrueEqDbl(double x)
    {
        int returnValue = 0;

        if (x == -1d) returnValue = 1;
        else if (x == 0d) returnValue = 2;
        else if (x == 1d) returnValue = 3;

        return returnValue;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnValue = Pass;

        if (JTrueEqDbl(-1d)             != 1) returnValue = Fail;
        if (JTrueEqDbl(0d)              != 2) returnValue = Fail;
        if (JTrueEqDbl(1d)              != 3) returnValue = Fail;

        return returnValue;
    }
}
