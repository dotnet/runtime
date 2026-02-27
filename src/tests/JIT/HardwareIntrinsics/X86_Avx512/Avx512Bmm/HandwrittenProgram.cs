// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;
using System.Runtime.CompilerServices;


namespace JIT.HardwareIntrinsics.X86._Avx512Bmm
{
    public static partial class Program
    {
        static Program()
        {

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector256<ushort> BitMultiplyMatrix16x16WithOrReduction_Vector256(Vector256<ushort> x, Vector256<ushort> y, Vector256<ushort> z)
        {
            return Avx512Bmm.BitMultiplyMatrix16x16WithOrReduction(x, y, z);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector512<ushort> BitMultiplyMatrix16x16WithOrReduction_Vector512(Vector512<ushort> x, Vector512<ushort> y, Vector512<ushort> z)
        {
            return Avx512Bmm.BitMultiplyMatrix16x16WithOrReduction(x, y, z);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector256<ushort> BitMultiplyMatrix16x16WithXorReduction_Vector256(Vector256<ushort> x, Vector256<ushort> y, Vector256<ushort> z)
        {
            return Avx512Bmm.BitMultiplyMatrix16x16WithXorReduction(x, y, z);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector512<ushort> BitMultiplyMatrix16x16WithXorReduction_Vector512(Vector512<ushort> x, Vector512<ushort> y, Vector512<ushort> z)
        {
            return Avx512Bmm.BitMultiplyMatrix16x16WithXorReduction(x, y, z);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<byte> ReverseBits_Vector128(Vector128<byte> values)
        {
            return Avx512Bmm.ReverseBits(values);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector256<byte> ReverseBits_Vector256(Vector256<byte> values)
        {
            return Avx512Bmm.ReverseBits(values);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector512<byte> ReverseBits_Vector512(Vector512<byte> values)
        {
            return Avx512Bmm.ReverseBits(values);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<byte> ReverseBits_Mask_Vector128(Vector128<byte> values, Vector128<byte> mask)
        {
            return Avx512BW.BlendVariable(values, Avx512Bmm.ReverseBits(values), mask);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector256<byte> ReverseBits_Mask_Vector256(Vector256<byte> values, Vector256<byte> mask)
        {
            return Avx512BW.BlendVariable(values, Avx512Bmm.ReverseBits(values), mask);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector512<byte> ReverseBits_Mask_Vector512(Vector512<byte> values, Vector512<byte> mask)
        {
            return Avx512BW.BlendVariable(values, Avx512Bmm.ReverseBits(values), mask);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<byte> ReverseBits_Maskz_Vector128(Vector128<byte> values, Vector128<byte> mask)
        {
            return Avx512BW.BlendVariable(Vector128<byte>.Zero, Avx512Bmm.ReverseBits(values), mask);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector256<byte> ReverseBits_Maskz_Vector256(Vector256<byte> values, Vector256<byte> mask)
        {
            return Avx512BW.BlendVariable(Vector256<byte>.Zero, Avx512Bmm.ReverseBits(values), mask);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector512<byte> ReverseBits_Maskz_Vector512(Vector512<byte> values, Vector512<byte> mask)
        {
            return Avx512BW.BlendVariable(Vector512<byte>.Zero, Avx512Bmm.ReverseBits(values), mask);
        }

        [Fact]
        public static void CheckSupported()
        {
            (int Eax, int Ebx, int Ecx, int Edx) = X86Base.CpuId(unchecked((int)0x80000021), (int)0x0);
            bool isSupported = (Eax & (1 << 23)) != 0;
            Assert.Equal(isSupported, Avx512Bmm.IsSupported);
        }

        [Fact]
        public static void BitMultiplyMatrix16x16WithOrReduction_Vector256_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector256<ushort> x = Vector256.Create((ushort)0x1);
            Vector256<ushort> y = Vector256.Create((ushort)0x1);
            Vector256<ushort> z = Vector256.Create((ushort)0x1011);
            Vector256<ushort> result = BitMultiplyMatrix16x16WithOrReduction_Vector256(x, y, z);
            Assert.Equal(result, Vector256.Create((ushort)0x1011));
        }

        [Fact]
        public static void BitMultiplyMatrix16x16WithOrReduction_Vector512_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector512<ushort> x = Vector512.Create((ushort)0x1);
            Vector512<ushort> y = Vector512.Create((ushort)0x1);
            Vector512<ushort> z = Vector512.Create((ushort)0x1011);
            Vector512<ushort> result = BitMultiplyMatrix16x16WithOrReduction_Vector512(x, y, z);
            Assert.Equal(result, Vector512.Create((ushort)0x1011));
        }

        [Fact]
        public static void BitMultiplyMatrix16x16WithXorReduction_Vector256_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector256<ushort> x = Vector256.Create((ushort)0x1);
            Vector256<ushort> y = Vector256.Create((ushort)0x1);
            Vector256<ushort> z = Vector256.Create((ushort)0x1011);
            Vector256<ushort> result = BitMultiplyMatrix16x16WithXorReduction_Vector256(x, y, z);
            Assert.Equal(result, Vector256.Create((ushort)0x1010));
        }

        [Fact]
        public static void BitMultiplyMatrix16x16WithXorReduction_Vector512_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector512<ushort> x = Vector512.Create((ushort)0x1);
            Vector512<ushort> y = Vector512.Create((ushort)0x1);
            Vector512<ushort> z = Vector512.Create((ushort)0x1011);
            Vector512<ushort> result = BitMultiplyMatrix16x16WithXorReduction_Vector512(x, y, z);
            Assert.Equal(result, Vector512.Create((ushort)0x1010));
        }

        [Fact]
        public static void ReverseBits_Vector128_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector128<byte> x = Vector128.Create((byte)0xAA);
            Vector128<byte> y = ReverseBits_Vector128(x);
            Assert.Equal(y, Vector128.Create((byte)0x55));
        }

        [Fact]
        public static void ReverseBits_Vector128_Mask_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector128<byte> x = Vector128.Create((byte)0xAA);
            Vector128<byte> mask = Vector128.Create(0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
            Vector128<byte> y = ReverseBits_Mask_Vector128(x, mask);
            Assert.Equal(y, Vector128.Create((byte)0x55, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA));
        }

        [Fact]
        public static void ReverseBits_Vector128_Maskz_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector128<byte> x = Vector128.Create((byte)0xAA);
            Vector128<byte> mask = Vector128.Create(0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
            Vector128<byte> y = ReverseBits_Maskz_Vector128(x, mask);
            Assert.Equal(y, Vector128.Create((byte)0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00));
        }

        [Fact]
        public static void ReverseBits_Vector256_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector256<byte> x = Vector256.Create((byte)0xAA);
            Vector256<byte> y = ReverseBits_Vector256(x);
            Assert.Equal(y, Vector256.Create((byte)0x55));
        }

        [Fact]
        public static void ReverseBits_Vector256_Mask_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector256<byte> x = Vector256.Create((byte)0xAA);
            Vector256<byte> mask = Vector256.Create(0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
            Vector256<byte> y = ReverseBits_Mask_Vector256(x, mask);
            Assert.Equal(y, Vector256.Create((byte)0x55, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA));
        }

        [Fact]
        public static void ReverseBits_Vector256_Maskz_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector256<byte> x = Vector256.Create((byte)0xAA);
            Vector256<byte> mask = Vector256.Create(0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
            Vector256<byte> y = ReverseBits_Maskz_Vector256(x, mask);
            Assert.Equal(y, Vector256.Create((byte)0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00));
        }

        [Fact]
        public static void ReverseBits_Vector512_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector512<byte> x = Vector512.Create((byte)0xAA);
            Vector512<byte> y = ReverseBits_Vector512(x);
            Assert.Equal(y, Vector512.Create((byte)0x55));
        }

        [Fact]
        public static void ReverseBits_Vector512_Mask_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector512<byte> x = Vector512.Create((byte)0xAA);
            Vector512<byte> mask = Vector512.Create(0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
            Vector512<byte> y = ReverseBits_Mask_Vector512(x, mask);
            Assert.Equal(y, Vector512.Create((byte)0x55, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA));
        }

        [Fact]
        public static void ReverseBits_Vector512_Maskz_Test()
        {
            if (!Avx512Bmm.IsSupported) return;
            Vector512<byte> x = Vector512.Create((byte)0xAA);
            Vector512<byte> mask = Vector512.Create(0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
            Vector512<byte> y = ReverseBits_Maskz_Vector512(x, mask);
            Assert.Equal(y, Vector512.Create((byte)0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00));
        }
    }
}
