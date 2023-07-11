// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

class P1<T>
{
    [ThreadStatic]
    public static long z;
    [ThreadStatic]
    public static List<T> list;
}

class Program
{
    [ThreadStatic]
    static uint x;
    //[ThreadStatic]
    //static string y;
    //[ThreadStatic]
    //static int z;
    //[ThreadStatic]
    //static List<int> list;


    static void Main()
    {
        //Program.x = 5;
        ////Program.y = "a";
        //Program.list = new List();
        P1<int>.z = 0x900DF00E;
        Program.x = 0x900DF00D;
        Program obj = new Program();
        obj.Test(1);
        //Console.WriteLine("Hello");
        //Console.WriteLine(Program.x + P1.z);
        //Console.WriteLine(Program.y);
        //Console.WriteLine(Program.z);
        //Console.WriteLine(Program.x + Program.z | P1.z);

        //Program.z = 15;

        Console.WriteLine(CultureInfo.CurrentCulture);

        Console.WriteLine(Program.x);
        Console.WriteLine(P1<int>.z);
        //Console.WriteLine(Program.z);
        Console.WriteLine(P1<int>.list);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test1() { Console.WriteLine("inside task"); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    long Test(int n)
    {
        //for (int i = 0; i < n; i++)
        {
            //Program.x = 0x900DF00D;
            //Program.z = 23;
            //Program.y = "a";
            P1<int>.list = new List<int>();
            //P1.z = 0x900DF00E;
        }
        return 0;
    }
}
