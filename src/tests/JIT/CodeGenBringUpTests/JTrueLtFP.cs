// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_JTrueLtFP
{
    const int Pass = 100;
    const int Fail = -1;


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int JTrueLtFP(float x)
    {
        int returnValue = -1;

        if (x < -1f) returnValue = 1;
        else if (x < 0f) returnValue = 2;
        else if (x < 1f) returnValue = 3;
        else if (x < Single.MaxValue) returnValue = 4;
        else returnValue = 5;

        return returnValue;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnValue = Pass;

        if (JTrueLtFP(Single.MinValue) != 1) returnValue = Fail;
        if (JTrueLtFP(-2f) != 1) returnValue = Fail;
        if (JTrueLtFP(-1f) != 2) returnValue = Fail;
        if (JTrueLtFP(0f) != 3) returnValue = Fail;
        if (JTrueLtFP(5f) != 4) returnValue = Fail;

        return returnValue;
    }
}
