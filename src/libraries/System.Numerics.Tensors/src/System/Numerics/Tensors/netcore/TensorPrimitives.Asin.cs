// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise angle in radians whose sine is the specifed number.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Asin(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Asin<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AsinOperator<T>>(x, destination);

        /// <summary>T.Asin(x)</summary>
        private readonly struct AsinOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
#if NET11_0_OR_GREATER
            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));
#else
            public static bool Vectorizable => false;
#endif

            public static T Invoke(T x) => T.Asin(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Asin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Asin(x.AsSingle()).As<float, T>();
                }
#else
                throw new NotSupportedException();
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Asin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Asin(x.AsSingle()).As<float, T>();
                }
#else
                throw new NotSupportedException();
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Asin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Asin(x.AsSingle()).As<float, T>();
                }
#else
                throw new NotSupportedException();
#endif
            }
        }
    }
}
