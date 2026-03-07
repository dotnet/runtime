// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.IO.Hashing;

public sealed partial class Adler32
{
    private static bool IsVectorizable(ReadOnlySpan<byte> source)
        => Vector128.IsHardwareAccelerated && source.Length >= Vector128<byte>.Count;

    private static uint UpdateVectorized(uint adler, ReadOnlySpan<byte> source)
        => Adler32Simd.UpdateVectorized(adler, source);
}

file static class Adler32Simd
{
    // VMax represents the maximum number of 16-byte vectors we can process before reducing
    // mod 65521. This is analogous to NMax in the scalar code, however because the accumulated
    // values are distributed across vector elements, we can process more bytes before possible
    // overflow in any individual element. For this implementation, the max is actually 460
    // vectors, but we choose 448, because it divides evenly by any reasonable block size.
    public const uint VMax = 448;

    private static ReadOnlySpan<byte> MaskBytes => [
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint UpdateVectorized(uint adler, ReadOnlySpan<byte> source)
    {
        if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
        {
            return UpdateCore<AdlerVector256, AccumulateX86, DotProductX86>(adler, source);
        }

        if (Ssse3.IsSupported)
        {
            return UpdateCore<AdlerVector128, AccumulateX86, DotProductX86>(adler, source);
        }

        if (AdvSimd.Arm64.IsSupported)
        {
            if (Dp.IsSupported)
            {
                return UpdateCore<AdlerVector128, AccumulateArm64, DotProductArm64Dp>(adler, source);
            }

            return UpdateCore<AdlerVector128, AccumulateArm64, DotProductArm64>(adler, source);
        }

        return UpdateCore<AdlerVector128, AccumulateXplat, DotProductXplat>(adler, source);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint UpdateCore<TSimdStrategy, TAccumulate, TDotProduct>(uint adler, ReadOnlySpan<byte> source)
        where TSimdStrategy : struct, ISimdStrategy
        where TAccumulate : struct, ISimdAccumulate
        where TDotProduct : struct, ISimdDotProduct
    {
        Debug.Assert(source.Length >= Vector128<byte>.Count);

        ref byte bufRef = ref MemoryMarshal.GetReference(source);
        uint totalLength = (uint)source.Length;
        uint totalVectors = totalLength / (uint)Vector128<byte>.Count;

        uint loopVectors = totalVectors & ~1u;
        uint tailVectors = totalVectors - loopVectors;
        uint tailLength = totalLength - totalVectors * (uint)Vector128<byte>.Count;

        uint s1 = (ushort)adler;
        uint s2 = adler >>> 16;

        Vector128<uint> vs1 = Vector128.CreateScalar(s1);
        Vector128<uint> vs2 = Vector128.CreateScalar(s2);

        (vs1, vs2) = TSimdStrategy.VectorLoop<TAccumulate, TDotProduct>(vs1, vs2, ref bufRef, loopVectors);
        bufRef = ref Unsafe.Add(ref bufRef, loopVectors * (uint)Vector128<byte>.Count);

        Vector128<byte> weights = Vector128.CreateSequence((byte)16, unchecked((byte)-1));

        if (tailVectors != 0)
        {
            Debug.Assert(tailVectors == 1);

            Vector128<byte> bytes = Vector128.LoadUnsafe(ref bufRef);
            bufRef = ref Unsafe.Add(ref bufRef, (uint)Vector128<byte>.Count);

            Vector128<uint> vps = vs1;

            vs1 = TAccumulate.Accumulate(vs1, bytes);
            vs2 = TDotProduct.DotProduct(vs2, bytes, weights);

            vs2 += vps << 4;
        }

        if (tailLength != 0)
        {
            Debug.Assert(tailLength < (uint)Vector128<byte>.Count);

            Vector128<byte> bytes = Vector128.LoadUnsafe(ref Unsafe.Subtract(ref bufRef, (uint)Vector128<byte>.Count - tailLength));
            bytes &= Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(MaskBytes), tailLength);

            Vector128<uint> vps = vs1;

            vs1 = TAccumulate.Accumulate(vs1, bytes);
            vs2 = TDotProduct.DotProduct(vs2, bytes, weights);

            vs2 += vps * Vector128.Create(tailLength);
        }

        s1 = Vector128.Sum(vs1) % Adler32.ModBase;
        s2 = Vector128.Sum(vs2) % Adler32.ModBase;

        return s1 | (s2 << 16);
    }
}

file struct AdlerVector128 : ISimdStrategy
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<uint> QuickModBase(Vector128<uint> values)
    {
        // Calculating the residual mod 65521 is impractical in SIMD, however we can reduce by
        // enough to prevent overflow without changing the final result of a modulo performed later.
        //
        // Essentially, the high word of the accumulator represents the number of times it has
        // wrapped to 65536.
        // 65536 % 65521 = 15, which is what would be carried forward from the high word.
        // We can simply multiply the high word by 15 and add that to the low word to perform
        // the reduction, resulting in a maximum possible residual of 0xFFFF0.
        //
        // This is further optimized to: `high * 16 - high + low`
        // and implemented as: `(high << 4) - high + low`.

        Vector128<uint> vlo = values & (Vector128<uint>.AllBitsSet >>> 16);
        Vector128<uint> vhi = values >>> 16;
        return (vhi << 4) - vhi + vlo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector128<uint> vs1, Vector128<uint> vs2) VectorLoop<TAccumulate, TDotProduct>(Vector128<uint> vs1, Vector128<uint> vs2, ref byte sourceRef, uint vectors)
        where TAccumulate : struct, ISimdAccumulate
        where TDotProduct : struct, ISimdDotProduct
    {
        Debug.Assert(uint.IsEvenInteger(vectors));

        const uint blockSize = 2;

        Vector128<byte> weights1 = Vector128.CreateSequence((byte)32, unchecked((byte)-1));
        Vector128<byte> weights2 = Vector128.CreateSequence((byte)16, unchecked((byte)-1));

        while (vectors >= blockSize)
        {
            Vector128<uint> vs3 = default;
            Vector128<uint> vps = default;

            uint blocks = uint.Min(vectors, Adler32Simd.VMax) / blockSize;
            vectors -= blocks * blockSize;

            do
            {
                Vector128<byte> bytes1 = Vector128.LoadUnsafe(ref sourceRef);
                Vector128<byte> bytes2 = Vector128.LoadUnsafe(ref sourceRef, (uint)Vector128<byte>.Count);
                sourceRef = ref Unsafe.Add(ref sourceRef, (uint)Vector128<byte>.Count * 2);

                vps += vs1;

                vs1 = TAccumulate.Accumulate(vs1, bytes1, bytes2);
                vs2 = TDotProduct.DotProduct(vs2, bytes1, weights1);
                vs3 = TDotProduct.DotProduct(vs3, bytes2, weights2);
            }
            while (--blocks != 0);

            vs2 += vps << 5;
            vs2 += vs3;

            vs1 = QuickModBase(vs1);
            vs2 = QuickModBase(vs2);
        }

        return (vs1, vs2);
    }
}

