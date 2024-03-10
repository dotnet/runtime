// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<char> GetSequentialClassC(char e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSequentialClassCOut(char e00, out SequentialClass<char> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSequentialClassCPtr")]
    public static extern ref readonly SequentialClass<char> GetSequentialClassCRef(char e00);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<char> AddSequentialClassC(SequentialClass<char> lhs, SequentialClass<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<char> AddSequentialClassCs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SequentialClass<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<char> AddSequentialClassCs(in SequentialClass<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestSequentialClassC()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassC('1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassCOut('1', out GenericsNative.SequentialClass<char> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassCRef('1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassC(default, default));

        GenericsNative.SequentialClass<char>[] values = new GenericsNative.SequentialClass<char>[] {
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassCs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassCs(in values[0], values.Length));
    }
}
