// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(ReadOnlyTensorSpan<TScalar>)
            where TScalar : IUnaryPlusOperators<TScalar, TScalar>
        {
            /// <summary>Performs element-wise unary plus on a tensor.</summary>
            /// <param name="tensor">The tensor to return.</param>
            /// <returns><paramref name="tensor" /></returns>
            public static ReadOnlyTensorSpan<TScalar> operator +(in ReadOnlyTensorSpan<TScalar> tensor) => tensor;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(Tensor<TScalar>)
            where TScalar : IUnaryPlusOperators<TScalar, TScalar>
        {
            /// <inheritdoc cref="op_UnaryPlus{T}(in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator +(Tensor<TScalar> tensor) => tensor;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(TensorSpan<TScalar>)
            where TScalar : IUnaryPlusOperators<TScalar, TScalar>
        {
            /// <inheritdoc cref="op_UnaryPlus{T}(in ReadOnlyTensorSpan{T})" />
            public static TensorSpan<TScalar> operator +(in TensorSpan<TScalar> tensor) => tensor;
        }
    }
}
