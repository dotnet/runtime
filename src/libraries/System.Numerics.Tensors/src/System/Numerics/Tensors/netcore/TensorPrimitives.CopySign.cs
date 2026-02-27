// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of copying the sign from one number to another number in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="sign">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="sign" />.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentException"><paramref name="sign"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.CopySign(<paramref name="x" />[i], <paramref name="sign" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void CopySign<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> sign, Span<T> destination)
            where T : INumber<T>
        {
            if (typeof(T) == typeof(Half) && TryBinaryInvokeHalfAsInt16<T, CopySignOperator<float>>(x, sign, destination))
            {
                return;
            }

            InvokeSpanSpanIntoSpan<T, CopySignOperator<T>>(x, sign, destination);
        }

        /// <summary>Computes the element-wise result of copying the sign from one number to another number in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="sign">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.CopySign(<paramref name="x" />[i], <paramref name="sign" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void CopySign<T>(ReadOnlySpan<T> x, T sign, Span<T> destination)
            where T : INumber<T>
        {
            if (typeof(T) == typeof(Half) && TryBinaryInvokeHalfAsInt16<T, CopySignOperator<float>>(x, sign, destination))
            {
                return;
            }

            InvokeSpanScalarIntoSpan<T, CopySignOperator<T>>(x, sign, destination);
        }

        private readonly struct CopySignOperator<T> : IBinaryOperator<T> where T : INumber<T>
        {
            public static bool Vectorizable => true;

            public static T Invoke(T x, T y) => T.CopySign(x, y);

            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y)
            {
                return Vector128.CopySign(x, y);
            }

            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y)
            {
                return Vector256.CopySign(x, y);
            }

            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y)
            {
                return Vector512.CopySign(x, y);
            }
        }
    }
}
