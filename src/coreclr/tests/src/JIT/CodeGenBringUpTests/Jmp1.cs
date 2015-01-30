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
    public static int Jmp1(int x)
    {
        goto L1;
L2:
        x = x+1;
        goto L3;
L1:
        x = x+1;
        goto L2;
L3:
        return x+1;
    }

    public static int Main()
    {
        int y = Jmp1(1);
        if (y == 4) return Pass;
        else return Fail;
    }
}
