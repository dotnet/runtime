// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <summary>Performs an element-wise arithmetic right shift on a tensor.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to arithmetic right shift.</param>
        /// <param name="shiftAmount">The amount to shift each element in <paramref name="x" />.</param>
        /// <returns>A new tensor containing the result of <paramref name="x" /> &gt;&gt; <paramref name="shiftAmount" />.</returns>
        public static Tensor<T> ShiftRightArithmetic<T>(in ReadOnlyTensorSpan<T> x, int shiftAmount)
            where T : IShiftOperators<T, int, T>
        {
            Tensor<T> destination = CreateFromShapeUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.ShiftRightArithmetic<T>, T, int, T>(x, shiftAmount, destination);
            return destination;
        }

        /// <summary>Performs an element-wise arithmetic right shift on a tensor.</summary>
        /// <typeparam name="T">The type of the elements in the tensor.</typeparam>
        /// <param name="x">The tensor to arithmetic right shift.</param>
        /// <param name="shiftAmount">The amount to shift each element in <paramref name="x" />.</param>
        /// <param name="destination">The destination where the result of <paramref name="x" /> &gt;&gt; <paramref name="shiftAmount" /> is written.</param>
        /// <returns>A reference to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">The shapes of <paramref name="x" /> and <paramref name="destination" /> are not compatible.</exception>
        public static ref readonly TensorSpan<T> ShiftRightArithmetic<T>(scoped in ReadOnlyTensorSpan<T> x, int shiftAmount, in TensorSpan<T> destination)
            where T : IShiftOperators<T, int, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.ShiftRightArithmetic<T>, T, int, T>(x, shiftAmount, destination);
            return ref destination;
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(ReadOnlyTensorSpan<TScalar>)
            where TScalar : IShiftOperators<TScalar, int, TScalar>
        {
            /// <summary>Performs an element-wise arithmetic right shift on a tensor.</summary>
            /// <param name="tensor">The tensor to arithmetic right shift.</param>
            /// <param name="shiftAmount">The amount to shift each element in <paramref name="tensor" />.</param>
            /// <returns>A new tensor containing the result of <paramref name="tensor" /> &gt;&gt; <paramref name="shiftAmount" />.</returns>
            public static Tensor<TScalar> operator >>(in ReadOnlyTensorSpan<TScalar> tensor, int shiftAmount) => ShiftRightArithmetic(tensor, shiftAmount);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(Tensor<TScalar>)
            where TScalar : IShiftOperators<TScalar, int, TScalar>
        {
            /// <inheritdoc cref="op_LeftShift{T}(in ReadOnlyTensorSpan{T}, int)" />
            public static Tensor<TScalar> operator >>(Tensor<TScalar> tensor, int shiftAmount) => ShiftRightArithmetic<TScalar>(tensor, shiftAmount);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        /// <param name="tensor">The tensor to operate on.</param>
        extension<TScalar>(Tensor<TScalar> tensor)
            where TScalar : IShiftOperators<TScalar, int, TScalar>
        {
            /// <inheritdoc cref="op_LeftShiftAssignment{T}(ref TensorSpan{T}, int)" />
            public void operator >>=(int shiftAmount) => ShiftRightArithmetic<TScalar>(tensor, shiftAmount, tensor);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        extension<TScalar>(TensorSpan<TScalar>)
            where TScalar : IShiftOperators<TScalar, int, TScalar>
        {
            /// <inheritdoc cref="op_LeftShift{T}(in ReadOnlyTensorSpan{T}, int)" />
            public static Tensor<TScalar> operator >>(in TensorSpan<TScalar> tensor, int shiftAmount) => ShiftRightArithmetic<TScalar>(tensor, shiftAmount);
        }

        /// <typeparam name="TScalar">The type of the elements in the tensor.</typeparam>
        /// <param name="tensor">The tensor to operate on.</param>
        extension<TScalar>(ref TensorSpan<TScalar> tensor)
            where TScalar : IShiftOperators<TScalar, int, TScalar>
        {
            /// <summary>Performs in-place element-wise arithmetic right shift on a tensor.</summary>
            /// <param name="shiftAmount">The amount to shift each element in the tensor.</param>
            public void operator >>=(int shiftAmount) => ShiftRightArithmetic<TScalar>(tensor, shiftAmount, tensor);
        }
    }
}
