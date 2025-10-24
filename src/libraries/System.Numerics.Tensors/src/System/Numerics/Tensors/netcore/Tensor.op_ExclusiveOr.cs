// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <summary>Performs exclusive-or between two tensors.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to exclusive-or with <paramref name="y" />.</param>
        /// <param name="y">The tensor to exclusive-or with <paramref name="x" />.</param>
        /// <returns>A new tensor containing the result of <paramref name="x" /> ^ <paramref name="y" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="y" /> are not compatible.</exception>
        public static Tensor<T> Xor<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Xor<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Performs exclusive-or between a tensor and scalar.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to exclusive-or with <paramref name="y" />.</param>
        /// <param name="y">The scalar to exclusive-or with <paramref name="x" />.</param>
        /// <returns>A new tensor containing the result of <paramref name="x" /> ^ <paramref name="y" />.</returns>
        public static Tensor<T> Xor<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Xor<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Performs exclusive-or between two tensors.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to exclusive-or with <paramref name="y" />.</param>
        /// <param name="y">The tensor to exclusive-or with <paramref name="x" />.</param>
        /// <param name="destination">The destination where the result of <paramref name="x" /> ^ <paramref name="y" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" />, <paramref name="y" />, and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Xor<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Xor<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Performs exclusive-or between a tensor and scalar.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to exclusive-or with <paramref name="y" />.</param>
        /// <param name="y">The scalar to exclusive-or with <paramref name="x" />.</param>
        /// <param name="destination">The destination where the result of <paramref name="x" /> ^ <paramref name="y" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Xor<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Xor<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(ReadOnlyTensorSpan<TScalar>)
            where TScalar : IBitwiseOperators<TScalar, TScalar, TScalar>
        {
            /// <summary>Performs exclusive-or between two tensors.</summary>
            /// <param name="left">The tensor to exclusive-or with <paramref name="right" />.</param>
            /// <param name="right">The tensor to exclusive-or with <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> ^ <paramref name="right" />.</returns>
            /// <exception cref="ArgumentException">The shapes of <paramref name="left" /> and <paramref name="right" /> are not compatible.</exception>
            public static Tensor<TScalar> operator ^(in ReadOnlyTensorSpan<TScalar> left, in ReadOnlyTensorSpan<TScalar> right) => Xor(left, right);

            /// <summary>Performs exclusive-or between a tensor and scalar.</summary>
            /// <param name="left">The tensor to exclusive-or with <paramref name="right" />.</param>
            /// <param name="right">The scalar to exclusive-or with <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> ^ <paramref name="right" />.</returns>
            public static Tensor<TScalar> operator ^(in ReadOnlyTensorSpan<TScalar> left, TScalar right) => Xor(left, right);

            /// <summary>Performs exclusive-or between a tensor and scalar.</summary>
            /// <param name="left">The scalar to exclusive-or with <paramref name="right" />.</param>
            /// <param name="right">The tensor to exclusive-or with <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> ^ <paramref name="right" />.</returns>
            public static Tensor<TScalar> operator ^(TScalar left, in ReadOnlyTensorSpan<TScalar> right) => Xor(right, left);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(Tensor<TScalar> tensor)
            where TScalar : IBitwiseOperators<TScalar, TScalar, TScalar>
        {
            /// <inheritdoc cref="op_ExclusiveOr{T}(in ReadOnlyTensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator ^(Tensor<TScalar> left, Tensor<TScalar> right) => Xor<TScalar>(left, right);

            /// <inheritdoc cref="op_ExclusiveOr{T}(in ReadOnlyTensorSpan{T}, T)" />
            public static Tensor<TScalar> operator ^(Tensor<TScalar> left, TScalar right) => Xor(left, right);

            /// <inheritdoc cref="op_ExclusiveOr{T}(T, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator ^(TScalar left, Tensor<TScalar> right) => Xor(right, left);

            /// <inheritdoc cref="op_ExclusiveOrAssignment{T}(ref TensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public void operator ^=(in ReadOnlyTensorSpan<TScalar> other) => Xor(tensor, other, tensor);

            /// <inheritdoc cref="op_ExclusiveOrAssignment{T}(ref TensorSpan{T}, T)" />
            public void operator ^=(TScalar other) => Xor(tensor, other, tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        /// <param name="tensor">The tensor to operate on.</param>
        extension<TScalar>(ref TensorSpan<TScalar> tensor)
            where TScalar : IBitwiseOperators<TScalar, TScalar, TScalar>
        {
            /// <inheritdoc cref="op_ExclusiveOr{T}(in ReadOnlyTensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator ^(in TensorSpan<TScalar> left, in TensorSpan<TScalar> right) => Xor<TScalar>(left, right);

            /// <inheritdoc cref="op_ExclusiveOr{T}(in ReadOnlyTensorSpan{T}, T)" />
            public static Tensor<TScalar> operator ^(in TensorSpan<TScalar> left, TScalar right) => Xor(left, right);

            /// <inheritdoc cref="op_ExclusiveOr{T}(T, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator ^(TScalar left, in TensorSpan<TScalar> right) => Xor(right, left);

            /// <summary>Performs in-place exclusive-or between two tensors.</summary>
            /// <param name="other">The tensor to exclusive-or with the tensor being operated on.</param>
            public void operator ^=(in ReadOnlyTensorSpan<TScalar> other) => Xor(tensor, other, tensor);

            /// <summary>Performs in-place exclusive-or between a tensor and scalar.</summary>
            /// <param name="other">The scalar to exclusive-or with the tensor being operated on.</param>
            public void operator ^=(TScalar other) => Xor(tensor, other, tensor);
        }
    }
}
