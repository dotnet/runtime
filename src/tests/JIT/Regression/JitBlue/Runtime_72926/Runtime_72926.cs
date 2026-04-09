// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_72926
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (CallForLongAsVector64_Zero() != Vector64<double>.Zero)
        {
            return 101;
        }
        if (CallForLongAsVector64_AllBitsSet().AsInt64() != Vector64<long>.AllBitsSet)
        {
            return 102;
        }
        if (CallForDoubleAsVector64_Zero() != Vector64<double>.Zero)
        {
            return 103;
        }
        if (CallForDoubleAsVector64_AllBitsSet().AsInt64() != Vector64<long>.AllBitsSet)
        {
            return 104;
        }
        if (CallForStructWithLongAsStructWithDouble().Dbl != 0)
        {
            return 105;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> CallForLongAsVector64_Zero()
    {
        long value = 0;
        return *(Vector64<double>*)&value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> CallForLongAsVector64_AllBitsSet()
    {
        long value = -1;
        return *(Vector64<double>*)&value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> CallForDoubleAsVector64_Zero()
    {
        double value = 0;
        return *(Vector64<double>*)&value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> CallForDoubleAsVector64_AllBitsSet()
    {
        double value = BitConverter.Int64BitsToDouble(-1);
        return *(Vector64<double>*)&value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static StructWithDouble CallForStructWithLongAsStructWithDouble()
    {
        StructWithLong lng = GetStructWithLong();
        return *(StructWithDouble*)&lng;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static StructWithLong GetStructWithLong()
    {
        return default;
    }

    struct StructWithDouble
    {
        public double Dbl;
    }

    struct StructWithLong
    {
        public long Lng;
    }
}
