// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise rotation left of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.RotateLeft(<paramref name="x" />[i], <paramref name="rotateAmount"/>)</c>.
        /// </para>
        /// </remarks>
        public static void RotateLeft<T>(ReadOnlySpan<T> x, int rotateAmount, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan(x, new RotateLeftOperator<T>(rotateAmount), destination);

        /// <summary>Computes the element-wise rotation right of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.RotateRight(<paramref name="x" />[i], <paramref name="rotateAmount"/>)</c>.
        /// </para>
        /// </remarks>
        public static void RotateRight<T>(ReadOnlySpan<T> x, int rotateAmount, Span<T> destination)
            where T : IBinaryInteger<T> =>
            InvokeSpanIntoSpan(x, new RotateRightOperator<T>(rotateAmount), destination);

        /// <summary>T.RotateLeft(amount)</summary>
        private readonly unsafe struct RotateLeftOperator<T>(int amount) : IStatefulUnaryOperator<T> where T : IBinaryInteger<T>
        {
            private readonly int _amount = amount;

            public static bool Vectorizable => true;

            public T Invoke(T x) => T.RotateLeft(x, _amount);
            public Vector128<T> Invoke(Vector128<T> x) => (x << _amount) | (x >>> ((sizeof(T) * 8) - _amount));
            public Vector256<T> Invoke(Vector256<T> x) => (x << _amount) | (x >>> ((sizeof(T) * 8) - _amount));
            public Vector512<T> Invoke(Vector512<T> x) => (x << _amount) | (x >>> ((sizeof(T) * 8) - _amount));
        }

        /// <summary>T.RotateRight(amount)</summary>
        private readonly unsafe struct RotateRightOperator<T>(int amount) : IStatefulUnaryOperator<T> where T : IBinaryInteger<T>
        {
            private readonly int _amount = amount;

            public static bool Vectorizable => true;

            public T Invoke(T x) => T.RotateRight(x, _amount);
            public Vector128<T> Invoke(Vector128<T> x) => (x >>> _amount) | (x << ((sizeof(T) * 8) - _amount));
            public Vector256<T> Invoke(Vector256<T> x) => (x >>> _amount) | (x << ((sizeof(T) * 8) - _amount));
            public Vector512<T> Invoke(Vector512<T> x) => (x >>> _amount) | (x << ((sizeof(T) * 8) - _amount));
        }
    }
}
