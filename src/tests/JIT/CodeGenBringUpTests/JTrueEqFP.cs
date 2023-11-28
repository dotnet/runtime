// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_JTrueEqFP
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int JTrueEqFP(float x)
    {
        int returnValue = 0;

        if (x == -1f) returnValue = 1;
        else if (x == 0f) returnValue = 2;
        else if (x == 1f) returnValue = 3;

        return returnValue;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnValue = Pass;

        if (JTrueEqFP(-1f)             != 1) returnValue = Fail;
        if (JTrueEqFP(0f)              != 2) returnValue = Fail;
        if (JTrueEqFP(1f)              != 3) returnValue = Fail;

        return returnValue;
    }
}
