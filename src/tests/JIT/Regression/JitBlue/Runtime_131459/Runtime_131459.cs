// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_131459;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Sequential, Size = 16)]
internal struct S16
{
    public byte Value;
}

[StructLayout(LayoutKind.Sequential, Size = 252)]
internal struct S252
{
    public byte Value;
}

[StructLayout(LayoutKind.Sequential, Size = 256)]
internal struct S256
{
    public byte Value;
}

[StructLayout(LayoutKind.Sequential, Size = 512)]
internal struct S512
{
    public byte Value;
}

public class Runtime_131459
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ref byte Address(bool small, object array)
    {
        if (small)
        {
            ref S16 element = ref Unsafe.As<S16[,]>(array)[0, 1];
            return ref Unsafe.As<S16, byte>(ref element);
        }
        else
        {
            ref S252 element = ref Unsafe.As<S252[,]>(array)[0, 1];
            return ref Unsafe.As<S252, byte>(ref element);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ref byte AddressBig(bool small, object array)
    {
        if (small)
        {
            ref S256 element = ref Unsafe.As<S256[,]>(array)[0, 1];
            return ref Unsafe.As<S256, byte>(ref element);
        }
        else
        {
            ref S512 element = ref Unsafe.As<S512[,]>(array)[0, 1];
            return ref Unsafe.As<S512, byte>(ref element);
        }
    }


    [Fact]
    public static void TestEntryPoint()
    {
        S16[,] small = new S16[1, 2];
        small[0, 1].Value = 16;

        S252[,] large = new S252[1, 2];
        large[0, 1].Value = 252;

        Assert.Equal(16, Address(true, small));
        Assert.Equal(252, Address(false, large));

        S256[,] smallBig = new S256[1, 2];
        smallBig[0, 1].Value = 16;

        S512[,] largeBig = new S512[1, 2];
        largeBig[0, 1].Value = 252;

        Assert.Equal(16, AddressBig(true, smallBig));
        Assert.Equal(252, AddressBig(false, largeBig));
    }

    // GT_ARR_ADDR carries the array element type and class handle. If GenTree::Compare ignores
    // them, tail merge folds the two arms below into one and the surviving node claims long[],
    // which lets value numbering treat the store through 'r' as not aliasing da[1].
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double ArrayElementTypeMerge(bool useLongArray, double[] da, object array)
    {
        ref byte r = ref Unsafe.NullRef<byte>();
        if (useLongArray)
        {
            r = ref Unsafe.As<long, byte>(ref Unsafe.As<long[]>(array)[1]);
        }
        else
        {
            r = ref Unsafe.As<double, byte>(ref Unsafe.As<double[]>(array)[1]);
        }

        double before = da[1];
        Unsafe.WriteUnaligned<long>(ref r, 0x4045000000000000L);
        return da[1] - before;
    }

    [Fact]
    public static void ArrayElementType()
    {
        double[] da = new double[2];
        Assert.Equal(42.0, ArrayElementTypeMerge(false, da, da));
    }
}
