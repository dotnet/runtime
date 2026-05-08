// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <summary>Performs a one's complement on a tensor.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to one's complement.</param>
        /// <returns>A new tensor containing the result of ~<paramref name="x" />.</returns>
        public static Tensor<T> OnesComplement<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.OnesComplement<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Performs a one's complement on a tensor.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to one's complement.</param>
        /// <param name="destination">The destination where the result of ~<paramref name="x" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> OnesComplement<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.OnesComplement<T>, T, T>(x, destination);
            return ref destination;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(ReadOnlyTensorSpan<TScalar>)
            where TScalar : IBitwiseOperators<TScalar, TScalar, TScalar>
        {
            /// <summary>Performs a one's complement on a tensor.</summary>
            /// <param name="tensor">The tensor to one's complement.</param>
            /// <returns>A new tensor containing the result of ~<paramref name="tensor" />.</returns>
            public static Tensor<TScalar> operator ~(in ReadOnlyTensorSpan<TScalar> tensor) => OnesComplement(tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(Tensor<TScalar>)
            where TScalar : IBitwiseOperators<TScalar, TScalar, TScalar>
        {
            /// <inheritdoc cref="op_OnesComplement{T}(in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator ~(Tensor<TScalar> tensor) => OnesComplement<TScalar>(tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(TensorSpan<TScalar>)
            where TScalar : IBitwiseOperators<TScalar, TScalar, TScalar>
        {
            /// <inheritdoc cref="op_OnesComplement{T}(in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator ~(in TensorSpan<TScalar> tensor) => OnesComplement<TScalar>(tensor);
        }
    }
}
