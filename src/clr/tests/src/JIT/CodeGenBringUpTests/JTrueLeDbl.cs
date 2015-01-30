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
    public static int JTrueLeDbl(double x)
    {
        int returnValue = -1;

        if (x <= -2d)                       returnValue = 1;
        else if (x <= -1d)                  returnValue = 2;
        else if (x <= 0d)                   returnValue = 3;
        else if (x <= 1d)                   returnValue = 4;

        return returnValue;
    }

    public static int Main()
    {
        int returnValue = Pass;

        if (JTrueLeDbl(-2d)               != 1) returnValue = Fail;
        if (JTrueLeDbl(-1d)               != 2) returnValue = Fail;
        if (JTrueLeDbl(0d)                != 3) returnValue = Fail;
        if (JTrueLeDbl(1d)                != 4) returnValue = Fail;

        return returnValue;
    }
}