file struct AdlerVector256 : ISimdStrategy
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<uint> QuickModBase(Vector256<uint> values)
    {
        Vector256<uint> vlo = values & (Vector256<uint>.AllBitsSet >>> 16);
        Vector256<uint> vhi = values >>> 16;
        return (vhi << 4) - vhi + vlo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<uint> Accumulate(Vector256<uint> sums, Vector256<byte> bytes)
        => Avx2.SumAbsoluteDifferences(bytes, default).AsUInt32() + sums;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<uint> Accumulate(Vector256<uint> sums, Vector256<byte> bytes1, Vector256<byte> bytes2)
    {
        Vector256<byte> zero = default;
        Vector256<uint> sad = Avx2.SumAbsoluteDifferences(bytes1, zero).AsUInt32();
        return sad + Avx2.SumAbsoluteDifferences(bytes2, zero).AsUInt32() + sums;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<uint> DotProduct(Vector256<uint> addend, Vector256<byte> left, Vector256<byte> right)
    {
        Vector256<short> mad = Avx2.MultiplyAddAdjacent(left, right.AsSByte());
        return Avx2.MultiplyAddAdjacent(mad, Vector256<short>.One).AsUInt32() + addend;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector128<uint> vs1, Vector128<uint> vs2) VectorLoop<TAccumulate, TDotProduct>(Vector128<uint> vs1, Vector128<uint> vs2, ref byte sourceRef, uint vectors)
        where TAccumulate : struct, ISimdAccumulate
        where TDotProduct : struct, ISimdDotProduct
    {
        Debug.Assert(uint.IsEvenInteger(vectors));

        const uint blockSize = 4;

        Vector256<byte> weights1 = Vector256.CreateSequence((byte)64, unchecked((byte)-1));
        Vector256<byte> weights2 = Vector256.CreateSequence((byte)32, unchecked((byte)-1));

        Vector256<uint> ws1 = vs1.ToVector256Unsafe();
        Vector256<uint> ws2 = vs2.ToVector256Unsafe();

        while (vectors >= blockSize)
        {
            Vector256<uint> ws3 = default;
            Vector256<uint> wps = default;

            uint blocks = uint.Min(vectors, Adler32Simd.VMax) / blockSize;
            vectors -= blocks * blockSize;

            do
            {
                Vector256<byte> bytes1 = Vector256.LoadUnsafe(ref sourceRef);
                Vector256<byte> bytes2 = Vector256.LoadUnsafe(ref sourceRef, (uint)Vector256<byte>.Count);
                sourceRef = ref Unsafe.Add(ref sourceRef, (uint)Vector256<byte>.Count * 2);

                wps += ws1;

                ws1 = Accumulate(ws1, bytes1, bytes2);
                ws2 = DotProduct(ws2, bytes1, weights1);
                ws3 = DotProduct(ws3, bytes2, weights2);
            }
            while (--blocks != 0);

            ws2 += wps << 6;
            ws2 += ws3;

            ws1 = QuickModBase(ws1);
            ws2 = QuickModBase(ws2);
        }

        if (vectors != 0)
        {
            Debug.Assert(vectors == 2);

            Vector256<byte> bytes = Vector256.LoadUnsafe(ref sourceRef);
            Vector256<uint> wps = ws1;

            ws1 = Accumulate(ws1, bytes);
            ws2 = DotProduct(ws2, bytes, weights2);

            ws2 += wps << 5;
        }

        vs1 = ws1.GetLower() + ws1.GetUpper();
        vs2 = ws2.GetLower() + ws2.GetUpper();

        return (vs1, vs2);
    }
}

file struct AccumulateX86 : ISimdAccumulate
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Accumulate(Vector128<uint> sums, Vector128<byte> bytes)
        => Sse2.SumAbsoluteDifferences(bytes, default).AsUInt32() + sums;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Accumulate(Vector128<uint> sums, Vector128<byte> bytes1, Vector128<byte> bytes2)
    {
        Vector128<byte> zero = default;
        Vector128<uint> sad = Sse2.SumAbsoluteDifferences(bytes1, zero).AsUInt32();
        return sad + Sse2.SumAbsoluteDifferences(bytes2, zero).AsUInt32() + sums;
    }
}

