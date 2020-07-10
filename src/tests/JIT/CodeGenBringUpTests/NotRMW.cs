// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
