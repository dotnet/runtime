// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.aa

// Found by Antigen
//
// Test ARM64 read-modify-write (RMW) intrinsics with identical target/accumulator local to arguments

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Numerics;
using Xunit;

public class Runtime_91209
{
    //////////////////////////// test AdvSimd.MultiplyAddByScalar()

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<int> GetVector64IntValue()
    {
        return Vector64.Create(6);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<int> Problem1()
    {
        Vector64<int> l = GetVector64IntValue();
        return AdvSimd.MultiplyAddByScalar(l, l, l);
    }

    [Fact]
    public static int Test1()
    {
        Console.WriteLine("Test1");

        if (!AdvSimd.IsSupported)
        {
            return 100;
        }

        Vector64<int> r1 = Problem1();
        return (r1.GetElement(0) + r1.GetElement(1)) == 84 ? 100 : 101;
    }

    //////////////////////////// test AdvSimd.MultiplyAdd()

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<sbyte> GetVector64SbyteValue()
    {
        return Vector64.Create((sbyte)2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<sbyte> Problem2()
    {
        Vector64<sbyte> l = GetVector64SbyteValue();
        return AdvSimd.MultiplyAdd(l, l, l);
    }

    [Fact]
    public static int Test2()
    {
        Console.WriteLine("Test2");

        if (!AdvSimd.IsSupported)
        {
            return 100;
        }

        Vector64<sbyte> r1 = Problem2();
        return (r1.GetElement(0) + r1.GetElement(1)) == 12 ? 100 : 101;
    }

    //////////////////////////// test AdvSimd.VectorTableLookupExtension()

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<byte> GetVector64ByteValue()
    {
        return Vector64.Create((byte)2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<byte> Problem3()
    {
        Vector64<byte> l = GetVector64ByteValue();
        Vector128<byte> t = Vector128.Create((byte)7);
        return AdvSimd.VectorTableLookupExtension(l, (t,t), l);
    }

    [Fact]
    public static int Test3()
    {
        Console.WriteLine("Test3");

        if (!AdvSimd.IsSupported)
        {
            return 100;
        }

        Vector64<byte> r1 = Problem3();
        return r1.GetElement(2) == 7 ? 100 : 101;
    }

    //////////////////////////// test AdvSimd.VectorTableLookupExtension() with identical table + target

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<byte> GetVector128ByteValue()
    {
        return Vector128.Create((byte)7);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<byte> Problem4()
    {
        Vector128<byte> l = GetVector128ByteValue();
        return AdvSimd.VectorTableLookupExtension(Vector128.GetLower<byte>(l), (l,l), Vector128.GetLower<byte>(l));
    }

    [Fact]
    public static int Test4()
    {
        Console.WriteLine("Test4");

        if (!AdvSimd.IsSupported)
        {
            return 100;
        }

        Vector64<byte> r1 = Problem4();
        return r1.GetElement(7) == 7 ? 100 : 101;
    }
}
