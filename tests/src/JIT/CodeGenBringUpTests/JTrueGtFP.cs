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
    public static int JTrueGtFP(float x)
    {
        int returnValue = -1;

        if (x > 1f)                returnValue = 4;
        else if (x > 0f)                returnValue = 3;
        else if (x > -1f)               returnValue = 2;
        else if (x > Single.MinValue)     returnValue = 1;

        return returnValue;
    }

    public static int Main()
    {
        int returnValue = Pass;

        if (JTrueGtFP(-1f) != 1) returnValue = Fail;
        if (JTrueGtFP(0f) != 2) returnValue = Fail;
        if (JTrueGtFP(1f) != 3) returnValue = Fail;
        if (JTrueGtFP(2f) != 4) returnValue = Fail;

        return returnValue;
    }
}
