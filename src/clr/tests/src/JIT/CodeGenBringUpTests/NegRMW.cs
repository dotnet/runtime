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
    public static void NegRMW(ref int x) { x = -x; }

    public static int Main()
    {
        int x = 12;
        NegRMW(ref x);
        if (x == -12) return Pass;
        else return Fail;
    }
}
