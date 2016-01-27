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
    public static int Args5(int a, int b, int c, int d, int e)
    {
        return a+b+c+d+e;
    }

    public static int Main()
    {
        int y = Args5(1,2,3,4,5);
        if (y == 15) return Pass;
        else return Fail;
    }
}
