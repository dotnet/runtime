// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise sine and cosine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="sinPiDestination">The destination tensor for the element-wise sine result, represented as a span.</param>
        /// <param name="cosPiDestination">The destination tensor for the element-wise cosine result, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="sinPiDestination"/> or <paramref name="cosPiDestination" /> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>(<paramref name="sinPiDestination" />[i], <paramref name="cosPiDestination" />[i]) = <typeparamref name="T"/>.SinCos(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void SinCosPi<T>(ReadOnlySpan<T> x, Span<T> sinPiDestination, Span<T> cosPiDestination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan_TwoOutputs<T, SinCosPiOperator<T>>(x, sinPiDestination, cosPiDestination);

        /// <summary>T.SinCosPi(x)</summary>
        private readonly struct SinCosPiOperator<T> : IUnaryInputBinaryOutput<T> where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static (T, T) Invoke(T x) => T.SinCosPi(x);

            public static (Vector128<T> First, Vector128<T> Second) Invoke(Vector128<T> x)
            {
                Vector128<T> xpi = x * Vector128.Create(T.Pi);

#if !NET9_0_OR_GREATER
                if (typeof(T) == typeof(float))
                {
                    if (Vector128.GreaterThanAny(xpi.AsUInt32() & Vector128.Create(SinOperatorSingle.SignMask), Vector128.Create(SinOperatorSingle.MaxVectorizedValue)) ||
                        Vector128.GreaterThanAny(xpi.AsUInt32() & Vector128.Create(CosOperatorSingle.SignMask), Vector128.Create(CosOperatorSingle.MaxVectorizedValue)))
                    {
                        (Vector128<float> sin, Vector128<float> cos) = Apply2xScalar<SinCosPiOperator<float>>(x.AsSingle());
                        return (sin.As<float, T>(), cos.As<float, T>());
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector128.GreaterThanAny(xpi.AsUInt64() & Vector128.Create(SinOperatorDouble.SignMask), Vector128.Create(SinOperatorDouble.MaxVectorizedValue)) ||
                        Vector128.GreaterThanAny(xpi.AsUInt64() & Vector128.Create(CosOperatorDouble.SignMask), Vector128.Create(CosOperatorDouble.MaxVectorizedValue)))
                    {
                        (Vector128<double> sin, Vector128<double> cos) = Apply2xScalar<SinCosPiOperator<double>>(x.AsDouble());
                        return (sin.As<double, T>(), cos.As<double, T>());
                    }
                }
#endif

                return SinCosOperator<T>.Invoke(xpi);
            }

            public static (Vector256<T> First, Vector256<T> Second) Invoke(Vector256<T> x)
            {
                Vector256<T> xpi = x * Vector256.Create(T.Pi);

#if !NET9_0_OR_GREATER
                if (typeof(T) == typeof(float))
                {
                    if (Vector256.GreaterThanAny(xpi.AsUInt32() & Vector256.Create(SinOperatorSingle.SignMask), Vector256.Create(SinOperatorSingle.MaxVectorizedValue)) ||
                        Vector256.GreaterThanAny(xpi.AsUInt32() & Vector256.Create(CosOperatorSingle.SignMask), Vector256.Create(CosOperatorSingle.MaxVectorizedValue)))
                    {
                        (Vector256<float> sin, Vector256<float> cos) = Apply2xScalar<SinCosPiOperator<float>>(x.AsSingle());
                        return (sin.As<float, T>(), cos.As<float, T>());
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector256.GreaterThanAny(xpi.AsUInt64() & Vector256.Create(SinOperatorDouble.SignMask), Vector256.Create(SinOperatorDouble.MaxVectorizedValue)) ||
                        Vector256.GreaterThanAny(xpi.AsUInt64() & Vector256.Create(CosOperatorDouble.SignMask), Vector256.Create(CosOperatorDouble.MaxVectorizedValue)))
                    {
                        (Vector256<double> sin, Vector256<double> cos) = Apply2xScalar<SinCosPiOperator<double>>(x.AsDouble());
                        return (sin.As<double, T>(), cos.As<double, T>());
                    }
                }
#endif

                return SinCosOperator<T>.Invoke(xpi);
            }

            public static (Vector512<T> First, Vector512<T> Second) Invoke(Vector512<T> x)
            {
                Vector512<T> xpi = x * Vector512.Create(T.Pi);

#if !NET9_0_OR_GREATER
                if (typeof(T) == typeof(float))
                {
                    if (Vector512.GreaterThanAny(xpi.AsUInt32() & Vector512.Create(SinOperatorSingle.SignMask), Vector512.Create(SinOperatorSingle.MaxVectorizedValue)) ||
                        Vector512.GreaterThanAny(xpi.AsUInt32() & Vector512.Create(CosOperatorSingle.SignMask), Vector512.Create(CosOperatorSingle.MaxVectorizedValue)))
                    {
                        (Vector512<float> sin, Vector512<float> cos) = Apply2xScalar<SinCosPiOperator<float>>(x.AsSingle());
                        return (sin.As<float, T>(), cos.As<float, T>());
                    }
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    if (Vector512.GreaterThanAny(xpi.AsUInt64() & Vector512.Create(SinOperatorDouble.SignMask), Vector512.Create(SinOperatorDouble.MaxVectorizedValue)) ||
                        Vector512.GreaterThanAny(xpi.AsUInt64() & Vector512.Create(CosOperatorDouble.SignMask), Vector512.Create(CosOperatorDouble.MaxVectorizedValue)))
                    {
                        (Vector512<double> sin, Vector512<double> cos) = Apply2xScalar<SinCosPiOperator<double>>(x.AsDouble());
                        return (sin.As<double, T>(), cos.As<double, T>());
                    }
                }
#endif

                return SinCosOperator<T>.Invoke(xpi);
            }
        }
    }
}
