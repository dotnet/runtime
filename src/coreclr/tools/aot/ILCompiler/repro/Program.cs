//i = 9;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class A { [ThreadStatic] public static string a; }
public class B { public static string s = "a"; }

class Program
{
    //[ThreadStatic]
    //static string i = 5.ToString();
    ////[ThreadStatic]
    //static string j = 10.ToString();
    //[ThreadStatic] // problematic
    //static string k = 5.ToString();

    //static int l = GetInt();
    [ThreadStatic] static int m = 6;
    [ThreadStatic] static int n = 5;

    //[MethodImpl(MethodImplOptions.NoInlining)]
    //private static int GetInt() => 1;

    //[ThreadStatic]
    //static string j = 5.ToString();
    //static Program()
    //{
    //    i = 6;
    //}
    static void Main()
    {
        Test1();
        Test2();
        //A.a = "a";
        //i = 9;
        //Consume(A.a, i);

        //i = 9;
        //j = 10;
        //for (int k = 0; k < 10; k++)
        {
            //Consume(j);
            //for (int _i = 0;_i < 10; _i++)
            //{
            //    Test();
            //    //Consume(i);
            //    //Consume(n);

            //    //Consume(j);

            //    //Consume(l);
            //Consume(n);

            //    //Consume(l);
            //    //Consume(m);


            //}

            //Consume(j);
            //Consume(k);

            //Consume(A.a);
            //Consume(j);
        }
        //Consume(k);

        //Consume(A.a, B.s);

        //A.a = "a";
        //Consume(A.a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test1()
    {
        Consume(m);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test2()
    {
        Consume(n);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(string a)
    {
        Console.WriteLine("here" + a);
        //i = 5;
        //Consume(i);
        //i = "a";
        //j = "a";
        Consume("i", "j");
        //Consume(i, j);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(int a)
    {

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(string a, string b)
    {

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(string a, int b)
    {

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Produce()
    {
        return 1;
    }

}
