// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_JTrueLeFP
{
    const int Pass = 100;
    const int Fail = -1;


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int JTrueLeFP(float x)
    {
        int returnValue = -1;

        if (x <= -2f)                       returnValue = 1;
        else if (x <= -1f)                  returnValue = 2;
        else if (x <= 0f)                   returnValue = 3;
        else if (x <= 1f)                   returnValue = 4;

        return returnValue;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnValue = Pass;

        if (JTrueLeFP(-2f)               != 1) returnValue = Fail;
        if (JTrueLeFP(-1f)               != 2) returnValue = Fail;
        if (JTrueLeFP(0f)                != 3) returnValue = Fail;
        if (JTrueLeFP(1f)                != 4) returnValue = Fail;

        return returnValue;
    }
}
