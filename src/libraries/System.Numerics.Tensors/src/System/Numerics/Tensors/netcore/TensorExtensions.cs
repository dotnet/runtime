// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Buffers;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography;
using System.Runtime.Serialization;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        #region AsReadOnlySpan
        /// <summary>
        /// Extension method to more easily create a TensorSpan from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array</typeparam>
        /// <param name="array">The <see cref="Array"/> with the data</param>
        /// <param name="lengths">The shape for the <see cref="TensorSpan{T}"/></param>
        /// <returns></returns>
        public static ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan<T>(this T[]? array, params scoped ReadOnlySpan<nint> lengths) => new(array, 0, lengths, default);
        #endregion

        #region AsTensorSpan
        /// <summary>
        /// Extension method to more easily create a TensorSpan from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array</typeparam>
        /// <param name="array">The <see cref="Array"/> with the data</param>
        /// <param name="lengths">The shape for the <see cref="TensorSpan{T}"/></param>
        /// <returns></returns>
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array, params scoped ReadOnlySpan<nint> lengths) => new(array, 0, lengths, default);
        #endregion

        #region Average
        /// <summary>
        /// Returns the average of the elements in the <paramref name="x"/> tensor.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the mean of.</param>
        /// <returns><typeparamref name="T"/> representing the mean.</returns>
        public static T Average<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            T sum = Sum(x);
            return T.CreateChecked(sum / T.CreateChecked(x._shape._memoryLength));
        }
        #endregion

        #region Broadcast
        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to the smallest broadcastable shape compatible with <paramref name="lengthsSource"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// </summary>
        /// <param name="source">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengthsSource">Other <see cref="Tensor{T}"/> to make shapes broadcastable.</param>
        public static Tensor<T> Broadcast<T>(scoped in ReadOnlyTensorSpan<T> source, scoped in ReadOnlyTensorSpan<T> lengthsSource)
        {
            return Broadcast(source, lengthsSource.Lengths);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to the new shape <paramref name="lengths"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// If the shape of the <paramref name="source"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="source">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        public static Tensor<T> Broadcast<T>(scoped in ReadOnlyTensorSpan<T> source, scoped ReadOnlySpan<nint> lengths)
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(source.Lengths, lengths);

            ReadOnlyTensorSpan<T> intermediate = LazyBroadcast(source, newSize);
            Tensor<T> output = Tensor.CreateUninitialized<T>(intermediate.Lengths);
            intermediate.FlattenTo(MemoryMarshal.CreateSpan(ref output._values[0], (int)output.FlattenedLength));
            return output;
        }
        #endregion

        #region BroadcastTo
        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static void BroadcastTo<T>(this Tensor<T> source, in TensorSpan<T> destination)
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(source.Lengths, destination.Lengths);
            if (!destination.Lengths.SequenceEqual(newSize))
                ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

            ReadOnlyTensorSpan<T> intermediate = LazyBroadcast(source, newSize);
            intermediate.FlattenTo(MemoryMarshal.CreateSpan(ref destination._reference, (int)destination.FlattenedLength));
        }

        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination">Other <see cref="TensorSpan{T}"/> to make shapes broadcastable.</param>
        public static void BroadcastTo<T>(in this TensorSpan<T> source, in TensorSpan<T> destination)
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(source.Lengths, destination.Lengths);
            if (!destination.Lengths.SequenceEqual(newSize))
                ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

            ReadOnlyTensorSpan<T> intermediate = LazyBroadcast(source, newSize);
            intermediate.FlattenTo(MemoryMarshal.CreateSpan(ref destination._reference, (int)destination.FlattenedLength));
        }

        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static void BroadcastTo<T>(in this ReadOnlyTensorSpan<T> source, in TensorSpan<T> destination)
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(source.Lengths, destination.Lengths);
            if (!destination.Lengths.SequenceEqual(newSize))
                ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

            ReadOnlyTensorSpan<T> intermediate = LazyBroadcast(source, newSize);
            intermediate.FlattenTo(MemoryMarshal.CreateSpan(ref destination._reference, (int)destination.FlattenedLength));
        }

        // Lazy/non-copy broadcasting, internal only for now.
        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the new shape <paramref name="shape"/>. Creates a new <see cref="Tensor{T}"/>
        /// but no memory is allocated. It manipulates the strides to achieve this affect.
        /// If the shape of the <paramref name="input"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        internal static TensorSpan<T> LazyBroadcast<T>(in TensorSpan<T> input, ReadOnlySpan<nint> shape)
        {
            if (input.Lengths.SequenceEqual(shape))
                return new TensorSpan<T>(ref input._reference, shape, input.Strides, input._shape._memoryLength);

            if (!TensorHelpers.IsBroadcastableTo(input.Lengths, shape))
                ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);

            if (newSize == input.FlattenedLength)
                return Reshape(input, shape);

            nint[] intermediateShape = TensorHelpers.GetIntermediateShape(input.Lengths, shape.Length);
            nint[] strides = new nint[shape.Length];

            nint stride = 1;

            for (int i = strides.Length - 1; i >= 0; i--)
            {
                if ((intermediateShape[i] == 1 && shape[i] != 1) || (intermediateShape[i] == 1 && shape[i] == 1))
                    strides[i] = 0;
                else
                {
                    strides[i] = stride;
                    stride *= intermediateShape[i];
                }
            }

            TensorSpan<T> output = new TensorSpan<T>(ref input._reference, shape, strides, input._shape._memoryLength);

            return output;
        }

        // Lazy/non-copy broadcasting, internal only for now.
        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the new shape <paramref name="shape"/>. Creates a new <see cref="Tensor{T}"/>
        /// but no memory is allocated. It manipulates the strides to achieve this affect.
        /// If the shape of the <paramref name="input"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        internal static ReadOnlyTensorSpan<T> LazyBroadcast<T>(in ReadOnlyTensorSpan<T> input, ReadOnlySpan<nint> shape)
        {
            if (input.Lengths.SequenceEqual(shape))
                return new TensorSpan<T>(ref input._reference, shape, input.Strides, input._shape._memoryLength);

            if (!TensorHelpers.IsBroadcastableTo(input.Lengths, shape))
                ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);

            if (newSize == input.FlattenedLength)
                return Reshape(input, shape);

            nint[] intermediateShape = TensorHelpers.GetIntermediateShape(input.Lengths, shape.Length);
            nint[] strides = new nint[shape.Length];

            nint stride = 1;

            for (int i = strides.Length - 1; i >= 0; i--)
            {
                if ((intermediateShape[i] == 1 && shape[i] != 1) || (intermediateShape[i] == 1 && shape[i] == 1))
                    strides[i] = 0;
                else
                {
                    strides[i] = stride;
                    stride *= intermediateShape[i];
                }
            }

            TensorSpan<T> output = new TensorSpan<T>(ref input._reference, shape, strides, input._shape._memoryLength);

            return output;
        }

        // Lazy/non-copy broadcasting, internal only for now.
        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the new shape <paramref name="lengths"/>. Creates a new <see cref="Tensor{T}"/>
        /// but no memory is allocated. It manipulates the strides to achieve this affect.
        /// If the shape of the <paramref name="input"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        internal static Tensor<T> LazyBroadcast<T>(Tensor<T> input, ReadOnlySpan<nint> lengths)
        {
            if (input.Lengths.SequenceEqual(lengths))
                return new Tensor<T>(input._values, lengths, false);

            if (!TensorHelpers.IsBroadcastableTo(input.Lengths, lengths))
                ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

            nint newSize = TensorSpanHelpers.CalculateTotalLength(lengths);

            if (newSize == input.FlattenedLength)
                return Reshape(input, lengths);

            nint[] intermediateShape = TensorHelpers.GetIntermediateShape(input.Lengths, lengths.Length);
            nint[] strides = new nint[lengths.Length];

            nint stride = 1;

            for (int i = strides.Length - 1; i >= 0; i--)
            {
                if ((intermediateShape[i] == 1 && lengths[i] != 1) || (intermediateShape[i] == 1 && lengths[i] == 1))
                    strides[i] = 0;
                else
                {
                    strides[i] = stride;
                    stride *= intermediateShape[i];
                }
            }

            Tensor<T> output = new Tensor<T>(input._values, lengths, strides);

            return output;
        }
        #endregion

        #region Concatenate
        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        public static Tensor<T> Concatenate<T>(params scoped ReadOnlySpan<Tensor<T>> tensors)
        {
            return ConcatenateOnDimension(0, tensors);
        }

        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="dimension">The axis along which the tensors will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
        public static Tensor<T> ConcatenateOnDimension<T>(int dimension, params scoped ReadOnlySpan<Tensor<T>> tensors)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_ConcatenateTooFewTensors();

            if (dimension < -1 || dimension > tensors[0].Rank)
                ThrowHelper.ThrowArgument_InvalidAxis();

            // Calculate total space needed.
            nint totalLength = 0;
            for (int i = 0; i < tensors.Length; i++)
                totalLength += TensorSpanHelpers.CalculateTotalLength(tensors[i].Lengths);

            nint sumOfAxis = 0;
            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (dimension != -1)
            {
                sumOfAxis = tensors[0].Lengths[dimension];
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (tensors[0].Rank != tensors[i].Rank)
                        ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                    for (int j = 0; j < tensors[0].Rank; j++)
                    {
                        if (j != dimension)
                        {
                            if (tensors[0].Lengths[j] != tensors[i].Lengths[j])
                                ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                        }
                    }
                    sumOfAxis += tensors[i].Lengths[dimension];
                }
            }

            Tensor<T> tensor;
            if (dimension == -1)
            {
                tensor = Tensor.Create<T>([totalLength]);
            }
            else
            {
                nint[] lengths = new nint[tensors[0].Rank];
                tensors[0].Lengths.CopyTo(lengths);
                lengths[dimension] = sumOfAxis;
                tensor = Tensor.Create<T>(lengths);
            }

            ConcatenateOnDimension(dimension, tensors, tensor);
            return tensor;
        }

        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Concatenate<T>(scoped ReadOnlySpan<Tensor<T>> tensors, in TensorSpan<T> destination)
        {
            return ref ConcatenateOnDimension(0, tensors, destination);
        }

        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="dimension">The axis along which the tensors will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
        /// <param name="destination"></param>

        public static ref readonly TensorSpan<T> ConcatenateOnDimension<T>(int dimension, scoped ReadOnlySpan<Tensor<T>> tensors, in TensorSpan<T> destination)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_ConcatenateTooFewTensors();

            if (dimension < -1 || dimension > tensors[0].Rank)
                ThrowHelper.ThrowArgument_InvalidAxis();

            // Calculate total space needed.
            nint totalLength = 0;
            for (int i = 0; i < tensors.Length; i++)
                totalLength += TensorSpanHelpers.CalculateTotalLength(tensors[i].Lengths);

            nint sumOfAxis = 0;
            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (dimension != -1)
            {
                sumOfAxis = tensors[0].Lengths[dimension];
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (tensors[0].Rank != tensors[i].Rank)
                        ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                    for (int j = 0; j < tensors[0].Rank; j++)
                    {
                        if (j != dimension)
                        {
                            if (tensors[0].Lengths[j] != tensors[i].Lengths[j])
                                ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                        }
                    }
                    sumOfAxis += tensors[i].Lengths[dimension];
                }

                // Make sure the destination tensor has the correct shape.
                nint[] lengths = new nint[tensors[0].Rank];
                tensors[0].Lengths.CopyTo(lengths);
                lengths[dimension] = sumOfAxis;

                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, lengths))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
            }
            Span<T> dstSpan = MemoryMarshal.CreateSpan(ref destination._reference, (int)totalLength);
            nint valuesCopied = 0;

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (tensors[0].Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(tensors[0].Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[tensors[0].Rank];
            }
            nint srcIndex;
            nint copyLength;

            while (valuesCopied < totalLength)
            {
                for (int i = 0; i < tensors.Length; i++)
                {
                    srcIndex = TensorSpanHelpers.ComputeLinearIndex(curIndex, tensors[i].Strides, tensors[i].Lengths);
                    copyLength = CalculateCopyLength(tensors[i].Lengths, dimension);
                    Span<T> srcSpan = MemoryMarshal.CreateSpan(ref tensors[i]._values[srcIndex], (int)copyLength);
                    TensorSpanHelpers.Memmove(dstSpan, srcSpan, copyLength, valuesCopied);
                    valuesCopied += copyLength;
                }
                TensorSpanHelpers.AdjustIndexes(dimension - 1, 1, curIndex, tensors[0].Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        private static nint CalculateCopyLength(ReadOnlySpan<nint> lengths, int startingAxis)
        {
            // When starting axis is -1 we want all the data at once same as if starting axis is 0
            if (startingAxis == -1)
                startingAxis = 0;
            nint length = 1;
            for (int i = startingAxis; i < lengths.Length; i++)
            {
                length *= lengths[i];
            }
            return length;
        }
        #endregion

        #region Equals
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static Tensor<bool> Equals<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IEqualityOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                result = Tensor.Create<bool>(x.Lengths, false);
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                result = Tensor.Create<bool>(newSize, false);
            }

            Equals(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> Equals<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IEqualityOperators<T, T, bool>
        {
            scoped ReadOnlyTensorSpan<T> left;
            scoped ReadOnlyTensorSpan<T> right;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = x;
                right = y;
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, newSize))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = LazyBroadcast(x, newSize);
                right = LazyBroadcast(y, newSize);
            }

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (right.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(right.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[right.Rank];
            }

            for (int i = 0; i < left.FlattenedLength; i++)
            {
                destination[curIndex] = left[curIndex] == right[curIndex];
                TensorSpanHelpers.AdjustIndexes(right.Rank - 1, 1, curIndex, right.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second value to compare.</param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static Tensor<bool> Equals<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IEqualityOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(x.Lengths, false);
            Equals(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second value to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> Equals<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IEqualityOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                destination[curIndex] = x[curIndex] == y;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }
        #endregion

        #region EqualsAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are eqaul to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are equal to <paramref name="y"/>.</returns>
        public static bool EqualsAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IEqualityOperators<T, T, bool>
        {

            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedLeft.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Rank];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] != broadcastedRight[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are eqaul to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are equal to <paramref name="y"/>.</returns>
        public static bool EqualsAll<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IEqualityOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                if (x[curIndex] != y)
                    return false;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }
        #endregion

        #region EqualsAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are equal to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are equal to <paramref name="y"/>.</returns>
        public static bool EqualsAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IEqualityOperators<T, T, bool>
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedRight.Lengths.Length > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Lengths.Length);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Lengths.Length];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] == broadcastedRight[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are equal to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are equal to <paramref name="y"/>.</returns>
        public static bool EqualsAny<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IEqualityOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                if (x[curIndex] == y)
                    return true;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }
        #endregion

        #region FilteredUpdate
        /// <summary>
        /// Updates the <paramref name="tensor"/> tensor with the <paramref name="value"/> where the <paramref name="filter"/> is true.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="filter">Input filter where if the index is true then it will update the <paramref name="tensor"/>.</param>
        /// <param name="value">Value to update in the <paramref name="tensor"/>.</param>
        public static ref readonly TensorSpan<T> FilteredUpdate<T>(in this TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<bool> filter, T value)
        {
            if (filter.Lengths.Length != tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(filter));

            Span<T> srcSpan = MemoryMarshal.CreateSpan(ref tensor._reference, (int)tensor._shape._memoryLength);
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._reference, (int)tensor._shape._memoryLength);

            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                {
                    srcSpan[i] = value;
                }
            }

            return ref tensor;
        }

        /// <summary>
        /// Updates the <paramref name="tensor"/> tensor with the <paramref name="values"/> where the <paramref name="filter"/> is true.
        /// If dimensions are not the same an exception is thrown.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="filter">Input filter where if the index is true then it will update the <paramref name="tensor"/>.</param>
        /// <param name="values">Values to update in the <paramref name="tensor"/>.</param>
        public static ref readonly TensorSpan<T> FilteredUpdate<T>(in this TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<bool> filter, scoped in ReadOnlyTensorSpan<T> values)
        {
            if (filter.Lengths.Length != tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(filter));
            if (values.Rank != 1)
                ThrowHelper.ThrowArgument_1DTensorRequired(nameof(values));

            Span<T> dstSpan = MemoryMarshal.CreateSpan(ref tensor._reference, (int)tensor._shape._memoryLength);
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._reference, (int)tensor._shape._memoryLength);
            Span<T> valuesSpan = MemoryMarshal.CreateSpan(ref values._reference, (int)values._shape._memoryLength);

            int index = 0;
            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                {
                    dstSpan[i] = valuesSpan[index++];
                }
            }

            return ref tensor;
        }
        #endregion

        #region GreaterThan
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> GreaterThan<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                result = Tensor.Create<bool>(x.Lengths, false);
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                result = Tensor.Create<bool>(newSize, false);
            }

            GreaterThan(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThan<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped ReadOnlyTensorSpan<T> left;
            scoped ReadOnlyTensorSpan<T> right;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = x;
                right = y;
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, newSize))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = LazyBroadcast(x, newSize);
                right = LazyBroadcast(y, newSize);
            }

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (right.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(right.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[right.Rank];
            }

            for (int i = 0; i < left.FlattenedLength; i++)
            {
                destination[curIndex] = left[curIndex] > right[curIndex];
                TensorSpanHelpers.AdjustIndexes(right.Rank - 1, 1, curIndex, right.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="ReadOnlyTensorSpan{T}"/> to see which elements are greater than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThan<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(x.Lengths, false);
            GreaterThan(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of a <see cref="ReadOnlyTensorSpan{T}"/> to see which elements are greater than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThan<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                destination[curIndex] = x[curIndex] > y;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares <paramref name="x"/> to see which elements are greater than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThan<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(y.Lengths, false);
            GreaterThan(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares <paramref name="x"/> to see which elements are greater than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThan<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, y.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                destination[curIndex] = x > y[curIndex];
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }
        #endregion

        #region GreaterThanOrEqual
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are greater than or equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> GreaterThanOrEqual<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                result = Tensor.Create<bool>(x.Lengths, false);
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                result = Tensor.Create<bool>(newSize, false);
            }

            GreaterThanOrEqual(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are greater than or equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThanOrEqual<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped ReadOnlyTensorSpan<T> left;
            scoped ReadOnlyTensorSpan<T> right;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = x;
                right = y;
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, newSize))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = LazyBroadcast(x, newSize);
                right = LazyBroadcast(y, newSize);
            }

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (right.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(right.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[right.Rank];
            }

            for (int i = 0; i < left.FlattenedLength; i++)
            {
                destination[curIndex] = left[curIndex] >= right[curIndex];
                TensorSpanHelpers.AdjustIndexes(right.Rank - 1, 1, curIndex, right.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="ReadOnlyTensorSpan{T}"/> to see which elements are greater than or equal to <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThanOrEqual<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(x.Lengths, false);
            GreaterThanOrEqual(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of a <see cref="ReadOnlyTensorSpan{T}"/> to see which elements are greater than or equal to <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThanOrEqual<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                destination[curIndex] = x[curIndex] >= y;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares <paramref name="x"/> to see which elements are greater than or equal to <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThanOrEqual<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(y.Lengths, false);
            GreaterThanOrEqual(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares <paramref name="x"/> to see which elements are greater than or equal to <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThanOrEqual<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, y.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                destination[curIndex] = x >= y[curIndex];
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }
        #endregion

        #region GreaterThanAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedRight.Lengths.Length > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Lengths.Length);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Lengths.Length];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] > broadcastedRight[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAny<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                if (x[curIndex] > y)
                    return true;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="y"/> are greater than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are greater than <paramref name="x"/>.
        /// </summary>
        /// <param name="y">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="x">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are greater than <paramref name="x"/>.</returns>
        public static bool GreaterThanAny<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                if (x > y[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }
        #endregion

        #region GreaterThanOrEqualAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedRight.Lengths.Length > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Lengths.Length);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Lengths.Length];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] >= broadcastedRight[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAny<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                if (x[curIndex] >= y)
                    return true;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="y"/> are greater than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are greater than <paramref name="x"/>.
        /// </summary>
        /// <param name="y">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="x">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are greater than <paramref name="x"/>.</returns>
        public static bool GreaterThanOrEqualAny<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                if (x >= y[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }
        #endregion

        #region GreaterThanAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {

            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedLeft.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Rank];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] <= broadcastedRight[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAll<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                if (x[curIndex] <= y)
                    return false;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="y"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAll<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                if (x <= y[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }
        #endregion

        #region GreaterThanOrEqualAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {

            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedLeft.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Rank];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] < broadcastedRight[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="s"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="s"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="s">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="s"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAll<T>(in ReadOnlyTensorSpan<T> s, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (s.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(s.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[s.Rank];
            }

            for (int i = 0; i < s.FlattenedLength; i++)
            {
                if (s[curIndex] < y)
                    return false;
                TensorSpanHelpers.AdjustIndexes(s.Rank - 1, 1, curIndex, s.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="y"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAll<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                if (x < y[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }
        #endregion

        #region LessThan
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> LessThan<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                result = Tensor.Create<bool>(x.Lengths, false);
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                result = Tensor.Create<bool>(newSize, false);
            }

            LessThan(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThan<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped ReadOnlyTensorSpan<T> left;
            scoped ReadOnlyTensorSpan<T> right;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = x;
                right = y;
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, newSize))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = LazyBroadcast(x, newSize);
                right = LazyBroadcast(y, newSize);
            }

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (right.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(right.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[right.Rank];
            }

            for (int i = 0; i < left.FlattenedLength; i++)
            {
                destination[curIndex] = left[curIndex] < right[curIndex];
                TensorSpanHelpers.AdjustIndexes(right.Rank - 1, 1, curIndex, right.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThan<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(x.Lengths, false);
            LessThan(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThan<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                destination[curIndex] = x[curIndex] < y;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThan<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(y.Lengths, false);
            LessThan(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThan<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, y.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                destination[curIndex] = x < y[curIndex];
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }
        #endregion

        #region LessThanOrEqual
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> LessThanOrEqual<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                result = Tensor.Create<bool>(x.Lengths, false);
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                result = Tensor.Create<bool>(newSize, false);
            }

            LessThanOrEqual(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThanOrEqual<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped ReadOnlyTensorSpan<T> left;
            scoped ReadOnlyTensorSpan<T> right;
            if (TensorHelpers.AreLengthsTheSame(x, y))
            {
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = x;
                right = y;
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, newSize))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
                left = LazyBroadcast(x, newSize);
                right = LazyBroadcast(y, newSize);
            }

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (right.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(right.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[right.Rank];
            }

            for (int i = 0; i < left.FlattenedLength; i++)
            {
                destination[curIndex] = left[curIndex] <= right[curIndex];
                TensorSpanHelpers.AdjustIndexes(right.Rank - 1, 1, curIndex, right.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThanOrEqual<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(x.Lengths, false);
            LessThanOrEqual(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThanOrEqual<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, x.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (x.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(x.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[x.Rank];
            }

            for (int i = 0; i < x.FlattenedLength; i++)
            {
                destination[curIndex] = x[curIndex] <= y;
                TensorSpanHelpers.AdjustIndexes(x.Rank - 1, 1, curIndex, x.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThanOrEqual<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(y.Lengths, false);
            LessThanOrEqual(x, y, result);
            return result;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThanOrEqual<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, y.Lengths))
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                destination[curIndex] = x <= y[curIndex];
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return ref destination;
        }
        #endregion

        #region LessThanAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {

            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedRight.Lengths.Length > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Lengths.Length);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Lengths.Length];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] < broadcastedRight[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="f"/> are less than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="f"/> are less than <paramref name="x"/>.
        /// </summary>
        /// <param name="f">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="x">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="f"/> are less than <paramref name="x"/>.</returns>
        public static bool LessThanAny<T>(in ReadOnlyTensorSpan<T> f, T x)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (f.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(f.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[f.Rank];
            }

            for (int i = 0; i < f.FlattenedLength; i++)
            {
                if (f[curIndex] < x)
                    return true;
                TensorSpanHelpers.AdjustIndexes(f.Rank - 1, 1, curIndex, f.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="y"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First value to compare.</param>
        /// <param name="y">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanAny<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                if (x < y[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }
        #endregion

        #region LessThanOrEqualAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanOrEqualAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {

            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedRight.Lengths.Length > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Lengths.Length);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Lengths.Length];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] <= broadcastedRight[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="f"/> are less than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="f"/> are less than <paramref name="x"/>.
        /// </summary>
        /// <param name="f">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="x">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="f"/> are less than <paramref name="x"/>.</returns>
        public static bool LessThanOrEqualAny<T>(in ReadOnlyTensorSpan<T> f, T x)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (f.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(f.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[f.Rank];
            }

            for (int i = 0; i < f.FlattenedLength; i++)
            {
                if (f[curIndex] <= x)
                    return true;
                TensorSpanHelpers.AdjustIndexes(f.Rank - 1, 1, curIndex, f.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="y"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First value to compare.</param>
        /// <param name="y">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanOrEqualAny<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i <= y.FlattenedLength; i++)
            {
                if (x <= y[curIndex])
                    return true;
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return false;
        }
        #endregion

        #region LessThanAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedRight.Lengths.Length > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Lengths.Length);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Lengths.Length];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] >= broadcastedRight[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="f"/> are less than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="f"/> are less than <paramref name="x"/>.
        /// </summary>
        /// <param name="f">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="x">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="f"/> are less than <paramref name="x"/>.</returns>
        public static bool LessThanAll<T>(in ReadOnlyTensorSpan<T> f, T x)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (f.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(f.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[f.Rank];
            }

            for (int i = 0; i < f.FlattenedLength; i++)
            {
                if (f[curIndex] >= x)
                    return false;
                TensorSpanHelpers.AdjustIndexes(f.Rank - 1, 1, curIndex, f.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="y"/> are less than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are less than <paramref name="x"/>.
        /// </summary>
        /// <param name="y">First value to compare.</param>
        /// <param name="x">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are less than <paramref name="x"/>.</returns>
        public static bool LessThanAll<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                if (x >= y[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }
        #endregion

        #region LessThanOrEqualAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanOrEqualAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(x.Lengths, y.Lengths);
            ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(x, newSize);
            ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(y, newSize);

            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (broadcastedRight.Lengths.Length > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Lengths.Length);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[broadcastedRight.Lengths.Length];
            }

            for (int i = 0; i < broadcastedLeft.FlattenedLength; i++)
            {
                if (broadcastedLeft[curIndex] > broadcastedRight[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="f"/> are less than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="f"/> are less than <paramref name="x"/>.
        /// </summary>
        /// <param name="f">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="x">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="f"/> are less than <paramref name="x"/>.</returns>
        public static bool LessThanOrEqualAll<T>(in ReadOnlyTensorSpan<T> f, T x)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (f.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(f.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[f.Rank];
            }

            for (int i = 0; i < f.FlattenedLength; i++)
            {
                if (f[curIndex] > x)
                    return false;
                TensorSpanHelpers.AdjustIndexes(f.Rank - 1, 1, curIndex, f.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="y"/> are less than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are less than <paramref name="x"/>.
        /// </summary>
        /// <param name="y">First value to compare.</param>
        /// <param name="x">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are less than <paramref name="x"/>.</returns>
        public static bool LessThanOrEqualAll<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            scoped Span<nint> curIndex;
            nint[]? curIndexArray;

            if (y.Rank > 6)
            {
                curIndexArray = ArrayPool<nint>.Shared.Rent(y.Rank);
                curIndex = curIndexArray;
            }
            else
            {
                curIndexArray = null;
                curIndex = stackalloc nint[y.Rank];
            }

            for (int i = 0; i < y.FlattenedLength; i++)
            {
                if (x > y[curIndex])
                    return false;
                TensorSpanHelpers.AdjustIndexes(y.Rank - 1, 1, curIndex, y.Lengths);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return true;
        }
        #endregion

        #region Permute

        /// <summary>
        /// Swaps the dimensions of the <paramref name="tensor"/> tensor according to the <paramref name="dimensions"/> parameter.
        /// If <paramref name="tensor"/> is a 1D tensor, it will return <paramref name="tensor"/>. Otherwise it creates a new <see cref="Tensor{T}"/>
        /// with the new axis ordering by allocating new memory.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/></param>
        /// <param name="dimensions"><see cref="ReadOnlySpan{T}"/> with the new axis ordering.</param>
        public static Tensor<T> PermuteDimensions<T>(this Tensor<T> tensor, params ReadOnlySpan<int> dimensions)
        {
            if (tensor.Rank == 1)
            {
                return tensor;
            }
            else
            {
                T[] values = tensor.IsPinned ? GC.AllocateArray<T>((int)tensor._flattenedLength) : (new T[tensor._flattenedLength]);
                nint[] lengths = new nint[tensor.Rank];
                Tensor<T> outTensor;
                TensorSpan<T> ospan;
                TensorSpan<T> ispan;
                ReadOnlySpan<int> permutation;

                if (dimensions.IsEmpty)
                {
                    lengths = tensor._lengths.Reverse().ToArray();
                    permutation = Enumerable.Range(0, tensor.Rank).Reverse().ToArray();
                }
                else
                {
                    if (dimensions.Length != tensor.Lengths.Length)
                        ThrowHelper.ThrowArgument_PermuteAxisOrder();
                    for (int i = 0; i < lengths.Length; i++)
                        lengths[i] = tensor.Lengths[dimensions[i]];
                    permutation = dimensions.ToArray();
                }
                outTensor = new Tensor<T>(values, lengths, Array.Empty<nint>(), tensor._isPinned);

                ospan = outTensor.AsTensorSpan();
                ispan = tensor.AsTensorSpan();

                scoped Span<nint> indexes;
                nint[]? indicesArray;
                scoped Span<nint> permutedIndices;
                nint[]? permutedIndicesArray;
                if (outTensor.Rank > 6)
                {
                    indicesArray = ArrayPool<nint>.Shared.Rent(outTensor.Rank);
                    indexes = indicesArray;
                    permutedIndicesArray = ArrayPool<nint>.Shared.Rent(outTensor.Rank);
                    permutedIndices = permutedIndicesArray;
                }
                else
                {
                    indicesArray = null;
                    indexes = stackalloc nint[outTensor.Rank];
                    permutedIndicesArray = null;
                    permutedIndices = stackalloc nint[outTensor.Rank];
                }

                for (int i = 0; i < tensor._flattenedLength; i++)
                {
                    TensorHelpers.PermuteIndices(indexes, permutedIndices, permutation);
                    ospan[permutedIndices] = ispan[indexes];
                    TensorSpanHelpers.AdjustIndexes(outTensor.Rank - 1, 1, indexes, tensor._lengths);
                }

                if (indicesArray != null && permutedIndicesArray != null)
                {
                    ArrayPool<nint>.Shared.Return(indicesArray);
                    ArrayPool<nint>.Shared.Return(permutedIndicesArray);
                }

                return outTensor;
            }
        }
        #endregion

        #region Reshape
        /// <summary>
        /// Reshapes the <paramref name="tensor"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="tensor"><see cref="Tensor{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static Tensor<T> Reshape<T>(this Tensor<T> tensor, params ReadOnlySpan<nint> lengths)
        {
            nint[] arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = tensor._flattenedLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                arrLengths[lengths.IndexOf(-1)] = tempTotal;

            }

            nint tempLinear = TensorSpanHelpers.CalculateTotalLength(arrLengths);
            if (tempLinear != tensor.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
            nint[] strides = TensorSpanHelpers.CalculateStrides(arrLengths);
            return new Tensor<T>(tensor._values, arrLengths, strides);
        }

        /// <summary>
        /// Reshapes the <paramref name="tensor"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="tensor"><see cref="TensorSpan{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static TensorSpan<T> Reshape<T>(in this TensorSpan<T> tensor, params scoped ReadOnlySpan<nint> lengths)
        {
            nint[] arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = tensor.FlattenedLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                arrLengths[lengths.IndexOf(-1)] = tempTotal;

            }

            nint tempLinear = TensorSpanHelpers.CalculateTotalLength(arrLengths);
            if (tempLinear != tensor.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
            nint[] strides = TensorSpanHelpers.CalculateStrides(arrLengths);
            TensorSpan<T> output = new TensorSpan<T>(ref tensor._reference, arrLengths, strides, tensor._shape._memoryLength);
            return output;
        }

        /// <summary>
        /// Reshapes the <paramref name="tensor"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="tensor"><see cref="TensorSpan{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static ReadOnlyTensorSpan<T> Reshape<T>(in this ReadOnlyTensorSpan<T> tensor, params scoped ReadOnlySpan<nint> lengths)
        {
            nint[] arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = tensor.FlattenedLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                arrLengths[lengths.IndexOf(-1)] = tempTotal;

            }

            nint tempLinear = TensorSpanHelpers.CalculateTotalLength(arrLengths);
            if (tempLinear != tensor.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
            nint[] strides = TensorSpanHelpers.CalculateStrides(arrLengths);
            ReadOnlyTensorSpan<T> output = new ReadOnlyTensorSpan<T>(ref tensor._reference, arrLengths, strides, tensor._shape._memoryLength);
            return output;
        }
        #endregion

        #region Resize
        /// <summary>
        /// Creates a new <see cref="Tensor{T}"/>, allocates new memory, and copies the data from <paramref name="tensor"/>. If the final shape is smaller all data after
        /// that point is ignored.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        public static Tensor<T> Resize<T>(Tensor<T> tensor, ReadOnlySpan<nint> lengths)
        {
            nint newSize = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = tensor.IsPinned ? GC.AllocateArray<T>((int)newSize) : (new T[newSize]);
            Tensor<T> output = new Tensor<T>(values, lengths, false);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref tensor.AsTensorSpan()._reference, (int)tensor._values.Length);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsTensorSpan()._reference, (int)output.FlattenedLength);
            if (newSize > tensor._values.Length)
                TensorSpanHelpers.Memmove(ospan, span, tensor._values.Length);
            else
                TensorSpanHelpers.Memmove(ospan, span, newSize);

            return output;
        }

        /// <summary>
        /// Copies the data from <paramref name="tensor"/>. If the final shape is smaller all data after that point is ignored.
        /// If the final shape is bigger it is filled with 0s.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/> with the desired new shape.</param>
        public static void ResizeTo<T>(scoped in Tensor<T> tensor, in TensorSpan<T> destination)
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref tensor._values[0], tensor._values.Length);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            if (destination._shape._memoryLength > tensor._values.Length)
                TensorSpanHelpers.Memmove(ospan, span, tensor._values.Length);
            else
                TensorSpanHelpers.Memmove(ospan, span, destination._shape._memoryLength);
        }

        /// <summary>
        /// Copies the data from <paramref name="tensor"/>. If the final shape is smaller all data after that point is ignored.
        /// If the final shape is bigger it is filled with 0s.
        /// </summary>
        /// <param name="tensor">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/> with the desired new shape.</param>
        public static void ResizeTo<T>(scoped in TensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref tensor._reference, (int)tensor._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            if (destination._shape._memoryLength > tensor._shape._memoryLength)
                TensorSpanHelpers.Memmove(ospan, span, tensor._shape._memoryLength);
            else
                TensorSpanHelpers.Memmove(ospan, span, destination._shape._memoryLength);
        }

        /// <summary>
        /// Copies the data from <paramref name="tensor"/>. If the final shape is smaller all data after that point is ignored.
        /// If the final shape is bigger it is filled with 0s.
        /// </summary>
        /// <param name="tensor">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/> with the desired new shape.</param>
        public static void ResizeTo<T>(scoped in ReadOnlyTensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref tensor._reference, (int)tensor._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            if (destination._shape._memoryLength > tensor._shape._memoryLength)
                TensorSpanHelpers.Memmove(ospan, span, tensor._shape._memoryLength);
            else
                TensorSpanHelpers.Memmove(ospan, span, destination._shape._memoryLength);
        }
        #endregion

        #region Reverse
        /// <summary>
        /// Reverse the order of elements in the <paramref name="tensor"/>. The shape of the tensor is preserved, but the elements are reordered.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Reverse<T>(in ReadOnlyTensorSpan<T> tensor)
        {
            Tensor<T> output = Tensor.Create<T>(tensor.Lengths);
            ReverseDimension(tensor, output, -1);

            return output;
        }

        /// <summary>
        /// Reverse the order of elements in the <paramref name="tensor"/> along the given dimension. The shape of the tensor is preserved, but the elements are reordered.
        /// <paramref name="dimension"/> defaults to -1 when not provided, which reverses the entire tensor.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="dimension">dimension along which to reverse over. -1 will reverse over all of the dimensions of the left tensor.</param>
        public static Tensor<T> ReverseDimension<T>(in ReadOnlyTensorSpan<T> tensor, int dimension)
        {
            Tensor<T> output = Tensor.Create<T>(tensor.Lengths);
            ReverseDimension(tensor, output, dimension);

            return output;
        }

        /// <summary>
        /// Reverse the order of elements in the <paramref name="tensor"/>. The shape of the tensor is preserved, but the elements are reordered.
        /// </summary>
        /// <param name="tensor">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Reverse<T>(scoped in ReadOnlyTensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            return ref ReverseDimension(tensor, destination, -1);
        }

        /// <summary>
        /// Reverse the order of elements in the <paramref name="tensor"/> along the given axis. The shape of the tensor is preserved, but the elements are reordered.
        /// <paramref name="dimension"/> defaults to -1 when not provided, which reverses the entire span.
        /// </summary>
        /// <param name="tensor">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        /// <param name="dimension">dimension along which to reverse over. -1 will reverse over all of the dimensions of the left tensor.</param>
        public static ref readonly TensorSpan<T> ReverseDimension<T>(scoped in ReadOnlyTensorSpan<T> tensor, in TensorSpan<T> destination, int dimension)
        {
            if (dimension == -1)
            {
                nint index = tensor._shape._memoryLength - 1;
                Span<T> inputSpan = MemoryMarshal.CreateSpan(ref tensor._reference, (int)tensor._shape._memoryLength);
                Span<T> outputSpan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
                for (int i = 0; i <= tensor._shape._memoryLength / 2; i++)
                {
                    outputSpan[i] = inputSpan[(int)index];
                    outputSpan[(int)index--] = inputSpan[i];
                }
            }
            else
            {
                nint copyLength = 1;
                for (nint i = dimension; i < tensor.Lengths.Length; i++)
                {
                    copyLength *= tensor.Lengths[(int)i];
                }
                copyLength /= tensor.Lengths[(int)dimension];

                scoped Span<nint> oIndices;
                nint[]? oIndicesArray;
                scoped Span<nint> iIndices;
                nint[]? iIndicesArray;
                if (tensor.Rank > 6)
                {
                    oIndicesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                    oIndices = oIndicesArray;
                    iIndicesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                    iIndices = iIndicesArray;
                }
                else
                {
                    oIndicesArray = null;
                    oIndices = stackalloc nint[tensor.Rank];
                    iIndicesArray = null;
                    iIndices = stackalloc nint[tensor.Rank];
                }

                iIndices[(int)dimension] = tensor.Lengths[(int)dimension] - 1;
                nint copiedValues = 0;
                ReadOnlyTensorSpan<T> islice = tensor.Slice(tensor.Lengths);

                while (copiedValues < tensor.FlattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref destination._reference, TensorSpanHelpers.ComputeLinearIndex(oIndices, tensor.Strides, tensor.Lengths)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes((int)dimension, 1, oIndices, tensor.Lengths);
                    TensorSpanHelpers.AdjustIndexesDown((int)dimension, 1, iIndices, tensor.Lengths);
                    copiedValues += copyLength;
                }

                if (oIndicesArray != null && iIndicesArray != null)
                {
                    ArrayPool<nint>.Shared.Return(oIndicesArray);
                    ArrayPool<nint>.Shared.Return(iIndicesArray);
                }
            }

            return ref destination;
        }
        #endregion

        #region SequenceEqual
        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this scoped in TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<T> other)
            where T : IEquatable<T>?
        {
            return tensor.FlattenedLength == other.FlattenedLength
                && tensor._shape._memoryLength == other._shape._memoryLength
                && tensor.Lengths.SequenceEqual(other.Lengths)
                && MemoryMarshal.CreateReadOnlySpan(in tensor.GetPinnableReference(), (int)tensor._shape._memoryLength).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(in other.GetPinnableReference(), (int)other._shape._memoryLength));
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this scoped in ReadOnlyTensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<T> other)
            where T : IEquatable<T>?
        {
            return tensor.FlattenedLength == other.FlattenedLength
                && tensor._shape._memoryLength == other._shape._memoryLength
                && tensor.Lengths.SequenceEqual(other.Lengths)
                && MemoryMarshal.CreateReadOnlySpan(in tensor.GetPinnableReference(), (int)tensor._shape._memoryLength).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(in other.GetPinnableReference(), (int)other._shape._memoryLength));
        }
        #endregion

        #region SetSlice
        /// <summary>
        /// Sets a slice of the given <paramref name="tensor"/> with the provided <paramref name="values"/> for the given <paramref name="ranges"/>
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="values">The values you want to set in the <paramref name="tensor"/>.</param>
        /// <param name="ranges">The ranges you want to set.</param>
        public static Tensor<T> SetSlice<T>(this Tensor<T> tensor, in ReadOnlyTensorSpan<T> values, params ReadOnlySpan<NRange> ranges)
        {
            SetSlice((TensorSpan<T>)tensor, values, ranges);

            return tensor;
        }

        /// <summary>
        /// Sets a slice of the given <paramref name="tensor"/> with the provided <paramref name="values"/> for the given <paramref name="ranges"/>
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="values">The values you want to set in the <paramref name="tensor"/>.</param>
        /// <param name="ranges">The ranges you want to set.</param>
        public static ref readonly TensorSpan<T> SetSlice<T>(this in TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<T> values, params scoped ReadOnlySpan<NRange> ranges)
        {
            TensorSpan<T> srcSpan;
            if (ranges == ReadOnlySpan<NRange>.Empty)
            {
                if (!tensor.Lengths.SequenceEqual(values.Lengths))
                    ThrowHelper.ThrowArgument_SetSliceNoRange(nameof(values));
                srcSpan = tensor.Slice(tensor.Lengths);
            }
            else
                srcSpan = tensor.Slice(ranges);

            if (!srcSpan.Lengths.SequenceEqual(values.Lengths))
                ThrowHelper.ThrowArgument_SetSliceInvalidShapes(nameof(values));

            values.CopyTo(srcSpan);

            return ref tensor;
        }
        #endregion

        #region Split
        /// <summary>
        /// Split a <see cref="Tensor{T}"/> into <paramref name="splitCount"/> along the given <paramref name="dimension"/>. If the tensor cannot be split
        /// evenly on the given <paramref name="dimension"/> an exception is thrown.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="splitCount">How many times to split the <paramref name="tensor"/></param>
        /// <param name="dimension">The axis to split on.</param>
        public static Tensor<T>[] Split<T>(scoped in ReadOnlyTensorSpan<T> tensor, int splitCount, nint dimension)
        {
            if (tensor.Lengths[(int)dimension] % splitCount != 0)
                ThrowHelper.ThrowArgument_SplitNotSplitEvenly();

            Tensor<T>[] outputs = new Tensor<T>[splitCount];

            nint totalToCopy = tensor.FlattenedLength / splitCount;
            nint copyLength = 1;
            for (nint i = dimension; i < tensor.Lengths.Length; i++)
            {
                copyLength *= tensor.Lengths[(int)i];
            }
            copyLength /= splitCount;
            nint[] newShape = tensor.Lengths.ToArray();
            newShape[(int)dimension] = newShape[(int)dimension] / splitCount;

            scoped Span<nint> oIndices;
            nint[]? oIndicesArray;
            scoped Span<nint> iIndices;
            nint[]? iIndicesArray;
            if (tensor.Rank > 6)
            {
                oIndicesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                oIndices = oIndicesArray;
                iIndicesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                iIndices = iIndicesArray;
            }
            else
            {
                oIndicesArray = null;
                oIndices = stackalloc nint[tensor.Rank];
                iIndicesArray = null;
                iIndices = stackalloc nint[tensor.Rank];
            }

            for (int i = 0; i < outputs.Length; i++)
            {
                T[] values = new T[(int)totalToCopy];
                outputs[i] = new Tensor<T>(values, newShape);
                oIndices.Clear();
                iIndices.Clear();

                iIndices[(int)dimension] = i;
                ReadOnlyTensorSpan<T> islice = tensor.Slice(tensor.Lengths);
                TensorSpan<T> oslice = outputs[i].AsTensorSpan().Slice(outputs[i]._lengths);

                nint copiedValues = 0;
                while (copiedValues < totalToCopy)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, TensorSpanHelpers.ComputeLinearIndex(oIndices, outputs[0].Strides, outputs[0].Lengths)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes((int)dimension, 1, oIndices, outputs[i]._lengths);
                    TensorSpanHelpers.AdjustIndexes((int)dimension - 1, 1, iIndices, tensor.Lengths);
                    copiedValues += copyLength;
                }
            }

            if (oIndicesArray != null && iIndicesArray != null)
            {
                ArrayPool<nint>.Shared.Return(oIndicesArray);
                ArrayPool<nint>.Shared.Return(iIndicesArray);
            }

            return outputs;
        }
        #endregion

        #region Squeeze
        /// <summary>
        /// Removes all dimensions of length one from the <paramref name="tensor"/>.
        /// </summary>
        /// <param name="tensor">The <see cref="Tensor{T}"/> to remove all dimensions of length 1.</param>
        public static Tensor<T> Squeeze<T>(this Tensor<T> tensor)
        {
            return SqueezeDimension(tensor, -1);
        }

        /// <summary>
        /// Removes axis of length one from the <paramref name="tensor"/> for the given <paramref name="dimension"/>.
        /// If the dimension is not of length one it will throw an exception.
        /// </summary>
        /// <param name="tensor">The <see cref="Tensor{T}"/> to remove dimension of length 1.</param>
        /// <param name="dimension">The dimension to remove.</param>
        public static Tensor<T> SqueezeDimension<T>(this Tensor<T> tensor, int dimension)
        {
            if (dimension >= tensor.Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            nint[] lengths;
            nint[] strides;

            List<nint> tempLengths = new List<nint>();
            if (dimension == -1)
            {
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (tensor.Lengths[i] != 1)
                    {
                        tempLengths.Add(tensor.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }
            else
            {
                if (tensor.Lengths[dimension] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (i != dimension)
                    {
                        tempLengths.Add(tensor.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }

            return new Tensor<T>(tensor._values, lengths, strides);
        }

        /// <summary>
        /// Removes all dimensions of length one from the <paramref name="tensor"/>.
        /// </summary>
        /// <param name="tensor">The <see cref="TensorSpan{T}"/> to remove all dimensions of length 1.</param>
        public static TensorSpan<T> Squeeze<T>(in this TensorSpan<T> tensor)
        {
            return SqueezeDimension(tensor, -1);
        }

        /// <summary>
        /// Removes axis of length one from the <paramref name="tensor"/> for the given <paramref name="dimension"/>.
        /// If the dimension is not of length one it will throw an exception.
        /// </summary>
        /// <param name="tensor">The <see cref="TensorSpan{T}"/> to remove dimension of length 1.</param>
        /// <param name="dimension">The dimension to remove.</param>
        public static TensorSpan<T> SqueezeDimension<T>(in this TensorSpan<T> tensor, int dimension)
        {
            if (dimension >= tensor.Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            nint[] lengths;
            nint[] strides;

            List<nint> tempLengths = new List<nint>();
            if (dimension == -1)
            {
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (tensor.Lengths[i] != 1)
                    {
                        tempLengths.Add(tensor.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }
            else
            {
                if (tensor.Lengths[dimension] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (i != dimension)
                    {
                        tempLengths.Add(tensor.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }

            return new TensorSpan<T>(ref tensor._reference, lengths, strides, tensor._shape._memoryLength);
        }

        /// <summary>
        /// Removes all dimensions of length one from the <paramref name="tensor"/>.
        /// </summary>
        /// <param name="tensor">The <see cref="ReadOnlyTensorSpan{T}"/> to remove all dimensions of length 1.</param>
        public static ReadOnlyTensorSpan<T> Squeeze<T>(in this ReadOnlyTensorSpan<T> tensor)
        {
            return SqueezeDimension(tensor, -1);
        }

        /// <summary>
        /// Removes axis of length one from the <paramref name="tensor"/> for the given <paramref name="dimension"/>.
        /// If the dimension is not of length one it will throw an exception.
        /// </summary>
        /// <param name="tensor">The <see cref="ReadOnlyTensorSpan{T}"/> to remove dimension of length 1.</param>
        /// <param name="dimension">The dimension to remove.</param>
        public static ReadOnlyTensorSpan<T> SqueezeDimension<T>(in this ReadOnlyTensorSpan<T> tensor, int dimension)
        {
            if (dimension >= tensor.Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            nint[] lengths;
            nint[] strides;

            List<nint> tempLengths = new List<nint>();
            if (dimension == -1)
            {
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (tensor.Lengths[i] != 1)
                    {
                        tempLengths.Add(tensor.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }
            else
            {
                if (tensor.Lengths[dimension] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (i != dimension)
                    {
                        tempLengths.Add(tensor.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }

            return new ReadOnlyTensorSpan<T>(ref tensor._reference, lengths, strides, tensor._shape._memoryLength);
        }
        #endregion

        #region Stack
        /// <summary>
        /// Join multiple <see cref="Tensor{T}"/> along a new dimension that is added at position 0. All tensors must have the same shape.
        /// </summary>
        /// <param name="tensors">Input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Stack<T>(params ReadOnlySpan<Tensor<T>> tensors)
        {
            return StackAlongDimension(0, tensors);
        }

        /// <summary>
        /// Join multiple <see cref="Tensor{T}"/> along a new dimension. The axis parameter specifies the index of the new dimension. All tensors must have the same shape.
        /// </summary>
        /// <param name="tensors">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="dimension">Index of where the new dimension will be.</param>
        public static Tensor<T> StackAlongDimension<T>(int dimension, params ReadOnlySpan<Tensor<T>> tensors)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_StackTooFewTensors();

            for (int i = 1; i < tensors.Length; i++)
            {
                if (!TensorHelpers.AreLengthsTheSame<T>(tensors[0], tensors[i]))
                    ThrowHelper.ThrowArgument_StackShapesNotSame();
            }

            if (dimension < 0)
                dimension = tensors[0].Rank - dimension;

            Tensor<T>[] outputs = new Tensor<T>[tensors.Length];
            for (int i = 0; i < tensors.Length; i++)
            {
                outputs[i] = Tensor.Unsqueeze(tensors[0], dimension);
            }
            return Tensor.ConcatenateOnDimension<T>(dimension, outputs);
        }

        /// <summary>
        /// Join multiple <see cref="Tensor{T}"/> along a new dimension that is added at position 0. All tensors must have the same shape.
        /// </summary>
        /// <param name="tensors">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Stack<T>(scoped in ReadOnlySpan<Tensor<T>> tensors, in TensorSpan<T> destination)
        {
            return ref StackAlongDimension(tensors, destination, 0);
        }

        /// <summary>
        /// Join multiple <see cref="Tensor{T}"/> along a new dimension. The axis parameter specifies the index of the new dimension. All tensors must have the same shape.
        /// </summary>
        /// <param name="tensors">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination"></param>
        /// <param name="dimension">Index of where the new dimension will be.</param>
        public static ref readonly TensorSpan<T> StackAlongDimension<T>(scoped ReadOnlySpan<Tensor<T>> tensors, in TensorSpan<T> destination, int dimension)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_StackTooFewTensors();

            for (int i = 1; i < tensors.Length; i++)
            {
                if (!TensorHelpers.AreLengthsTheSame<T>(tensors[0], tensors[i]))
                    ThrowHelper.ThrowArgument_StackShapesNotSame();
            }

            if (dimension < 0)
                dimension = tensors[0].Rank - dimension;

            Tensor<T>[] outputs = new Tensor<T>[tensors.Length];
            for (int i = 0; i < tensors.Length; i++)
            {
                outputs[i] = Tensor.Unsqueeze(tensors[0], dimension);
            }
            return ref Tensor.ConcatenateOnDimension<T>(dimension, tensors, destination);
        }
        #endregion

        #region StdDev
        /// <summary>
        /// Returns the standard deviation of the elements in the <paramref name="x"/> tensor.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the standard deviation of.</param>
        /// <returns><typeparamref name="T"/> representing the standard deviation.</returns>
        public static T StdDev<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>, IPowerFunctions<T>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            T mean = Average(x);
            Span<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            Span<T> output = new T[x.FlattenedLength];
            TensorPrimitives.Subtract(span, mean, output);
            TensorPrimitives.Abs(output, output);
            TensorPrimitives.Pow((ReadOnlySpan<T>)output, T.CreateChecked(2), output);
            T sum = TensorPrimitives.Sum((ReadOnlySpan<T>)output);
            return T.CreateChecked(sum / T.CreateChecked(x._shape._memoryLength));
        }
        #endregion

        #region ToString
        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="tensor">The <see cref="TensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="tensor"/></returns>
        public static string ToString<T>(this in TensorSpan<T> tensor, params ReadOnlySpan<nint> maximumLengths) => ((ReadOnlyTensorSpan<T>)tensor).ToString(maximumLengths);

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="ReadOnlyTensorSpan{T}"/>."/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tensor">The <see cref="ReadOnlyTensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        public static string ToString<T>(this in ReadOnlyTensorSpan<T> tensor, params ReadOnlySpan<nint> maximumLengths)
        {

            if (maximumLengths.Length != tensor.Rank)
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(tensor));

            var sb = new StringBuilder();
            scoped Span<nint> curIndexes;
            nint[]? curIndexesArray;
            if (tensor.Rank > 6)
            {
                curIndexesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                curIndexes = curIndexesArray;
            }
            else
            {
                curIndexesArray = null;
                curIndexes = stackalloc nint[tensor.Rank];
            }

            nint copiedValues = 0;

            T[] values = new T[tensor.Lengths[tensor.Rank - 1]];
            while (copiedValues < tensor.FlattenedLength)
            {
                var sp = new ReadOnlyTensorSpan<T>(ref Unsafe.Add(ref tensor._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, tensor.Strides, tensor.Lengths)), [tensor.Lengths[tensor.Rank - 1]], [1], tensor.Lengths[tensor.Rank - 1]);
                sb.Append('{');
                sp.FlattenTo(values);
                sb.Append(string.Join(",", values));
                sb.AppendLine("}");

                TensorSpanHelpers.AdjustIndexes(tensor.Rank - 2, 1, curIndexes, tensor.Lengths);
                copiedValues += tensor.Lengths[tensor.Rank - 1];
            }

            if (curIndexesArray != null)
                ArrayPool<nint>.Shared.Return(curIndexesArray);

            return sb.ToString();
        }

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="Tensor{T}"/>."/>
        /// </summary>
        /// <param name="tensor">The <see cref="Span{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="tensor"/></returns>
        public static string ToString<T>(this Tensor<T> tensor, params ReadOnlySpan<nint> maximumLengths) => ((ReadOnlyTensorSpan<T>)tensor).ToString(maximumLengths);

        #endregion

        #region Transpose
        /// <summary>
        /// Swaps the last two dimensions of the <paramref name="tensor"/> tensor.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Transpose<T>(Tensor<T> tensor)
        {
            if (tensor.Lengths.Length < 2)
                ThrowHelper.ThrowArgument_TransposeTooFewDimensions();
            int[] dimension = Enumerable.Range(0, tensor.Rank).ToArray();
            int temp = dimension[tensor.Rank - 1];
            dimension[tensor.Rank - 1] = dimension[tensor.Rank - 2];
            dimension[tensor.Rank - 2] = temp;
            return PermuteDimensions(tensor, dimension.AsSpan());
        }
        #endregion

        #region TryBroadcastTo
        /// <summary>
        /// Broadcast the data from <paramref name="tensor"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(this Tensor<T> tensor, in TensorSpan<T> destination)
        {
            return TryBroadcastTo((ReadOnlyTensorSpan<T>)tensor, destination);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="tensor"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="tensor">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(in this TensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            return TryBroadcastTo((ReadOnlyTensorSpan<T>)tensor, destination);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="tensor"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="tensor">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(in this ReadOnlyTensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            if (!TensorHelpers.IsBroadcastableTo(tensor.Lengths, destination.Lengths))
                return false;

            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(tensor.Lengths, destination.Lengths);
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, newSize))
                return false;

            LazyBroadcast(tensor, newSize).CopyTo(destination);
            return true;
        }
        #endregion

        #region Unsqueeze
        /// <summary>
        /// Insert a new dimension of length 1 that will appear at the dimension position.
        /// </summary>
        /// <param name="tensor">The <see cref="Tensor{T}"/> to add a dimension of length 1.</param>
        /// <param name="dimension">The index of the dimension to add.</param>
        public static Tensor<T> Unsqueeze<T>(this Tensor<T> tensor, int dimension)
        {
            if (dimension > tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (dimension < 0)
                dimension = tensor.Rank - dimension;

            List<nint> tempLengths = tensor._lengths.ToList();
            tempLengths.Insert(dimension, 1);
            nint[] lengths = tempLengths.ToArray();
            nint[] strides = TensorSpanHelpers.CalculateStrides(lengths);
            return new Tensor<T>(tensor._values, lengths, strides);
        }

        /// <summary>
        /// Insert a new dimension of length 1 that will appear at the dimension position.
        /// </summary>
        /// <param name="tensor">The <see cref="TensorSpan{T}"/> to add a dimension of length 1.</param>
        /// <param name="dimension">The index of the dimension to add.</param>
        public static TensorSpan<T> Unsqueeze<T>(in this TensorSpan<T> tensor, int dimension)
        {
            if (dimension > tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (dimension < 0)
                dimension = tensor.Rank - dimension;

            List<nint> tempLengths = tensor.Lengths.ToArray().ToList();
            tempLengths.Insert(dimension, 1);
            nint[] lengths = tempLengths.ToArray();
            nint[] strides = TensorSpanHelpers.CalculateStrides(lengths);
            return new TensorSpan<T>(ref tensor._reference, lengths, strides, tensor._shape._memoryLength);
        }

        /// <summary>
        /// Insert a new dimension of length 1 that will appear at the dimension position.
        /// </summary>
        /// <param name="tensor">The <see cref="ReadOnlyTensorSpan{T}"/> to add a dimension of length 1.</param>
        /// <param name="dimension">The index of the dimension to add.</param>
        public static ReadOnlyTensorSpan<T> Unsqueeze<T>(in this ReadOnlyTensorSpan<T> tensor, int dimension)
        {
            if (dimension > tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (dimension < 0)
                dimension = tensor.Rank - dimension;

            List<nint> tempLengths = tensor.Lengths.ToArray().ToList();
            tempLengths.Insert(dimension, 1);
            nint[] lengths = tempLengths.ToArray();
            nint[] strides = TensorSpanHelpers.CalculateStrides(lengths);
            return new ReadOnlyTensorSpan<T>(ref tensor._reference, lengths, strides, tensor._shape._memoryLength);
        }
        #endregion

        #region TensorPrimitives
        #region Abs
        /// <summary>
        /// Takes the absolute value of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the abs of.</param>
        public static Tensor<T> Abs<T>(in ReadOnlyTensorSpan<T> x)
            where T : INumberBase<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Abs(x, output);
            return output;
        }

        /// <summary>
        /// Takes the absolute value of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the abs of.</param>
        /// <param name="destination">The <see cref="TensorSpan{T}"/> destination.</param>
        public static ref readonly TensorSpan<T> Abs<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : INumberBase<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Abs);
        }
        #endregion

        #region Acos
        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Acos<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Acos(x, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Acos<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Acos);
        }
        #endregion

        #region Acosh
        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Acosh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Acosh(x, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Acosh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Acosh);
        }
        #endregion

        #region AcosPi
        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> AcosPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            AcosPi(x, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AcosPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.AcosPi);
        }
        #endregion

        #region Add
        /// <summary>
        /// Adds each element of <paramref name="x"/> to each element of <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="y">The second <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        public static Tensor<T> Add<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Add(x, y, output);
            return output;
        }

        /// <summary>
        /// Adds <paramref name="y"/> to each element of <paramref name="x"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="y">The <typeparamref name="T"/> to add to each element of <paramref name="x"/>.</param>
        public static Tensor<T> Add<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Add(x, y, output);
            return output;
        }

        /// <summary>
        /// Adds each element of <paramref name="x"/> to each element of <paramref name="y"/> and returns a new <see cref="ReadOnlyTensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="y">The second <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Add<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Add);
        }

        /// <summary>
        /// Adds <paramref name="y"/> to each element of <paramref name="x"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="y">The <typeparamref name="T"/> to add to each element of <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Add<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Add);
        }
        #endregion

        #region Asin
        /// <summary>
        /// Takes the inverse sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Asin<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Asin(x, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Asin<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Asin);
        }
        #endregion

        #region Asinh
        /// <summary>
        /// Takes the inverse hyperbolic sine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Asinh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Asinh(x, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Asinh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Asinh);
        }
        #endregion

        #region AsinPi
        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> AsinPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            AsinPi(x, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AsinPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.AsinPi);
        }
        #endregion

        #region Atan
        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> Atan<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Atan(x, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Atan);
        }
        #endregion

        #region Atan2
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Atan2(x, y, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Atan2);
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);

            Atan2(x, y, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Atan2);
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output = Tensor.Create<T>(y.Lengths);

            Atan2(x, y, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTInSpanInSpanOut(x, y, destination, TensorPrimitives.Atan2);
        }
        #endregion

        #region Atan2Pi
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2Pi<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Atan2Pi(x, y, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2Pi<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Atan2Pi);
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2Pi<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);

            Atan2Pi(x, y, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2Pi<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Atan2Pi);
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2Pi<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output = Tensor.Create<T>(y.Lengths);

            Atan2Pi(x, y, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2Pi<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTInSpanInSpanOut(x, y, destination, TensorPrimitives.Atan2Pi);

        }
        #endregion

        #region Atanh
        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atanh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Atanh(x, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atanh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Atanh);
        }
        #endregion

        #region AtanPi
        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input<see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> AtanPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            AtanPi(x, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input<see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AtanPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.AtanPi);
        }
        #endregion

        #region BitwiseAnd
        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> BitwiseAnd<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            BitwiseAnd(x, y, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseAnd<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.BitwiseAnd);
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        public static Tensor<T> BitwiseAnd<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);

            BitwiseAnd(x, y, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseAnd<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.BitwiseAnd);
        }
        #endregion

        #region BitwiseOr
        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> BitwiseOr<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            BitwiseOr(x, y, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseOr<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.BitwiseOr);
        }

        /// <summary>
        /// Computes the element-wise bitwise or of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        public static Tensor<T> BitwiseOr<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);

            BitwiseOr(x, y, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise bitwise or of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseOr<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.BitwiseOr);
        }
        #endregion

        #region CubeRoot
        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Cbrt<T>(in ReadOnlyTensorSpan<T> x)
            where T : IRootFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Cbrt(x, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cbrt<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Cbrt);
        }
        #endregion

        #region Ceiling
        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Ceiling<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Ceiling(x, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ceiling<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Ceiling);
        }
        #endregion

        #region ConvertChecked
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="ReadOnlyTensorSpan{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<TTo> ConvertChecked<TFrom, TTo>(in ReadOnlyTensorSpan<TFrom> source)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            Tensor<TTo> output = Tensor.Create<TTo>(source.Lengths);

            ConvertChecked<TFrom, TTo>(source, output);
            return output;
        }

        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TFrom}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<TTo> ConvertChecked<TFrom, TTo>(scoped in ReadOnlyTensorSpan<TFrom> source, in TensorSpan<TTo> destination)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            return ref TensorPrimitivesHelperTFromSpanInTToSpanOut(source, destination, TensorPrimitives.ConvertChecked);
        }
        #endregion

        #region ConvertSaturating
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="ReadOnlyTensorSpan{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<TTo> ConvertSaturating<TFrom, TTo>(in ReadOnlyTensorSpan<TFrom> source)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            Tensor<TTo> output = Tensor.Create<TTo>(source.Lengths);

            ConvertSaturating<TFrom, TTo>(source, output);
            return output;
        }

        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TFrom}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<TTo> ConvertSaturating<TFrom, TTo>(scoped in ReadOnlyTensorSpan<TFrom> source, in TensorSpan<TTo> destination)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            return ref TensorPrimitivesHelperTFromSpanInTToSpanOut(source, destination, TensorPrimitives.ConvertSaturating);
        }
        #endregion

        #region ConvertTruncating
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="ReadOnlyTensorSpan{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<TTo> ConvertTruncating<TFrom, TTo>(in ReadOnlyTensorSpan<TFrom> source)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            Tensor<TTo> output = Tensor.Create<TTo>(source.Lengths);

            ConvertTruncating<TFrom, TTo>(source, output);
            return output;
        }

        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TFrom}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<TTo> ConvertTruncating<TFrom, TTo>(scoped in ReadOnlyTensorSpan<TFrom> source, in TensorSpan<TTo> destination)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            return ref TensorPrimitivesHelperTFromSpanInTToSpanOut(source, destination, TensorPrimitives.ConvertTruncating);
        }
        #endregion

        #region CopySign
        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new tensor with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        public static Tensor<T> CopySign<T>(in ReadOnlyTensorSpan<T> x, T sign)
            where T : INumber<T>
        {
            Tensor<T> output = Create<T>(x.Lengths);

            CopySign(x, sign, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The <see cref="ReadOnlyTensorSpan{T}"/> with the associated signs.</param>
        public static Tensor<T> CopySign<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> sign)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(sign.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, sign.Lengths));
            }

            CopySign(x, sign, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new tensor with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> CopySign<T>(scoped in ReadOnlyTensorSpan<T> x, T sign, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, sign, destination, TensorPrimitives.CopySign);
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The <see cref="ReadOnlyTensorSpan{T}"/> with the associated signs.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> CopySign<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> sign, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, sign, destination, TensorPrimitives.CopySign);
        }
        #endregion

        #region Cos
        /// <summary>
        /// Takes the cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        public static Tensor<T> Cos<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Cos(x, output);
            return output;
        }

        /// <summary>
        /// Takes the cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cos<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Cos);
        }
        #endregion

        #region Cosh
        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        public static Tensor<T> Cosh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Cosh(x, output);
            return output;
        }

        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cosh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Cosh);
        }
        #endregion

        #region CosineSimilarity
        /// <summary>
        /// Compute cosine similarity between <paramref name="x"/> and <paramref name="y"/>.
        /// </summary>
        /// <param name="x">The first <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="y">The second <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> CosineSimilarity<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IRootFunctions<T>
        {
            if (x.Rank != 2)
                ThrowHelper.ThrowArgument_2DTensorRequired(nameof(x));

            if (y.Rank != 2)
                ThrowHelper.ThrowArgument_2DTensorRequired(nameof(y));

            if (x.Lengths[1] != y.Lengths[1])
                ThrowHelper.ThrowArgument_IncompatibleDimensions(x.Lengths[1], y.Lengths[1]);

            nint dim1 = x.Lengths[0];
            nint dim2 = y.Lengths[0];

            T[] values = new T[dim1 * dim2];

            Tensor<T> output = Tensor.Create<T>(values, [dim1, dim2]);

            CosineSimilarity(x, y, output);

            return output;
        }

        /// <summary>
        /// Compute cosine similarity between <paramref name="x"/> and <paramref name="y"/>.
        /// </summary>
        /// <param name="x">The first <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="y">The second <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> CosineSimilarity<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            if (x.Rank != 2)
                ThrowHelper.ThrowArgument_2DTensorRequired(nameof(x));

            if (y.Rank != 2)
                ThrowHelper.ThrowArgument_2DTensorRequired(nameof(y));

            if (x.Lengths[1] != y.Lengths[1])
                ThrowHelper.ThrowArgument_IncompatibleDimensions(x.Lengths[1], y.Lengths[1]);

            nint dim1 = x.Lengths[0];
            nint dim2 = y.Lengths[0];

            if (destination.Lengths[0] != dim1 || destination.Lengths[1] != dim2)
                ThrowHelper.ThrowArgument_IncompatibleDimensions(x.Lengths[1], y.Lengths[1]);

            Span<T> values = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);

            scoped Span<nint> leftIndexes = stackalloc nint[2];
            scoped Span<nint> rightIndexes = stackalloc nint[2];

            int outputOffset = 0;

            ReadOnlySpan<T> lspan;
            ReadOnlySpan<T> rspan;
            int rowLength = (int)x.Lengths[1];
            for (int i = 0; i < dim1; i++)
            {
                for (int j = 0; j < dim2; j++)
                {
                    lspan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref x._reference, TensorSpanHelpers.ComputeLinearIndex(leftIndexes, x.Strides, x.Lengths)), (int)rowLength);
                    rspan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref y._reference, TensorSpanHelpers.ComputeLinearIndex(rightIndexes, y.Strides, y.Lengths)), (int)rowLength);
                    values[outputOffset++] = TensorPrimitives.CosineSimilarity(lspan, rspan);
                    rightIndexes[0]++;
                }
                rightIndexes[0] = 0;
                leftIndexes[0]++;
            }

            return ref destination;

        }
        #endregion

        #region CosPi
        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi and returns a new <see cref="Tensor{T}"/> with the results.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><typeparamref name="T"/>.CosPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static Tensor<T> CosPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            CosPi(x, output);
            return output;
        }

        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi and returns a new <see cref="TensorSpan{T}"/> with the results.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><typeparamref name="T"/>.CosPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static ref readonly TensorSpan<T> CosPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.CosPi);
        }
        #endregion

        #region DegreesToRadians
        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians and returns a new tensor with the results.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> DegreesToRadians<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            DegreesToRadians(x, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians and returns a new tensor with the results.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> DegreesToRadians<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.DegreesToRadians);
        }
        #endregion

        #region Distance
        /// <summary>
        /// Computes the distance between two points, specified as non-empty, equal-length tensors of numbers, in Euclidean space.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Distance<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y)
            where T : IRootFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInTOut(x, y, TensorPrimitives.Distance);
        }
        #endregion

        #region Divide
        /// <summary>
        /// Divides each element of <paramref name="x"/> by <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The divisor</param>
        public static Tensor<T> Divide<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IDivisionOperators<T, T, T>
        {
            Tensor<T> output = Create<T>(x.Lengths);
            Divide(x, y, output);
            return output;
        }

        /// <summary>
        /// Divides <paramref name="x"/> by each element of <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result."/>
        /// </summary>
        /// <param name="x">The value to be divided.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        public static Tensor<T> Divide<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IDivisionOperators<T, T, T>
        {
            Tensor<T> output = Tensor.Create<T>(y.Lengths);
            Divide(x, y, output);
            return output;
        }

        /// <summary>
        /// Divides each element of <paramref name="x"/> by its corresponding element in <paramref name="y"/> and returns
        /// a new <see cref="ReadOnlyTensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to be divided.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        public static Tensor<T> Divide<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IDivisionOperators<T, T, T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Divide(x, y, output);
            return output;
        }

        /// <summary>
        /// Divides each element of <paramref name="x"/> by <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The divisor</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Divide);
        }

        /// <summary>
        /// Divides <paramref name="x"/> by each element of <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result."/>
        /// </summary>
        /// <param name="x">The value to be divided.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTInSpanInSpanOut(x, y, destination, TensorPrimitives.Divide);
        }

        /// <summary>
        /// Divides each element of <paramref name="x"/> by its corresponding element in <paramref name="y"/> and returns
        /// a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to be divided.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Divide);
        }
        #endregion

        #region Dot
        /// <summary>
        /// Computes the dot product of two tensors containing numbers.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Dot<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInTOut(x, y, TensorPrimitives.Dot);
        }
        #endregion

        #region Exp
        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Exp(x, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Exp);
        }
        #endregion

        #region Exp10
        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp10<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Exp10(x, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp10<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Exp10);
        }
        #endregion

        #region Exp10M1
        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp10M1<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Exp10M1(x, output);
            return output;
        }

        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp10M1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Exp10M1);
        }
        #endregion

        #region Exp2
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp2<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Exp2(x, output);
            return output;
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp2<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Exp2);
        }
        #endregion

        #region Exp2M1
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp2M1<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Exp2M1(x, output);
            return output;
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp2M1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Exp2M1);
        }
        #endregion

        #region ExpM1
        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> ExpM1<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            ExpM1(x, output);
            return output;
        }

        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> ExpM1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.ExpM1);
        }
        #endregion

        #region Floor
        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Floor<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Floor(x, output);
            return output;
        }

        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Floor<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Floor);
        }
        #endregion

        #region Hypotenuse
        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Hypot<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IRootFunctions<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Hypot(x, y, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Hypot<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Hypot);
        }
        #endregion

        #region Ieee754Remainder
        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Ieee754Remainder<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Ieee754Remainder(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ieee754Remainder<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Ieee754Remainder);
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Ieee754Remainder<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);

            Ieee754Remainder(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ieee754Remainder<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Ieee754Remainder);
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Ieee754Remainder<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output = Tensor.Create<T>(y.Lengths);

            Ieee754Remainder(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ieee754Remainder<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTInSpanInSpanOut(x, y, destination, TensorPrimitives.Ieee754Remainder);
        }
        #endregion

        #region ILogB
        /// <summary>Computes the element-wise integer logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<int> ILogB<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<int> output = Tensor.Create<int>(x.Lengths, x.Strides);
            ILogB(x, output);
            return output;
        }

        /// <summary>Computes the element-wise integer logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<int> ILogB<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<int> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperSpanInIntSpanOut(x, destination, TensorPrimitives.ILogB);
        }
        #endregion

        #region IndexOfMax
        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static int IndexOfMax<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            return TensorPrimitives.IndexOfMax(span);
        }
        #endregion

        #region IndexOfMaxMagnitude
        /// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static int IndexOfMaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            return TensorPrimitives.IndexOfMaxMagnitude(span);
        }
        #endregion

        #region IndexOfMin
        /// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static int IndexOfMin<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            return TensorPrimitives.IndexOfMin(span);
        }
        #endregion

        #region IndexOfMinMagnitude
        /// <summary>
        /// Searches for the index of the number with the smallest magnitude in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static int IndexOfMinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            return TensorPrimitives.IndexOfMinMagnitude(span);
        }
        #endregion

        #region LeadingZeroCount
        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> LeadingZeroCount<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBinaryInteger<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            LeadingZeroCount(x, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> LeadingZeroCount<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.LeadingZeroCount);
        }
        #endregion

        #region Log
        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> Log<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Log(x, output);
            return output;
        }

        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Log);
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor</param>
        /// <param name="y">The second tensor</param>
        public static Tensor<T> Log<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Log(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor</param>
        /// <param name="y">The second tensor</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Log<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Log);
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor</param>
        /// <param name="y">The second tensor</param>
        public static Tensor<T> Log<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);

            Log(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor</param>
        /// <param name="y">The second tensor</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Log<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Log);
        }
        #endregion

        #region Log10
        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Log10(x, output);
            return output;
        }

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log10<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Log10);
        }
        #endregion

        #region Log10P1
        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10P1<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Log10P1(x, output);
            return output;
        }

        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log10P1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Log10P1);
        }
        #endregion

        #region Log2
        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Log2(x, output);
            return output;
        }

        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log2<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Log2);
        }
        #endregion

        #region Log2P1
        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2P1<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Log2P1(x, output);
            return output;
        }

        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log2P1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Log2P1);
        }
        #endregion

        #region LogP1
        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> LogP1<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            LogP1(x, output);
            return output;
        }

        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> LogP1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.LogP1);
        }
        #endregion

        #region Max
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>..</param>
        public static T Max<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.Max);
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> Max<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Max(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Max<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Max);
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> Max<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Max(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Max<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Max);
        }
        #endregion

        #region MaxMagnitude
        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>..</param>
        public static T MaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.MaxMagnitude);
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxMagnitude<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            MaxMagnitude(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.MaxMagnitude);
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxMagnitude<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            MaxMagnitude(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.MaxMagnitude);
        }
        #endregion

        #region MaxMagnitudeNumber
        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>..</param>
        public static T MaxMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumberBase<T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.MaxMagnitudeNumber);
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxMagnitudeNumber<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            MaxMagnitudeNumber(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.MaxMagnitudeNumber);
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxMagnitudeNumber<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            MaxMagnitudeNumber(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.MaxMagnitudeNumber);
        }
        #endregion

        #region MaxNumber
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>..</param>
        public static T MaxNumber<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.MaxNumber);
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxNumber<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            MaxNumber(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxNumber<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.MaxNumber);
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxNumber<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            MaxNumber(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxNumber<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.MaxNumber);
        }
        #endregion

        #region Min
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Min<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.Min);
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> Min<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Min(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Min<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Min);
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> Min<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Min(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Min<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Min);
        }
        #endregion

        #region MinMagnitude
        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T MinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.MinMagnitude);
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinMagnitude<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            MinMagnitude(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.MinMagnitude);
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinMagnitude<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            MinMagnitude(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.MinMagnitude);
        }
        #endregion

        #region MinMagnitudeNumber
        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>..</param>
        public static T MinMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumberBase<T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.MinMagnitudeNumber);
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinMagnitudeNumber<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            MinMagnitudeNumber(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.MinMagnitudeNumber);
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinMagnitudeNumber<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            MinMagnitudeNumber(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.MinMagnitudeNumber);
        }
        #endregion

        #region MinNumber
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="TensorSpan{T}"/>..</param>
        public static T MinNumber<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.MinNumber);
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinNumber<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            MinNumber(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinNumber<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.MinNumber);
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinNumber<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            MinNumber(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinNumber<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.MinNumber);
        }
        #endregion

        #region Multiply
        /// <summary>
        /// Multiplies each element of <paramref name="x"/> with <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="y"><typeparamref name="T"/> value to multiply by.</param>
        public static Tensor<T> Multiply<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Multiply((ReadOnlyTensorSpan<T>)x, y, output);
            return output;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="x"/> with <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        public static Tensor<T> Multiply<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Multiply((ReadOnlyTensorSpan<T>)x, y, output);
            return output;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="x"/> with <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="y"><typeparamref name="T"/> value to multiply by.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Multiply<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Multiply(span, y, ospan);
            return ref destination;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="x"/> with <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Multiply<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Multiply);
        }
        #endregion

        #region Negate
        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> Negate<T>(in ReadOnlyTensorSpan<T> x)
            where T : IUnaryNegationOperators<T, T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Negate(x, output);
            return output;
        }

        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Negate<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IUnaryNegationOperators<T, T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Negate);
        }
        #endregion

        #region Norm
        /// <summary>
        ///  Takes the norm of the <see cref="ReadOnlyTensorSpan{T}"/> and returns the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the norm of.</param>
        public static T Norm<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : IRootFunctions<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            return TensorPrimitives.Norm(span);
        }
        #endregion

        #region OnesComplement
        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> OnesComplement<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            OnesComplement(x, output);
            return output;
        }

        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> OnesComplement<T>(scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(y, destination, TensorPrimitives.OnesComplement);
        }
        #endregion

        #region PopCount
        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> PopCount<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBinaryInteger<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            PopCount(x, output);
            return output;
        }

        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> PopCount<T>(scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(y, destination, TensorPrimitives.PopCount);
        }
        #endregion

        #region Pow
        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> Pow<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IPowerFunctions<T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Pow(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Pow<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IPowerFunctions<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Pow);
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input</param>
        public static Tensor<T> Pow<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IPowerFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);

            Pow(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Pow<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IPowerFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Pow);
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input</param>
        public static Tensor<T> Pow<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IPowerFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(y.Lengths);

            Pow(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Pow<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IPowerFunctions<T>
        {
            return ref TensorPrimitivesHelperTInSpanInSpanOut(x, y, destination, TensorPrimitives.Pow);
        }
        #endregion

        #region Product
        /// <summary>Computes the product of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Product<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return TensorPrimitivesHelperSpanInTOut(x, TensorPrimitives.Product);
        }
        #endregion

        #region RadiansToDegrees
        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> RadiansToDegrees<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            RadiansToDegrees(x, output);
            return output;
        }

        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> RadiansToDegrees<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.RadiansToDegrees);
        }
        #endregion

        #region Reciprocal
        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Reciprocal<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Reciprocal(x, output);
            return output;
        }

        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Reciprocal<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Reciprocal);
        }
        #endregion

        #region RootN
        /// <summary>Computes the element-wise n-th root of the values in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="n">The degree of the root to be computed, represented as a scalar.</param>
        public static Tensor<T> RootN<T>(in ReadOnlyTensorSpan<T> x, int n)
            where T : IRootFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            RootN(x, n, output);
            return output;
        }

        /// <summary>Computes the element-wise n-th root of the values in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="n">The degree of the root to be computed, represented as a scalar.</param>
        public static ref readonly TensorSpan<T> RootN<T>(scoped in ReadOnlyTensorSpan<T> x, int n, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            if (destination._shape._memoryLength < x._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.RootN(span, n, ospan);
            return ref destination;
        }
        #endregion

        #region RotateLeft
        /// <summary>Computes the element-wise rotation left of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static Tensor<T> RotateLeft<T>(in ReadOnlyTensorSpan<T> x, int rotateAmount)
            where T : IBinaryInteger<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            RotateLeft(x, rotateAmount, output);
            return output;
        }

        /// <summary>Computes the element-wise rotation left of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static ref readonly TensorSpan<T> RotateLeft<T>(scoped in ReadOnlyTensorSpan<T> x, int rotateAmount, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            if (destination._shape._memoryLength < x._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.RotateLeft(span, rotateAmount, ospan);
            return ref destination;
        }
        #endregion

        #region RotateRight
        /// <summary>Computes the element-wise rotation right of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static Tensor<T> RotateRight<T>(in ReadOnlyTensorSpan<T> x, int rotateAmount)
            where T : IBinaryInteger<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            RotateRight(x, rotateAmount, output);
            return output;
        }

        /// <summary>Computes the element-wise rotation right of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static ref readonly TensorSpan<T> RotateRight<T>(scoped in ReadOnlyTensorSpan<T> x, int rotateAmount, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            if (destination._shape._memoryLength < x._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.RotateRight(span, rotateAmount, ospan);
            return ref destination;
        }
        #endregion

        #region Round
        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Round<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Round(x, output);
            return output;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Round);
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="digits"></param>
        /// <param name="mode"></param>
        public static Tensor<T> Round<T>(in ReadOnlyTensorSpan<T> x, int digits, MidpointRounding mode)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Round(x, digits, mode, output);
            return output;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="digits"></param>
        /// <param name="mode"></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> x, int digits, MidpointRounding mode, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            if (destination._shape._memoryLength < x._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Round(span, digits, mode, ospan);
            return ref destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="digits"></param>
        public static Tensor<T> Round<T>(in ReadOnlyTensorSpan<T> x, int digits)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Round(x, digits, output);
            return output;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="digits"></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> x, int digits, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            if (destination._shape._memoryLength < x._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Round(span, digits, ospan);
            return ref destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="mode"></param>
        public static Tensor<T> Round<T>(in ReadOnlyTensorSpan<T> x, MidpointRounding mode)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Round(x, mode, output);
            return output;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="mode"></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> x, MidpointRounding mode, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            if (destination._shape._memoryLength < x._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Round(span, mode, ospan);
            return ref destination;
        }
        #endregion

        #region Sigmoid
        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Sigmoid<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Sigmoid(x, output);
            return output;
        }

        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sigmoid<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Sigmoid);
        }
        #endregion

        #region Sin
        /// <summary>
        /// Takes the sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Sin<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Sin(x, output);
            return output;
        }

        /// <summary>
        /// Takes the sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sin<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Sin);
        }
        #endregion

        #region Sinh
        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Sinh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Sinh(x, output);
            return output;
        }

        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sinh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Sinh);
        }
        #endregion

        #region SinPi
        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> SinPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            SinPi(x, output);
            return output;
        }

        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> SinPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.SinPi);
        }
        #endregion

        #region SoftMax
        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> SoftMax<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            SoftMax(x, output);
            return output;
        }

        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> SoftMax<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.SoftMax);
        }
        #endregion

        #region Sqrt
        /// <summary>
        /// Takes the square root of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the square root of.</param>
        public static Tensor<T> Sqrt<T>(in ReadOnlyTensorSpan<T> x)
            where T : IRootFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Sqrt(x, output);
            return output;
        }

        /// <summary>
        /// Takes the square root of each element of the <paramref name="x"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the square root of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sqrt<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Sqrt);
        }
        #endregion

        #region Subtract
        /// <summary>
        /// Subtracts <paramref name="y"/> from each element of <paramref name="x"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The <typeparamref name="T"/> to subtract.</param>
        public static Tensor<T> Subtract<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> output = Create<T>(x.Lengths);
            Subtract(x, y, output);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="y"/> from <paramref name="x"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <typeparamref name="T"/> to be subtracted from.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> of values to subtract.</param>
        public static Tensor<T> Subtract<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> output = Create<T>(y.Lengths);
            Subtract(x, y, output);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="x"/> from <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> with values to be subtracted from.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> with values to subtract.</param>
        public static Tensor<T> Subtract<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Subtract(x, y, output);
            return output;
        }

        /// <summary>
        /// Subtracts <paramref name="y"/> from each element of <paramref name="x"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> with values to be subtracted from.</param>
        /// <param name="y">The <typeparamref name="T"/> value to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Subtract);
        }

        /// <summary>
        /// Subtracts each element of <paramref name="y"/> from <paramref name="x"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <typeparamref name="T"/> value to be subtracted from.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> values to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTInSpanInSpanOut(x, y, destination, TensorPrimitives.Subtract);
        }

        /// <summary>
        /// Subtracts each element of <paramref name="x"/> from <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to be subtracted from.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/>of values to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Subtract);
        }
        #endregion

        #region Sum
        /// <summary>
        /// Sums the elements of the specified tensor.
        /// </summary>
        /// <param name="x">Tensor to sum</param>
        /// <returns></returns>
        public static T Sum<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape._memoryLength);
            return TensorPrimitives.Sum(span);
        }
        #endregion

        #region Tan
        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Tan<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Tan(x, output);
            return output;
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Tan<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Tan);
        }
        #endregion

        #region Tanh
        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Tanh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Tanh(x, output);
            return output;
        }

        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Tanh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Tanh);
        }
        #endregion

        #region TanPi
        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> TanPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            TanPi(x, output);
            return output;
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> TanPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.TanPi);
        }
        #endregion

        #region TrailingZeroCount
        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> TrailingZeroCount<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBinaryInteger<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            TrailingZeroCount(x, output);
            return output;
        }

        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> TrailingZeroCount<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.TrailingZeroCount);
        }
        #endregion

        #region Truncate
        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Truncate<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Truncate(x, output);
            return output;
        }

        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Truncate<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(x, destination, TensorPrimitives.Truncate);
        }
        #endregion

        #region Xor
        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Xor<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output;
            if (x.Lengths.SequenceEqual(y.Lengths))
            {
                output = Tensor.Create<T>(x.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(x.Lengths, y.Lengths));
            }

            Xor(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Xor<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(x, y, destination, TensorPrimitives.Xor);
        }

        /// <summary>
        /// Computes the element-wise Xor of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        public static Tensor<T> Xor<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output = Tensor.Create<T>(x.Lengths);
            Xor(x, y, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise Xor of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Xor<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(x, y, destination, TensorPrimitives.Xor);
        }
        #endregion

        public static nint[] GetSmallestBroadcastableLengths(ReadOnlySpan<nint> shape1, ReadOnlySpan<nint> shape2)
        {
            if (!TensorHelpers.IsBroadcastableTo(shape1, shape2))
                throw new Exception("Lengths are not broadcast compatible");

            nint[] intermediateShape = TensorHelpers.GetIntermediateShape(shape1, shape2.Length);
            for (int i = 1; i <= shape1.Length; i++)
            {
                intermediateShape[^i] = Math.Max(intermediateShape[^i], shape1[^i]);
            }
            for (int i = 1; i <= shape2.Length; i++)
            {
                intermediateShape[^i] = Math.Max(intermediateShape[^i], shape2[^i]);
            }

            return intermediateShape;
        }

        #region TensorPrimitivesHelpers
        private delegate void PerformCalculationSpanInSpanOut<T>(ReadOnlySpan<T> input, Span<T> output);

        private delegate void PerformCalculationSpanInTInSpanOut<T>(ReadOnlySpan<T> input, T value, Span<T> output);

        private delegate void PerformCalculationTInSpanInSpanOut<T>(T value, ReadOnlySpan<T> input, Span<T> output);

        private delegate void PerformCalculationTwoSpanInSpanOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo, Span<T> output);

        private delegate void PerformCalculationTFromSpanInTToSpanOut<TFrom, TTo>(ReadOnlySpan<TFrom> input, Span<TTo> output)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>;

        private delegate T PerformCalculationTwoSpanInTOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo);

        private delegate void PerformCalculationSpanInIntSpanOut<T>(ReadOnlySpan<T> input, Span<int> output);

        private delegate T PerformCalculationSpanInTOut<T>(ReadOnlySpan<T> input);

        private static T TensorPrimitivesHelperSpanInTOut<T>(scoped in ReadOnlyTensorSpan<T> input, PerformCalculationSpanInTOut<T> performCalculation)
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            return performCalculation(span);
        }

        private static ref readonly TensorSpan<int> TensorPrimitivesHelperSpanInIntSpanOut<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<int> destination, PerformCalculationSpanInIntSpanOut<T> performCalculation)
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<int> data = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            performCalculation(span, data);
            return ref destination;
        }

        private static T TensorPrimitivesHelperTwoSpanInTOut<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, PerformCalculationTwoSpanInTOut<T> performCalculation)
        {
            // If sizes are the same.
            if (TensorHelpers.AreLengthsTheSame(left, right) && TensorHelpers.IsUnderlyingStorageSameSize(left, right))
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left._shape._memoryLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right._shape._memoryLength);
                return performCalculation(span, rspan);
            }
            // Broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the right has column contiguous memory (i.e. a 1x5 and a 5x1)
                // Because we are returning a single T though we need to actual realize the broadcasts at this point to perform the calculations.

                nint[] newLengths = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                nint newLength = TensorSpanHelpers.CalculateTotalLength(newLengths);
                TensorSpan<T> broadcastedLeft = new TensorSpan<T>(new T[newLength], newLengths, ReadOnlySpan<nint>.Empty);
                TensorSpan<T> broadcastedRight = new TensorSpan<T>(new T[newLength], newLengths, ReadOnlySpan<nint>.Empty);
                BroadcastTo(left, broadcastedLeft);
                BroadcastTo(right, broadcastedRight);

                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref broadcastedLeft._reference, (int)broadcastedLeft.FlattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref broadcastedRight._reference, (int)broadcastedRight.FlattenedLength);
                return performCalculation(span, rspan);
            }
        }

        private static ref readonly TensorSpan<T> TensorPrimitivesHelperSpanInSpanOut<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination, PerformCalculationSpanInSpanOut<T> performCalculation)
        {
            if (destination._shape._memoryLength < input._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            performCalculation(span, ospan);
            return ref destination;
        }

        private static ref readonly TensorSpan<T> TensorPrimitivesHelperSpanInTInSpanOut<T>(scoped in ReadOnlyTensorSpan<T> input, T value, in TensorSpan<T> destination, PerformCalculationSpanInTInSpanOut<T> performCalculation)
        {
            if (destination._shape._memoryLength < input._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            performCalculation(span, value, ospan);
            return ref destination;
        }

        private static ref readonly TensorSpan<T> TensorPrimitivesHelperTInSpanInSpanOut<T>(T value, scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination, PerformCalculationTInSpanInSpanOut<T> performCalculation)
        {
            if (destination._shape._memoryLength < input._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            performCalculation(value, span, ospan);
            return ref destination;
        }

        private static ref readonly TensorSpan<TTo> TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(scoped in ReadOnlyTensorSpan<TFrom> input, in TensorSpan<TTo> destination, PerformCalculationTFromSpanInTToSpanOut<TFrom, TTo> performCalculation)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            ReadOnlySpan<TFrom> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<TTo> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            performCalculation(span, ospan);
            return ref destination;
        }

        private static ref readonly TensorSpan<T> TensorPrimitivesHelperTwoSpanInSpanOut<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination, PerformCalculationTwoSpanInSpanOut<T> performCalculation)
        {
            // If sizes are the same.
            if (TensorHelpers.AreLengthsTheSame(left, right) && TensorHelpers.IsUnderlyingStorageSameSize(left, right))
            {
                if (!TensorHelpers.IsUnderlyingStorageSameSize(left, destination))
                    ThrowHelper.ThrowArgument_DestinationTooShort();
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left._shape._memoryLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right._shape._memoryLength);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
                performCalculation(span, rspan, ospan);
                return ref destination;
            }
            // Broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the right has column contiguous memory (i.e. a 1x5 and a 5x1)

                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);

                ReadOnlyTensorSpan<T> broadcastedLeft = Tensor.LazyBroadcast(left, newSize);
                ReadOnlyTensorSpan<T> broadcastedRight = Tensor.LazyBroadcast(right, newSize);
                if (!destination.Lengths.SequenceEqual(newSize) || destination._shape._memoryLength < broadcastedLeft.FlattenedLength)
                    ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

                nint rowLength = newSize[^1];
                Span<T> ospan;
                ReadOnlySpan<T> ispan;
                Span<T> buffer = new T[rowLength];

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;
                if (newSize.Length > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(newSize.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[newSize.Length];
                }

                int outputOffset = 0;
                // ADD IN CASE WHERE NEITHER ARE ROW CONTIGUOUS
                // tensor not row contiguous
                if (broadcastedLeft.Strides[^1] == 0)
                {
                    while (outputOffset < destination.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination._reference, outputOffset), (int)rowLength);
                        buffer.Fill(broadcastedLeft[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref broadcastedRight._reference, TensorSpanHelpers.ComputeLinearIndex(curIndex, broadcastedRight.Strides, broadcastedRight.Lengths)), (int)rowLength);
                        performCalculation(buffer, ispan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndex, broadcastedLeft.Lengths);
                    }
                }
                // right not row contiguous
                else if (broadcastedRight.Strides[^1] == 0)
                {
                    while (outputOffset < destination.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination._reference, outputOffset), (int)rowLength);
                        buffer.Fill(broadcastedRight[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref broadcastedLeft._reference, TensorSpanHelpers.ComputeLinearIndex(curIndex, broadcastedLeft.Strides, broadcastedLeft.Lengths)), (int)rowLength);
                        performCalculation(ispan, buffer, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndex, broadcastedLeft.Lengths);
                    }
                }
                // both row contiguous
                else
                {
                    Span<T> rspan;
                    while (outputOffset < destination.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination._reference, outputOffset), (int)rowLength);
                        ispan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref broadcastedLeft._reference, TensorSpanHelpers.ComputeLinearIndex(curIndex, broadcastedLeft.Strides, broadcastedLeft.Lengths)), (int)rowLength);
                        rspan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref broadcastedRight._reference, TensorSpanHelpers.ComputeLinearIndex(curIndex, broadcastedRight.Strides, broadcastedRight.Lengths)), (int)rowLength);
                        performCalculation(ispan, rspan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndex, broadcastedLeft.Lengths);
                    }
                }

                if (curIndexArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexArray);
            }
            return ref destination;
        }

        #endregion

        #endregion
    }
}
