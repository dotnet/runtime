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
    static int ArrayMD1()
    {
        int[,] a = {{1, 2}, {3, 4}};
        return a[0, 1];
    }

    static int Main()
    {
        if (ArrayMD1() != 2) return Fail;
        return Pass;
    }
}