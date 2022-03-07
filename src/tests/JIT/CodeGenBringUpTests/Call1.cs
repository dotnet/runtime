// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest_Call1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void  M() { Console.WriteLine("Hello"); }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void  Call1()
    {
        M();
    }
    public static int Main()
    {
        Call1();
        return 100;
    }
}

