// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;

struct MyStruct
{
    // Struct containing 4 fields, 3 of which are longs that will be decomposed.
    // The bug was that this resulted in 7 input registers to the GT_FIELD_LIST
    // parameter, which can't be accommodated by the register allocator.

    public MyStruct(long l1, long l2, long l3, int i)
    {
        f1 = l1;
        f2 = l2;
        f3 = l3;
        f4 = new int[i];
        f4[0] = i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static MyStruct newMyStruct(long l1, long l2, long l3, int i)
    {
        return new MyStruct(l1, l2, l3, i);
    }

    public long f1;
    public long f2;
    public long f3;
    public int[] f4;
}

struct MyStruct2
{
    // This is a variation that includes a double field, to ensure that a mix of
    // field types are supported.
    public MyStruct2(long l1, long l2, double d, int i)
    {
        f1 = l1;
        f2 = l2;
        f3 = d;
        f4 = new int[i];
        f4[0] = i;
    }

    public long f1;
    public long f2;
    public double f3;
    public int[] f4;
}

struct MyStruct3
{
    // And finally one that includes longs and a double, but no ref.
    public MyStruct3(long l1, long l2, double d, int i)
    {
        f1 = l1;
        f2 = l2;
        f3 = d;
        f4 = i;
    }

    public long f1;
    public long f2;
    public double f3;
    public int f4;
}

public class Program
{

    static int Pass = 100;
    static int Fail = -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddFields(MyStruct s)
    {
        return (int)(s.f1 + s.f2 + s.f3 + s.f4[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddFields2(MyStruct2 s)
    {
        return (int)(s.f1 + s.f2 + (int)s.f3 + s.f4[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddFields3(MyStruct3 s)
    {
        return (int)(s.f1 + s.f2 + (int)s.f3 + s.f4);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;
        MyStruct s = new MyStruct(1, 2, 3, 4);
        int sum = AddFields(s);
        if (sum != 10)
        {
            Console.WriteLine("Failed first call");
            returnVal = Fail;
        }
        s = MyStruct.newMyStruct(1, 2, 3, 4);
        sum = AddFields(s);
        if (sum != 10)
        {
            Console.WriteLine("Failed second call");
            returnVal = Fail;
        }
        MyStruct2 s2 = new MyStruct2(1, 2, 3.0, 4);
        sum = AddFields2(s2);
        if (sum != 10)
        {
            Console.WriteLine("Failed third call");
            returnVal = Fail;
        }
        MyStruct3 s3 = new MyStruct3(1, 2, 3.0, 4);
        sum = AddFields3(s3);
        if (sum != 10)
        {
            Console.WriteLine("Failed fourth call");
            returnVal = Fail;
        }
        if (returnVal == Pass)
        {
            Console.WriteLine("Pass");
        }
        return returnVal;
    }
}
