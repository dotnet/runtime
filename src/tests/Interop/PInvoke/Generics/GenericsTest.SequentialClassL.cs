// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<long> GetSequentialClassL(long e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSequentialClassLOut(long e00, out SequentialClass<long> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSequentialClassLPtr")]
    public static extern ref readonly SequentialClass<long> GetSequentialClassLRef(long e00);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<long> AddSequentialClassL(SequentialClass<long> lhs, SequentialClass<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<long> AddSequentialClassLs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SequentialClass<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<long> AddSequentialClassLs(in SequentialClass<long> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestSequentialClassL()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassL(1L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassLOut(1L, out GenericsNative.SequentialClass<long> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassLRef(1L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassL(default, default));

        GenericsNative.SequentialClass<long>[] values = new GenericsNative.SequentialClass<long>[] {
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassLs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassLs(in values[0], values.Length));
    }
}
