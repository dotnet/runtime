// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise n-th root of the values in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="n">The degree of the root to be computed, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.RootN(<paramref name="x" />[i], <paramref name="n"/>)</c>.
        /// </para>
        /// </remarks>
        public static void RootN<T>(ReadOnlySpan<T> x, int n, Span<T> destination)
            where T : IRootFunctions<T> =>
            InvokeSpanIntoSpan(x, new RootNOperator<T>(n), destination);

        /// <summary>T.RootN(x, n)</summary>
        private readonly struct RootNOperator<T>(int n) : IStatefulUnaryOperator<T> where T : IRootFunctions<T>
        {
            private readonly int _n = n;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public T Invoke(T x) => T.RootN(x, _n);

            public Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector128.Create((float)_n)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector128.Create((double)_n)).As<double, T>();
                }
            }

            public Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector256.Create((float)_n)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector256.Create((double)_n)).As<double, T>();
                }
            }

            public Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / Vector512.Create((float)_n)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / Vector512.Create((double)_n)).As<double, T>();
                }
            }
        }
    }
}
