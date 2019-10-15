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
    public static int FiboRec(int f0, int f1, int n)
    {
        if (n <= 0) return f0;
        if (n == 1) return f1;

        // splitting the expression to avoid running into rationalizer issue
        int a = FiboRec(f0, f1, n - 1) + FiboRec(f0, f1, n - 2);
        return a;
    }

    public static int Main()
    {
        int y = FiboRec(0, 1, 7);
        if (y == 13) return Pass;
        else return Fail;
    }
}
