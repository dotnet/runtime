// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using Xunit;

namespace IntelHardwareIntrinsicTest.General
{
    public partial class Program
    {
        // Each method of LoadOrder loads from p in one operand and then, in a later operand, atomically replaces the
        // first element at p. Arguments are evaluated from left to right, so the load must see the original element.
        // The JIT can fold a load into the instruction that consumes it, which moves the load to that instruction, so
        // it must not do that when a write to the same memory comes in between.
        [Fact]
        public static unsafe void LoadsAreNotFoldedPastLaterWrites()
        {
            void* p = NativeMemory.AlignedAlloc(64, 64);

            try
            {
                if (Sse2.IsSupported)
                {
                    FillInt32((int*)p);
                    Vector128<int> original = Vector128.Load((int*)p);
                    Assert.Equal(original + Vector128.Create(-1), LoadOrder.AddAligned((int*)p));

                    FillInt32((int*)p);
                    Assert.Equal(original + Vector128.Create(-1), LoadOrder.AddCrossPlatformAligned((int*)p));
                }

                if (Sse41.IsSupported)
                {
                    FillInt32((int*)p);
                    Vector128<int> original = Vector128.Load((int*)p);
                    Assert.Equal(original, LoadOrder.BlendAligned(Vector128.Create(7), (int*)p));
                }

                if (Avx2.IsSupported)
                {
                    FillInt32((int*)p);
                    Vector256<int> original = Vector256.Load((int*)p);
                    Assert.Equal(original + Vector256.Create(-1), LoadOrder.AddAligned256((int*)p));
                }

                if (Avx512F.IsSupported)
                {
                    FillInt32((int*)p);
                    Vector512<int> original = Vector512.Load((int*)p);
                    Assert.Equal(original + Vector512.Create(-1), LoadOrder.AddAligned512((int*)p));
                }

                if (Fma.IsSupported)
                {
                    *(float*)p = 2.0f;
                    Assert.Equal(Vector128.CreateScalar(8.0f), LoadOrder.MultiplyAddScalar(Vector128.CreateScalar(3.0f), (float*)p));
                }

                if (Avx.IsSupported)
                {
                    *(float*)p = 2.0f;
                    Assert.Equal(Vector256.Create(4.0f), LoadOrder.AddBroadcast((float*)p));
                }

                if (Sse3.IsSupported)
                {
                    *(double*)p = 2.0;
                    Assert.Equal(Vector128.Create(4.0), LoadOrder.AddDuplicate((double*)p));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(p);
            }
        }

        private static unsafe void FillInt32(int* p)
        {
            // The first element is negative, so that BlendVariable selects every element of the loaded vector.
            p[0] = -1;

            for (int i = 1; i < 16; i++)
            {
                p[i] = i + 1;
            }
        }

        private static unsafe class LoadOrder
        {
            private const int Replacement = 0x01020304;
            private const int FloatReplacement = 0x42C80000; // 100.0f
            private const long DoubleReplacement = 0x4059000000000000; // 100.0

            // The load is the first operand of a commutative operation, so the JIT can only fold it by swapping them.

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<int> AddAligned(int* p) => Sse2.Add(Sse2.LoadAlignedVector128(p), Vector128.Create(Interlocked.Exchange(ref *p, Replacement)));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<int> AddCrossPlatformAligned(int* p) => Vector128.LoadAligned(p) + Vector128.Create(Interlocked.Exchange(ref *p, Replacement));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector256<int> AddAligned256(int* p) => Avx2.Add(Avx.LoadAlignedVector256(p), Vector256.Create(Interlocked.Exchange(ref *p, Replacement)));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector512<int> AddAligned512(int* p) => Avx512F.Add(Avx512F.LoadAlignedVector512(p), Vector512.Create(Interlocked.Exchange(ref *p, Replacement)));

            // The load is the middle operand and the write is in the last one.

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<int> BlendAligned(Vector128<int> a, int* p) => Sse41.BlendVariable(a, Sse2.LoadAlignedVector128(p), Vector128.Create(Interlocked.Exchange(ref *p, Replacement)));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<float> MultiplyAddScalar(Vector128<float> a, float* p) => Fma.MultiplyAddScalar(a, Sse.LoadScalarVector128(p), Vector128.Create(BitConverter.Int32BitsToSingle(Interlocked.Exchange(ref *(int*)p, FloatReplacement))));

            // With AVX-512, the JIT can fold these loads as embedded broadcasts.

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector256<float> AddBroadcast(float* p) => Avx.Add(Avx.BroadcastScalarToVector256(p), Vector256.Create(BitConverter.Int32BitsToSingle(Interlocked.Exchange(ref *(int*)p, FloatReplacement))));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<double> AddDuplicate(double* p) => Sse2.Add(Sse3.LoadAndDuplicateToVector128(p), Vector128.Create(BitConverter.Int64BitsToDouble(Interlocked.Exchange(ref *(long*)p, DoubleReplacement))));
        }
    }
}
