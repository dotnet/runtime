// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
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

    public static int Main()
    {
        int returnValue = Pass;

        if (JTrueEqDbl(-1d)             != 1) returnValue = Fail;
        if (JTrueEqDbl(0d)              != 2) returnValue = Fail;
        if (JTrueEqDbl(1d)              != 3) returnValue = Fail;

        return returnValue;
    }
}
