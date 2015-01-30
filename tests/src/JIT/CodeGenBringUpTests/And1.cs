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
    public static int And1(int x) { return x & 1; }

    public static int Main()
    {
        int y = And1(17);
        if (y == 1) return Pass;
        else return Fail;
    }
}
