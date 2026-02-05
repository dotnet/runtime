// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <summary>Performs element-wise unary negation on a tensor.</summary>
        /// <param name="x">The tensor to negate.</param>
        /// <returns>A new tensor containing the result of -<paramref name="x" />.</returns>
        public static Tensor<T> Negate<T>(in ReadOnlyTensorSpan<T> x)
            where T : IUnaryNegationOperators<T, T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Negate<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Performs element-wise unary negation on a tensor.</summary>
        /// <param name="x">The tensor to negate.</param>
        /// <param name="destination">The destination where the result of -<paramref name="x" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Negate<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IUnaryNegationOperators<T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Negate<T>, T, T>(x, destination);
            return ref destination;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(ReadOnlyTensorSpan<TScalar>)
            where TScalar : IUnaryNegationOperators<TScalar, TScalar>
        {
            /// <summary>Performs element-wise unary negation on a tensor.</summary>
            /// <param name="tensor">The tensor to negate.</param>
            /// <returns>A new tensor containing the result of -<paramref name="tensor" />.</returns>
            public static Tensor<TScalar> operator -(in ReadOnlyTensorSpan<TScalar> tensor) => Negate(tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(Tensor<TScalar>)
            where TScalar : IUnaryNegationOperators<TScalar, TScalar>
        {
            /// <inheritdoc cref="op_UnaryNegation{T}(in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator -(Tensor<TScalar> tensor) => Negate<TScalar>(tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(TensorSpan<TScalar>)
            where TScalar : IUnaryNegationOperators<TScalar, TScalar>
        {
            /// <inheritdoc cref="op_UnaryNegation{T}(in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator -(in TensorSpan<TScalar> tensor) => Negate<TScalar>(tensor);
        }
    }
}
