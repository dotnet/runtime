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
    public static int NestedCall(int x)
    {
        return x * x;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int NestedCall(int a, int b)
    {
        int c = NestedCall(NestedCall(a)) + NestedCall(NestedCall(b));
        return c;
    }

    public static int Main()
    {
        int y = NestedCall(2, 3);
        if (y == 97) return Pass;
        else return Fail;
    }
}
