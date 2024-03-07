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
        private readonly struct CbrtOperator<T> : IUnaryOperator<T>
            where T : IRootFunctions<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Cbrt(x);

            public static TVector Invoke<TVector>(TVector x) where TVector : struct, ISimdVector<TVector, T>
            {
                if (typeof(T) == typeof(float))
                {
                    return ExpOperator<float>.Invoke(LogOperator<float>.Invoke(x.AsSingle()) / TVector.Create(3f)).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return ExpOperator<double>.Invoke(LogOperator<double>.Invoke(x.AsDouble()) / TVector.Create(3d)).As<double, T>();
                }
            }
        }
    }
}
