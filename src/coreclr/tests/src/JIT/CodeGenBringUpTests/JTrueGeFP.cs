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
    public static int JTrueGeFP(float x)
    {
        int returnValue = -1;

        if (x >= 2f)                     returnValue = 4;
        else if (x >= 1f)                returnValue = 3;
        else if (x >= 0f)                returnValue = 2;
        else if (x >= -1f)               returnValue = 1;

        return returnValue;
    }

    public static int Main()
    {
        int returnValue = Pass;

        if (JTrueGeFP(-1f)             != 1) returnValue = Fail;
        if (JTrueGeFP(0f)              != 2) returnValue = Fail;
        if (JTrueGeFP(1f)              != 3) returnValue = Fail;
        if (JTrueGeFP(2f)              != 4) returnValue = Fail;


        return returnValue;
    }
}
