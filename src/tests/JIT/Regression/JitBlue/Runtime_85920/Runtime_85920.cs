// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using Xunit;

public static class Test
{
    public const uint val0 = 0x00112233;
    public const uint val1 = 0x44556677;
    public const uint val2 = 0x8899aabb;
    public const uint val3 = 0xccddeeff;

    public static void CheckVector(Vector128<uint> v)
    {
        if (v.GetElement(0) != val0)
            Environment.Exit(1);
        if (v.GetElement(1) != val1)
            Environment.Exit(2);
        if (v.GetElement(2) != val2)
            Environment.Exit(3);
        if (v.GetElement(3) != val3)
            Environment.Exit(4);
    }

    public static void HoldInt<T>(int i1, T arg)
    {
        // In the caller method, keeps first arg on the stack before second arg is computed
    }

    public static Vector128<uint> GetVector()
    {
        return Vector128.Create(val0, val1, val2, val3);
    }
}

public class Obj {
    long field;

    public Obj(Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field = 0;
    }

    public Obj(int i1, Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field = i1;
    }

    public Obj(int i1, int i2, Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field = i1 + i2;
    }

    public static int Method1(Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        return 0;
    }

    public static int Method2(int i1, Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        return 0;
    }

    public static int Method3(int i1, int i2, Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        return 0;
    }
}

public struct VT8
{
    long field;

    public VT8(Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field = 0;
    }

    public VT8(int i1, Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field = i1;
    }

    public VT8(int i1, int i2, Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field = i1 + i2;
    }
}

public struct VT16
{
    long field1;
    long field2;

    public VT16(Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field1 = 0;
        field2 = 0;
    }

    public VT16(int i1, Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field1 = i1;
        field2 = 0;
    }

    public VT16(int i1, int i2, Vector128<uint> v1)
    {
        Test.CheckVector(v1);
        field1 = i1;
        field2 = i2;
    }
}

public class Program 
{
    [Fact]
    public static void TestCall ()
    {
        Vector128<uint> vec = Test.GetVector();
        Obj.Method1(vec);
        Obj.Method2(2, vec);
        Obj.Method3(2, 3, vec);
        Console.WriteLine("OK call empty stack");
        Test.HoldInt(1, Obj.Method1(vec));
        Test.HoldInt(1, Obj.Method2(2, vec));
        Test.HoldInt(1, Obj.Method3(2, 3, vec));
        Console.WriteLine("OK call nonempty stack");
    }

    [Fact]
    public static void TestNewobj ()
    {
        Vector128<uint> vec = Test.GetVector();
        Obj o = new Obj(vec);
        o = new Obj(2, vec);
        o = new Obj(2, 3, vec);
        Console.WriteLine("OK newobj empty stack");
        Test.HoldInt(1, new Obj(vec));
        Test.HoldInt(1, new Obj(2, vec));
        Test.HoldInt(1, new Obj(2, 3, vec));
        Console.WriteLine("OK newobj nonempty stack");
    }

    [Fact]
    public static void TestNewobjVT8 ()
    {
        Vector128<uint> vec = Test.GetVector();
        VT8 vt = new VT8(vec);
        vt = new VT8(2, vec);
        vt = new VT8(2, 3, vec);
        Console.WriteLine("OK newobj vt8 empty stack");
        Test.HoldInt(1, new VT8(vec));
        Test.HoldInt(1, new VT8(2, vec));
        Test.HoldInt(1, new VT8(2, 3, vec));
        Console.WriteLine("OK newobj vt8 nonempty stack");
    }

    [Fact]
    public static void TestNewobjVT16 ()
    {
        Vector128<uint> vec = Test.GetVector();
        VT16 vt = new VT16(vec);
        vt = new VT16(2, vec);
        vt = new VT16(2, 3, vec);
        Console.WriteLine("OK newobj vt16 empty stack");
        Test.HoldInt(1, new VT16(vec));
        Test.HoldInt(1, new VT16(2, vec));
        Test.HoldInt(1, new VT16(2, 3, vec));
        Console.WriteLine("OK newobj vt16 nonempty stack");
    }
}
