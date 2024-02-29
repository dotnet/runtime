// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Prog
{
    static void Main()
    {
        Foo();
        Console.ReadKey();
    }

    struct MyStruct
    {
        public string a1;
        public string a2;
        public string a3;
        public string a4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Foo()
    {
        MyStruct ms = new MyStruct
        {
            a1 = 10001.ToString(),
            a2 = 10002.ToString(),
            a3 = 10003.ToString(),
            a4 = 10004.ToString()
        };
        ms = Test(ms);
        Console.WriteLine(ms.a1);
        Console.WriteLine(ms.a2);
        Console.WriteLine(ms.a3);
        Console.WriteLine(ms.a4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static MyStruct Test(MyStruct ms) => ms;
}