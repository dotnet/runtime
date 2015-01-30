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
    public static int AsgAnd1(int x) { x &= 3; return x; }

    public static int Main()
    {
        if (AsgAnd1(0xf) == 3) return Pass;
        else return Fail;
    }
}
