// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise cube root of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Cbrt(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Cbrt<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IRootFunctions<T> =>
            InvokeSpanIntoSpan<T, CbrtOperator<T>>(x, destination);

        /// <summary>T.Cbrt(x)</summary>
        private readonly struct CbrtOperator<T> : IUnaryOperator<T, T>
            where T : IRootFunctions<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Cbrt(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector128.Create(3f)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector128.Create(3d)).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector256.Create(3f)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector256.Create(3d)).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector512.Create(3f)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector512.Create(3d)).As<double, T>();
                }
            }
        }
    }
}
