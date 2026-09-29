// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <summary>Performs an decrement on a tensor.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to decrement.</param>
        /// <returns>A new tensor containing the result of --<paramref name="x" />.</returns>
        public static Tensor<T> Decrement<T>(in ReadOnlyTensorSpan<T> x)
            where T : IDecrementOperators<T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Decrement<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Performs an decrement on a tensor.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to decrement.</param>
        /// <param name="destination">The destination where the result of --<paramref name="x" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Decrement<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IDecrementOperators<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Decrement<T>, T, T>(x, destination);
            return ref destination;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        /// <param name="tensor">The tensor to operate on.</param>
        extension<TScalar>(Tensor<TScalar> tensor)
            where TScalar : IDecrementOperators<TScalar>
        {
            /// <inheritdoc cref="op_DecrementAssignment{T}(ref TensorSpan{T})" />
            public void operator --() => Decrement<TScalar>(tensor, tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        /// <param name="tensor">The tensor to operate on.</param>
        extension<TScalar>(ref TensorSpan<TScalar> tensor)
            where TScalar : IDecrementOperators<TScalar>
        {
            /// <summary>Performs in-place decrement on a tensor.</summary>
            public void operator --() => Decrement<TScalar>(tensor, tensor);
        }
    }
}