file struct AccumulateArm64 : ISimdAccumulate
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Accumulate(Vector128<uint> sums, Vector128<byte> bytes)
        => AdvSimd.Arm64.AddAcrossWidening(bytes).AsUInt32().ToVector128Unsafe() + sums;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Accumulate(Vector128<uint> sums, Vector128<byte> bytes1, Vector128<byte> bytes2)
        => AdvSimd.AddPairwiseWideningAndAdd(sums, AdvSimd.AddPairwiseWideningAndAdd(AdvSimd.AddPairwiseWidening(bytes1), bytes2));
}

file struct AccumulateXplat : ISimdAccumulate
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Accumulate(Vector128<uint> sums, Vector128<byte> bytes)
    {
        (Vector128<ushort> bl, Vector128<ushort> bh) = Vector128.Widen(bytes);
        (Vector128<uint> sl, Vector128<uint> sh) = Vector128.Widen(bl + bh);
        return sums + sl + sh;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> Accumulate(Vector128<uint> sums, Vector128<byte> bytes1, Vector128<byte> bytes2)
    {
        (Vector128<ushort> b1l, Vector128<ushort> b1h) = Vector128.Widen(bytes1);
        (Vector128<ushort> b2l, Vector128<ushort> b2h) = Vector128.Widen(bytes2);
        (Vector128<uint> sl, Vector128<uint> sh) = Vector128.Widen(b1l + b1h + b2l + b2h);
        return sums + sl + sh;
    }
}

file struct DotProductX86 : ISimdDotProduct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> DotProduct(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right)
    {
        Vector128<short> mad = Ssse3.MultiplyAddAdjacent(left, right.AsSByte());
        return Sse2.MultiplyAddAdjacent(mad, Vector128<short>.One).AsUInt32() + addend;
    }
}

file struct DotProductArm64 : ISimdDotProduct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> DotProduct(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right)
    {
        Vector128<ushort> mad = AdvSimd.MultiplyWideningLower(left.GetLower(), right.GetLower());
        mad = AdvSimd.MultiplyWideningUpperAndAdd(mad, left, right);
        return AdvSimd.AddPairwiseWideningAndAdd(addend, mad);
    }
}

file struct DotProductArm64Dp : ISimdDotProduct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> DotProduct(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right)
        => Dp.DotProduct(addend, left, right);
}

file struct DotProductXplat : ISimdDotProduct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<uint> DotProduct(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right)
    {
        (Vector128<ushort> ll, Vector128<ushort> lh) = Vector128.Widen(left);
        (Vector128<ushort> rl, Vector128<ushort> rh) = Vector128.Widen(right);
        (Vector128<uint> ml, Vector128<uint> mh) = Vector128.Widen(ll * rl + lh * rh);
        return addend + ml + mh;
    }
}

file interface ISimdAccumulate
{
    static abstract Vector128<uint> Accumulate(Vector128<uint> sums, Vector128<byte> bytes);

    static abstract Vector128<uint> Accumulate(Vector128<uint> sums, Vector128<byte> bytes1, Vector128<byte> bytes2);
}

file interface ISimdDotProduct
{
    static abstract Vector128<uint> DotProduct(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right);
}

file interface ISimdStrategy
{
    static abstract (Vector128<uint> vs1, Vector128<uint> vs2) VectorLoop<TAccumulate, TDotProduct>(Vector128<uint> vs1, Vector128<uint> vs2, ref byte sourceRef, uint vectors)
        where TAccumulate : struct, ISimdAccumulate
        where TDotProduct : struct, ISimdDotProduct;
}
