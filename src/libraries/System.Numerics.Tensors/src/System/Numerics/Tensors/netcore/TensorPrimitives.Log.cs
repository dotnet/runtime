// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise natural (base <c>e</c>) logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its natural logarithm is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, LogOperator<float>>(x, destination))
            {
                return;
            }

            InvokeSpanIntoSpan<T, LogOperator<T>>(x, destination);
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="y"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i], <paramref name="y" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            if (typeof(T) == typeof(Half) && TryBinaryInvokeHalfAsInt16<T, LogBaseOperator<float>>(x, y, destination))
            {
                return;
            }

            InvokeSpanSpanIntoSpan<T, LogBaseOperator<T>>(x, y, destination);
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log(<paramref name="x" />[i], <paramref name="y" />)</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log<T>(ReadOnlySpan<T> x, T y, Span<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            if (typeof(T) == typeof(Half) && TryBinaryInvokeHalfAsInt16<T, LogBaseOperator<float>>(x, y, destination))
            {
                return;
            }

            InvokeSpanScalarIntoSpan<T, LogBaseOperator<T>>(x, y, destination);
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
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Log(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Log(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Log(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Log(x.AsSingle()).As<float, T>();
                }
            }
        }

        /// <summary>T.Log(x, y)</summary>
        private readonly struct LogBaseOperator<T> : IBinaryOperator<T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => false; //LogOperator<T>.Vectorizable; // TODO: https://github.com/dotnet/runtime/issues/100535
            public static T Invoke(T x, T y) => T.Log(x, y);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => LogOperator<T>.Invoke(x) / LogOperator<T>.Invoke(y);
        }
    }
}
