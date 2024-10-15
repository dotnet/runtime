// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise ceiling of numbers in the specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Ceiling(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Ceiling<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T> =>
            InvokeSpanIntoSpan<T, CeilingOperator<T>>(x, destination);

        private readonly struct CeilingOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
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
    }
}
