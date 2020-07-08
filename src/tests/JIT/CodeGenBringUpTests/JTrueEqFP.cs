// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
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

    public static int Main()
    {
        int returnValue = Pass;

        if (JTrueEqFP(-1f)             != 1) returnValue = Fail;
        if (JTrueEqFP(0f)              != 2) returnValue = Fail;
        if (JTrueEqFP(1f)              != 3) returnValue = Fail;

        return returnValue;
    }
}
