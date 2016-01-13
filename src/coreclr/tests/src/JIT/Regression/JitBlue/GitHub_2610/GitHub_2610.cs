// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

public struct MyValue
{
    public long val;
    public MyValue(long a)
    { val = a; }
}

public struct MyStruct
{
    public MyValue v1;
    public MyValue v2;

    public MyStruct(MyValue a, MyValue b)
    { v1 = a; v2 = b; }
}

class Program
{
    static int Main()
    {
        MyValue p1 = new MyValue(10);
        MyValue p2 = new MyValue(20);

        MyStruct c1 = new MyStruct(p1, p2);
        MyStruct c2 = new MyStruct(p2, p1);

        bool b1 = Program.IsXGeater(c1);
        bool b2 = Program.IsXGeater(c2);
        bool b3 = Program.IsXGeater(c1);
        bool b4 = Program.IsXGeater(c2);

        if (b1)
        {
            Console.WriteLine("Fail");
            return -1;
        }

        if (!b2)
        {
            Console.WriteLine("Fail");
            return -1;
        }

        if (b3)
        {
            Console.WriteLine("Fail");
            return -1;
        }

        if (!b4)
        {
            Console.WriteLine("Fail");
            return -1;
        }

        Console.WriteLine("Pass");
        return 100;
    }

    // Return true if p1.x > p2.y
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static bool IsXGeater(MyStruct line)
    {
        if (line.v1.val > line.v2.val)
        {
            line = new MyStruct(line.v2, line.v1);
            return true;
        }

        return false;
    }
}
