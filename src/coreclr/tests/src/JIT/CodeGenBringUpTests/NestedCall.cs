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
