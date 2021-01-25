// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
