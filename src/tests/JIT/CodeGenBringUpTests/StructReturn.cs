// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;

// int
public struct MyStructInt1
{
    public int int1;
}

public struct MyStructInt2
{
    public int int1;
    public int int2;
}

public struct MyStructInt4
{
    public int int1;
    public int int2;
    public int int3;
    public int int4;
}

// long
public struct MyStructLong1
{
    public long long1;
}

public struct MyStructLong2
{
    public long long1;
    public long long2;
}

public struct MyStructLong4
{
    public long long1;
    public long long2;
    public long long3;
    public long long4;
}

// float
public struct MyStructFloat1
{
    public float float1;
}

public struct MyStructFloat2
{
    public float float1;
    public float float2;
}

public struct MyStructFloat3
{
    public float float1;
    public float float2;
    public float float3;
}

public struct MyStructFloat4
{
    public float float1;
    public float float2;
    public float float3;
    public float float4;
}

public struct MyStructFloat5
{
    public float float1;
    public float float2;
    public float float3;
    public float float4;
    public float float5;
}

public struct MyStructFloat8
{
    public float float1;
    public float float2;
    public float float3;
    public float float4;
    public float float5;
    public float float6;
    public float float7;
    public float float8;
}

// double
public struct MyStructDouble1
{
    public double double1;
}

public struct MyStructDouble2
{
    public double double1;
    public double double2;
}

public struct MyStructDouble3
{
    public double double1;
    public double double2;
    public double double3;
}

public struct MyStructDouble4
{
    public double double1;
    public double double2;
    public double double3;
    public double double4;
}

public struct MyStructDouble5
{
    public double double1;
    public double double2;
    public double double3;
    public double double4;
    public double double5;
}

public struct MyStructDouble8
{
    public double double1;
    public double double2;
    public double double3;
    public double double4;
    public double double5;
    public double double6;
    public double double7;
    public double double8;
}

