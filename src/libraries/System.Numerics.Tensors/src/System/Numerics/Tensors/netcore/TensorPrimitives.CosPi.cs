// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.CosPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void CosPi<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, CosPiOperator<T>>(x, destination);

        /// <summary>T.CosPi(x)</summary>
        private readonly struct CosPiOperator<T> : IUnaryOperator<T, T>
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
    }
}
