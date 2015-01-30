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
    public static int Or1(int x) { return x | 0xa; }

    public static int Main()
    {
        int y = Or1(4);
        if (y == 14) return Pass;
        else return Fail;
    }
}
