// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Pow(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Pow<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IPowerFunctions<T> =>
            InvokeSpanSpanIntoSpan<T, PowOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Pow(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// </remarks>
        public static void Pow<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : IPowerFunctions<T> =>
            InvokeSpanScalarIntoSpan<T, PowOperator<T>>(x, y, destination);

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a scalar.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Pow(<paramref name="x" />, <paramref name="y" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Pow<T>(T x, ReadOnlySpan<T> y, Span<T> destination)
            where T : IPowerFunctions<T> =>
            InvokeScalarSpanIntoSpan<T, PowOperator<T>>(x, y, destination);

        /// <summary>T.Pow(x, y)</summary>
        private readonly struct PowOperator<T> : IBinaryOperator<T>
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
    }
}
