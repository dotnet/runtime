// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<double> GetSequentialClassD(double e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSequentialClassDOut(double e00, out SequentialClass<double> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSequentialClassDPtr")]
    public static extern ref readonly SequentialClass<double> GetSequentialClassDRef(double e00);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<double> AddSequentialClassD(SequentialClass<double> lhs, SequentialClass<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<double> AddSequentialClassDs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SequentialClass<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<double> AddSequentialClassDs(in SequentialClass<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestSequentialClassD()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassD(1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassDOut(1.0, out GenericsNative.SequentialClass<double> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassDRef(1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassD(default, default));

        GenericsNative.SequentialClass<double>[] values = new GenericsNative.SequentialClass<double>[] {
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassDs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassDs(in values[0], values.Length));
    }
}
