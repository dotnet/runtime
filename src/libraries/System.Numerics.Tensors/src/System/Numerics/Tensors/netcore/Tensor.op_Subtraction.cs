// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <summary>Performs element-wise subtraction between two tensors.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor from which to subtract <paramref name="y" />.</param>
        /// <param name="y">The tensor to subtract from <paramref name="x" />.</param>
        /// <returns>A new tensor containing the result of <paramref name="x" /> - <paramref name="y" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="y" /> are not compatible.</exception>
        public static Tensor<T> Subtract<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : ISubtractionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Performs element-wise subtraction between a tensor and scalar.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor from which to subtract <paramref name="y" />.</param>
        /// <param name="y">The scalar to subtract from <paramref name="x" />.</param>
        /// <returns>A new tensor containing the result of <paramref name="x" /> - <paramref name="y" />.</returns>
        public static Tensor<T> Subtract<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Performs element-wise subtraction between a tensor and scalar.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The scalar from which to subtract <paramref name="y" />.</param>
        /// <param name="y">The tensor to subtract from <paramref name="x" />.</param>
        /// <returns>A new tensor containing the result of <paramref name="x" /> - <paramref name="y" />.</returns>
        public static Tensor<T> Subtract<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(y.Lengths);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Performs element-wise subtraction between two tensors.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor from which to subtract <paramref name="y" />.</param>
        /// <param name="y">The tensor to subtract from <paramref name="x" />.</param>
        /// <param name="destination">The destination where the result of <paramref name="x" /> - <paramref name="y" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" />, <paramref name="y" />, and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Subtract<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Performs element-wise subtraction between a tensor and scalar.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor from which to subtract <paramref name="y" />.</param>
        /// <param name="y">The scalar to subtract from <paramref name="x" />.</param>
        /// <param name="destination">The destination where the result of <paramref name="x" /> - <paramref name="y" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Subtract<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Performs element-wise subtraction between a tensor and scalar.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The scalar from which to subtract <paramref name="y" />.</param>
        /// <param name="y">The tensor to subtract from <paramref name="x" />.</param>
        /// <param name="destination">The destination where the result of <paramref name="x" /> - <paramref name="y" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="y" /> and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Subtract<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(ReadOnlyTensorSpan<TScalar>)
            where TScalar : ISubtractionOperators<TScalar, TScalar, TScalar>
        {
            /// <summary>Performs element-wise subtraction between two tensors.</summary>
            /// <param name="left">The tensor from which to subtract <paramref name="right" />.</param>
            /// <param name="right">The tensor to subtract from <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> - <paramref name="right" />.</returns>
            /// <exception cref="ArgumentException">The shapes of <paramref name="left" /> and <paramref name="right" /> are not compatible.</exception>
            public static Tensor<TScalar> operator -(in ReadOnlyTensorSpan<TScalar> left, in ReadOnlyTensorSpan<TScalar> right) => Subtract(left, right);

            /// <summary>Performs element-wise subtraction between a tensor and scalar.</summary>
            /// <param name="left">The tensor from which to subtract <paramref name="right" />.</param>
            /// <param name="right">The scalar to subtract from <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> - <paramref name="right" />.</returns>
            public static Tensor<TScalar> operator -(in ReadOnlyTensorSpan<TScalar> left, TScalar right) => Subtract(left, right);

            /// <summary>Performs element-wise subtraction between a tensor and scalar.</summary>
            /// <param name="left">The scalar from which to subtract <paramref name="right" />.</param>
            /// <param name="right">The tensor to subtract from <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> - <paramref name="right" />.</returns>
            public static Tensor<TScalar> operator -(TScalar left, in ReadOnlyTensorSpan<TScalar> right) => Subtract(left, right);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(Tensor<TScalar> tensor)
            where TScalar : ISubtractionOperators<TScalar, TScalar, TScalar>
        {
            /// <inheritdoc cref="op_Subtraction{T}(in ReadOnlyTensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator -(Tensor<TScalar> left, Tensor<TScalar> right) => Subtract<TScalar>(left, right);

            /// <inheritdoc cref="op_Subtraction{T}(in ReadOnlyTensorSpan{T}, T)" />
            public static Tensor<TScalar> operator -(Tensor<TScalar> left, TScalar right) => Subtract(left, right);

            /// <inheritdoc cref="op_Subtraction{T}(T, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator -(TScalar left, Tensor<TScalar> right) => Subtract(left, right);

            /// <inheritdoc cref="op_SubtractionAssignment{T}(ref TensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public void operator -=(in ReadOnlyTensorSpan<TScalar> other) => Subtract(tensor, other, tensor);

            /// <inheritdoc cref="op_SubtractionAssignment{T}(ref TensorSpan{T}, T)" />
            public void operator -=(TScalar other) => Subtract(tensor, other, tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        /// <param name="tensor">The tensor to operate on.</param>
        extension<TScalar>(ref TensorSpan<TScalar> tensor)
            where TScalar : ISubtractionOperators<TScalar, TScalar, TScalar>
        {
            /// <inheritdoc cref="op_Subtraction{T}(in ReadOnlyTensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator -(in TensorSpan<TScalar> left, in TensorSpan<TScalar> right) => Subtract<TScalar>(left, right);

            /// <inheritdoc cref="op_Subtraction{T}(in ReadOnlyTensorSpan{T}, T)" />
            public static Tensor<TScalar> operator -(in TensorSpan<TScalar> left, TScalar right) => Subtract(left, right);

            /// <inheritdoc cref="op_Subtraction{T}(T, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator -(TScalar left, in TensorSpan<TScalar> right) => Subtract(left, right);

            /// <summary>Performs in-place element-wise subtraction between two tensors.</summary>
            /// <param name="other">The tensor to subtract from the tensor being operated on.</param>
            public void operator -=(in ReadOnlyTensorSpan<TScalar> other) => Subtract(tensor, other, tensor);

            /// <summary>Performs in-place element-wise subtraction between a tensor and scalar.</summary>
            /// <param name="other">The scalar to subtract from the tensor being operated on.</param>
            public void operator -=(TScalar other) => Subtract(tensor, other, tensor);
        }
    }
}
