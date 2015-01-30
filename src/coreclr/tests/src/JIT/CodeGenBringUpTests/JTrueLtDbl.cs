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
    public static int JTrueLtDbl(double x)
    {
        int returnValue = -1;

        if (x < -1d) returnValue = 1;
        else if (x < 0d) returnValue = 2;
        else if (x < 1d) returnValue = 3;
        else if (x < Double.MaxValue) returnValue = 4;
        else returnValue = 5;

        return returnValue;
    }

    public static int Main()
    {
        int returnValue = Pass;

        if (JTrueLtDbl(Double.MinValue) != 1) returnValue = Fail;
        if (JTrueLtDbl(-2d) != 1) returnValue = Fail;
        if (JTrueLtDbl(-1d) != 2) returnValue = Fail;
        if (JTrueLtDbl(0d) != 3) returnValue = Fail;
        if (JTrueLtDbl(5d) != 4) returnValue = Fail;

        return returnValue;
    }
}
