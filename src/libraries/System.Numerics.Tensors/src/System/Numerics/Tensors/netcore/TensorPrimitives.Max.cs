// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <returns>The maximum element in <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be greater than zero.</exception>
        /// <remarks>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If any value equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>
        /// is present, the first is returned. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static T Max<T>(ReadOnlySpan<T> x)
            where T : INumber<T> =>
            MinMaxCore<T, MaxOperator<T>>(x);

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Max<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanSpanIntoSpan<T, MaxPropagateNaNOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Max(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// <para>
        /// The determination of the maximum element matches the IEEE 754:2019 `maximum` function. If either value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// that value is stored as the result. Positive 0 is considered greater than negative 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Max<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : INumber<T> =>
            InvokeSpanScalarIntoSpan<T, MaxPropagateNaNOperator<T>>(x, y, destination);

        /// <summary>T.Max(x, y) (but NaNs may not be propagated)</summary>
        internal readonly struct MaxOperator<T> : IAggregationOperator<T> where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y)
            {
                if (typeof(T) == typeof(Half) || typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return x == y ?
                        (IsNegative(x) ? y : x) :
                        (y > x ? y : x);
                }

                return T.Max(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TVector Invoke<TVector>(TVector x, TVector y) where TVector : struct, ISimdVector<TVector, T>
            {
                if (sizeof(TVector) == sizeof(Vector128<T>))
                {
                    if (AdvSimd.IsSupported)
                    {
                        if (typeof(T) == typeof(byte)) return (TVector)(object)AdvSimd.Max((Vector128<byte>)(object)x, (Vector128<byte>)(object)y);
                        if (typeof(T) == typeof(sbyte)) return (TVector)(object)AdvSimd.Max((Vector128<sbyte>)(object)x, (Vector128<sbyte>)(object)y);
                        if (typeof(T) == typeof(ushort)) return (TVector)(object)AdvSimd.Max((Vector128<ushort>)(object)x, (Vector128<ushort>)(object)y);
                        if (typeof(T) == typeof(short)) return (TVector)(object)AdvSimd.Max((Vector128<short>)(object)x, (Vector128<short>)(object)y);
                        if (typeof(T) == typeof(uint)) return (TVector)(object)AdvSimd.Max((Vector128<uint>)(object)x, (Vector128<uint>)(object)y);
                        if (typeof(T) == typeof(int)) return (TVector)(object)AdvSimd.Max((Vector128<int>)(object)x, (Vector128<int>)(object)y);
                        if (typeof(T) == typeof(float)) return (TVector)(object)AdvSimd.Max((Vector128<float>)(object)x, (Vector128<float>)(object)y);
                    }

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        if (typeof(T) == typeof(double)) return (TVector)(object)AdvSimd.Arm64.Max((Vector128<double>)(object)x, (Vector128<double>)(object)y);
                    }
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        Vector128.ConditionalSelect(TVector.Equals(x, y),
                            TVector.ConditionalSelect(IsNegative(x), y, x),
                            TVector.Max(x, y));
                }

                return Vector128.Max(x, y);
            }

            public static T Invoke<TVector>(TVector x) where TVector : struct, ISimdVector<TVector, T> => HorizontalAggregate<T, TVector, MaxOperator<T>>(x);
        }

        /// <summary>Max(x, y)</summary>
        internal readonly struct MaxPropagateNaNOperator<T> : IBinaryOperator<T>
             where T : INumber<T>
        {
            public static bool Vectorizable => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Invoke(T x, T y) => T.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TVector Invoke<TVector>(TVector x, TVector y) where TVector : struct, ISimdVector<TVector, T>
            {
                if (sizeof(TVector) == sizeof(Vector128<T>))
                {
                    if (AdvSimd.IsSupported)
                    {
                        if (typeof(T) == typeof(byte)) return (TVector)(object)AdvSimd.Max((Vector128<byte>)(object)x, (Vector128<byte>)(object)y);
                        if (typeof(T) == typeof(sbyte)) return (TVector)(object)AdvSimd.Max((Vector128<sbyte>)(object)x, (Vector128<sbyte>)(object)y);
                        if (typeof(T) == typeof(ushort)) return (TVector)(object)AdvSimd.Max((Vector128<ushort>)(object)x, (Vector128<ushort>)(object)y);
                        if (typeof(T) == typeof(short)) return (TVector)(object)AdvSimd.Max((Vector128<short>)(object)x, (Vector128<short>)(object)y);
                        if (typeof(T) == typeof(uint)) return (TVector)(object)AdvSimd.Max((Vector128<uint>)(object)x, (Vector128<uint>)(object)y);
                        if (typeof(T) == typeof(int)) return (TVector)(object)AdvSimd.Max((Vector128<int>)(object)x, (Vector128<int>)(object)y);
                        if (typeof(T) == typeof(float)) return (TVector)(object)AdvSimd.Max((Vector128<float>)(object)x, (Vector128<float>)(object)y);
                    }

                    if (AdvSimd.Arm64.IsSupported)
                    {
                        if (typeof(T) == typeof(double)) return (TVector)(object)AdvSimd.Arm64.Max((Vector128<double>)(object)x, (Vector128<double>)(object)y);
                    }
                }

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return
                        TVector.ConditionalSelect(TVector.Equals(x, x),
                            TVector.ConditionalSelect(TVector.Equals(y, y),
                                TVector.ConditionalSelect(TVector.Equals(x, y),
                                    TVector.ConditionalSelect(IsNegative(x), y, x),
                                    TVector.Max(x, y)),
                                y),
                            x);
                }

                return TVector.Max(x, y);
            }
        }

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TVector IsNegative<T, TVector>(TVector vector) where TVector : struct, ISimdVector<TVector, T>
        {
            if (typeof(T) == typeof(float))
            {
                if (sizeof(TVector) == sizeof(Vector128<T>)) return (TVector)(object)Vector128.LessThan(((Vector128<T>)(object)vector).AsInt32(), Vector128<int>.Zero).As<int, T>();
                if (sizeof(TVector) == sizeof(Vector256<T>)) return (TVector)(object)Vector256.LessThan(((Vector256<T>)(object)vector).AsInt32(), Vector256<int>.Zero).As<int, T>();
                if (sizeof(TVector) == sizeof(Vector512<T>)) return (TVector)(object)Vector512.LessThan(((Vector512<T>)(object)vector).AsInt32(), Vector512<int>.Zero).As<int, T>();
                Debug.Fail("Vector size unsupported.");
            }

            if (typeof(T) == typeof(double))
            {
                if (sizeof(TVector) == sizeof(Vector128<T>)) return (TVector)(object)Vector128.LessThan(((Vector128<T>)(object)vector).AsInt64(), Vector128<long>.Zero).As<long, T>();
                if (sizeof(TVector) == sizeof(Vector256<T>)) return (TVector)(object)Vector256.LessThan(((Vector256<T>)(object)vector).AsInt64(), Vector256<long>.Zero).As<long, T>();
                if (sizeof(TVector) == sizeof(Vector512<T>)) return (TVector)(object)Vector512.LessThan(((Vector512<T>)(object)vector).AsInt64(), Vector512<long>.Zero).As<long, T>();
                Debug.Fail("Vector size unsupported.");
            }

            return TVector.LessThan(vector, TVector.Zero);
        }

        /// <remarks>
        /// This is the same as <see cref="Aggregate{T, TTransformOperator, TAggregationOperator}(ReadOnlySpan{T})"/>
        /// with an identity transform, except it early exits on NaN.
        /// </remarks>
        private static T MinMaxCore<T, TMinMaxOperator>(ReadOnlySpan<T> x)
            where T : INumberBase<T>
            where TMinMaxOperator : struct, IAggregationOperator<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector512<T> result = Vector512.LoadUnsafe(ref xRef, 0);
                Vector512<T> current;

                Vector512<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector512.Equals(result, result);
                    if (nanMask != Vector512<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector512<T>.Count;
                int i = Vector512<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector512.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector512.Equals(current, current);
                        if (nanMask != Vector512<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector512<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count));

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector512.Equals(current, current);
                        if (nanMask != Vector512<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector256<T> result = Vector256.LoadUnsafe(ref xRef, 0);
                Vector256<T> current;

                Vector256<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector256.Equals(result, result);
                    if (nanMask != Vector256<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector256<T>.Count;
                int i = Vector256<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector256.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector256.Equals(current, current);
                        if (nanMask != Vector256<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector256<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count));


                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector256.Equals(current, current);
                        if (nanMask != Vector256<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                ref T xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector128<T> result = Vector128.LoadUnsafe(ref xRef, 0);
                Vector128<T> current;

                Vector128<T> nanMask;
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    // Check for NaNs
                    nanMask = ~Vector128.Equals(result, result);
                    if (nanMask != Vector128<T>.Zero)
                    {
                        return result.GetElement(IndexOfFirstMatch(nanMask));
                    }
                }

                int oneVectorFromEnd = x.Length - Vector128<T>.Count;
                int i = Vector128<T>.Count;

                // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                while (i <= oneVectorFromEnd)
                {
                    // Load the next vector, and early exit on NaN.
                    current = Vector128.LoadUnsafe(ref xRef, (uint)i);

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector128.Equals(current, current);
                        if (nanMask != Vector128<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                    i += Vector128<T>.Count;
                }

                // If any elements remain, handle them in one final vector.
                if (i != x.Length)
                {
                    current = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count));

                    if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                    {
                        // Check for NaNs
                        nanMask = ~Vector128.Equals(current, current);
                        if (nanMask != Vector128<T>.Zero)
                        {
                            return current.GetElement(IndexOfFirstMatch(nanMask));
                        }
                    }

                    result = TMinMaxOperator.Invoke(result, current);
                }

                // Aggregate the lanes in the vector to create the final scalar result.
                return TMinMaxOperator.Invoke(result);
            }

            // Scalar path used when either vectorization is not supported or the input is too small to vectorize.
            T curResult = x[0];
            if (T.IsNaN(curResult))
            {
                return curResult;
            }

            for (int i = 1; i < x.Length; i++)
            {
                T current = x[i];
                if (T.IsNaN(current))
                {
                    return current;
                }

                curResult = TMinMaxOperator.Invoke(curResult, current);
            }

            return curResult;
        }
    }
}
