// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<bool> GetSequentialClassB(bool e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSequentialClassBOut(bool e00, out SequentialClass<bool> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSequentialClassBPtr")]
    public static extern ref readonly SequentialClass<bool> GetSequentialClassBRef(bool e00);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<bool> AddSequentialClassB(SequentialClass<bool> lhs, SequentialClass<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<bool> AddSequentialClassBs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SequentialClass<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<bool> AddSequentialClassBs(in SequentialClass<bool> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestSequentialClassB()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassB(true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassBOut(true, out GenericsNative.SequentialClass<bool> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassBRef(true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassB(default, default));

        GenericsNative.SequentialClass<bool>[] values = new GenericsNative.SequentialClass<bool>[] {
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassBs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassBs(in values[0], values.Length));
    }
}
