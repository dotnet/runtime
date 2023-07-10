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
    //[ThreadStatic]
    //static string y;
    [ThreadStatic]
     int z;
    ////[ThreadStatic]
    //static List<int> list;


    static void Main()
    {
        //Program.x = 5;
        ////Program.y = "a";
        //Program.list = new List<int>();
        //P1.z = 5;
        Program obj = new Program();
        obj.Test(5);
        //Console.WriteLine("Hello");
        Console.WriteLine(Program.x + obj.z);
        //Console.WriteLine(Program.y);
        //Console.WriteLine(Program.z);
        Console.WriteLine(P1.z);
        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    long Test(int n)
    {
        for (int i = 0; i < n; i++)
        {
            Program.x = 20;
            new Program().z = 23;
            //Program.y = "a";
            //Program.list = new List<int>();
            P1.z = 5;
        }
        return 0;
    }
}
