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
    public static void NotRMW(ref int x) { x = ~x; }

    public static int Main()
    {
        int x = -1;
        NotRMW(ref x);
        if (x == 0) return Pass;
        else return Fail;
    }
}
