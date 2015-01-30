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
    public static void Ind1(ref int x) { x = 1; return; }

    public static int Main()
    {
        int y = 0;
        Ind1(ref y);
        if (y == 1) return Pass;
        else return Fail;
    }
}
