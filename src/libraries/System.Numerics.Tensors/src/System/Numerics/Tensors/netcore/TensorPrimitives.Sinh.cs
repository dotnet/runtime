// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Sinh(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value is equal to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, or <see cref="IFloatingPointIeee754{TSelf}.NaN"/>,
        /// the corresponding destination location is set to that value.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi / 180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Sinh<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IHyperbolicFunctions<T> =>
            InvokeSpanIntoSpan<T, SinhOperator<T>>(x, destination);

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
    }
}
