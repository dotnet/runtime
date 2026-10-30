// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_127260
{
    [ConditionalFact(typeof(Sse41), nameof(Sse41.IsSupported))]
    public static void TestBlendVariable()
    {
        Assert.Equal(Vector128<float>.Zero,
            BlendVariableSse41Single(Vector128.Create(-1.0f), Vector128<float>.Zero, Vector128.Create(-0.0f)));

        Assert.Equal(Vector128<double>.Zero,
            BlendVariableSse41Double(Vector128.Create(-1.0), Vector128<double>.Zero, Vector128.Create(-0.0)));

        Assert.Equal(Vector128<sbyte>.Zero,
            BlendVariableSse41Int8(Vector128.Create<sbyte>(-1), Vector128<sbyte>.Zero, Vector128.Create(sbyte.MinValue)));

        Assert.Equal(Vector128.Create<short>(0x00FF),
            BlendVariableSse41Int16(Vector128.Create<short>(-1), Vector128<short>.Zero, Vector128.Create(short.MinValue)));

        Assert.Equal(Vector128.Create<int>(0x00FFFFFF),
            BlendVariableSse41Int32(Vector128.Create<int>(-1), Vector128<int>.Zero, Vector128.Create(int.MinValue)));

        Assert.Equal(Vector128.Create<long>(0x00FFFFFF_FFFFFFFF),
            BlendVariableSse41Int64(Vector128.Create<long>(-1), Vector128<long>.Zero, Vector128.Create(long.MinValue)));
    }

    [ConditionalFact(typeof(Avx512BW.VL), nameof(Avx512BW.VL.IsSupported))]
    public static void TestBlendVariableMask()
    {
        Assert.Equal(Vector128<float>.Zero,
            BlendVariableAvx512Single(Vector128.Create(-1.0f), Vector128<float>.Zero, Vector128.Create(-0.0f)));

        Assert.Equal(Vector128<double>.Zero,
            BlendVariableAvx512Double(Vector128.Create(-1.0), Vector128<double>.Zero, Vector128.Create(-0.0)));

        Assert.Equal(Vector128<sbyte>.Zero,
            BlendVariableAvx512Int8(Vector128.Create<sbyte>(-1), Vector128<sbyte>.Zero, Vector128.Create(sbyte.MinValue)));

        Assert.Equal(Vector128<short>.Zero,
            BlendVariableAvx512Int16(Vector128.Create<short>(-1), Vector128<short>.Zero, Vector128.Create(short.MinValue)));

        Assert.Equal(Vector128<int>.Zero,
            BlendVariableAvx512Int32(Vector128.Create<int>(-1), Vector128<int>.Zero, Vector128.Create(int.MinValue)));

        Assert.Equal(Vector128<long>.Zero,
            BlendVariableAvx512Int64(Vector128.Create<long>(-1), Vector128<long>.Zero, Vector128.Create(long.MinValue)));
    }

    [ConditionalFact(typeof(Avx512BW.VL), nameof(Avx512BW.VL.IsSupported))]
    public static void TestContainableMask()
    {
        Assert.Equal(Vector128<float>.Zero,
            AddToNegativeSingle(Vector128.Create(-1.0f), Vector128<float>.One));

        Assert.Equal(Vector128<double>.Zero,
            AddToNegativeDouble(Vector128.Create(-1.0), Vector128<double>.One));

        Assert.Equal(Vector128<sbyte>.Zero,
            AddToNegativeInt8(Vector128.Create<sbyte>(-1), Vector128<sbyte>.One));

        Assert.Equal(Vector128<short>.Zero,
            AddToNegativeInt16(Vector128.Create<short>(-1), Vector128<short>.One));

        Assert.Equal(Vector128<int>.Zero,
            AddToNegativeInt32(Vector128.Create<int>(-1), Vector128<int>.One));

        Assert.Equal(Vector128<long>.Zero,
            AddToNegativeInt64(Vector128.Create<long>(-1), Vector128<long>.One));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> BlendVariableSse41Single(Vector128<float> left, Vector128<float> right, Vector128<float> mask)
        => Sse41.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> BlendVariableSse41Double(Vector128<double> left, Vector128<double> right, Vector128<double> mask)
        => Sse41.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<sbyte> BlendVariableSse41Int8(Vector128<sbyte> left, Vector128<sbyte> right, Vector128<sbyte> mask)
        => Sse41.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<short> BlendVariableSse41Int16(Vector128<short> left, Vector128<short> right, Vector128<short> mask)
        => Sse41.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> BlendVariableSse41Int32(Vector128<int> left, Vector128<int> right, Vector128<int> mask)
        => Sse41.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<long> BlendVariableSse41Int64(Vector128<long> left, Vector128<long> right, Vector128<long> mask)
        => Sse41.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> BlendVariableAvx512Single(Vector128<float> left, Vector128<float> right, Vector128<float> mask)
        => Avx512F.VL.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> BlendVariableAvx512Double(Vector128<double> left, Vector128<double> right, Vector128<double> mask)
        => Avx512F.VL.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<sbyte> BlendVariableAvx512Int8(Vector128<sbyte> left, Vector128<sbyte> right, Vector128<sbyte> mask)
        => Avx512BW.VL.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<short> BlendVariableAvx512Int16(Vector128<short> left, Vector128<short> right, Vector128<short> mask)
        => Avx512BW.VL.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> BlendVariableAvx512Int32(Vector128<int> left, Vector128<int> right, Vector128<int> mask)
        => Avx512F.VL.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<long> BlendVariableAvx512Int64(Vector128<long> left, Vector128<long> right, Vector128<long> mask)
        => Avx512F.VL.BlendVariable(left, right, mask);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AddToNegativeSingle(Vector128<float> left, Vector128<float> right)
        => Sse41.BlendVariable(left, left + right, left);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> AddToNegativeDouble(Vector128<double> left, Vector128<double> right)
        => Sse41.BlendVariable(left, left + right, left);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<sbyte> AddToNegativeInt8(Vector128<sbyte> left, Vector128<sbyte> right)
        => Sse41.BlendVariable(left, left + right, left);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<short> AddToNegativeInt16(Vector128<short> left, Vector128<short> right)
        => Avx512BW.VL.BlendVariable(left, left + right, left);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> AddToNegativeInt32(Vector128<int> left, Vector128<int> right)
        => Avx512F.VL.BlendVariable(left, left + right, left);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<long> AddToNegativeInt64(Vector128<long> left, Vector128<long> right)
        => Avx512F.VL.BlendVariable(left, left + right, left);
}