// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class P1
{
    [ThreadStatic]
    public static long z;
}

class Program
{
    [ThreadStatic]
    static long x;
    [ThreadStatic]
    static string y;
    [ThreadStatic]
    static List<int> list;


    static void Main()
    {
        Program.x = 5;
        //Program.y = 5;
        P1.z = 5;
        new Program().Test();
        Console.WriteLine(Program.x);
        Console.WriteLine(Program.y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    long Test()
    {
        Program.x = 5;
        Program.y = "a";
        Program.list = new List<int>();
        P1.z = 5;
        return 0;
    }
}
