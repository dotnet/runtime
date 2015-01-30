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

    public static int Main()
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