public class BringUpTest_StructReturn
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructInt1 returnMyStructInt1(int x)
    {
        MyStructInt1 s = new MyStructInt1();
        s.int1 = x + 1;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructInt2 returnMyStructInt2(int x)
    {
        MyStructInt2 s = new MyStructInt2();
        s.int1 = x + 1;
        s.int2 = x + 2;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructInt4 returnMyStructInt4(int x)
    {
        MyStructInt4 s = new MyStructInt4();
        s.int1 = x + 1;
        s.int2 = x + 2;
        s.int3 = x + 3;
        s.int4 = x + 4;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructLong1 returnMyStructLong1(long x)
    {
        MyStructLong1 s = new MyStructLong1();
        s.long1 = x + 1;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructLong2 returnMyStructLong2(long x)
    {
        MyStructLong2 s = new MyStructLong2();
        s.long1 = x + 1;
        s.long2 = x + 2;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructLong4 returnMyStructLong4(long x)
    {
        MyStructLong4 s = new MyStructLong4();
        s.long1 = x + 1;
        s.long2 = x + 2;
        s.long3 = x + 3;
        s.long4 = x + 4;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructFloat1 returnMyStructFloat1(float x)
    {
        MyStructFloat1 s = new MyStructFloat1();
        s.float1 = x + 1.1f;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructFloat2 returnMyStructFloat2(float x)
    {
        MyStructFloat2 s = new MyStructFloat2();
        s.float1 = x + 1.1f;
        s.float2 = x + 2.1f;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructFloat3 returnMyStructFloat3(float x)
    {
        MyStructFloat3 s = new MyStructFloat3();
        s.float1 = x + 1.1f;
        s.float2 = x + 2.1f;
        s.float3 = x + 3.1f;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructFloat4 returnMyStructFloat4(float x)
    {
        MyStructFloat4 s = new MyStructFloat4();
        s.float1 = x + 1.1f;
        s.float2 = x + 2.1f;
        s.float3 = x + 3.1f;
        s.float4 = x + 4.1f;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructFloat5 returnMyStructFloat5(float x)
    {
        MyStructFloat5 s = new MyStructFloat5();
        s.float1 = x + 1.1f;
        s.float2 = x + 2.1f;
        s.float3 = x + 3.1f;
        s.float4 = x + 4.1f;
        s.float5 = x + 5.1f;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructFloat8 returnMyStructFloat8(float x)
    {
        MyStructFloat8 s = new MyStructFloat8();
        s.float1 = x + 1.1f;
        s.float2 = x + 2.1f;
        s.float3 = x + 3.1f;
        s.float4 = x + 4.1f;
        s.float5 = x + 5.1f;
        s.float6 = x + 6.1f;
        s.float7 = x + 7.1f;
        s.float8 = x + 8.1f;
        return s;
    }

    // double
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructDouble1 returnMyStructDouble1(double x)
    {
        MyStructDouble1 s = new MyStructDouble1();
        s.double1 = x + 1.1;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructDouble2 returnMyStructDouble2(double x)
    {
        MyStructDouble2 s = new MyStructDouble2();
        s.double1 = x + 1.1;
        s.double2 = x + 2.1;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructDouble3 returnMyStructDouble3(double x)
    {
        MyStructDouble3 s = new MyStructDouble3();
        s.double1 = x + 1.1;
        s.double2 = x + 2.1;
        s.double3 = x + 3.1;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructDouble4 returnMyStructDouble4(double x)
    {
        MyStructDouble4 s = new MyStructDouble4();
        s.double1 = x + 1.1;
        s.double2 = x + 2.1;
        s.double3 = x + 3.1;
        s.double4 = x + 4.1;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructDouble5 returnMyStructDouble5(double x)
    {
        MyStructDouble5 s = new MyStructDouble5();
        s.double1 = x + 1.1;
        s.double2 = x + 2.1;
        s.double3 = x + 3.1;
        s.double4 = x + 4.1;
        s.double5 = x + 5.1;
        return s;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static MyStructDouble8 returnMyStructDouble8(double x)
    {
        MyStructDouble8 s = new MyStructDouble8();
        s.double1 = x + 1.1;
        s.double2 = x + 2.1;
        s.double3 = x + 3.1;
        s.double4 = x + 4.1;
        s.double5 = x + 5.1;
        s.double6 = x + 6.1;
        s.double7 = x + 7.1;
        s.double8 = x + 8.1;
        return s;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        // int
        MyStructInt1 sI1 = returnMyStructInt1(100);
        if (sI1.int1 != 101) return Fail;
        Console.WriteLine(sI1);

        MyStructInt2 sI2 = returnMyStructInt2(200);
        if (sI2.int1 != 201 || sI2.int2 != 202) return Fail;
        Console.WriteLine(sI2);

        MyStructInt4 sI4 = returnMyStructInt4(400);
        if (sI4.int1 != 401 || sI4.int2 != 402 || sI4.int3 != 403 || sI4.int4 != 404) return Fail;
        Console.WriteLine(sI4);

        // long
        MyStructLong1 sL1 = returnMyStructLong1(100);
        if (sL1.long1 != 101) return Fail;
        Console.WriteLine(sL1);

        MyStructLong2 sL2 = returnMyStructLong2(200);
        if (sL2.long1 != 201 || sL2.long2 != 202) return Fail;
        Console.WriteLine(sL2);

        MyStructLong4 sL4 = returnMyStructLong4(400);
        if (sL4.long1 != 401 || sL4.long2 != 402 || sL4.long3 != 403 || sL4.long4 != 404) return Fail;
        Console.WriteLine(sL4);


        // float
        MyStructFloat1 sF1 = returnMyStructFloat1(100.0f);
        if (sF1.float1 != 101.1f) return Fail;
        Console.WriteLine(sF1);

        MyStructFloat2 sF2 = returnMyStructFloat2(200.0f);
        if (sF2.float1 != 201.1f || sF2.float2 != 202.1f) return Fail;
        Console.WriteLine(sF2);

        MyStructFloat3 sF3 = returnMyStructFloat3(300.0f);
        if (sF3.float1 != 301.1f || sF3.float2 != 302.1f || sF3.float3 != 303.1f) return Fail;
        Console.WriteLine(sF3);

        MyStructFloat4 sF4 = returnMyStructFloat4(400.0f);
        if (sF4.float1 != 401.1f || sF4.float2 != 402.1f || sF4.float3 != 403.1f || sF4.float4 != 404.1f) return Fail;
        Console.WriteLine(sF4);

        MyStructFloat5 sF5 = returnMyStructFloat5(500.0f);
        if (sF5.float1 != 501.1f || sF5.float2 != 502.1f || sF5.float3 != 503.1f || sF5.float4 != 504.1f || sF5.float5 != 505.1f) return Fail;
        Console.WriteLine(sF5);

        MyStructFloat8 sF8 = returnMyStructFloat8(800.0f);
        if (sF8.float1 != 801.1f || sF8.float2 != 802.1f || sF8.float3 != 803.1f || sF8.float4 != 804.1f || sF8.float5 != 805.1f || sF8.float6 != 806.1f || sF8.float7 != 807.1f || sF8.float8 != 808.1f) return Fail;
        Console.WriteLine(sF8);

        // double
        MyStructDouble1 sD1 = returnMyStructDouble1(100.0d);
        if (sD1.double1 != 101.1d) return Fail;
        Console.WriteLine(sD1);

        MyStructDouble2 sD2 = returnMyStructDouble2(200.0d);
        if (sD2.double1 != 201.1d || sD2.double2 != 202.1d) return Fail;
        Console.WriteLine(sD2);

        MyStructDouble3 sD3 = returnMyStructDouble3(300.0d);
        if (sD3.double1 != 301.1d || sD3.double2 != 302.1d || sD3.double3 != 303.1d) return Fail;
        Console.WriteLine(sD3);

        MyStructDouble4 sD4 = returnMyStructDouble4(400.0d);
        if (sD4.double1 != 401.1d || sD4.double2 != 402.1d || sD4.double3 != 403.1d || sD4.double4 != 404.1d) return Fail;
        Console.WriteLine(sD4);

        MyStructDouble5 sD5 = returnMyStructDouble5(500.0d);
        if (sD5.double1 != 501.1d || sD5.double2 != 502.1d || sD5.double3 != 503.1d || sD5.double4 != 504.1d || sD5.double5 != 505.1d) return Fail;
        Console.WriteLine(sD5);

        MyStructDouble8 sD8 = returnMyStructDouble8(800.0d);
        if (sD8.double1 != 801.1d || sD8.double2 != 802.1d || sD8.double3 != 803.1d || sD8.double4 != 804.1d || sD8.double5 != 805.1d || sD8.double6 != 806.1d || sD8.double7 != 807.1d || sD8.double8 != 808.1d) return Fail;
        Console.WriteLine(sD8);

        return Pass;
    }
}
