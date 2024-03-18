// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Tanh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, the corresponding destination location is set to -1.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the corresponding destination location is set to 1.
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the corresponding destination location is set to NaN.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi / 180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Tanh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, TanhOperator<T>>(x, destination);

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
    }
}
