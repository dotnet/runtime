// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<float> GetSequentialClassF(float e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSequentialClassFOut(float e00, out SequentialClass<float> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSequentialClassFPtr")]
    public static extern ref readonly SequentialClass<float> GetSequentialClassFRef(float e00);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<float> AddSequentialClassF(SequentialClass<float> lhs, SequentialClass<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<float> AddSequentialClassFs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SequentialClass<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<float> AddSequentialClassFs(in SequentialClass<float> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestSequentialClassF()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassF(1.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassFOut(1.0f, out GenericsNative.SequentialClass<float> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassFRef(1.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassF(default, default));

        GenericsNative.SequentialClass<float>[] values = new GenericsNative.SequentialClass<float>[] {
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassFs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassFs(in values[0], values.Length));
    }
}
