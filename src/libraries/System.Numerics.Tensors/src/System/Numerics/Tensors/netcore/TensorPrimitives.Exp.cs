// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals <see cref="IFloatingPointIeee754{TSelf}.NaN"/> or <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value equals <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, the result stored into the corresponding destination location is set to 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T>
        {
            if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, ExpOperator<float>>(x, destination))
            {
                return;
            }

            InvokeSpanIntoSpan<T, ExpOperator<T>>(x, destination);
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
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Exp(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Exp(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Exp(x.AsSingle()).As<float, T>();
                }
            }

            // T.Exp(x * scale). For float, x is widened and the product is computed in double.
            // Rounding the product to float introduces tens of ulps of error in the result and, just below the
            // overflow threshold, can turn a finite result into infinity.
            public static Vector128<T> InvokeScaled(Vector128<T> x, double scale)
            {
                if (typeof(T) == typeof(float))
                {
                    (Vector128<double> lower, Vector128<double> upper) = Vector128.Widen(x.AsSingle());
                    Vector128<double> s = Vector128.Create(scale);
                    return Vector128.Narrow(Vector128.Exp(lower * s), Vector128.Exp(upper * s)).As<float, T>();
                }

                return Invoke(x * Vector128.Create(T.CreateTruncating(scale)));
            }

            public static Vector256<T> InvokeScaled(Vector256<T> x, double scale)
            {
                if (typeof(T) == typeof(float))
                {
                    (Vector256<double> lower, Vector256<double> upper) = Vector256.Widen(x.AsSingle());
                    Vector256<double> s = Vector256.Create(scale);
                    return Vector256.Narrow(Vector256.Exp(lower * s), Vector256.Exp(upper * s)).As<float, T>();
                }

                return Invoke(x * Vector256.Create(T.CreateTruncating(scale)));
            }

            public static Vector512<T> InvokeScaled(Vector512<T> x, double scale)
            {
                if (typeof(T) == typeof(float))
                {
                    (Vector512<double> lower, Vector512<double> upper) = Vector512.Widen(x.AsSingle());
                    Vector512<double> s = Vector512.Create(scale);
                    return Vector512.Narrow(Vector512.Exp(lower * s), Vector512.Exp(upper * s)).As<float, T>();
                }

                return Invoke(x * Vector512.Create(T.CreateTruncating(scale)));
            }
        }
    }
}
