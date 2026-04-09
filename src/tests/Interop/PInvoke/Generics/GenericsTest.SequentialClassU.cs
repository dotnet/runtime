// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<uint> GetSequentialClassU(uint e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSequentialClassUOut(uint e00, out SequentialClass<uint> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSequentialClassUPtr")]
    public static extern ref readonly SequentialClass<uint> GetSequentialClassURef(uint e00);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<uint> AddSequentialClassU(SequentialClass<uint> lhs, SequentialClass<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<uint> AddSequentialClassUs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SequentialClass<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern SequentialClass<uint> AddSequentialClassUs(in SequentialClass<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestSequentialClassU()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassU(1u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassUOut(1u, out GenericsNative.SequentialClass<uint> value2));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSequentialClassURef(1u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassU(default, default));

        GenericsNative.SequentialClass<uint>[] values = new GenericsNative.SequentialClass<uint>[] {
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassUs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSequentialClassUs(in values[0], values.Length));
    }
}
