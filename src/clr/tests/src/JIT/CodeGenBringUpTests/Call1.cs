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

