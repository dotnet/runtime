// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <summary>Performs element-wise addition between two tensors.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to add with <paramref name="y" />.</param>
        /// <param name="y">The tensor to add with <paramref name="x" />.</param>
        /// <returns>A new tensor containing the result of <paramref name="x" /> + <paramref name="y" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="y" /> are not compatible.</exception>
        public static Tensor<T> Add<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Add<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Performs element-wise addition between a tensor and scalar.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to add with <paramref name="y" />.</param>
        /// <param name="y">The scalar to add with <paramref name="x" />.</param>
        /// <returns>A new tensor containing the result of <paramref name="x" /> + <paramref name="y" />.</returns>
        public static Tensor<T> Add<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Add<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Performs element-wise addition between two tensors.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to add with <paramref name="y" />.</param>
        /// <param name="y">The tensor to add with <paramref name="x" />.</param>
        /// <param name="destination">The destination where the result of <paramref name="x" /> + <paramref name="y" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" />, <paramref name="y" />, and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Add<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Add<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Performs element-wise addition between a tensor and scalar.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to add with <paramref name="y" />.</param>
        /// <param name="y">The scalar to add with <paramref name="x" />.</param>
        /// <param name="destination">The destination where the result of <paramref name="x" /> + <paramref name="y" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> Add<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Add<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(ReadOnlyTensorSpan<TScalar>)
            where TScalar : IAdditionOperators<TScalar, TScalar, TScalar>, IAdditiveIdentity<TScalar, TScalar>
        {
            /// <summary>Performs element-wise addition between two tensors.</summary>
            /// <param name="left">The tensor to add with <paramref name="right" />.</param>
            /// <param name="right">The tensor to add with <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> + <paramref name="right" />.</returns>
            /// <exception cref="ArgumentException">The shapes of <paramref name="left" /> and <paramref name="right" /> are not compatible.</exception>
            public static Tensor<TScalar> operator +(in ReadOnlyTensorSpan<TScalar> left, in ReadOnlyTensorSpan<TScalar> right) => Add(left, right);

            /// <summary>Performs element-wise addition between a tensor and scalar.</summary>
            /// <param name="left">The tensor to add with <paramref name="right" />.</param>
            /// <param name="right">The scalar to add with <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> + <paramref name="right" />.</returns>
            public static Tensor<TScalar> operator +(in ReadOnlyTensorSpan<TScalar> left, TScalar right) => Add(left, right);

            /// <summary>Performs element-wise addition between a tensor and scalar.</summary>
            /// <param name="left">The scalar to add with <paramref name="right" />.</param>
            /// <param name="right">The tensor to add with <paramref name="left" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="left" /> + <paramref name="right" />.</returns>
            public static Tensor<TScalar> operator +(TScalar left, in ReadOnlyTensorSpan<TScalar> right) => Add(right, left);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(Tensor<TScalar> tensor)
            where TScalar : IAdditionOperators<TScalar, TScalar, TScalar>, IAdditiveIdentity<TScalar, TScalar>
        {
            /// <inheritdoc cref="op_Addition{T}(in ReadOnlyTensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator +(Tensor<TScalar> left, Tensor<TScalar> right) => Add<TScalar>(left, right);

            /// <inheritdoc cref="op_Addition{T}(in ReadOnlyTensorSpan{T}, T)" />
            public static Tensor<TScalar> operator +(Tensor<TScalar> left, TScalar right) => Add(left, right);

            /// <inheritdoc cref="op_Addition{T}(T, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator +(TScalar left, Tensor<TScalar> right) => Add(right, left);

            /// <inheritdoc cref="op_AdditionAssignment{T}(ref TensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public void operator +=(in ReadOnlyTensorSpan<TScalar> other) => Add(tensor, other, tensor);

            /// <inheritdoc cref="op_AdditionAssignment{T}(ref TensorSpan{T}, T)" />
            public void operator +=(TScalar other) => Add(tensor, other, tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        /// <param name="tensor">The tensor to operate on.</param>
        extension<TScalar>(ref TensorSpan<TScalar> tensor)
            where TScalar : IAdditionOperators<TScalar, TScalar, TScalar>, IAdditiveIdentity<TScalar, TScalar>
        {
            /// <inheritdoc cref="op_Addition{T}(in ReadOnlyTensorSpan{T}, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator +(in TensorSpan<TScalar> left, in TensorSpan<TScalar> right) => Add<TScalar>(left, right);

            /// <inheritdoc cref="op_Addition{T}(in ReadOnlyTensorSpan{T}, T)" />
            public static Tensor<TScalar> operator +(in TensorSpan<TScalar> left, TScalar right) => Add(left, right);

            /// <inheritdoc cref="op_Addition{T}(T, in ReadOnlyTensorSpan{T})" />
            public static Tensor<TScalar> operator +(TScalar left, in TensorSpan<TScalar> right) => Add(right, left);

            /// <summary>Performs in-place element-wise addition between two tensors.</summary>
            /// <param name="other">The tensor to add to the tensor being operated on.</param>
            public void operator +=(in ReadOnlyTensorSpan<TScalar> other) => Add(tensor, other, tensor);

            /// <summary>Performs in-place element-wise addition between a tensor and scalar.</summary>
            /// <param name="other">The scalar to add to the tensor being operated on.</param>
            public void operator +=(TScalar other) => Add(tensor, other, tensor);
        }
    }
}
