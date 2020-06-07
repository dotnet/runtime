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
    public static void BinaryRMW(ref int x, int y)
    {
        x += y;
        x |= 2;
    }

    public static int Main()
    {
        int x = 12;
        BinaryRMW(ref x, 17);
        if (x == 31) return Pass;
        else return Fail;
    }
}
