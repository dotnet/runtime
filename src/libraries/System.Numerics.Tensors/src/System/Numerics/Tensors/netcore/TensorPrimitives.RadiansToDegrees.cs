// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.RadiansToDegrees(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void RadiansToDegrees<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, RadiansToDegreesOperator<T>>(x, destination);

        /// <summary>T.RadiansToDegrees(x)</summary>
        private readonly struct RadiansToDegreesOperator<T> : IUnaryOperator<T, T> where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x) => T.RadiansToDegrees(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector128.RadiansToDegrees(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.RadiansToDegrees(x.AsSingle()).As<float, T>();
                }
#else
                return (x * T.CreateChecked(180)) / T.Pi;
#endif
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector256.RadiansToDegrees(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.RadiansToDegrees(x.AsSingle()).As<float, T>();
                }
#else
                return (x * T.CreateChecked(180)) / T.Pi;
#endif
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
#if NET9_0_OR_GREATER
                if (typeof(T) == typeof(double))
                {
                    return Vector512.RadiansToDegrees(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.RadiansToDegrees(x.AsSingle()).As<float, T>();
                }
#else
                return (x * T.CreateChecked(180)) / T.Pi;
#endif
            }
        }
    }
}
