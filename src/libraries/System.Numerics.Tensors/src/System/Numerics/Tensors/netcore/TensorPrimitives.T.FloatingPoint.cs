// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        // Defines vectorizable operators for types implementing IFloatingPoint<T> or related interfaces.

        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of single-precision floating-point numbers.</summary>
        /// <remarks>Assumes arguments have already been validated to be non-empty and equal length.</remarks>
        private static T CosineSimilarityCore<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y) where T : IRootFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // Compute the same as:
            // TensorPrimitives.Dot(x, y) / (Math.Sqrt(TensorPrimitives.SumOfSquares(x)) * Math.Sqrt(TensorPrimitives.SumOfSquares(y)))
            // but only looping over each span once.

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector512<T> dotProductVector = Vector512<T>.Zero;
                Vector512<T> xSumOfSquaresVector = Vector512<T>.Zero;
                Vector512<T> ySumOfSquaresVector = Vector512<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector512<T>.Count;
                int i = 0;
                do
                {
                    Vector512<T> xVec = Vector512.LoadUnsafe(ref xRef, (uint)i);
                    Vector512<T> yVec = Vector512.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);

                    i += Vector512<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector512<T> xVec = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count));
                    Vector512<T> yVec = Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<T>.Count));

                    Vector512<T> remainderMask = CreateRemainderMaskVector512<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector512.Sum(dotProductVector) /
                    (T.Sqrt(Vector512.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector512.Sum(ySumOfSquaresVector)));
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector256<T> dotProductVector = Vector256<T>.Zero;
                Vector256<T> xSumOfSquaresVector = Vector256<T>.Zero;
                Vector256<T> ySumOfSquaresVector = Vector256<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector256<T>.Count;
                int i = 0;
                do
                {
                    Vector256<T> xVec = Vector256.LoadUnsafe(ref xRef, (uint)i);
                    Vector256<T> yVec = Vector256.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);

                    i += Vector256<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector256<T> xVec = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count));
                    Vector256<T> yVec = Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<T>.Count));

                    Vector256<T> remainderMask = CreateRemainderMaskVector256<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector256.Sum(dotProductVector) /
                    (T.Sqrt(Vector256.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector256.Sum(ySumOfSquaresVector)));
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);
                ref T yRef = ref MemoryMarshal.GetReference(y);

                Vector128<T> dotProductVector = Vector128<T>.Zero;
                Vector128<T> xSumOfSquaresVector = Vector128<T>.Zero;
                Vector128<T> ySumOfSquaresVector = Vector128<T>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector128<T>.Count;
                int i = 0;
                do
                {
                    Vector128<T> xVec = Vector128.LoadUnsafe(ref xRef, (uint)i);
                    Vector128<T> yVec = Vector128.LoadUnsafe(ref yRef, (uint)i);

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);

                    i += Vector128<T>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector128<T> xVec = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count));
                    Vector128<T> yVec = Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<T>.Count));

                    Vector128<T> remainderMask = CreateRemainderMaskVector128<T>(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, yVec, dotProductVector);
                    xSumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(xVec, xVec, xSumOfSquaresVector);
                    ySumOfSquaresVector = MultiplyAddEstimateOperator<T>.Invoke(yVec, yVec, ySumOfSquaresVector);
                }

                // Sum(X * Y) / (|X| * |Y|)
                return
                    Vector128.Sum(dotProductVector) /
                    (T.Sqrt(Vector128.Sum(xSumOfSquaresVector)) * T.Sqrt(Vector128.Sum(ySumOfSquaresVector)));
            }

            // Vectorization isn't supported or there are too few elements to vectorize.
            // Use a scalar implementation.
            T dotProduct = T.Zero, xSumOfSquares = T.Zero, ySumOfSquares = T.Zero;
            for (int i = 0; i < x.Length; i++)
            {
                dotProduct = MultiplyAddEstimateOperator<T>.Invoke(x[i], y[i], dotProduct);
                xSumOfSquares = MultiplyAddEstimateOperator<T>.Invoke(x[i], x[i], xSumOfSquares);
                ySumOfSquares = MultiplyAddEstimateOperator<T>.Invoke(y[i], y[i], ySumOfSquares);
            }

            // Sum(X * Y) / (|X| * |Y|)
            return
                dotProduct /
                (T.Sqrt(xSumOfSquares) * T.Sqrt(ySumOfSquares));
        }

        // TODO: The uses of these ApplyScalar methods are all as part of operators when handling edge cases (NaN, Infinity, really large inputs, etc.)
        // Currently, these edge cases are not handled in a vectorized way and instead fall back to scalar processing. We can look into
        // handling those in a vectorized manner as well.

        private static Vector128<float> ApplyScalar<TOperator>(Vector128<float> floats) where TOperator : IUnaryOperator<float, float> =>
            Vector128.Create(TOperator.Invoke(floats[0]), TOperator.Invoke(floats[1]), TOperator.Invoke(floats[2]), TOperator.Invoke(floats[3]));

        private static Vector256<float> ApplyScalar<TOperator>(Vector256<float> floats) where TOperator : IUnaryOperator<float, float> =>
            Vector256.Create(ApplyScalar<TOperator>(floats.GetLower()), ApplyScalar<TOperator>(floats.GetUpper()));

        private static Vector512<float> ApplyScalar<TOperator>(Vector512<float> floats) where TOperator : IUnaryOperator<float, float> =>
            Vector512.Create(ApplyScalar<TOperator>(floats.GetLower()), ApplyScalar<TOperator>(floats.GetUpper()));

        private static Vector128<double> ApplyScalar<TOperator>(Vector128<double> doubles) where TOperator : IUnaryOperator<double, double> =>
            Vector128.Create(TOperator.Invoke(doubles[0]), TOperator.Invoke(doubles[1]));

        private static Vector256<double> ApplyScalar<TOperator>(Vector256<double> doubles) where TOperator : IUnaryOperator<double, double> =>
            Vector256.Create(ApplyScalar<TOperator>(doubles.GetLower()), ApplyScalar<TOperator>(doubles.GetUpper()));

        private static Vector512<double> ApplyScalar<TOperator>(Vector512<double> doubles) where TOperator : IUnaryOperator<double, double> =>
            Vector512.Create(ApplyScalar<TOperator>(doubles.GetLower()), ApplyScalar<TOperator>(doubles.GetUpper()));

        /// <summary>T.Ieee754Remainder(x, y)</summary>
        internal readonly struct Ieee754RemainderOperator<T> : IBinaryOperator<T> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => false;
            public static T Invoke(T x, T y) => T.Ieee754Remainder(x, y);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => throw new NotSupportedException();
        }

        // Ieee754Remainder
        internal readonly struct ReciprocalOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => T.One / x;
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128<T>.One / x;
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256<T>.One / x;
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512<T>.One / x;
        }

        private readonly struct ReciprocalSqrtOperator<T> : IUnaryOperator<T, T> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => T.One / T.Sqrt(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128<T>.One / Vector128.Sqrt(x);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256<T>.One / Vector256.Sqrt(x);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512<T>.One / Vector512.Sqrt(x);
        }

        private readonly struct ReciprocalEstimateOperator<T> : IUnaryOperator<T, T> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x) => T.ReciprocalEstimate(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (Sse.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Sse.Reciprocal(x.AsSingle()).As<float, T>();
                }

                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return AdvSimd.ReciprocalEstimate(x.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.ReciprocalEstimate(x.AsDouble()).As<double, T>();
                }

                return Vector128<T>.One / x;
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (Avx.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx.Reciprocal(x.AsSingle()).As<float, T>();
                }

                return Vector256<T>.One / x;
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (Avx512F.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx512F.Reciprocal14(x.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Avx512F.Reciprocal14(x.AsDouble()).As<double, T>();
                }

                return Vector512<T>.One / x;
            }
        }

        private readonly struct ReciprocalSqrtEstimateOperator<T> : IUnaryOperator<T, T> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x) => T.ReciprocalSqrtEstimate(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (Sse.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Sse.ReciprocalSqrt(x.AsSingle()).As<float, T>();
                }

                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return AdvSimd.ReciprocalSquareRootEstimate(x.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.ReciprocalSquareRootEstimate(x.AsDouble()).As<double, T>();
                }

                return Vector128<T>.One / Vector128.Sqrt(x);
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (Avx.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx.ReciprocalSqrt(x.AsSingle()).As<float, T>();
                }

                return Vector256<T>.One / Vector256.Sqrt(x);
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (Avx512F.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx512F.ReciprocalSqrt14(x.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Avx512F.ReciprocalSqrt14(x.AsDouble()).As<double, T>();
                }

                return Vector512<T>.One / Vector512.Sqrt(x);
            }
        }

        /// <summary>(x * y) + z</summary>
        internal readonly struct FusedMultiplyAddOperator<T> : ITernaryOperator<T> where T : IFloatingPointIeee754<T>
        {
            public static T Invoke(T x, T y, T z) => T.FusedMultiplyAdd(x, y, z);

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z)
            {
                if (Fma.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                if (AdvSimd.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return AdvSimd.FusedMultiplyAdd(z.AsSingle(), x.AsSingle(), y.AsSingle()).As<float, T>();
                }

                if (AdvSimd.Arm64.IsSupported)
                {
                    if (typeof(T) == typeof(double)) return AdvSimd.Arm64.FusedMultiplyAdd(z.AsDouble(), x.AsDouble(), y.AsDouble()).As<double, T>();
                }

                if (typeof(T) == typeof(float))
                {
                    Vector128<float> xFloats = x.AsSingle();
                    Vector128<float> yFloats = y.AsSingle();
                    Vector128<float> zFloats = z.AsSingle();
                    return Vector128.Create(
                        float.FusedMultiplyAdd(xFloats[0], yFloats[0], zFloats[0]),
                        float.FusedMultiplyAdd(xFloats[1], yFloats[1], zFloats[1]),
                        float.FusedMultiplyAdd(xFloats[2], yFloats[2], zFloats[2]),
                        float.FusedMultiplyAdd(xFloats[3], yFloats[3], zFloats[3])).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    Vector128<double> xDoubles = x.AsDouble();
                    Vector128<double> yDoubles = y.AsDouble();
                    Vector128<double> zDoubles = z.AsDouble();
                    return Vector128.Create(
                        double.FusedMultiplyAdd(xDoubles[0], yDoubles[0], zDoubles[0]),
                        double.FusedMultiplyAdd(xDoubles[1], yDoubles[1], zDoubles[1])).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z)
            {
                if (Fma.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Fma.MultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Fma.MultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                return Vector256.Create(
                    Invoke(x.GetLower(), y.GetLower(), z.GetLower()),
                    Invoke(x.GetUpper(), y.GetUpper(), z.GetUpper()));
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z)
            {
                if (Avx512F.IsSupported)
                {
                    if (typeof(T) == typeof(float)) return Avx512F.FusedMultiplyAdd(x.AsSingle(), y.AsSingle(), z.AsSingle()).As<float, T>();
                    if (typeof(T) == typeof(double)) return Avx512F.FusedMultiplyAdd(x.AsDouble(), y.AsDouble(), z.AsDouble()).As<double, T>();
                }

                return Vector512.Create(
                    Invoke(x.GetLower(), y.GetLower(), z.GetLower()),
                    Invoke(x.GetUpper(), y.GetUpper(), z.GetUpper()));
            }
        }

        /// <summary>(x * (1 - z)) + (y * z)</summary>
        internal readonly struct LerpOperator<T> : ITernaryOperator<T> where T : IFloatingPointIeee754<T>
        {
            public static T Invoke(T x, T y, T amount) => T.Lerp(x, y, amount);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> amount) => (x * (Vector128<T>.One - amount)) + (y * amount);
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> amount) => (x * (Vector256<T>.One - amount)) + (y * amount);
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> amount) => (x * (Vector512<T>.One - amount)) + (y * amount);
        }

        /// <summary>T.Exp(x)</summary>
        internal readonly struct ExpOperator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            public static bool Vectorizable => (typeof(T) == typeof(double))
                                            || (typeof(T) == typeof(float));

            public static T Invoke(T x) => T.Exp(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Exp(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return ExpOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return ExpOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Exp(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return ExpOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return ExpOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Exp(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return ExpOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return ExpOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }
        }

#if !NET9_0_OR_GREATER
        /// <summary>double.Exp(x)</summary>
        internal readonly struct ExpOperatorDouble : IUnaryOperator<double, double>
        {
            // This code is based on `vrd2_exp` from amd/aocl-libm-ose
            // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes
            // ----------------------
            // 1. Argument Reduction:
            //      e^x = 2^(x/ln2) = 2^(x*(64/ln(2))/64)     --- (1)
            //
            //      Choose 'n' and 'f', such that
            //      x * 64/ln2 = n + f                        --- (2) | n is integer
            //                            | |f| <= 0.5
            //     Choose 'm' and 'j' such that,
            //      n = (64 * m) + j                          --- (3)
            //
            //     From (1), (2) and (3),
            //      e^x = 2^((64*m + j + f)/64)
            //          = (2^m) * (2^(j/64)) * 2^(f/64)
            //          = (2^m) * (2^(j/64)) * e^(f*(ln(2)/64))
            //
            // 2. Table Lookup
            //      Values of (2^(j/64)) are precomputed, j = 0, 1, 2, 3 ... 63
            //
            // 3. Polynomial Evaluation
            //   From (2),
            //     f = x*(64/ln(2)) - n
            //   Let,
            //     r  = f*(ln(2)/64) = x - n*(ln(2)/64)
            //
            // 4. Reconstruction
            //      Thus,
            //        e^x = (2^m) * (2^(j/64)) * e^r

            private const ulong V_ARG_MAX = 0x40862000_00000000;
            private const ulong V_DP64_BIAS = 1023;

            private const double V_EXPF_MIN = -709.782712893384;
            private const double V_EXPF_MAX = +709.782712893384;

            private const double V_EXPF_HUGE = 6755399441055744;
            private const double V_TBL_LN2 = 1.4426950408889634;

            private const double V_LN2_HEAD = +0.693359375;
            private const double V_LN2_TAIL = -0.00021219444005469057;

            private const double C3 = 0.5000000000000018;
            private const double C4 = 0.1666666666666617;
            private const double C5 = 0.04166666666649277;
            private const double C6 = 0.008333333333559272;
            private const double C7 = 0.001388888895122404;
            private const double C8 = 0.00019841269432677495;
            private const double C9 = 2.4801486521374483E-05;
            private const double C10 = 2.7557622532543023E-06;
            private const double C11 = 2.7632293298250954E-07;
            private const double C12 = 2.499430431958571E-08;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Exp(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                // x * (64.0 / ln(2))
                Vector128<double> z = x * Vector128.Create(V_TBL_LN2);

                Vector128<double> dn = z + Vector128.Create(V_EXPF_HUGE);

                // n = (int)z
                Vector128<ulong> n = dn.AsUInt64();

                // dn = (double)n
                dn -= Vector128.Create(V_EXPF_HUGE);

                // r = x - (dn * (ln(2) / 64))
                // where ln(2) / 64 is split into Head and Tail values
                Vector128<double> r = x - (dn * Vector128.Create(V_LN2_HEAD)) - (dn * Vector128.Create(V_LN2_TAIL));

                Vector128<double> r2 = r * r;
                Vector128<double> r4 = r2 * r2;
                Vector128<double> r8 = r4 * r4;

                // Compute polynomial
                Vector128<double> poly = ((Vector128.Create(C12) * r + Vector128.Create(C11)) * r2 +
                                           Vector128.Create(C10) * r + Vector128.Create(C9))  * r8 +
                                         ((Vector128.Create(C8)  * r + Vector128.Create(C7))  * r2 +
                                          (Vector128.Create(C6)  * r + Vector128.Create(C5))) * r4 +
                                         ((Vector128.Create(C4)  * r + Vector128.Create(C3))  * r2 + (r + Vector128<double>.One));

                // m = (n - j) / 64
                // result = polynomial * 2^m
                Vector128<double> ret = poly * ((n + Vector128.Create(V_DP64_BIAS)) << 52).AsDouble();

                // Check if -709 < vx < 709
                if (Vector128.GreaterThanAny(Vector128.Abs(x).AsUInt64(), Vector128.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? double.PositiveInfinity : x
                    Vector128<double> infinityMask = Vector128.GreaterThan(x, Vector128.Create(V_EXPF_MAX));

                    ret = Vector128.ConditionalSelect(
                        infinityMask,
                        Vector128.Create(double.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector128.AndNot(ret, Vector128.LessThan(x, Vector128.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                // x * (64.0 / ln(2))
                Vector256<double> z = x * Vector256.Create(V_TBL_LN2);

                Vector256<double> dn = z + Vector256.Create(V_EXPF_HUGE);

                // n = (int)z
                Vector256<ulong> n = dn.AsUInt64();

                // dn = (double)n
                dn -= Vector256.Create(V_EXPF_HUGE);

                // r = x - (dn * (ln(2) / 64))
                // where ln(2) / 64 is split into Head and Tail values
                Vector256<double> r = x - (dn * Vector256.Create(V_LN2_HEAD)) - (dn * Vector256.Create(V_LN2_TAIL));

                Vector256<double> r2 = r * r;
                Vector256<double> r4 = r2 * r2;
                Vector256<double> r8 = r4 * r4;

                // Compute polynomial
                Vector256<double> poly = ((Vector256.Create(C12) * r + Vector256.Create(C11)) * r2 +
                                           Vector256.Create(C10) * r + Vector256.Create(C9))  * r8 +
                                         ((Vector256.Create(C8)  * r + Vector256.Create(C7))  * r2 +
                                          (Vector256.Create(C6)  * r + Vector256.Create(C5))) * r4 +
                                         ((Vector256.Create(C4)  * r + Vector256.Create(C3))  * r2 + (r + Vector256<double>.One));

                // m = (n - j) / 64
                // result = polynomial * 2^m
                Vector256<double> ret = poly * ((n + Vector256.Create(V_DP64_BIAS)) << 52).AsDouble();

                // Check if -709 < vx < 709
                if (Vector256.GreaterThanAny(Vector256.Abs(x).AsUInt64(), Vector256.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? double.PositiveInfinity : x
                    Vector256<double> infinityMask = Vector256.GreaterThan(x, Vector256.Create(V_EXPF_MAX));

                    ret = Vector256.ConditionalSelect(
                        infinityMask,
                        Vector256.Create(double.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector256.AndNot(ret, Vector256.LessThan(x, Vector256.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                // x * (64.0 / ln(2))
                Vector512<double> z = x * Vector512.Create(V_TBL_LN2);

                Vector512<double> dn = z + Vector512.Create(V_EXPF_HUGE);

                // n = (int)z
                Vector512<ulong> n = dn.AsUInt64();

                // dn = (double)n
                dn -= Vector512.Create(V_EXPF_HUGE);

                // r = x - (dn * (ln(2) / 64))
                // where ln(2) / 64 is split into Head and Tail values
                Vector512<double> r = x - (dn * Vector512.Create(V_LN2_HEAD)) - (dn * Vector512.Create(V_LN2_TAIL));

                Vector512<double> r2 = r * r;
                Vector512<double> r4 = r2 * r2;
                Vector512<double> r8 = r4 * r4;

                // Compute polynomial
                Vector512<double> poly = ((Vector512.Create(C12) * r + Vector512.Create(C11)) * r2 +
                                           Vector512.Create(C10) * r + Vector512.Create(C9))  * r8 +
                                         ((Vector512.Create(C8)  * r + Vector512.Create(C7))  * r2 +
                                          (Vector512.Create(C6)  * r + Vector512.Create(C5))) * r4 +
                                         ((Vector512.Create(C4)  * r + Vector512.Create(C3))  * r2 + (r + Vector512<double>.One));

                // m = (n - j) / 64
                // result = polynomial * 2^m
                Vector512<double> ret = poly * ((n + Vector512.Create(V_DP64_BIAS)) << 52).AsDouble();

                // Check if -709 < vx < 709
                if (Vector512.GreaterThanAny(Vector512.Abs(x).AsUInt64(), Vector512.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? double.PositiveInfinity : x
                    Vector512<double> infinityMask = Vector512.GreaterThan(x, Vector512.Create(V_EXPF_MAX));

                    ret = Vector512.ConditionalSelect(
                        infinityMask,
                        Vector512.Create(double.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector512.AndNot(ret, Vector512.LessThan(x, Vector512.Create(V_EXPF_MIN)));
                }

                return ret;
            }
        }

        /// <summary>float.Exp(x)</summary>
        internal readonly struct ExpOperatorSingle : IUnaryOperator<float, float>
        {
            // This code is based on `vrs4_expf` from amd/aocl-libm-ose
            // Copyright (C) 2019-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation Notes:
            // 1. Argument Reduction:
            //      e^x = 2^(x/ln2)                          --- (1)
            //
            //      Let x/ln(2) = z                          --- (2)
            //
            //      Let z = n + r , where n is an integer    --- (3)
            //                      |r| <= 1/2
            //
            //     From (1), (2) and (3),
            //      e^x = 2^z
            //          = 2^(N+r)
            //          = (2^N)*(2^r)                        --- (4)
            //
            // 2. Polynomial Evaluation
            //   From (4),
            //     r   = z - N
            //     2^r = C1 + C2*r + C3*r^2 + C4*r^3 + C5 *r^4 + C6*r^5
            //
            // 4. Reconstruction
            //      Thus,
            //        e^x = (2^N) * (2^r)

            private const uint V_ARG_MAX = 0x42AE0000;

            private const float V_EXPF_MIN = -103.97208f;
            private const float V_EXPF_MAX = +88.72284f;

            private const double V_EXPF_HUGE = 6755399441055744;
            private const double V_TBL_LN2 = 1.4426950408889634;

            private const double C1 = 1.0000000754895704;
            private const double C2 = 0.6931472254087585;
            private const double C3 = 0.2402210737432219;
            private const double C4 = 0.05550297297702539;
            private const double C5 = 0.009676036358193323;
            private const double C6 = 0.001341000536524434;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Exp(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                // Convert x to double precision
                (Vector128<double> xl, Vector128<double> xu) = Vector128.Widen(x);

                // x * (64.0 / ln(2))
                Vector128<double> v_tbl_ln2 = Vector128.Create(V_TBL_LN2);

                Vector128<double> zl = xl * v_tbl_ln2;
                Vector128<double> zu = xu * v_tbl_ln2;

                Vector128<double> v_expf_huge = Vector128.Create(V_EXPF_HUGE);

                Vector128<double> dnl = zl + v_expf_huge;
                Vector128<double> dnu = zu + v_expf_huge;

                // n = (int)z
                Vector128<ulong> nl = dnl.AsUInt64();
                Vector128<ulong> nu = dnu.AsUInt64();

                // dn = (double)n
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector128<double> c1 = Vector128.Create(C1);
                Vector128<double> c2 = Vector128.Create(C2);
                Vector128<double> c3 = Vector128.Create(C3);
                Vector128<double> c4 = Vector128.Create(C4);
                Vector128<double> c5 = Vector128.Create(C5);
                Vector128<double> c6 = Vector128.Create(C6);

                Vector128<double> rl = zl - dnl;

                Vector128<double> rl2 = rl * rl;
                Vector128<double> rl4 = rl2 * rl2;

                Vector128<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector128<double> ru = zu - dnu;

                Vector128<double> ru2 = ru * ru;
                Vector128<double> ru4 = ru2 * ru2;

                Vector128<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)(poly + (n << 52))
                Vector128<float> ret = Vector128.Narrow(
                    (polyl.AsUInt64() + (nl << 52)).AsDouble(),
                    (polyu.AsUInt64() + (nu << 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector128.GreaterThanAny(Vector128.Abs(x).AsUInt32(), Vector128.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector128<float> infinityMask = Vector128.GreaterThan(x, Vector128.Create(V_EXPF_MAX));

                    ret = Vector128.ConditionalSelect(
                        infinityMask,
                        Vector128.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector128.AndNot(ret, Vector128.LessThan(x, Vector128.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                // Convert x to double precision
                (Vector256<double> xl, Vector256<double> xu) = Vector256.Widen(x);

                // x * (64.0 / ln(2))
                Vector256<double> v_tbl_ln2 = Vector256.Create(V_TBL_LN2);

                Vector256<double> zl = xl * v_tbl_ln2;
                Vector256<double> zu = xu * v_tbl_ln2;

                Vector256<double> v_expf_huge = Vector256.Create(V_EXPF_HUGE);

                Vector256<double> dnl = zl + v_expf_huge;
                Vector256<double> dnu = zu + v_expf_huge;

                // n = (int)z
                Vector256<ulong> nl = dnl.AsUInt64();
                Vector256<ulong> nu = dnu.AsUInt64();

                // dn = (double)n
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector256<double> c1 = Vector256.Create(C1);
                Vector256<double> c2 = Vector256.Create(C2);
                Vector256<double> c3 = Vector256.Create(C3);
                Vector256<double> c4 = Vector256.Create(C4);
                Vector256<double> c5 = Vector256.Create(C5);
                Vector256<double> c6 = Vector256.Create(C6);

                Vector256<double> rl = zl - dnl;

                Vector256<double> rl2 = rl * rl;
                Vector256<double> rl4 = rl2 * rl2;

                Vector256<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector256<double> ru = zu - dnu;

                Vector256<double> ru2 = ru * ru;
                Vector256<double> ru4 = ru2 * ru2;

                Vector256<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)(poly + (n << 52))
                Vector256<float> ret = Vector256.Narrow(
                    (polyl.AsUInt64() + (nl << 52)).AsDouble(),
                    (polyu.AsUInt64() + (nu << 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector256.GreaterThanAny(Vector256.Abs(x).AsUInt32(), Vector256.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector256<float> infinityMask = Vector256.GreaterThan(x, Vector256.Create(V_EXPF_MAX));

                    ret = Vector256.ConditionalSelect(
                        infinityMask,
                        Vector256.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector256.AndNot(ret, Vector256.LessThan(x, Vector256.Create(V_EXPF_MIN)));
                }

                return ret;
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                // Convert x to double precision
                (Vector512<double> xl, Vector512<double> xu) = Vector512.Widen(x);

                // x * (64.0 / ln(2))
                Vector512<double> v_tbl_ln2 = Vector512.Create(V_TBL_LN2);

                Vector512<double> zl = xl * v_tbl_ln2;
                Vector512<double> zu = xu * v_tbl_ln2;

                Vector512<double> v_expf_huge = Vector512.Create(V_EXPF_HUGE);

                Vector512<double> dnl = zl + v_expf_huge;
                Vector512<double> dnu = zu + v_expf_huge;

                // n = (int)z
                Vector512<ulong> nl = dnl.AsUInt64();
                Vector512<ulong> nu = dnu.AsUInt64();

                // dn = (double)n
                dnl -= v_expf_huge;
                dnu -= v_expf_huge;

                // r = z - dn
                Vector512<double> c1 = Vector512.Create(C1);
                Vector512<double> c2 = Vector512.Create(C2);
                Vector512<double> c3 = Vector512.Create(C3);
                Vector512<double> c4 = Vector512.Create(C4);
                Vector512<double> c5 = Vector512.Create(C5);
                Vector512<double> c6 = Vector512.Create(C6);

                Vector512<double> rl = zl - dnl;

                Vector512<double> rl2 = rl * rl;
                Vector512<double> rl4 = rl2 * rl2;

                Vector512<double> polyl = (c4 * rl + c3) * rl2
                                       + ((c6 * rl + c5) * rl4
                                        + (c2 * rl + c1));


                Vector512<double> ru = zu - dnu;

                Vector512<double> ru2 = ru * ru;
                Vector512<double> ru4 = ru2 * ru2;

                Vector512<double> polyu = (c4 * ru + c3) * ru2
                                       + ((c6 * ru + c5) * ru4
                                        + (c2 * ru + c1));

                // result = (float)(poly + (n << 52))
                Vector512<float> ret = Vector512.Narrow(
                    (polyl.AsUInt64() + (nl << 52)).AsDouble(),
                    (polyu.AsUInt64() + (nu << 52)).AsDouble()
                );

                // Check if -103 < |x| < 88
                if (Vector512.GreaterThanAny(Vector512.Abs(x).AsUInt32(), Vector512.Create(V_ARG_MAX)))
                {
                    // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                    Vector512<float> infinityMask = Vector512.GreaterThan(x, Vector512.Create(V_EXPF_MAX));

                    ret = Vector512.ConditionalSelect(
                        infinityMask,
                        Vector512.Create(float.PositiveInfinity),
                        ret
                    );

                    // (x < V_EXPF_MIN) ? 0 : x
                    ret = Vector512.AndNot(ret, Vector512.LessThan(x, Vector512.Create(V_EXPF_MIN)));
                }

                return ret;
            }
        }
#endif

        /// <summary>T.ExpM1(x)</summary>
        internal readonly struct ExpM1Operator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            public static bool Vectorizable => ExpOperator<T>.Vectorizable;

            public static T Invoke(T x) => T.ExpM1(x);
            public static Vector128<T> Invoke(Vector128<T> x) => ExpOperator<T>.Invoke(x) - Vector128<T>.One;
            public static Vector256<T> Invoke(Vector256<T> x) => ExpOperator<T>.Invoke(x) - Vector256<T>.One;
            public static Vector512<T> Invoke(Vector512<T> x) => ExpOperator<T>.Invoke(x) - Vector512<T>.One;
        }

        /// <summary>T.Exp2(x)</summary>
        internal readonly struct Exp2Operator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            private const double NaturalLog2 = 0.6931471805599453;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Exp2(x);
            public static Vector128<T> Invoke(Vector128<T> x) => ExpOperator<T>.Invoke(x * Vector128.Create(T.CreateTruncating(NaturalLog2)));
            public static Vector256<T> Invoke(Vector256<T> x) => ExpOperator<T>.Invoke(x * Vector256.Create(T.CreateTruncating(NaturalLog2)));
            public static Vector512<T> Invoke(Vector512<T> x) => ExpOperator<T>.Invoke(x * Vector512.Create(T.CreateTruncating(NaturalLog2)));
        }

        /// <summary>T.Exp2M1(x)</summary>
        internal readonly struct Exp2M1Operator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            public static bool Vectorizable => Exp2Operator<T>.Vectorizable;

            public static T Invoke(T x) => T.Exp2M1(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Exp2Operator<T>.Invoke(x) - Vector128<T>.One;
            public static Vector256<T> Invoke(Vector256<T> x) => Exp2Operator<T>.Invoke(x) - Vector256<T>.One;
            public static Vector512<T> Invoke(Vector512<T> x) => Exp2Operator<T>.Invoke(x) - Vector512<T>.One;
        }

        /// <summary>T.Exp10(x)</summary>
        internal readonly struct Exp10Operator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            private const double NaturalLog10 = 2.302585092994046;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Exp10(x);
            public static Vector128<T> Invoke(Vector128<T> x) => ExpOperator<T>.Invoke(x * Vector128.Create(T.CreateTruncating(NaturalLog10)));
            public static Vector256<T> Invoke(Vector256<T> x) => ExpOperator<T>.Invoke(x * Vector256.Create(T.CreateTruncating(NaturalLog10)));
            public static Vector512<T> Invoke(Vector512<T> x) => ExpOperator<T>.Invoke(x * Vector512.Create(T.CreateTruncating(NaturalLog10)));
        }

        /// <summary>T.Exp10M1(x)</summary>
        internal readonly struct Exp10M1Operator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            public static bool Vectorizable => Exp2Operator<T>.Vectorizable;

            public static T Invoke(T x) => T.Exp10M1(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Exp10Operator<T>.Invoke(x) - Vector128<T>.One;
            public static Vector256<T> Invoke(Vector256<T> x) => Exp10Operator<T>.Invoke(x) - Vector256<T>.One;
            public static Vector512<T> Invoke(Vector512<T> x) => Exp10Operator<T>.Invoke(x) - Vector512<T>.One;
        }

        /// <summary>T.Pow(x, y)</summary>
        internal readonly struct PowOperator<T> : IBinaryOperator<T>
            where T : IPowerFunctions<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x, T y) => T.Pow(x, y);

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(y.AsSingle() * LogOperator<float>.Invoke(x.AsSingle())).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(y.AsDouble() * LogOperator<double>.Invoke(x.AsDouble())).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(y.AsSingle() * LogOperator<float>.Invoke(x.AsSingle())).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(y.AsDouble() * LogOperator<double>.Invoke(x.AsDouble())).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(y.AsSingle() * LogOperator<float>.Invoke(x.AsSingle())).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(y.AsDouble() * LogOperator<double>.Invoke(x.AsDouble())).As<double, T>();
                }
            }
        }

        /// <summary>T.Sqrt(x)</summary>
        internal readonly struct SqrtOperator<T> : IUnaryOperator<T, T>
            where T : IRootFunctions<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => T.Sqrt(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.Sqrt(x);
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.Sqrt(x);
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.Sqrt(x);
        }

        /// <summary>T.Cbrt(x)</summary>
        internal readonly struct CbrtOperator<T> : IUnaryOperator<T, T>
            where T : IRootFunctions<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Cbrt(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector128.Create(3f)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector128.Create(3d)).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector256.Create(3f)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector256.Create(3d)).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector512.Create(3f)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector512.Create(3d)).As<double, T>();
                }
            }
        }

        /// <summary>T.Hypot(x, y)</summary>
        internal readonly struct HypotOperator<T> : IBinaryOperator<T>
            where T : IRootFunctions<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x, T y) => T.Hypot(x, y);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => Vector128.Sqrt((x * x) + (y * y));
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => Vector256.Sqrt((x * x) + (y * y));
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => Vector512.Sqrt((x * x) + (y * y));
        }

        /// <summary>T.Acos(x)</summary>
        internal readonly struct AcosOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: Vectorize
            public static T Invoke(T x) => T.Acos(x);
            public static Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.Acosh(x)</summary>
        internal readonly struct AcoshOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: Vectorize
            public static T Invoke(T x) => T.Acosh(x);
            public static Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.AcosPi(x)</summary>
        internal readonly struct AcosPiOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => AcosOperator<T>.Vectorizable;
            public static T Invoke(T x) => T.AcosPi(x);
            public static Vector128<T> Invoke(Vector128<T> x) => AcosOperator<T>.Invoke(x) / Vector128.Create(T.Pi);
            public static Vector256<T> Invoke(Vector256<T> x) => AcosOperator<T>.Invoke(x) / Vector256.Create(T.Pi);
            public static Vector512<T> Invoke(Vector512<T> x) => AcosOperator<T>.Invoke(x) / Vector512.Create(T.Pi);
        }

        /// <summary>T.Asin(x)</summary>
        internal readonly struct AsinOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: Vectorize
            public static T Invoke(T x) => T.Asin(x);
            public static Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.Asinh(x)</summary>
        internal readonly struct AsinhOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: Vectorize
            public static T Invoke(T x) => T.Asinh(x);
            public static Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.AsinPi(x)</summary>
        internal readonly struct AsinPiOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => AsinOperator<T>.Vectorizable;
            public static T Invoke(T x) => T.AsinPi(x);
            public static Vector128<T> Invoke(Vector128<T> x) => AsinOperator<T>.Invoke(x) / Vector128.Create(T.Pi);
            public static Vector256<T> Invoke(Vector256<T> x) => AsinOperator<T>.Invoke(x) / Vector256.Create(T.Pi);
            public static Vector512<T> Invoke(Vector512<T> x) => AsinOperator<T>.Invoke(x) / Vector512.Create(T.Pi);
        }

        /// <summary>T.Atan(x)</summary>
        internal readonly struct AtanOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: Vectorize
            public static T Invoke(T x) => T.Atan(x);
            public static Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.Atanh(x)</summary>
        internal readonly struct AtanhOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: Vectorize
            public static T Invoke(T x) => T.Atanh(x);
            public static Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.AtanPi(x)</summary>
        internal readonly struct AtanPiOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => AtanOperator<T>.Vectorizable;
            public static T Invoke(T x) => T.AtanPi(x);
            public static Vector128<T> Invoke(Vector128<T> x) => AtanOperator<T>.Invoke(x) / Vector128.Create(T.Pi);
            public static Vector256<T> Invoke(Vector256<T> x) => AtanOperator<T>.Invoke(x) / Vector256.Create(T.Pi);
            public static Vector512<T> Invoke(Vector512<T> x) => AtanOperator<T>.Invoke(x) / Vector512.Create(T.Pi);
        }

        /// <summary>T.Atan2(y, x)</summary>
        internal readonly struct Atan2Operator<T> : IBinaryOperator<T>
            where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => false; // TODO: Vectorize
            public static T Invoke(T y, T x) => T.Atan2(y, x);
            public static Vector128<T> Invoke(Vector128<T> y, Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> y, Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> y, Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.Atan2Pi(y, x)</summary>
        internal readonly struct Atan2PiOperator<T> : IBinaryOperator<T>
            where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => Atan2Operator<T>.Vectorizable;
            public static T Invoke(T y, T x) => T.Atan2Pi(y, x);
            public static Vector128<T> Invoke(Vector128<T> y, Vector128<T> x) => Atan2Operator<T>.Invoke(y, x) / Vector128.Create(T.Pi);
            public static Vector256<T> Invoke(Vector256<T> y, Vector256<T> x) => Atan2Operator<T>.Invoke(y, x) / Vector256.Create(T.Pi);
            public static Vector512<T> Invoke(Vector512<T> y, Vector512<T> x) => Atan2Operator<T>.Invoke(y, x) / Vector512.Create(T.Pi);
        }

        /// <summary>T.Cos(x)</summary>
        internal readonly struct CosOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            // This code is based on `vrs4_cos` and `vrd2_cos` from amd/aocl-libm-ose
            // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation notes from amd/aocl-libm-ose:
            // --------------------------------------------
            // To compute cosf(float x)
            // Using the identity,
            // cos(x) = sin(x + pi/2)           (1)
            //
            // 1. Argument Reduction
            //      Now, let x be represented as,
            //          |x| = N * pi + f        (2) | N is an integer,
            //                                        -pi/2 <= f <= pi/2
            //
            //      From (2), N = int( (x + pi/2) / pi) - 0.5
            //                f = |x| - (N * pi)
            //
            // 2. Polynomial Evaluation
            //       From (1) and (2),sin(f) can be calculated using a polynomial
            //       sin(f) = f*(1 + C1*f^2 + C2*f^4 + C3*f^6 + c4*f^8)
            //
            // 3. Reconstruction
            //      Hence, cos(x) = sin(x + pi/2) = (-1)^N * sin(f)

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Cos(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return CosOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return CosOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return CosOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return CosOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return CosOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return CosOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }
        }

        /// <summary>float.Cos(x)</summary>
        private readonly struct CosOperatorSingle : IUnaryOperator<float, float>
        {
            internal const uint MaxVectorizedValue = 0x4A989680u;
            internal const uint SignMask = 0x7FFFFFFFu;
            private const float AlmHuge = 1.2582912e7f;
            private const float Pi_Tail1 = 8.742278e-8f;
            private const float Pi_Tail2 = 3.430249e-15f;
            private const float C1 = -0.16666657f;
            private const float C2 = 0.008332962f;
            private const float C3 = -1.9801206e-4f;
            private const float C4 = 2.5867037e-6f;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Cos(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt32(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorSingle>(x);
                }

                Vector128<float> almHuge = Vector128.Create(AlmHuge);
                Vector128<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked + Vector128.Create(float.Pi / 2), Vector128.Create(1 / float.Pi), almHuge);
                Vector128<uint> odd = dn.AsUInt32() << 31;
                dn = dn - almHuge - Vector128.Create(0.5f);

                Vector128<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector128<float> f2 = f * f;
                Vector128<float> f4 = f2 * f2;
                Vector128<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C2), f2, Vector128.Create(C1));
                Vector128<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector128<float>.One);
                Vector128<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C3), f2, Vector128.Create(C4) * f4);
                Vector128<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector128<float> poly = f * a3;

                return (poly.AsUInt32() ^ odd).AsSingle();
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt32(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorSingle>(x);
                }

                Vector256<float> almHuge = Vector256.Create(AlmHuge);
                Vector256<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked + Vector256.Create(float.Pi / 2), Vector256.Create(1 / float.Pi), almHuge);
                Vector256<uint> odd = dn.AsUInt32() << 31;
                dn = dn - almHuge - Vector256.Create(0.5f);

                Vector256<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector256<float> f2 = f * f;
                Vector256<float> f4 = f2 * f2;
                Vector256<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C2), f2, Vector256.Create(C1));
                Vector256<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector256<float>.One);
                Vector256<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C3), f2, Vector256.Create(C4) * f4);
                Vector256<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector256<float> poly = f * a3;

                return (poly.AsUInt32() ^ odd).AsSingle();
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt32(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorSingle>(x);
                }

                Vector512<float> almHuge = Vector512.Create(AlmHuge);
                Vector512<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked + Vector512.Create(float.Pi / 2), Vector512.Create(1 / float.Pi), almHuge);
                Vector512<uint> odd = dn.AsUInt32() << 31;
                dn = dn - almHuge - Vector512.Create(0.5f);

                Vector512<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector512<float> f2 = f * f;
                Vector512<float> f4 = f2 * f2;
                Vector512<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C2), f2, Vector512.Create(C1));
                Vector512<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector512<float>.One);
                Vector512<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C3), f2, Vector512.Create(C4) * f4);
                Vector512<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector512<float> poly = f * a3;

                return (poly.AsUInt32() ^ odd).AsSingle();
            }
        }

        /// <summary>double.Cos(x)</summary>
        internal readonly struct CosOperatorDouble : IUnaryOperator<double, double>
        {
            internal const ulong SignMask = 0x7FFFFFFFFFFFFFFFul;
            internal const ulong MaxVectorizedValue = 0x4160000000000000ul;
            private const double AlmHuge = 6.755399441055744E15;
            private const double Pi_Tail2 = -1.2246467991473532E-16;
            private const double Pi_Tail3 = 2.9947698097183397E-33;
            private const double C1 = -0.16666666666666666;
            private const double C2 = 0.008333333333333165;
            private const double C3 = -1.984126984120184E-4;
            private const double C4 = 2.7557319210152756E-6;
            private const double C5 = -2.5052106798274616E-8;
            private const double C6 = 1.6058936490373254E-10;
            private const double C7 = -7.642917806937501E-13;
            private const double C8 = 2.7204790963151784E-15;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Cos(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                Vector128<double> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt64(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorDouble>(x);
                }

                Vector128<double> almHuge = Vector128.Create(AlmHuge);
                Vector128<double> dn = (uxMasked * Vector128.Create(1 / double.Pi)) + Vector128.Create(double.Pi / 2) + almHuge;
                Vector128<ulong> odd = dn.AsUInt64() << 63;
                dn = dn - almHuge - Vector128.Create(0.5);

                Vector128<double> f = uxMasked;
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector128.Create(-double.Pi), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector128.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector128.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_17
                Vector128<double> f2 = f * f;
                Vector128<double> f4 = f2 * f2;
                Vector128<double> f6 = f4 * f2;
                Vector128<double> f10 = f6 * f4;
                Vector128<double> f14 = f10 * f4;
                Vector128<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C2), f2, Vector128.Create(C1));
                Vector128<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C4), f2, Vector128.Create(C3));
                Vector128<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C6), f2, Vector128.Create(C5));
                Vector128<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C8), f2, Vector128.Create(C7));
                Vector128<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector128<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector128<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ odd).AsDouble();
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                Vector256<double> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt64(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorDouble>(x);
                }

                Vector256<double> almHuge = Vector256.Create(AlmHuge);
                Vector256<double> dn = (uxMasked * Vector256.Create(1 / double.Pi)) + Vector256.Create(double.Pi / 2) + almHuge;
                Vector256<ulong> odd = dn.AsUInt64() << 63;
                dn = dn - almHuge - Vector256.Create(0.5);

                Vector256<double> f = uxMasked;
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector256.Create(-double.Pi), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector256.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector256.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_17
                Vector256<double> f2 = f * f;
                Vector256<double> f4 = f2 * f2;
                Vector256<double> f6 = f4 * f2;
                Vector256<double> f10 = f6 * f4;
                Vector256<double> f14 = f10 * f4;
                Vector256<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C2), f2, Vector256.Create(C1));
                Vector256<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C4), f2, Vector256.Create(C3));
                Vector256<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C6), f2, Vector256.Create(C5));
                Vector256<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C8), f2, Vector256.Create(C7));
                Vector256<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector256<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector256<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ odd).AsDouble();
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                Vector512<double> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt64(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorDouble>(x);
                }

                Vector512<double> almHuge = Vector512.Create(AlmHuge);
                Vector512<double> dn = (uxMasked * Vector512.Create(1 / double.Pi)) + Vector512.Create(double.Pi / 2) + almHuge;
                Vector512<ulong> odd = dn.AsUInt64() << 63;
                dn = dn - almHuge - Vector512.Create(0.5);

                Vector512<double> f = uxMasked;
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector512.Create(-double.Pi), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector512.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector512.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_17
                Vector512<double> f2 = f * f;
                Vector512<double> f4 = f2 * f2;
                Vector512<double> f6 = f4 * f2;
                Vector512<double> f10 = f6 * f4;
                Vector512<double> f14 = f10 * f4;
                Vector512<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C2), f2, Vector512.Create(C1));
                Vector512<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C4), f2, Vector512.Create(C3));
                Vector512<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C6), f2, Vector512.Create(C5));
                Vector512<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C8), f2, Vector512.Create(C7));
                Vector512<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector512<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector512<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ odd).AsDouble();
            }
        }

        /// <summary>T.CosPi(x)</summary>
        internal readonly struct CosPiOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.CosPi(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                Vector128<T> xpi = x * Vector128.Create(T.Pi);
                if (typeof(T) == typeof(float))
                {
                    if (Vector128.GreaterThanAny(xpi.AsUInt32() & Vector128.Create(CosOperatorSingle.SignMask), Vector128.Create(CosOperatorSingle.MaxVectorizedValue)))
                    {
                        return ApplyScalar<CosPiOperator<float>>(x.AsSingle()).As<float, T>();
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector128.GreaterThanAny(xpi.AsUInt64() & Vector128.Create(CosOperatorDouble.SignMask), Vector128.Create(CosOperatorDouble.MaxVectorizedValue)))
                    {
                        return ApplyScalar<CosPiOperator<double>>(x.AsDouble()).As<double, T>();
                    }
                }

                return CosOperator<T>.Invoke(xpi);
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                Vector256<T> xpi = x * Vector256.Create(T.Pi);
                if (typeof(T) == typeof(float))
                {
                    if (Vector256.GreaterThanAny(xpi.AsUInt32() & Vector256.Create(CosOperatorSingle.SignMask), Vector256.Create(CosOperatorSingle.MaxVectorizedValue)))
                    {
                        return ApplyScalar<CosPiOperator<float>>(x.AsSingle()).As<float, T>();
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector256.GreaterThanAny(xpi.AsUInt64() & Vector256.Create(CosOperatorDouble.SignMask), Vector256.Create(CosOperatorDouble.MaxVectorizedValue)))
                    {
                        return ApplyScalar<CosPiOperator<double>>(x.AsDouble()).As<double, T>();
                    }
                }

                return CosOperator<T>.Invoke(xpi);
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                Vector512<T> xpi = x * Vector512.Create(T.Pi);
                if (typeof(T) == typeof(float))
                {
                    if (Vector512.GreaterThanAny(xpi.AsUInt32() & Vector512.Create(CosOperatorSingle.SignMask), Vector512.Create(CosOperatorSingle.MaxVectorizedValue)))
                    {
                        return ApplyScalar<CosPiOperator<float>>(x.AsSingle()).As<float, T>();
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector512.GreaterThanAny(xpi.AsUInt64() & Vector512.Create(CosOperatorDouble.SignMask), Vector512.Create(CosOperatorDouble.MaxVectorizedValue)))
                    {
                        return ApplyScalar<CosPiOperator<double>>(x.AsDouble()).As<double, T>();
                    }
                }

                return CosOperator<T>.Invoke(xpi);
            }
        }

        /// <summary>T.Cosh(x)</summary>
        internal readonly struct CoshOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            // This code is based on `vrs4_coshf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Spec:
            //   coshf(|x| > 89.415985107421875) = Infinity
            //   coshf(Infinity)  = infinity
            //   coshf(-Infinity) = infinity
            //
            // cosh(x) = (exp(x) + exp(-x))/2
            // cosh(-x) = +cosh(x)
            //
            // checks for special cases
            // if ( asint(x) > infinity) return x with overflow exception and
            // return x.
            // if x is NaN then raise invalid FP operation exception and return x.
            //
            // coshf = v/2 * exp(x - log(v)) where v = 0x1.0000e8p-1

            private const float Single_LOGV = 0.693161f;
            private const float Single_HALFV = 1.0000138f;
            private const float Single_INVV2 = 0.24999309f;

            private const double Double_LOGV = 0.6931471805599453;
            private const double Double_HALFV = 1.0;
            private const double Double_INVV2 = 0.25;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Cosh(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector128<float> x = t.AsSingle();

                    Vector128<float> y = Vector128.Abs(x);
                    Vector128<float> z = ExpOperator<float>.Invoke(y - Vector128.Create((float)Single_LOGV));
                    return (Vector128.Create((float)Single_HALFV) * (z + (Vector128.Create((float)Single_INVV2) / z))).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    Vector128<double> x = t.AsDouble();

                    Vector128<double> y = Vector128.Abs(x);
                    Vector128<double> z = ExpOperator<double>.Invoke(y - Vector128.Create(Double_LOGV));
                    return (Vector128.Create(Double_HALFV) * (z + (Vector128.Create(Double_INVV2) / z))).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<float> x = t.AsSingle();

                    Vector256<float> y = Vector256.Abs(x);
                    Vector256<float> z = ExpOperator<float>.Invoke(y - Vector256.Create((float)Single_LOGV));
                    return (Vector256.Create((float)Single_HALFV) * (z + (Vector256.Create((float)Single_INVV2) / z))).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    Vector256<double> x = t.AsDouble();

                    Vector256<double> y = Vector256.Abs(x);
                    Vector256<double> z = ExpOperator<double>.Invoke(y - Vector256.Create(Double_LOGV));
                    return (Vector256.Create(Double_HALFV) * (z + (Vector256.Create(Double_INVV2) / z))).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector512<float> x = t.AsSingle();

                    Vector512<float> y = Vector512.Abs(x);
                    Vector512<float> z = ExpOperator<float>.Invoke(y - Vector512.Create((float)Single_LOGV));
                    return (Vector512.Create((float)Single_HALFV) * (z + (Vector512.Create((float)Single_INVV2) / z))).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    Vector512<double> x = t.AsDouble();

                    Vector512<double> y = Vector512.Abs(x);
                    Vector512<double> z = ExpOperator<double>.Invoke(y - Vector512.Create(Double_LOGV));
                    return (Vector512.Create(Double_HALFV) * (z + (Vector512.Create(Double_INVV2) / z))).As<double, T>();
                }
            }
        }

        /// <summary>T.Sin(x)</summary>
        internal readonly struct SinOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            // This code is based on `vrs4_sin` and `vrd2_sin` from amd/aocl-libm-ose
            // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation notes from amd/aocl-libm-ose:
            // -----------------------------------------------------------------
            // Convert given x into the form
            // |x| = N * pi + f where N is an integer and f lies in [-pi/2,pi/2]
            // N is obtained by : N = round(x/pi)
            // f is obtained by : f = abs(x)-N*pi
            // sin(x) = sin(N * pi + f) = sin(N * pi)*cos(f) + cos(N*pi)*sin(f)
            // sin(x) = sign(x)*sin(f)*(-1)**N
            //
            // The term sin(f) can be approximated by using a polynomial

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Sin(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return SinOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return SinOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return SinOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return SinOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return SinOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return SinOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }
        }

        /// <summary>float.Sin(x)</summary>
        private readonly struct SinOperatorSingle : IUnaryOperator<float, float>
        {
            internal const uint SignMask = 0x7FFFFFFFu;
            internal const uint MaxVectorizedValue = 0x49800000u;
            private const float AlmHuge = 1.2582912e7f;
            private const float Pi_Tail1 = 8.742278e-8f;
            private const float Pi_Tail2 = 3.430249e-15f;
            private const float C1 = -0.16666657f;
            private const float C2 = 0.0083330255f;
            private const float C3 = -1.980742e-4f;
            private const float C4 = 2.6019031e-6f;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Sin(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt32(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<SinOperatorSingle>(x);
                }

                Vector128<float> almHuge = Vector128.Create(AlmHuge);
                Vector128<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector128.Create(1 / float.Pi), almHuge);
                Vector128<uint> odd = dn.AsUInt32() << 31;
                dn -= almHuge;

                Vector128<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector128<float> f2 = f * f;
                Vector128<float> f4 = f2 * f2;
                Vector128<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C2), f2, Vector128.Create(C1));
                Vector128<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector128<float>.One);
                Vector128<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C3), f2, Vector128.Create(C4) * f4);
                Vector128<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector128<float> poly = f * a3;

                return (poly.AsUInt32() ^ (x.AsUInt32() & Vector128.Create(~SignMask)) ^ odd).AsSingle();
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt32(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<SinOperatorSingle>(x);
                }

                Vector256<float> almHuge = Vector256.Create(AlmHuge);
                Vector256<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector256.Create(1 / float.Pi), almHuge);
                Vector256<uint> odd = dn.AsUInt32() << 31;
                dn -= almHuge;

                Vector256<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector256<float> f2 = f * f;
                Vector256<float> f4 = f2 * f2;
                Vector256<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C2), f2, Vector256.Create(C1));
                Vector256<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector256<float>.One);
                Vector256<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C3), f2, Vector256.Create(C4) * f4);
                Vector256<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector256<float> poly = f * a3;

                return (poly.AsUInt32() ^ (x.AsUInt32() & Vector256.Create(~SignMask)) ^ odd).AsSingle();
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt32(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<SinOperatorSingle>(x);
                }

                Vector512<float> almHuge = Vector512.Create(AlmHuge);
                Vector512<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector512.Create(1 / float.Pi), almHuge);
                Vector512<uint> odd = dn.AsUInt32() << 31;
                dn -= almHuge;

                Vector512<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector512<float> f2 = f * f;
                Vector512<float> f4 = f2 * f2;
                Vector512<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C2), f2, Vector512.Create(C1));
                Vector512<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector512<float>.One);
                Vector512<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C3), f2, Vector512.Create(C4) * f4);
                Vector512<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector512<float> poly = f * a3;

                return (poly.AsUInt32() ^ (x.AsUInt32() & Vector512.Create(~SignMask)) ^ odd).AsSingle();
            }
        }

        /// <summary>double.Sin(x)</summary>
        private readonly struct SinOperatorDouble : IUnaryOperator<double, double>
        {
            internal const ulong SignMask = 0x7FFFFFFFFFFFFFFFul;
            internal const ulong MaxVectorizedValue = 0x4160000000000000ul;
            private const double AlmHuge = 6.755399441055744e15;
            private const double Pi_Tail1 = 1.224646799147353e-16;
            private const double Pi_Tail2 = 2.165713347843828e-32;
            private const double C0 = -0.16666666666666666;
            private const double C2 = 0.008333333333333165;
            private const double C4 = -1.984126984120184e-4;
            private const double C6 = 2.7557319210152756e-6;
            private const double C8 = -2.5052106798274583e-8;
            private const double C10 = 1.605893649037159e-10;
            private const double C12 = -7.642917806891047e-13;
            private const double C14 = 2.7204790957888847e-15;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Sin(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                Vector128<double> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt64(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<SinOperatorDouble>(x);
                }

                Vector128<double> almHuge = Vector128.Create(AlmHuge);
                Vector128<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector128.Create(1 / double.Pi), almHuge);
                Vector128<ulong> odd = dn.AsUInt64() << 63;
                dn -= almHuge;
                Vector128<double> f = uxMasked - (dn * Vector128.Create(double.Pi)) - (dn * Vector128.Create(Pi_Tail1)) - (dn * Vector128.Create(Pi_Tail2));

                // POLY_EVAL_ODD_17
                Vector128<double> f2 = f * f;
                Vector128<double> f4 = f2 * f2;
                Vector128<double> f6 = f4 * f2;
                Vector128<double> f10 = f6 * f4;
                Vector128<double> f14 = f10 * f4;
                Vector128<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C2), f2, Vector128.Create(C0));
                Vector128<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C6), f2, Vector128.Create(C4));
                Vector128<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C10), f2, Vector128.Create(C8));
                Vector128<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C14), f2, Vector128.Create(C12));
                Vector128<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector128<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector128<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ (x.AsUInt64() & Vector128.Create(~SignMask)) ^ odd).AsDouble();
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                Vector256<double> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt64(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<SinOperatorDouble>(x);
                }

                Vector256<double> almHuge = Vector256.Create(AlmHuge);
                Vector256<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector256.Create(1 / double.Pi), almHuge);
                Vector256<ulong> odd = dn.AsUInt64() << 63;
                dn -= almHuge;
                Vector256<double> f = uxMasked - (dn * Vector256.Create(double.Pi)) - (dn * Vector256.Create(Pi_Tail1)) - (dn * Vector256.Create(Pi_Tail2));

                // POLY_EVAL_ODD_17
                Vector256<double> f2 = f * f;
                Vector256<double> f4 = f2 * f2;
                Vector256<double> f6 = f4 * f2;
                Vector256<double> f10 = f6 * f4;
                Vector256<double> f14 = f10 * f4;
                Vector256<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C2), f2, Vector256.Create(C0));
                Vector256<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C6), f2, Vector256.Create(C4));
                Vector256<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C10), f2, Vector256.Create(C8));
                Vector256<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C14), f2, Vector256.Create(C12));
                Vector256<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector256<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector256<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ (x.AsUInt64() & Vector256.Create(~SignMask)) ^ odd).AsDouble();
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                Vector512<double> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt64(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<SinOperatorDouble>(x);
                }

                Vector512<double> almHuge = Vector512.Create(AlmHuge);
                Vector512<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector512.Create(1 / double.Pi), almHuge);
                Vector512<ulong> odd = dn.AsUInt64() << 63;
                dn -= almHuge;
                Vector512<double> f = uxMasked - (dn * Vector512.Create(double.Pi)) - (dn * Vector512.Create(Pi_Tail1)) - (dn * Vector512.Create(Pi_Tail2));

                // POLY_EVAL_ODD_17
                Vector512<double> f2 = f * f;
                Vector512<double> f4 = f2 * f2;
                Vector512<double> f6 = f4 * f2;
                Vector512<double> f10 = f6 * f4;
                Vector512<double> f14 = f10 * f4;
                Vector512<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C2), f2, Vector512.Create(C0));
                Vector512<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C6), f2, Vector512.Create(C4));
                Vector512<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C10), f2, Vector512.Create(C8));
                Vector512<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C14), f2, Vector512.Create(C12));
                Vector512<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector512<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector512<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ (x.AsUInt64() & Vector512.Create(~SignMask)) ^ odd).AsDouble();
            }
        }

        /// <summary>T.SinPi(x)</summary>
        internal readonly struct SinPiOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.SinPi(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                Vector128<T> xpi = x * Vector128.Create(T.Pi);
                if (typeof(T) == typeof(float))
                {
                    if (Vector128.GreaterThanAny(xpi.AsUInt32() & Vector128.Create(SinOperatorSingle.SignMask), Vector128.Create(SinOperatorSingle.MaxVectorizedValue)))
                    {
                        return ApplyScalar<SinPiOperator<float>>(x.AsSingle()).As<float, T>();
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector128.GreaterThanAny(xpi.AsUInt64() & Vector128.Create(SinOperatorDouble.SignMask), Vector128.Create(SinOperatorDouble.MaxVectorizedValue)))
                    {
                        return ApplyScalar<SinPiOperator<double>>(x.AsDouble()).As<double, T>();
                    }
                }

                return SinOperator<T>.Invoke(xpi);
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                Vector256<T> xpi = x * Vector256.Create(T.Pi);
                if (typeof(T) == typeof(float))
                {
                    if (Vector256.GreaterThanAny(xpi.AsUInt32() & Vector256.Create(SinOperatorSingle.SignMask), Vector256.Create(SinOperatorSingle.MaxVectorizedValue)))
                    {
                        return ApplyScalar<SinPiOperator<float>>(x.AsSingle()).As<float, T>();
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector256.GreaterThanAny(xpi.AsUInt64() & Vector256.Create(SinOperatorDouble.SignMask), Vector256.Create(SinOperatorDouble.MaxVectorizedValue)))
                    {
                        return ApplyScalar<SinPiOperator<double>>(x.AsDouble()).As<double, T>();
                    }
                }

                return SinOperator<T>.Invoke(xpi);
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                Vector512<T> xpi = x * Vector512.Create(T.Pi);
                if (typeof(T) == typeof(float))
                {
                    if (Vector512.GreaterThanAny(xpi.AsUInt32() & Vector512.Create(SinOperatorSingle.SignMask), Vector512.Create(SinOperatorSingle.MaxVectorizedValue)))
                    {
                        return ApplyScalar<SinPiOperator<float>>(x.AsSingle()).As<float, T>();
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector512.GreaterThanAny(xpi.AsUInt64() & Vector512.Create(SinOperatorDouble.SignMask), Vector512.Create(SinOperatorDouble.MaxVectorizedValue)))
                    {
                        return ApplyScalar<SinPiOperator<double>>(x.AsDouble()).As<double, T>();
                    }
                }

                return SinOperator<T>.Invoke(xpi);
            }
        }

        /// <summary>T.Sinh(x)</summary>
        internal readonly struct SinhOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            // Same as cosh, but with `z -` rather than `z +`, and with the sign
            // flipped on the result based on the sign of the input.

            private const float Single_LOGV = 0.693161f;
            private const float Single_HALFV = 1.0000138f;
            private const float Single_INVV2 = 0.24999309f;

            private const double Double_LOGV = 0.6931471805599453;
            private const double Double_HALFV = 1.0;
            private const double Double_INVV2 = 0.25;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Sinh(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector128<float> x = t.AsSingle();

                    Vector128<float> y = Vector128.Abs(x);
                    Vector128<float> z = ExpOperator<float>.Invoke(y - Vector128.Create((float)Single_LOGV));
                    Vector128<float> result = Vector128.Create((float)Single_HALFV) * (z - (Vector128.Create((float)Single_INVV2) / z));
                    Vector128<uint> sign = x.AsUInt32() & Vector128.Create(~(uint)int.MaxValue);
                    return (sign ^ result.AsUInt32()).As<uint, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    Vector128<double> x = t.AsDouble();

                    Vector128<double> y = Vector128.Abs(x);
                    Vector128<double> z = ExpOperator<double>.Invoke(y - Vector128.Create(Double_LOGV));
                    Vector128<double> result = Vector128.Create(Double_HALFV) * (z - (Vector128.Create(Double_INVV2) / z));
                    Vector128<ulong> sign = x.AsUInt64() & Vector128.Create(~(ulong)long.MaxValue);
                    return (sign ^ result.AsUInt64()).As<ulong, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<float> x = t.AsSingle();

                    Vector256<float> y = Vector256.Abs(x);
                    Vector256<float> z = ExpOperator<float>.Invoke(y - Vector256.Create((float)Single_LOGV));
                    Vector256<float> result = Vector256.Create((float)Single_HALFV) * (z - (Vector256.Create((float)Single_INVV2) / z));
                    Vector256<uint> sign = x.AsUInt32() & Vector256.Create(~(uint)int.MaxValue);
                    return (sign ^ result.AsUInt32()).As<uint, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    Vector256<double> x = t.AsDouble();

                    Vector256<double> y = Vector256.Abs(x);
                    Vector256<double> z = ExpOperator<double>.Invoke(y - Vector256.Create(Double_LOGV));
                    Vector256<double> result = Vector256.Create(Double_HALFV) * (z - (Vector256.Create(Double_INVV2) / z));
                    Vector256<ulong> sign = x.AsUInt64() & Vector256.Create(~(ulong)long.MaxValue);
                    return (sign ^ result.AsUInt64()).As<ulong, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector512<float> x = t.AsSingle();

                    Vector512<float> y = Vector512.Abs(x);
                    Vector512<float> z = ExpOperator<float>.Invoke(y - Vector512.Create((float)Single_LOGV));
                    Vector512<float> result = Vector512.Create((float)Single_HALFV) * (z - (Vector512.Create((float)Single_INVV2) / z));
                    Vector512<uint> sign = x.AsUInt32() & Vector512.Create(~(uint)int.MaxValue);
                    return (sign ^ result.AsUInt32()).As<uint, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    Vector512<double> x = t.AsDouble();

                    Vector512<double> y = Vector512.Abs(x);
                    Vector512<double> z = ExpOperator<double>.Invoke(y - Vector512.Create(Double_LOGV));
                    Vector512<double> result = Vector512.Create(Double_HALFV) * (z - (Vector512.Create(Double_INVV2) / z));
                    Vector512<ulong> sign = x.AsUInt64() & Vector512.Create(~(ulong)long.MaxValue);
                    return (sign ^ result.AsUInt64()).As<ulong, T>();
                }
            }
        }

        /// <summary>T.Tan(x)</summary>
        internal readonly struct TanOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            // This code is based on `vrs4_tan` and `vrd2_tan` from amd/aocl-libm-ose
            // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation notes from amd/aocl-libm-ose:
            // --------------------------------------------
            // A given x is reduced into the form:
            //          |x| = (N * π/2) + F
            // Where N is an integer obtained using:
            //         N = round(x * 2/π)
            // And F is a fraction part lying in the interval
            //         [-π/4, +π/4];
            // obtained as F = |x| - (N * π/2)
            // Thus tan(x) is given by
            //         tan(x) = tan((N * π/2) + F) = tan(F)
            //         when N is even, = -cot(F) = -1/tan(F)
            //         when N is odd, tan(F) is approximated using a polynomial
            //         obtained from Remez approximation from Sollya.

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Tan(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TanOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TanOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TanOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TanOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TanOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TanOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }
        }

        /// <summary>float.Tan(x)</summary>
        internal readonly struct TanOperatorSingle : IUnaryOperator<float, float>
        {
            internal const uint SignMask = 0x7FFFFFFFu;
            internal const uint MaxVectorizedValue = 0x49800000u;
            private const float AlmHuge = 1.2582912e7f;
            private const float Pi_Tail2 = 4.371139e-8f;
            private const float Pi_Tail3 = 1.7151245e-15f;
            private const float C1 = 0.33333358f;
            private const float C2 = 0.13332522f;
            private const float C3 = 0.05407107f;
            private const float C4 = 0.021237267f;
            private const float C5 = 0.010932301f;
            private const float C6 = -1.5722344e-5f;
            private const float C7 = 0.0044221194f;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Tan(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt32(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorSingle>(x);
                }

                Vector128<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector128.Create(2 / float.Pi), Vector128.Create(AlmHuge));
                Vector128<uint> odd = dn.AsUInt32() << 31;
                dn -= Vector128.Create(AlmHuge);

                Vector128<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(-float.Pi / 2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_15
                Vector128<float> f2 = f * f;
                Vector128<float> f4 = f2 * f2;
                Vector128<float> f8 = f4 * f4;
                Vector128<float> f12 = f8 * f4;
                Vector128<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C2), f2, Vector128.Create(C1));
                Vector128<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C4), f2, Vector128.Create(C3));
                Vector128<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C6), f2, Vector128.Create(C5));
                Vector128<float> b1 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector128<float> b2 = MultiplyAddEstimateOperator<float>.Invoke(f8, a3, f12 * Vector128.Create(C7));
                Vector128<float> poly = MultiplyAddEstimateOperator<float>.Invoke(f * f2, b1 + b2, f);

                Vector128<float> result = (poly.AsUInt32() ^ (x.AsUInt32() & Vector128.Create(~SignMask))).AsSingle();
                return Vector128.ConditionalSelect(Vector128.Equals(odd, Vector128<uint>.Zero).AsSingle(),
                    result,
                    Vector128.Create(-1f) / result);
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt32(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorSingle>(x);
                }

                Vector256<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector256.Create(2 / float.Pi), Vector256.Create(AlmHuge));
                Vector256<uint> odd = dn.AsUInt32() << 31;
                dn -= Vector256.Create(AlmHuge);

                Vector256<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(-float.Pi / 2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_15
                Vector256<float> f2 = f * f;
                Vector256<float> f4 = f2 * f2;
                Vector256<float> f8 = f4 * f4;
                Vector256<float> f12 = f8 * f4;
                Vector256<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C2), f2, Vector256.Create(C1));
                Vector256<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C4), f2, Vector256.Create(C3));
                Vector256<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C6), f2, Vector256.Create(C5));
                Vector256<float> b1 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector256<float> b2 = MultiplyAddEstimateOperator<float>.Invoke(f8, a3, f12 * Vector256.Create(C7));
                Vector256<float> poly = MultiplyAddEstimateOperator<float>.Invoke(f * f2, b1 + b2, f);

                Vector256<float> result = (poly.AsUInt32() ^ (x.AsUInt32() & Vector256.Create(~SignMask))).AsSingle();
                return Vector256.ConditionalSelect(Vector256.Equals(odd, Vector256<uint>.Zero).AsSingle(),
                    result,
                    Vector256.Create(-1f) / result);
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt32(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorSingle>(x);
                }

                Vector512<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector512.Create(2 / float.Pi), Vector512.Create(AlmHuge));
                Vector512<uint> odd = dn.AsUInt32() << 31;
                dn -= Vector512.Create(AlmHuge);

                Vector512<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(-float.Pi / 2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_15
                Vector512<float> f2 = f * f;
                Vector512<float> f4 = f2 * f2;
                Vector512<float> f8 = f4 * f4;
                Vector512<float> f12 = f8 * f4;
                Vector512<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C2), f2, Vector512.Create(C1));
                Vector512<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C4), f2, Vector512.Create(C3));
                Vector512<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C6), f2, Vector512.Create(C5));
                Vector512<float> b1 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector512<float> b2 = MultiplyAddEstimateOperator<float>.Invoke(f8, a3, f12 * Vector512.Create(C7));
                Vector512<float> poly = MultiplyAddEstimateOperator<float>.Invoke(f * f2, b1 + b2, f);

                Vector512<float> result = (poly.AsUInt32() ^ (x.AsUInt32() & Vector512.Create(~SignMask))).AsSingle();
                return Vector512.ConditionalSelect(Vector512.Equals(odd, Vector512<uint>.Zero).AsSingle(),
                    result,
                    Vector512.Create(-1f) / result);
            }
        }

        /// <summary>double.Tan(x)</summary>
        internal readonly struct TanOperatorDouble : IUnaryOperator<double, double>
        {
            internal const ulong SignMask = 0x7FFFFFFFFFFFFFFFul;
            internal const ulong MaxVectorizedValue = 0x4160000000000000ul;
            private const double AlmHuge = 6.755399441055744e15;
            private const double HalfPi2 = 6.123233995736766E-17;
            private const double HalfPi3 = -1.4973849048591698E-33;
            private const double C1 = 0.33333333333332493;
            private const double C3 = 0.133333333334343;
            private const double C5 = 0.0539682539203796;
            private const double C7 = 0.02186948972198256;
            private const double C9 = 0.008863217894198291;
            private const double C11 = 0.003592298593761111;
            private const double C13 = 0.0014547086183165365;
            private const double C15 = 5.952456856028558E-4;
            private const double C17 = 2.2190741289936845E-4;
            private const double C19 = 1.3739809957985104E-4;
            private const double C21 = -2.7500197359895707E-5;
            private const double C23 = 9.038741690184683E-5;
            private const double C25 = -4.534076545538694E-5;
            private const double C27 = 2.0966522562190197E-5;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Tan(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                Vector128<double> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt64(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorDouble>(x);
                }

                Vector128<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector128.Create(2 / double.Pi), Vector128.Create(AlmHuge));
                Vector128<ulong> odd = dn.AsUInt64() << 63;
                dn -= Vector128.Create(AlmHuge);
                Vector128<double> f = uxMasked.AsDouble() - (dn * (double.Pi / 2)) - (dn * HalfPi2) - (dn * HalfPi3);

                // POLY_EVAL_ODD_29
                Vector128<double> g = f * f;
                Vector128<double> g2 = g * g;
                Vector128<double> g3 = g * g2;
                Vector128<double> g5 = g3 * g2;
                Vector128<double> g7 = g5 * g2;
                Vector128<double> g9 = g7 * g2;
                Vector128<double> g11 = g9 * g2;
                Vector128<double> g13 = g11 * g2;
                Vector128<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C3), g, Vector128.Create(C1));
                Vector128<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C7), g, Vector128.Create(C5));
                Vector128<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C11), g, Vector128.Create(C9));
                Vector128<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C15), g, Vector128.Create(C13));
                Vector128<double> a5 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C19), g, Vector128.Create(C17));
                Vector128<double> a6 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C23), g, Vector128.Create(C21));
                Vector128<double> a7 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C27), g, Vector128.Create(C25));
                Vector128<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(g, a1, g3 * a2);
                Vector128<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(g5, a3, g7 * a4);
                Vector128<double> b3 = MultiplyAddEstimateOperator<double>.Invoke(g9, a5, g11 * a6);
                Vector128<double> q = MultiplyAddEstimateOperator<double>.Invoke(g13, a7, b1 + b2 + b3);
                Vector128<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, q, f);

                Vector128<double> result = (poly.AsUInt64() ^ (x.AsUInt64() & Vector128.Create(~SignMask))).AsDouble();
                return Vector128.ConditionalSelect(Vector128.Equals(odd, Vector128<ulong>.Zero).AsDouble(),
                    result,
                    Vector128.Create(-1.0) / result);
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                Vector256<double> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt64(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorDouble>(x);
                }

                Vector256<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector256.Create(2 / double.Pi), Vector256.Create(AlmHuge));
                Vector256<ulong> odd = dn.AsUInt64() << 63;
                dn -= Vector256.Create(AlmHuge);
                Vector256<double> f = uxMasked.AsDouble() - (dn * (double.Pi / 2)) - (dn * HalfPi2) - (dn * HalfPi3);

                // POLY_EVAL_ODD_29
                Vector256<double> g = f * f;
                Vector256<double> g2 = g * g;
                Vector256<double> g3 = g * g2;
                Vector256<double> g5 = g3 * g2;
                Vector256<double> g7 = g5 * g2;
                Vector256<double> g9 = g7 * g2;
                Vector256<double> g11 = g9 * g2;
                Vector256<double> g13 = g11 * g2;
                Vector256<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C3), g, Vector256.Create(C1));
                Vector256<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C7), g, Vector256.Create(C5));
                Vector256<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C11), g, Vector256.Create(C9));
                Vector256<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C15), g, Vector256.Create(C13));
                Vector256<double> a5 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C19), g, Vector256.Create(C17));
                Vector256<double> a6 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C23), g, Vector256.Create(C21));
                Vector256<double> a7 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C27), g, Vector256.Create(C25));
                Vector256<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(g, a1, g3 * a2);
                Vector256<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(g5, a3, g7 * a4);
                Vector256<double> b3 = MultiplyAddEstimateOperator<double>.Invoke(g9, a5, g11 * a6);
                Vector256<double> q = MultiplyAddEstimateOperator<double>.Invoke(g13, a7, b1 + b2 + b3);
                Vector256<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, q, f);

                Vector256<double> result = (poly.AsUInt64() ^ (x.AsUInt64() & Vector256.Create(~SignMask))).AsDouble();
                return Vector256.ConditionalSelect(Vector256.Equals(odd, Vector256<ulong>.Zero).AsDouble(),
                    result,
                    Vector256.Create(-1.0) / result);
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                Vector512<double> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt64(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorDouble>(x);
                }

                Vector512<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector512.Create(2 / double.Pi), Vector512.Create(AlmHuge));
                Vector512<ulong> odd = dn.AsUInt64() << 63;
                dn -= Vector512.Create(AlmHuge);
                Vector512<double> f = uxMasked.AsDouble() - (dn * (double.Pi / 2)) - (dn * HalfPi2) - (dn * HalfPi3);

                // POLY_EVAL_ODD_29
                Vector512<double> g = f * f;
                Vector512<double> g2 = g * g;
                Vector512<double> g3 = g * g2;
                Vector512<double> g5 = g3 * g2;
                Vector512<double> g7 = g5 * g2;
                Vector512<double> g9 = g7 * g2;
                Vector512<double> g11 = g9 * g2;
                Vector512<double> g13 = g11 * g2;
                Vector512<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C3), g, Vector512.Create(C1));
                Vector512<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C7), g, Vector512.Create(C5));
                Vector512<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C11), g, Vector512.Create(C9));
                Vector512<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C15), g, Vector512.Create(C13));
                Vector512<double> a5 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C19), g, Vector512.Create(C17));
                Vector512<double> a6 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C23), g, Vector512.Create(C21));
                Vector512<double> a7 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C27), g, Vector512.Create(C25));
                Vector512<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(g, a1, g3 * a2);
                Vector512<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(g5, a3, g7 * a4);
                Vector512<double> b3 = MultiplyAddEstimateOperator<double>.Invoke(g9, a5, g11 * a6);
                Vector512<double> q = MultiplyAddEstimateOperator<double>.Invoke(g13, a7, b1 + b2 + b3);
                Vector512<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, q, f);

                Vector512<double> result = (poly.AsUInt64() ^ (x.AsUInt64() & Vector512.Create(~SignMask))).AsDouble();
                return Vector512.ConditionalSelect(Vector512.Equals(odd, Vector512<ulong>.Zero).AsDouble(),
                    result,
                    Vector512.Create(-1.0) / result);
            }
        }

        /// <summary>T.TanPi(x)</summary>
        internal readonly struct TanPiOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => false;
            public static T Invoke(T x) => T.TanPi(x);
            public static Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.Tanh(x)</summary>
        internal readonly struct TanhOperator<T> : IUnaryOperator<T, T>
            where T : IHyperbolicFunctions<T>
        {
            // This code is based on `vrs4_tanhf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // To compute vrs4_tanhf(v_f32x4_t x)
            // Let y = |x|
            // If 0 <= y < 0x1.154246p3
            //    Let z = e^(-2.0 * y) - 1      -(1)
            //
            //    Using (1), tanhf(y) can be calculated as,
            //    tanhf(y) = -z / (z + 2.0)
            //
            // For other cases, call scalar tanhf()
            //
            // If x < 0, then we use the identity
            //    tanhf(-x) = -tanhf(x)

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Tanh(x);

            public static Vector128<T> Invoke(Vector128<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector128<float> x = t.AsSingle();

                    Vector128<float> y = Vector128.Abs(x);
                    Vector128<float> z = ExpM1Operator<float>.Invoke(Vector128.Create(-2f) * y);
                    Vector128<uint> sign = x.AsUInt32() & Vector128.Create(~(uint)int.MaxValue);
                    return (sign ^ (-z / (z + Vector128.Create(2f))).AsUInt32()).As<uint, T>();
                }
                else
                {
                    Vector128<double> x = t.AsDouble();

                    Vector128<double> y = Vector128.Abs(x);
                    Vector128<double> z = ExpM1Operator<double>.Invoke(Vector128.Create(-2d) * y);
                    Vector128<ulong> sign = x.AsUInt64() & Vector128.Create(~(ulong)long.MaxValue);
                    return (sign ^ (-z / (z + Vector128.Create(2d))).AsUInt64()).As<ulong, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<float> x = t.AsSingle();

                    Vector256<float> y = Vector256.Abs(x);
                    Vector256<float> z = ExpM1Operator<float>.Invoke(Vector256.Create(-2f) * y);
                    Vector256<uint> sign = x.AsUInt32() & Vector256.Create(~(uint)int.MaxValue);
                    return (sign ^ (-z / (z + Vector256.Create(2f))).AsUInt32()).As<uint, T>();
                }
                else
                {
                    Vector256<double> x = t.AsDouble();

                    Vector256<double> y = Vector256.Abs(x);
                    Vector256<double> z = ExpM1Operator<double>.Invoke(Vector256.Create(-2d) * y);
                    Vector256<ulong> sign = x.AsUInt64() & Vector256.Create(~(ulong)long.MaxValue);
                    return (sign ^ (-z / (z + Vector256.Create(2d))).AsUInt64()).As<ulong, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> t)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector512<float> x = t.AsSingle();

                    Vector512<float> y = Vector512.Abs(x);
                    Vector512<float> z = ExpM1Operator<float>.Invoke(Vector512.Create(-2f) * y);
                    Vector512<uint> sign = x.AsUInt32() & Vector512.Create(~(uint)int.MaxValue);
                    return (sign ^ (-z / (z + Vector512.Create(2f))).AsUInt32()).As<uint, T>();
                }
                else
                {
                    Vector512<double> x = t.AsDouble();

                    Vector512<double> y = Vector512.Abs(x);
                    Vector512<double> z = ExpM1Operator<double>.Invoke(Vector512.Create(-2d) * y);
                    Vector512<ulong> sign = x.AsUInt64() & Vector512.Create(~(ulong)long.MaxValue);
                    return (sign ^ (-z / (z + Vector512.Create(2d))).AsUInt64()).As<ulong, T>();
                }
            }
        }

        /// <summary>T.Log(x)</summary>
        internal readonly struct LogOperator<T> : IUnaryOperator<T, T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => (typeof(T) == typeof(double))
                                            || (typeof(T) == typeof(float));

            public static T Invoke(T x) => T.Log(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Log(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return LogOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return LogOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Log(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return LogOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return LogOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Log(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return LogOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return LogOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }
        }

#if !NET9_0_OR_GREATER
        /// <summary>double.Log(x)</summary>
        internal readonly struct LogOperatorDouble : IUnaryOperator<double, double>
        {
            // This code is based on `vrd2_log` from amd/aocl-libm-ose
            // Copyright (C) 2018-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Reduce x into the form:
            //        x = (-1)^s*2^n*m
            // s will be always zero, as log is defined for positive numbers
            // n is an integer known as the exponent
            // m is mantissa
            //
            // x is reduced such that the mantissa, m lies in [2/3,4/3]
            //      x = 2^n*m where m is in [2/3,4/3]
            //      log(x) = log(2^n*m)                 We have log(a*b) = log(a)+log(b)
            //             = log(2^n) + log(m)          We have log(a^n) = n*log(a)
            //             = n*log(2) + log(m)
            //             = n*log(2) + log(1+(m-1))
            //             = n*log(2) + log(1+f)        Where f = m-1
            //             = n*log(2) + log1p(f)        f lies in [-1/3,+1/3]
            //
            // Thus we have :
            // log(x) = n*log(2) + log1p(f)
            // In the above, the first term n*log(2), n can be calculated by using right shift operator and the value of log(2)
            // is known and is stored as a constant
            // The second term log1p(F) is approximated by using a polynomial

            private const ulong V_MIN = 0x00100000_00000000;    // SmallestNormal
            private const ulong V_MAX = 0x7FF00000_00000000;    // +Infinity
            private const ulong V_MSK = 0x000FFFFF_FFFFFFFF;    // (1 << 52) - 1
            private const ulong V_OFF = 0x3FE55555_55555555;    // 2.0 / 3.0

            private const double LN2_HEAD = 0.693359375;
            private const double LN2_TAIL = -0.00021219444005469057;

            private const double C02 = -0.499999999999999560;
            private const double C03 = +0.333333333333414750;
            private const double C04 = -0.250000000000297430;
            private const double C05 = +0.199999999975985220;
            private const double C06 = -0.166666666608919500;
            private const double C07 = +0.142857145600277100;
            private const double C08 = -0.125000005127831270;
            private const double C09 = +0.111110952357159440;
            private const double C10 = -0.099999750495501240;
            private const double C11 = +0.090914349823462390;
            private const double C12 = -0.083340600527551860;
            private const double C13 = +0.076817603328311300;
            private const double C14 = -0.071296718946287310;
            private const double C15 = +0.067963465211535730;
            private const double C16 = -0.063995035098960040;
            private const double C17 = +0.049370587082412105;
            private const double C18 = -0.045370170994891980;
            private const double C19 = +0.088970636003577750;
            private const double C20 = -0.086906174116908760;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Log(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                Vector128<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector128<ulong> specialMask = Vector128.GreaterThanOrEqual(x.AsUInt64() - Vector128.Create(V_MIN), Vector128.Create(V_MAX - V_MIN));

                if (specialMask != Vector128<ulong>.Zero)
                {
                    Vector128<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector128<double> lessThanZeroMask = Vector128.LessThan(xBits, Vector128<long>.Zero).AsDouble();

                    specialResult = Vector128.ConditionalSelect(
                        lessThanZeroMask,
                        Vector128.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector128<double> zeroMask = Vector128.Equals(xBits << 1, Vector128<long>.Zero).AsDouble();

                    specialResult = Vector128.ConditionalSelect(
                        zeroMask,
                        Vector128.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector128<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector128.GreaterThanOrEqual(xBits, Vector128.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector128<double> subnormalMask = Vector128.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector128.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector128.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector128<ulong> vx = x.AsUInt64() - Vector128.Create(V_OFF);
                Vector128<double> n = Vector128.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector128.Create(V_MSK)) + Vector128.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector128<double> r = vx.AsDouble() - Vector128<double>.One;

                Vector128<double> r02 = r * r;
                Vector128<double> r04 = r02 * r02;
                Vector128<double> r08 = r04 * r04;
                Vector128<double> r16 = r08 * r08;

                // Compute log(x + 1) using Polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector128<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector128.Create(C18)) * r02)
                                          + ((r * C17) + Vector128.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector128.Create(C14)) * r02)
                                          + ((r * C13) + Vector128.Create(C12))) * r04)
                                        + ((((r * C11) + Vector128.Create(C10)) * r02)
                                          + ((r * C09) + Vector128.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector128.Create(C06)) * r02)
                                          + ((r * C05) + Vector128.Create(C04))) * r04)
                                        + ((((r * C03) + Vector128.Create(C02)) * r02) + r);

                return Vector128.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (n * LN2_HEAD) + ((n * LN2_TAIL) + poly)
                );
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                Vector256<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector256<ulong> specialMask = Vector256.GreaterThanOrEqual(x.AsUInt64() - Vector256.Create(V_MIN), Vector256.Create(V_MAX - V_MIN));

                if (specialMask != Vector256<ulong>.Zero)
                {
                    Vector256<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector256<double> lessThanZeroMask = Vector256.LessThan(xBits, Vector256<long>.Zero).AsDouble();

                    specialResult = Vector256.ConditionalSelect(
                        lessThanZeroMask,
                        Vector256.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector256<double> zeroMask = Vector256.Equals(xBits << 1, Vector256<long>.Zero).AsDouble();

                    specialResult = Vector256.ConditionalSelect(
                        zeroMask,
                        Vector256.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector256<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector256.GreaterThanOrEqual(xBits, Vector256.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector256<double> subnormalMask = Vector256.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector256.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector256.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector256<ulong> vx = x.AsUInt64() - Vector256.Create(V_OFF);
                Vector256<double> n = Vector256.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector256.Create(V_MSK)) + Vector256.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector256<double> r = vx.AsDouble() - Vector256<double>.One;

                Vector256<double> r02 = r * r;
                Vector256<double> r04 = r02 * r02;
                Vector256<double> r08 = r04 * r04;
                Vector256<double> r16 = r08 * r08;

                // Compute log(x + 1) using Polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector256<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector256.Create(C18)) * r02)
                                          + ((r * C17) + Vector256.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector256.Create(C14)) * r02)
                                          + ((r * C13) + Vector256.Create(C12))) * r04)
                                        + ((((r * C11) + Vector256.Create(C10)) * r02)
                                          + ((r * C09) + Vector256.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector256.Create(C06)) * r02)
                                          + ((r * C05) + Vector256.Create(C04))) * r04)
                                        + ((((r * C03) + Vector256.Create(C02)) * r02) + r);

                return Vector256.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (n * LN2_HEAD) + ((n * LN2_TAIL) + poly)
                );
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                Vector512<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector512<ulong> specialMask = Vector512.GreaterThanOrEqual(x.AsUInt64() - Vector512.Create(V_MIN), Vector512.Create(V_MAX - V_MIN));

                if (specialMask != Vector512<ulong>.Zero)
                {
                    Vector512<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector512<double> lessThanZeroMask = Vector512.LessThan(xBits, Vector512<long>.Zero).AsDouble();

                    specialResult = Vector512.ConditionalSelect(
                        lessThanZeroMask,
                        Vector512.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector512<double> zeroMask = Vector512.Equals(xBits << 1, Vector512<long>.Zero).AsDouble();

                    specialResult = Vector512.ConditionalSelect(
                        zeroMask,
                        Vector512.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector512<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector512.GreaterThanOrEqual(xBits, Vector512.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector512<double> subnormalMask = Vector512.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector512.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector512.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector512<ulong> vx = x.AsUInt64() - Vector512.Create(V_OFF);
                Vector512<double> n = Vector512.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector512.Create(V_MSK)) + Vector512.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector512<double> r = vx.AsDouble() - Vector512<double>.One;

                Vector512<double> r02 = r * r;
                Vector512<double> r04 = r02 * r02;
                Vector512<double> r08 = r04 * r04;
                Vector512<double> r16 = r08 * r08;

                // Compute log(x + 1) using Polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector512<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector512.Create(C18)) * r02)
                                          + ((r * C17) + Vector512.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector512.Create(C14)) * r02)
                                          + ((r * C13) + Vector512.Create(C12))) * r04)
                                        + ((((r * C11) + Vector512.Create(C10)) * r02)
                                          + ((r * C09) + Vector512.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector512.Create(C06)) * r02)
                                          + ((r * C05) + Vector512.Create(C04))) * r04)
                                        + ((((r * C03) + Vector512.Create(C02)) * r02) + r);

                return Vector512.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (n * LN2_HEAD) + ((n * LN2_TAIL) + poly)
                );
            }
        }

        /// <summary>float.Log(x)</summary>
        internal readonly struct LogOperatorSingle : IUnaryOperator<float, float>
        {
            // This code is based on `vrs4_logf` from amd/aocl-libm-ose
            // Copyright (C) 2018-2019 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Spec:
            //   logf(x)
            //          = logf(x)           if x ∈ F and x > 0
            //          = x                 if x = qNaN
            //          = 0                 if x = 1
            //          = -inf              if x = (-0, 0}
            //          = NaN               otherwise
            //
            // Assumptions/Expectations
            //      - ULP is derived to be << 4 (always)
            // - Some FPU Exceptions may not be available
            //      - Performance is at least 3x
            //
            // Implementation Notes:
            //  1. Range Reduction:
            //      x = 2^n*(1+f)                                          .... (1)
            //         where n is exponent and is an integer
            //             (1+f) is mantissa ∈ [1,2). i.e., 1 ≤ 1+f < 2    .... (2)
            //
            //    From (1), taking log on both sides
            //      log(x) = log(2^n * (1+f))
            //             = log(2^n) + log(1+f)
            //             = n*log(2) + log(1+f)                           .... (3)
            //
            //      let z = 1 + f
            //             log(z) = log(k) + log(z) - log(k)
            //             log(z) = log(kz) - log(k)
            //
            //    From (2), range of z is [1, 2)
            //       by simply dividing range by 'k', z is in [1/k, 2/k)  .... (4)
            //       Best choice of k is the one which gives equal and opposite values
            //       at extrema        +-      -+
            //              1          | 2      |
            //             --- - 1 = - |--- - 1 |
            //              k          | k      |                         .... (5)
            //                         +-      -+
            //
            //       Solving for k, k = 3/2,
            //    From (4), using 'k' value, range is therefore [-0.3333, 0.3333]
            //
            //  2. Polynomial Approximation:
            //     More information refer to tools/sollya/vrs4_logf.sollya
            //
            //     7th Deg -   Error abs: 0x1.04c4ac98p-22   rel: 0x1.2216e6f8p-19
            //     6th Deg -   Error abs: 0x1.179e97d8p-19   rel: 0x1.db676c1p-17

            private const uint V_MIN = 0x00800000;
            private const uint V_MAX = 0x7F800000;
            private const uint V_MASK = 0x007FFFFF;
            private const uint V_OFF = 0x3F2AAAAB;

            private const float V_LN2 = 0.6931472f;

            private const float C0 = 0.0f;
            private const float C1 = 1.0f;
            private const float C2 = -0.5000001f;
            private const float C3 = 0.33332965f;
            private const float C4 = -0.24999046f;
            private const float C5 = 0.20018855f;
            private const float C6 = -0.16700386f;
            private const float C7 = 0.13902695f;
            private const float C8 = -0.1197452f;
            private const float C9 = 0.14401625f;
            private const float C10 = -0.13657966f;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Log(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector128<uint> specialMask = Vector128.GreaterThanOrEqual(x.AsUInt32() - Vector128.Create(V_MIN), Vector128.Create(V_MAX - V_MIN));

                if (specialMask != Vector128<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector128<float> zeroMask = Vector128.Equals(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        zeroMask,
                        Vector128.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector128<float> lessThanZeroMask = Vector128.LessThan(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        lessThanZeroMask,
                        Vector128.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector128<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector128.Equals(x, x)
                                          | Vector128.Equals(x, Vector128.Create(float.PositiveInfinity));

                    // subnormal
                    Vector128<float> subnormalMask = Vector128.AndNot(specialMask.AsSingle(), temp);

                    x = Vector128.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector128.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector128<uint> vx = x.AsUInt32() - Vector128.Create(V_OFF);
                Vector128<float> n = Vector128.ConvertToSingle(Vector128.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector128.Create(V_MASK)) + Vector128.Create(V_OFF);

                Vector128<float> r = vx.AsSingle() - Vector128<float>.One;

                Vector128<float> r2 = r * r;
                Vector128<float> r4 = r2 * r2;
                Vector128<float> r8 = r4 * r4;

                Vector128<float> q = (Vector128.Create(C10) * r2 + (Vector128.Create(C9) * r + Vector128.Create(C8)))
                                                          * r8 + (((Vector128.Create(C7) * r + Vector128.Create(C6))
                                                            * r2 + (Vector128.Create(C5) * r + Vector128.Create(C4)))
                                                           * r4 + ((Vector128.Create(C3) * r + Vector128.Create(C2))
                                                            * r2 + (Vector128.Create(C1) * r + Vector128.Create(C0))));

                return Vector128.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector128.Create(V_LN2) + q
                );
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector256<uint> specialMask = Vector256.GreaterThanOrEqual(x.AsUInt32() - Vector256.Create(V_MIN), Vector256.Create(V_MAX - V_MIN));

                if (specialMask != Vector256<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector256<float> zeroMask = Vector256.Equals(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        zeroMask,
                        Vector256.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector256<float> lessThanZeroMask = Vector256.LessThan(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        lessThanZeroMask,
                        Vector256.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector256<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector256.Equals(x, x)
                                          | Vector256.Equals(x, Vector256.Create(float.PositiveInfinity));

                    // subnormal
                    Vector256<float> subnormalMask = Vector256.AndNot(specialMask.AsSingle(), temp);

                    x = Vector256.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector256.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector256<uint> vx = x.AsUInt32() - Vector256.Create(V_OFF);
                Vector256<float> n = Vector256.ConvertToSingle(Vector256.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector256.Create(V_MASK)) + Vector256.Create(V_OFF);

                Vector256<float> r = vx.AsSingle() - Vector256<float>.One;

                Vector256<float> r2 = r * r;
                Vector256<float> r4 = r2 * r2;
                Vector256<float> r8 = r4 * r4;

                Vector256<float> q = (Vector256.Create(C10) * r2 + (Vector256.Create(C9) * r + Vector256.Create(C8)))
                                                          * r8 + (((Vector256.Create(C7) * r + Vector256.Create(C6))
                                                            * r2 + (Vector256.Create(C5) * r + Vector256.Create(C4)))
                                                           * r4 + ((Vector256.Create(C3) * r + Vector256.Create(C2))
                                                            * r2 + (Vector256.Create(C1) * r + Vector256.Create(C0))));

                return Vector256.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector256.Create(V_LN2) + q
                );
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector512<uint> specialMask = Vector512.GreaterThanOrEqual(x.AsUInt32() - Vector512.Create(V_MIN), Vector512.Create(V_MAX - V_MIN));

                if (specialMask != Vector512<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector512<float> zeroMask = Vector512.Equals(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        zeroMask,
                        Vector512.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector512<float> lessThanZeroMask = Vector512.LessThan(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        lessThanZeroMask,
                        Vector512.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector512<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector512.Equals(x, x)
                                          | Vector512.Equals(x, Vector512.Create(float.PositiveInfinity));

                    // subnormal
                    Vector512<float> subnormalMask = Vector512.AndNot(specialMask.AsSingle(), temp);

                    x = Vector512.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector512.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector512<uint> vx = x.AsUInt32() - Vector512.Create(V_OFF);
                Vector512<float> n = Vector512.ConvertToSingle(Vector512.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector512.Create(V_MASK)) + Vector512.Create(V_OFF);

                Vector512<float> r = vx.AsSingle() - Vector512<float>.One;

                Vector512<float> r2 = r * r;
                Vector512<float> r4 = r2 * r2;
                Vector512<float> r8 = r4 * r4;

                Vector512<float> q = (Vector512.Create(C10) * r2 + (Vector512.Create(C9) * r + Vector512.Create(C8)))
                                                          * r8 + (((Vector512.Create(C7) * r + Vector512.Create(C6))
                                                            * r2 + (Vector512.Create(C5) * r + Vector512.Create(C4)))
                                                           * r4 + ((Vector512.Create(C3) * r + Vector512.Create(C2))
                                                            * r2 + (Vector512.Create(C1) * r + Vector512.Create(C0))));

                return Vector512.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n * Vector512.Create(V_LN2) + q
                );
            }
        }
#endif

        /// <summary>T.Log2(x)</summary>
        internal readonly struct Log2Operator<T> : IUnaryOperator<T, T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => (typeof(T) == typeof(double))
                                            || (typeof(T) == typeof(float));

            public static T Invoke(T x) => T.Log2(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Log2(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Log2(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return Log2OperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Log2OperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Log2(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Log2(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return Log2OperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Log2OperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Log2(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Log2(x.AsSingle()).As<float, T>();
                }
#else
                if (typeof(T) == typeof(double))
                {
                    return Log2OperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Log2OperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
#endif
            }
        }

#if !NET9_0_OR_GREATER
        /// <summary>double.Log2(x)</summary>
        internal readonly struct Log2OperatorDouble : IUnaryOperator<double, double>
        {
            // This code is based on `vrd2_log2` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Reduce x into the form:
            //        x = (-1)^s*2^n*m
            // s will be always zero, as log is defined for positive numbers
            // n is an integer known as the exponent
            // m is mantissa
            //
            // x is reduced such that the mantissa, m lies in [2/3,4/3]
            //      x = 2^n*m where m is in [2/3,4/3]
            //      log2(x) = log2(2^n*m)              We have log(a*b) = log(a)+log(b)
            //             = log2(2^n) + log2(m)       We have log(a^n) = n*log(a)
            //             = n + log2(m)
            //             = n + log2(1+(m-1))
            //             = n + ln(1+f) * log2(e)          Where f = m-1
            //             = n + log1p(f) * log2(e)         f lies in [-1/3,+1/3]
            //
            // Thus we have :
            // log(x) = n + log1p(f) * log2(e)
            // The second term log1p(F) is approximated by using a polynomial

            private const ulong V_MIN = 0x00100000_00000000;    // SmallestNormal
            private const ulong V_MAX = 0x7FF00000_00000000;    // +Infinity
            private const ulong V_MSK = 0x000FFFFF_FFFFFFFF;    // (1 << 52) - 1
            private const ulong V_OFF = 0x3FE55555_55555555;    // 2.0 / 3.0

            private const double LN2_HEAD = 1.44269180297851562500E+00;
            private const double LN2_TAIL = 3.23791044778235969970E-06;

            private const double C02 = -0.499999999999999560;
            private const double C03 = +0.333333333333414750;
            private const double C04 = -0.250000000000297430;
            private const double C05 = +0.199999999975985220;
            private const double C06 = -0.166666666608919500;
            private const double C07 = +0.142857145600277100;
            private const double C08 = -0.125000005127831270;
            private const double C09 = +0.111110952357159440;
            private const double C10 = -0.099999750495501240;
            private const double C11 = +0.090914349823462390;
            private const double C12 = -0.083340600527551860;
            private const double C13 = +0.076817603328311300;
            private const double C14 = -0.071296718946287310;
            private const double C15 = +0.067963465211535730;
            private const double C16 = -0.063995035098960040;
            private const double C17 = +0.049370587082412105;
            private const double C18 = -0.045370170994891980;
            private const double C19 = +0.088970636003577750;
            private const double C20 = -0.086906174116908760;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Log2(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                Vector128<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector128<ulong> specialMask = Vector128.GreaterThanOrEqual(x.AsUInt64() - Vector128.Create(V_MIN), Vector128.Create(V_MAX - V_MIN));

                if (specialMask != Vector128<ulong>.Zero)
                {
                    Vector128<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector128<double> lessThanZeroMask = Vector128.LessThan(xBits, Vector128<long>.Zero).AsDouble();

                    specialResult = Vector128.ConditionalSelect(
                        lessThanZeroMask,
                        Vector128.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector128<double> zeroMask = Vector128.Equals(xBits << 1, Vector128<long>.Zero).AsDouble();

                    specialResult = Vector128.ConditionalSelect(
                        zeroMask,
                        Vector128.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector128<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector128.GreaterThanOrEqual(xBits, Vector128.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector128<double> subnormalMask = Vector128.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector128.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector128.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector128<ulong> vx = x.AsUInt64() - Vector128.Create(V_OFF);
                Vector128<double> n = Vector128.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector128.Create(V_MSK)) + Vector128.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector128<double> r = vx.AsDouble() - Vector128<double>.One;

                Vector128<double> r02 = r * r;
                Vector128<double> r04 = r02 * r02;
                Vector128<double> r08 = r04 * r04;
                Vector128<double> r16 = r08 * r08;

                // Compute log(x + 1) using polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector128<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector128.Create(C18)) * r02)
                                          + ((r * C17) + Vector128.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector128.Create(C14)) * r02)
                                          + ((r * C13) + Vector128.Create(C12))) * r04)
                                        + ((((r * C11) + Vector128.Create(C10)) * r02)
                                          + ((r * C09) + Vector128.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector128.Create(C06)) * r02)
                                          + ((r * C05) + Vector128.Create(C04))) * r04)
                                        + ((((r * C03) + Vector128.Create(C02)) * r02) + r);

                return Vector128.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (poly * LN2_HEAD) + ((poly * LN2_TAIL) + n)
                );
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                Vector256<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector256<ulong> specialMask = Vector256.GreaterThanOrEqual(x.AsUInt64() - Vector256.Create(V_MIN), Vector256.Create(V_MAX - V_MIN));

                if (specialMask != Vector256<ulong>.Zero)
                {
                    Vector256<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector256<double> lessThanZeroMask = Vector256.LessThan(xBits, Vector256<long>.Zero).AsDouble();

                    specialResult = Vector256.ConditionalSelect(
                        lessThanZeroMask,
                        Vector256.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector256<double> zeroMask = Vector256.Equals(xBits << 1, Vector256<long>.Zero).AsDouble();

                    specialResult = Vector256.ConditionalSelect(
                        zeroMask,
                        Vector256.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector256<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector256.GreaterThanOrEqual(xBits, Vector256.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector256<double> subnormalMask = Vector256.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector256.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector256.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector256<ulong> vx = x.AsUInt64() - Vector256.Create(V_OFF);
                Vector256<double> n = Vector256.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector256.Create(V_MSK)) + Vector256.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector256<double> r = vx.AsDouble() - Vector256<double>.One;

                Vector256<double> r02 = r * r;
                Vector256<double> r04 = r02 * r02;
                Vector256<double> r08 = r04 * r04;
                Vector256<double> r16 = r08 * r08;

                // Compute log(x + 1) using polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector256<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector256.Create(C18)) * r02)
                                          + ((r * C17) + Vector256.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector256.Create(C14)) * r02)
                                          + ((r * C13) + Vector256.Create(C12))) * r04)
                                        + ((((r * C11) + Vector256.Create(C10)) * r02)
                                          + ((r * C09) + Vector256.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector256.Create(C06)) * r02)
                                          + ((r * C05) + Vector256.Create(C04))) * r04)
                                        + ((((r * C03) + Vector256.Create(C02)) * r02) + r);

                return Vector256.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (poly * LN2_HEAD) + ((poly * LN2_TAIL) + n)
                );
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                Vector512<double> specialResult = x;

                // x is zero, subnormal, infinity, or NaN
                Vector512<ulong> specialMask = Vector512.GreaterThanOrEqual(x.AsUInt64() - Vector512.Create(V_MIN), Vector512.Create(V_MAX - V_MIN));

                if (specialMask != Vector512<ulong>.Zero)
                {
                    Vector512<long> xBits = x.AsInt64();

                    // (x < 0) ? float.NaN : x
                    Vector512<double> lessThanZeroMask = Vector512.LessThan(xBits, Vector512<long>.Zero).AsDouble();

                    specialResult = Vector512.ConditionalSelect(
                        lessThanZeroMask,
                        Vector512.Create(double.NaN),
                        specialResult
                    );

                    // double.IsZero(x) ? double.NegativeInfinity : x
                    Vector512<double> zeroMask = Vector512.Equals(xBits << 1, Vector512<long>.Zero).AsDouble();

                    specialResult = Vector512.ConditionalSelect(
                        zeroMask,
                        Vector512.Create(double.NegativeInfinity),
                        specialResult
                    );

                    // double.IsZero(x) | (x < 0) | double.IsNaN(x) | double.IsPositiveInfinity(x)
                    Vector512<double> temp = zeroMask
                                           | lessThanZeroMask
                                           | Vector512.GreaterThanOrEqual(xBits, Vector512.Create(double.PositiveInfinity).AsInt64()).AsDouble();

                    // subnormal
                    Vector512<double> subnormalMask = Vector512.AndNot(specialMask.AsDouble(), temp);

                    // multiply by 2^52, then normalize
                    x = Vector512.ConditionalSelect(
                        subnormalMask,
                        ((x * 4503599627370496.0).AsUInt64() - Vector512.Create(52ul << 52)).AsDouble(),
                        x
                    );

                    specialMask = temp.AsUInt64();
                }

                // Reduce the mantissa to [+2/3, +4/3]
                Vector512<ulong> vx = x.AsUInt64() - Vector512.Create(V_OFF);
                Vector512<double> n = Vector512.ConvertToDouble(vx.AsInt64() >> 52);
                vx = (vx & Vector512.Create(V_MSK)) + Vector512.Create(V_OFF);

                // Adjust the mantissa to [-1/3, +1/3]
                Vector512<double> r = vx.AsDouble() - Vector512<double>.One;

                Vector512<double> r02 = r * r;
                Vector512<double> r04 = r02 * r02;
                Vector512<double> r08 = r04 * r04;
                Vector512<double> r16 = r08 * r08;

                // Compute log(x + 1) using polynomial approximation
                //      C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)

                Vector512<double> poly = (((r04 * C20)
                                        + ((((r * C19) + Vector512.Create(C18)) * r02)
                                          + ((r * C17) + Vector512.Create(C16)))) * r16)
                                     + (((((((r * C15) + Vector512.Create(C14)) * r02)
                                          + ((r * C13) + Vector512.Create(C12))) * r04)
                                        + ((((r * C11) + Vector512.Create(C10)) * r02)
                                          + ((r * C09) + Vector512.Create(C08)))) * r08)
                                       + (((((r * C07) + Vector512.Create(C06)) * r02)
                                          + ((r * C05) + Vector512.Create(C04))) * r04)
                                        + ((((r * C03) + Vector512.Create(C02)) * r02) + r);

                return Vector512.ConditionalSelect(
                    specialMask.AsDouble(),
                    specialResult,
                    (poly * LN2_HEAD) + ((poly * LN2_TAIL) + n)
                );
            }
        }

        /// <summary>float.Log2(x)</summary>
        internal readonly struct Log2OperatorSingle : IUnaryOperator<float, float>
        {
            // This code is based on `vrs4_log2f` from amd/aocl-libm-ose
            // Copyright (C) 2021-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Spec:
            //   log2f(x)
            //          = log2f(x)          if x ∈ F and x > 0
            //          = x                 if x = qNaN
            //          = 0                 if x = 1
            //          = -inf              if x = (-0, 0}
            //          = NaN               otherwise
            //
            // Assumptions/Expectations
            //      - Maximum ULP is observed to be at 4
            //      - Some FPU Exceptions may not be available
            //      - Performance is at least 3x
            //
            // Implementation Notes:
            //  1. Range Reduction:
            //      x = 2^n*(1+f)                                          .... (1)
            //         where n is exponent and is an integer
            //             (1+f) is mantissa ∈ [1,2). i.e., 1 ≤ 1+f < 2    .... (2)
            //
            //    From (1), taking log on both sides
            //      log2(x) = log2(2^n * (1+f))
            //             = n + log2(1+f)                           .... (3)
            //
            //      let z = 1 + f
            //             log2(z) = log2(k) + log2(z) - log2(k)
            //             log2(z) = log2(kz) - log2(k)
            //
            //    From (2), range of z is [1, 2)
            //       by simply dividing range by 'k', z is in [1/k, 2/k)  .... (4)
            //       Best choice of k is the one which gives equal and opposite values
            //       at extrema        +-      -+
            //              1          | 2      |
            //             --- - 1 = - |--- - 1 |
            //              k          | k      |                         .... (5)
            //                         +-      -+
            //
            //       Solving for k, k = 3/2,
            //    From (4), using 'k' value, range is therefore [-0.3333, 0.3333]
            //
            //  2. Polynomial Approximation:
            //     More information refer to tools/sollya/vrs4_logf.sollya
            //
            //     7th Deg -   Error abs: 0x1.04c4ac98p-22   rel: 0x1.2216e6f8p-19

            private const uint V_MIN = 0x00800000;
            private const uint V_MAX = 0x7F800000;
            private const uint V_MASK = 0x007FFFFF;
            private const uint V_OFF = 0x3F2AAAAB;

            private const float C0 = 0.0f;
            private const float C1 = 1.4426951f;
            private const float C2 = -0.72134554f;
            private const float C3 = 0.48089063f;
            private const float C4 = -0.36084408f;
            private const float C5 = 0.2888971f;
            private const float C6 = -0.23594281f;
            private const float C7 = 0.19948183f;
            private const float C8 = -0.22616665f;
            private const float C9 = 0.21228963f;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Log2(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector128<uint> specialMask = Vector128.GreaterThanOrEqual(x.AsUInt32() - Vector128.Create(V_MIN), Vector128.Create(V_MAX - V_MIN));

                if (specialMask != Vector128<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector128<float> zeroMask = Vector128.Equals(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        zeroMask,
                        Vector128.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector128<float> lessThanZeroMask = Vector128.LessThan(x, Vector128<float>.Zero);

                    specialResult = Vector128.ConditionalSelect(
                        lessThanZeroMask,
                        Vector128.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector128<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector128.Equals(x, x)
                                          | Vector128.Equals(x, Vector128.Create(float.PositiveInfinity));

                    // subnormal
                    Vector128<float> subnormalMask = Vector128.AndNot(specialMask.AsSingle(), temp);

                    x = Vector128.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector128.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector128<uint> vx = x.AsUInt32() - Vector128.Create(V_OFF);
                Vector128<float> n = Vector128.ConvertToSingle(Vector128.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector128.Create(V_MASK)) + Vector128.Create(V_OFF);

                Vector128<float> r = vx.AsSingle() - Vector128<float>.One;

                Vector128<float> r2 = r * r;
                Vector128<float> r4 = r2 * r2;
                Vector128<float> r8 = r4 * r4;

                Vector128<float> poly = (Vector128.Create(C9) * r + Vector128.Create(C8)) * r8
                                    + (((Vector128.Create(C7) * r + Vector128.Create(C6)) * r2
                                      + (Vector128.Create(C5) * r + Vector128.Create(C4))) * r4
                                     + ((Vector128.Create(C3) * r + Vector128.Create(C2)) * r2
                                      + (Vector128.Create(C1) * r + Vector128.Create(C0))));

                return Vector128.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n + poly
                );
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector256<uint> specialMask = Vector256.GreaterThanOrEqual(x.AsUInt32() - Vector256.Create(V_MIN), Vector256.Create(V_MAX - V_MIN));

                if (specialMask != Vector256<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector256<float> zeroMask = Vector256.Equals(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        zeroMask,
                        Vector256.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector256<float> lessThanZeroMask = Vector256.LessThan(x, Vector256<float>.Zero);

                    specialResult = Vector256.ConditionalSelect(
                        lessThanZeroMask,
                        Vector256.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector256<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector256.Equals(x, x)
                                          | Vector256.Equals(x, Vector256.Create(float.PositiveInfinity));

                    // subnormal
                    Vector256<float> subnormalMask = Vector256.AndNot(specialMask.AsSingle(), temp);

                    x = Vector256.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector256.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector256<uint> vx = x.AsUInt32() - Vector256.Create(V_OFF);
                Vector256<float> n = Vector256.ConvertToSingle(Vector256.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector256.Create(V_MASK)) + Vector256.Create(V_OFF);

                Vector256<float> r = vx.AsSingle() - Vector256<float>.One;

                Vector256<float> r2 = r * r;
                Vector256<float> r4 = r2 * r2;
                Vector256<float> r8 = r4 * r4;

                Vector256<float> poly = (Vector256.Create(C9) * r + Vector256.Create(C8)) * r8
                                    + (((Vector256.Create(C7) * r + Vector256.Create(C6)) * r2
                                      + (Vector256.Create(C5) * r + Vector256.Create(C4))) * r4
                                     + ((Vector256.Create(C3) * r + Vector256.Create(C2)) * r2
                                      + (Vector256.Create(C1) * r + Vector256.Create(C0))));

                return Vector256.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n + poly
                );
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> specialResult = x;

                // x is subnormal or infinity or NaN
                Vector512<uint> specialMask = Vector512.GreaterThanOrEqual(x.AsUInt32() - Vector512.Create(V_MIN), Vector512.Create(V_MAX - V_MIN));

                if (specialMask != Vector512<uint>.Zero)
                {
                    // float.IsZero(x) ? float.NegativeInfinity : x
                    Vector512<float> zeroMask = Vector512.Equals(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        zeroMask,
                        Vector512.Create(float.NegativeInfinity),
                        specialResult
                    );

                    // (x < 0) ? float.NaN : x
                    Vector512<float> lessThanZeroMask = Vector512.LessThan(x, Vector512<float>.Zero);

                    specialResult = Vector512.ConditionalSelect(
                        lessThanZeroMask,
                        Vector512.Create(float.NaN),
                        specialResult
                    );

                    // float.IsZero(x) | (x < 0) | float.IsNaN(x) | float.IsPositiveInfinity(x)
                    Vector512<float> temp = zeroMask
                                          | lessThanZeroMask
                                          | ~Vector512.Equals(x, x)
                                          | Vector512.Equals(x, Vector512.Create(float.PositiveInfinity));

                    // subnormal
                    Vector512<float> subnormalMask = Vector512.AndNot(specialMask.AsSingle(), temp);

                    x = Vector512.ConditionalSelect(
                        subnormalMask,
                        ((x * 8388608.0f).AsUInt32() - Vector512.Create(23u << 23)).AsSingle(),
                        x
                    );

                    specialMask = temp.AsUInt32();
                }

                Vector512<uint> vx = x.AsUInt32() - Vector512.Create(V_OFF);
                Vector512<float> n = Vector512.ConvertToSingle(Vector512.ShiftRightArithmetic(vx.AsInt32(), 23));

                vx = (vx & Vector512.Create(V_MASK)) + Vector512.Create(V_OFF);

                Vector512<float> r = vx.AsSingle() - Vector512<float>.One;

                Vector512<float> r2 = r * r;
                Vector512<float> r4 = r2 * r2;
                Vector512<float> r8 = r4 * r4;

                Vector512<float> poly = (Vector512.Create(C9) * r + Vector512.Create(C8)) * r8
                                    + (((Vector512.Create(C7) * r + Vector512.Create(C6)) * r2
                                      + (Vector512.Create(C5) * r + Vector512.Create(C4))) * r4
                                     + ((Vector512.Create(C3) * r + Vector512.Create(C2)) * r2
                                      + (Vector512.Create(C1) * r + Vector512.Create(C0))));

                return Vector512.ConditionalSelect(
                    specialMask.AsSingle(),
                    specialResult,
                    n + poly
                );
            }
        }
#endif

        /// <summary>T.Log10(x)</summary>
        internal readonly struct Log10Operator<T> : IUnaryOperator<T, T>
            where T : ILogarithmicFunctions<T>
        {
            private const double NaturalLog10 = 2.302585092994046;
            public static bool Vectorizable => LogOperator<T>.Vectorizable;
            public static T Invoke(T x) => T.Log10(x);
            public static Vector128<T> Invoke(Vector128<T> x) => LogOperator<T>.Invoke(x) / Vector128.Create(T.CreateTruncating(NaturalLog10));
            public static Vector256<T> Invoke(Vector256<T> x) => LogOperator<T>.Invoke(x) / Vector256.Create(T.CreateTruncating(NaturalLog10));
            public static Vector512<T> Invoke(Vector512<T> x) => LogOperator<T>.Invoke(x) / Vector512.Create(T.CreateTruncating(NaturalLog10));
        }

        /// <summary>T.LogP1(x)</summary>
        internal readonly struct LogP1Operator<T> : IUnaryOperator<T, T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => LogOperator<T>.Vectorizable;
            public static T Invoke(T x) => T.LogP1(x);
            public static Vector128<T> Invoke(Vector128<T> x) => LogOperator<T>.Invoke(x + Vector128<T>.One);
            public static Vector256<T> Invoke(Vector256<T> x) => LogOperator<T>.Invoke(x + Vector256<T>.One);
            public static Vector512<T> Invoke(Vector512<T> x) => LogOperator<T>.Invoke(x + Vector512<T>.One);
        }

        /// <summary>T.Log2P1(x)</summary>
        internal readonly struct Log2P1Operator<T> : IUnaryOperator<T, T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => Log2Operator<T>.Vectorizable;
            public static T Invoke(T x) => T.Log2P1(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Log2Operator<T>.Invoke(x + Vector128<T>.One);
            public static Vector256<T> Invoke(Vector256<T> x) => Log2Operator<T>.Invoke(x + Vector256<T>.One);
            public static Vector512<T> Invoke(Vector512<T> x) => Log2Operator<T>.Invoke(x + Vector512<T>.One);
        }

        /// <summary>T.Log10P1(x)</summary>
        internal readonly struct Log10P1Operator<T> : IUnaryOperator<T, T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => Log10Operator<T>.Vectorizable;
            public static T Invoke(T x) => T.Log10P1(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Log10Operator<T>.Invoke(x + Vector128<T>.One);
            public static Vector256<T> Invoke(Vector256<T> x) => Log10Operator<T>.Invoke(x + Vector256<T>.One);
            public static Vector512<T> Invoke(Vector512<T> x) => Log10Operator<T>.Invoke(x + Vector512<T>.One);
        }

        /// <summary>T.Log(x, y)</summary>
        internal readonly struct LogBaseOperator<T> : IBinaryOperator<T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => LogOperator<T>.Vectorizable;
            public static T Invoke(T x, T y) => T.Log(x, y);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
        }

        /// <summary>1 / (1 + T.Exp(-x))</summary>
        internal readonly struct SigmoidOperator<T> : IUnaryOperator<T, T> where T : IExponentialFunctions<T>
        {
            public static bool Vectorizable => ExpOperator<T>.Vectorizable;
            public static T Invoke(T x) => T.One / (T.One + T.Exp(-x));
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.Create(T.One) / (Vector128.Create(T.One) + ExpOperator<T>.Invoke(-x));
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.Create(T.One) / (Vector256.Create(T.One) + ExpOperator<T>.Invoke(-x));
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.Create(T.One) / (Vector512.Create(T.One) + ExpOperator<T>.Invoke(-x));
        }

        internal readonly struct CeilingOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Ceiling(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector128.Ceiling(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return Vector128.Ceiling(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector256.Ceiling(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return Vector256.Ceiling(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector512.Ceiling(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return Vector512.Ceiling(x.AsDouble()).As<double, T>();
                }
            }
        }

        internal readonly struct FloorOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Floor(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector128.Floor(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return Vector128.Floor(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector256.Floor(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return Vector256.Floor(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return Vector512.Floor(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return Vector512.Floor(x.AsDouble()).As<double, T>();
                }
            }
        }

        private readonly struct TruncateOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Truncate(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    if (Sse41.IsSupported) return Sse41.RoundToZero(x.AsSingle()).As<float, T>();
                    if (AdvSimd.IsSupported) return AdvSimd.RoundToZero(x.AsSingle()).As<float, T>();

                    return Vector128.ConditionalSelect(Vector128.GreaterThanOrEqual(x, Vector128<T>.Zero),
                        Vector128.Floor(x.AsSingle()).As<float, T>(),
                        Vector128.Ceiling(x.AsSingle()).As<float, T>());
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));

                    if (Sse41.IsSupported) return Sse41.RoundToZero(x.AsDouble()).As<double, T>();
                    if (AdvSimd.Arm64.IsSupported) return AdvSimd.Arm64.RoundToZero(x.AsDouble()).As<double, T>();

                    return Vector128.ConditionalSelect(Vector128.GreaterThanOrEqual(x, Vector128<T>.Zero),
                        Vector128.Floor(x.AsDouble()).As<double, T>(),
                        Vector128.Ceiling(x.AsDouble()).As<double, T>());
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    if (Avx.IsSupported) return Avx.RoundToZero(x.AsSingle()).As<float, T>();

                    return Vector256.ConditionalSelect(Vector256.GreaterThanOrEqual(x, Vector256<T>.Zero),
                        Vector256.Floor(x.AsSingle()).As<float, T>(),
                        Vector256.Ceiling(x.AsSingle()).As<float, T>());
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));

                    if (Avx.IsSupported) return Avx.RoundToZero(x.AsDouble()).As<double, T>();

                    return Vector256.ConditionalSelect(Vector256.GreaterThanOrEqual(x, Vector256<T>.Zero),
                        Vector256.Floor(x.AsDouble()).As<double, T>(),
                        Vector256.Ceiling(x.AsDouble()).As<double, T>());
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    if (Avx512F.IsSupported) return Avx512F.RoundScale(x.AsSingle(), 0b11).As<float, T>();

                    return Vector512.ConditionalSelect(Vector512.GreaterThanOrEqual(x, Vector512<T>.Zero),
                        Vector512.Floor(x.AsSingle()).As<float, T>(),
                        Vector512.Ceiling(x.AsSingle()).As<float, T>());
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));

                    if (Avx512F.IsSupported) return Avx512F.RoundScale(x.AsDouble(), 0b11).As<double, T>();

                    return Vector512.ConditionalSelect(Vector512.GreaterThanOrEqual(x, Vector512<T>.Zero),
                        Vector512.Floor(x.AsDouble()).As<double, T>(),
                        Vector512.Ceiling(x.AsDouble()).As<double, T>());
                }
            }
        }

        /// <summary>T.DegreesToRadians(x)</summary>
        internal readonly struct DegreesToRadiansOperator<T> : IUnaryOperator<T, T> where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => T.DegreesToRadians(x);
            public static Vector128<T> Invoke(Vector128<T> x) => (x * T.Pi) / T.CreateChecked(180);
            public static Vector256<T> Invoke(Vector256<T> x) => (x * T.Pi) / T.CreateChecked(180);
            public static Vector512<T> Invoke(Vector512<T> x) => (x * T.Pi) / T.CreateChecked(180);
        }

        /// <summary>T.RadiansToDegrees(x)</summary>
        internal readonly struct RadiansToDegreesOperator<T> : IUnaryOperator<T, T> where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => T.RadiansToDegrees(x);
            public static Vector128<T> Invoke(Vector128<T> x) => (x * T.CreateChecked(180)) / T.Pi;
            public static Vector256<T> Invoke(Vector256<T> x) => (x * T.CreateChecked(180)) / T.Pi;
            public static Vector512<T> Invoke(Vector512<T> x) => (x * T.CreateChecked(180)) / T.Pi;
        }

        /// <summary>T.ScaleB(x, n)</summary>
        internal readonly struct ScaleBOperator<T>(int n) : IStatefulUnaryOperator<T> where T : IFloatingPointIeee754<T>
        {
            private readonly int _n = n;
            private readonly T _pow2n = typeof(T) == typeof(float) || typeof(T) == typeof(double) ? T.Pow(T.CreateTruncating(2), T.CreateTruncating(n)) : default!;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public T Invoke(T x) => T.ScaleB(x, _n);
            public Vector128<T> Invoke(Vector128<T> x) => x * Vector128.Create(_pow2n);
            public Vector256<T> Invoke(Vector256<T> x) => x * Vector256.Create(_pow2n);
            public Vector512<T> Invoke(Vector512<T> x) => x * Vector512.Create(_pow2n);
        }

        /// <summary>T.RootN(x, n)</summary>
        internal readonly struct RootNOperator<T>(int n) : IStatefulUnaryOperator<T> where T : IRootFunctions<T>
        {
            private readonly int _n = n;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public T Invoke(T x) => T.RootN(x, _n);

            public Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector128.Create((float)_n)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector128.Create((double)_n)).As<double, T>();
                }
            }

            public Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector256.Create((float)_n)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector256.Create((double)_n)).As<double, T>();
                }
            }

            public Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector512.Create((float)_n)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector512.Create((double)_n)).As<double, T>();
                }
            }
        }

        /// <summary>T.Round(x)</summary>
        internal readonly struct RoundToEvenOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            // This code is based on `nearbyint` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Round(x);

            private const float SingleBoundary = 8388608.0f; // 2^23
            private const double DoubleBoundary = 4503599627370496.0; // 2^52

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                Vector128<T> boundary = Vector128.Create(typeof(T) == typeof(float) ? T.CreateTruncating(SingleBoundary) : T.CreateTruncating(DoubleBoundary));
                Vector128<T> temp = CopySignOperator<T>.Invoke(boundary, x);
                return Vector128.ConditionalSelect(Vector128.GreaterThan(Vector128.Abs(x), boundary), x, CopySignOperator<T>.Invoke((x + temp) - temp, x));
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                Vector256<T> boundary = Vector256.Create(typeof(T) == typeof(float) ? T.CreateTruncating(SingleBoundary) : T.CreateTruncating(DoubleBoundary));
                Vector256<T> temp = CopySignOperator<T>.Invoke(boundary, x);
                return Vector256.ConditionalSelect(Vector256.GreaterThan(Vector256.Abs(x), boundary), x, CopySignOperator<T>.Invoke((x + temp) - temp, x));
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                Vector512<T> boundary = Vector512.Create(typeof(T) == typeof(float) ? T.CreateTruncating(SingleBoundary) : T.CreateTruncating(DoubleBoundary));
                Vector512<T> temp = CopySignOperator<T>.Invoke(boundary, x);
                return Vector512.ConditionalSelect(Vector512.GreaterThan(Vector512.Abs(x), boundary), x, CopySignOperator<T>.Invoke((x + temp) - temp, x));
            }
        }

        /// <summary>T.Round(x, MidpointRounding.AwayFromZero)</summary>
        internal readonly struct RoundAwayFromZeroOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Round(x, MidpointRounding.AwayFromZero);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    if (AdvSimd.IsSupported)
                    {
                        return AdvSimd.RoundAwayFromZero(x.AsSingle()).As<float, T>();
                    }

                    return TruncateOperator<float>.Invoke(x.AsSingle() + CopySignOperator<float>.Invoke(Vector128.Create(0.49999997f), x.AsSingle())).As<float, T>();
                }
                else
                {
                    if (AdvSimd.Arm64.IsSupported)
                    {
                        return AdvSimd.Arm64.RoundAwayFromZero(x.AsDouble()).As<double, T>();
                    }

                    Debug.Assert(typeof(T) == typeof(double));
                    return TruncateOperator<double>.Invoke(x.AsDouble() + CopySignOperator<double>.Invoke(Vector128.Create(0.49999999999999994), x.AsDouble())).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TruncateOperator<float>.Invoke(x.AsSingle() + CopySignOperator<float>.Invoke(Vector256.Create(0.49999997f), x.AsSingle())).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TruncateOperator<double>.Invoke(x.AsDouble() + CopySignOperator<double>.Invoke(Vector256.Create(0.49999999999999994), x.AsDouble())).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TruncateOperator<float>.Invoke(x.AsSingle() + CopySignOperator<float>.Invoke(Vector512.Create(0.49999997f), x.AsSingle())).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TruncateOperator<double>.Invoke(x.AsDouble() + CopySignOperator<double>.Invoke(Vector512.Create(0.49999999999999994), x.AsDouble())).As<double, T>();
                }
            }
        }

        /// <summary>(T.Round(x * power10, digits, mode)) / power10</summary>
        internal readonly struct MultiplyRoundDivideOperator<T, TDelegatedRound> : IStatefulUnaryOperator<T>
            where T : IFloatingPoint<T>
            where TDelegatedRound : IUnaryOperator<T, T>
        {
            private readonly T _factor;

            public MultiplyRoundDivideOperator(T factor)
            {
                Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
                _factor = factor;
            }

            public static bool Vectorizable => true;

            private const float Single_RoundLimit = 1e8f;
            private const double Double_RoundLimit = 1e16d;

            public T Invoke(T x)
            {
                T limit = typeof(T) == typeof(float) ? T.CreateTruncating(Single_RoundLimit) : T.CreateTruncating(Double_RoundLimit);
                return T.Abs(x) < limit ?
                    TDelegatedRound.Invoke(x * _factor) / _factor :
                    x;
            }

            public Vector128<T> Invoke(Vector128<T> x)
            {
                Vector128<T> limit = Vector128.Create(typeof(T) == typeof(float) ? T.CreateTruncating(Single_RoundLimit) : T.CreateTruncating(Double_RoundLimit));
                return Vector128.ConditionalSelect(Vector128.LessThan(Vector128.Abs(x), limit),
                    TDelegatedRound.Invoke(x * _factor) / _factor,
                    x);
            }

            public Vector256<T> Invoke(Vector256<T> x)
            {
                Vector256<T> limit = Vector256.Create(typeof(T) == typeof(float) ? T.CreateTruncating(Single_RoundLimit) : T.CreateTruncating(Double_RoundLimit));
                return Vector256.ConditionalSelect(Vector256.LessThan(Vector256.Abs(x), limit),
                    TDelegatedRound.Invoke(x * _factor) / _factor,
                    x);
            }

            public Vector512<T> Invoke(Vector512<T> x)
            {
                Vector512<T> limit = Vector512.Create(typeof(T) == typeof(float) ? T.CreateTruncating(Single_RoundLimit) : T.CreateTruncating(Double_RoundLimit));
                return Vector512.ConditionalSelect(Vector512.LessThan(Vector512.Abs(x), limit),
                    TDelegatedRound.Invoke(x * _factor) / _factor,
                    x);
            }
        }

        /// <summary>T.Round(x, digits, mode)</summary>
        internal readonly struct RoundFallbackOperator<T>(int digits, MidpointRounding mode) : IStatefulUnaryOperator<T>
            where T : IFloatingPoint<T>
        {
            private readonly int _digits = digits;
            private readonly MidpointRounding _mode = mode;

            public static bool Vectorizable => false;

            public T Invoke(T x) => T.Round(x, _digits, _mode);

            public Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.ILogB(x)</summary>
        internal readonly struct ILogBOperator<T> : IUnaryOperator<T, int> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => false; // TODO: vectorize for float

            public static int Invoke(T x) => T.ILogB(x);
            public static Vector128<int> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<int> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<int> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>double.ILogB(x)</summary>
        internal readonly struct ILogBDoubleOperator : IUnaryTwoToOneOperator<double, int>
        {
            public static bool Vectorizable => false; // TODO: vectorize

            public static int Invoke(double x) => double.ILogB(x);
            public static Vector128<int> Invoke(Vector128<double> lower, Vector128<double> upper) => throw new NotSupportedException();
            public static Vector256<int> Invoke(Vector256<double> lower, Vector256<double> upper) => throw new NotSupportedException();
            public static Vector512<int> Invoke(Vector512<double> lower, Vector512<double> upper) => throw new NotSupportedException();
        }

        /// <summary>T.SinCos(x)</summary>
        internal readonly struct SinCosOperator<T> : IUnaryInputBinaryOutput<T> where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: vectorize

            public static (T, T) Invoke(T x) => T.SinCos(x);
            public static (Vector128<T> First, Vector128<T> Second) Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static (Vector256<T> First, Vector256<T> Second) Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static (Vector512<T> First, Vector512<T> Second) Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>T.SinCosPi(x)</summary>
        internal readonly struct SinCosPiOperator<T> : IUnaryInputBinaryOutput<T> where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: vectorize

            public static (T, T) Invoke(T x) => T.SinCosPi(x);
            public static (Vector128<T> First, Vector128<T> Second) Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static (Vector256<T> First, Vector256<T> Second) Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static (Vector512<T> First, Vector512<T> Second) Invoke(Vector512<T> x) => throw new NotSupportedException();
        }
    }
}
