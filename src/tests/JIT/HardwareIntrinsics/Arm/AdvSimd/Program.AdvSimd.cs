// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

namespace JIT.HardwareIntrinsics.Arm._AdvSimd
{
    public static partial class Program
    {
        static Program()
        {
            JIT.HardwareIntrinsics.Arm.Program.PrintSupportedIsa();
        }

        [ConditionalTheory(typeof(AdvSimd), nameof(AdvSimd.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void WideningLowerConversions(bool subtract)
        {
            TestWideningLower<byte, ushort>(WideningLowerGetLower, WideningLowerToVector128Unsafe, subtract);
            TestWideningLower<sbyte, short>(WideningLowerGetLower, WideningLowerToVector128Unsafe, subtract);
            TestWideningLower<ushort, uint>(WideningLowerGetLower, WideningLowerToVector128Unsafe, subtract);
            TestWideningLower<short, int>(WideningLowerGetLower, WideningLowerToVector128Unsafe, subtract);
            TestWideningLower<uint, ulong>(WideningLowerGetLower, WideningLowerToVector128Unsafe, subtract);
            TestWideningLower<int, long>(WideningLowerGetLower, WideningLowerToVector128Unsafe, subtract);
        }

        private static void TestWideningLower<T, TWide>(
            Func<Vector128<T>, Vector64<T>, bool, Vector128<TWide>> getLower,
            Func<Vector64<TWide>, Vector64<T>, bool, Vector128<TWide>> toVector128Unsafe,
            bool subtract)
            where T : unmanaged, IBinaryInteger<T>
            where TWide : unmanaged, IBinaryInteger<TWide>
        {
            Vector128<T> left = Vector128<T>.Zero;
            for (int i = 0; i < Vector128<T>.Count; i++)
            {
                left = left.WithElement(i, T.CreateTruncating((i % 2 == 0 ? -1 : 1) * (i * 37 + 11)));
            }
            Vector64<T> right = Vector64.Create(T.CreateTruncating(128));
            Vector128<TWide> expected = Vector128<TWide>.Zero;
            for (int i = 0; i < Vector128<TWide>.Count; i++)
            {
                TWide x = TWide.CreateTruncating(left.GetElement(i));
                TWide y = TWide.CreateTruncating(right.GetElement(i));
                expected = expected.WithElement(i, subtract ? x - y : x + y);
            }
            Assert.Equal(expected, getLower(left, right, subtract));

            Vector64<TWide> wideLeft = Vector64<TWide>.Zero;
            Vector64<TWide> expectedWide = Vector64<TWide>.Zero;
            for (int i = 0; i < Vector64<TWide>.Count; i++)
            {
                // Exercise the upper 32 bits in the ulong/long cases.
                TWide x = TWide.CreateTruncating((1L << 32) + ((i + 1) * 1000));
                TWide y = TWide.CreateTruncating(right.GetElement(i));
                wideLeft = wideLeft.WithElement(i, x);
                expectedWide = expectedWide.WithElement(i, subtract ? x - y : x + y);
            }
            // ToVector128Unsafe leaves the upper half undefined.
            Assert.Equal(expectedWide, toVector128Unsafe(wideLeft, right, subtract).GetLower());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<ushort> WideningLowerGetLower(Vector128<byte> left, Vector64<byte> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.GetLower(), right) : AdvSimd.AddWideningLower(left.GetLower(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<short> WideningLowerGetLower(Vector128<sbyte> left, Vector64<sbyte> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.GetLower(), right) : AdvSimd.AddWideningLower(left.GetLower(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<uint> WideningLowerGetLower(Vector128<ushort> left, Vector64<ushort> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.GetLower(), right) : AdvSimd.AddWideningLower(left.GetLower(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<int> WideningLowerGetLower(Vector128<short> left, Vector64<short> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.GetLower(), right) : AdvSimd.AddWideningLower(left.GetLower(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<ulong> WideningLowerGetLower(Vector128<uint> left, Vector64<uint> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.GetLower(), right) : AdvSimd.AddWideningLower(left.GetLower(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<long> WideningLowerGetLower(Vector128<int> left, Vector64<int> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.GetLower(), right) : AdvSimd.AddWideningLower(left.GetLower(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<ushort> WideningLowerToVector128Unsafe(Vector64<ushort> left, Vector64<byte> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.ToVector128Unsafe(), right) : AdvSimd.AddWideningLower(left.ToVector128Unsafe(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<short> WideningLowerToVector128Unsafe(Vector64<short> left, Vector64<sbyte> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.ToVector128Unsafe(), right) : AdvSimd.AddWideningLower(left.ToVector128Unsafe(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<uint> WideningLowerToVector128Unsafe(Vector64<uint> left, Vector64<ushort> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.ToVector128Unsafe(), right) : AdvSimd.AddWideningLower(left.ToVector128Unsafe(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<int> WideningLowerToVector128Unsafe(Vector64<int> left, Vector64<short> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.ToVector128Unsafe(), right) : AdvSimd.AddWideningLower(left.ToVector128Unsafe(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<ulong> WideningLowerToVector128Unsafe(Vector64<ulong> left, Vector64<uint> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.ToVector128Unsafe(), right) : AdvSimd.AddWideningLower(left.ToVector128Unsafe(), right);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector128<long> WideningLowerToVector128Unsafe(Vector64<long> left, Vector64<int> right, bool subtract) =>
            subtract ? AdvSimd.SubtractWideningLower(left.ToVector128Unsafe(), right) : AdvSimd.AddWideningLower(left.ToVector128Unsafe(), right);
    }
}
