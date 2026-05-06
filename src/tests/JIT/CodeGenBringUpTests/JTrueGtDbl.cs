// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_JTrueGtDbl
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int JTrueGtDbl(double x)
    {
        int returnValue = -1;

        if (x > 1d)                returnValue = 4;
        else if (x > 0d)                returnValue = 3;
        else if (x > -1d)               returnValue = 2;
        else if (x > Double.MinValue)     returnValue = 1;

        return returnValue;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnValue = Pass;

        if (JTrueGtDbl(-1d) != 1) returnValue = Fail;
        if (JTrueGtDbl(0d) != 2) returnValue = Fail;
        if (JTrueGtDbl(1d) != 3) returnValue = Fail;
        if (JTrueGtDbl(2d) != 4) returnValue = Fail;

        return returnValue;
    }
}
