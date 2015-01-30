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
    public static void RightShiftRef(ref int x, int y) { x >>= y; }

    public static int Main()
    {
        int x = 36;
        RightShiftRef(ref x, 3);
        if (x == 4) return Pass;
        else return Fail;
    }
}
