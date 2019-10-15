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
    static void Array1(int[] a)
    {
        a[1] = 5;
    }

    static int Main()
    {
        int[] a = {1, 2, 3, 4};
        Array1(a);

        if (a[1] != 5) return Fail;
        return Pass;
    }
}