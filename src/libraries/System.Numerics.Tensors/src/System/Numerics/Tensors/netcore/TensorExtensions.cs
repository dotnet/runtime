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

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        #region AsTensorSpan
        /// <summary>
        /// Extension method to more easily create a TensorSpan from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array</typeparam>
        /// <param name="array">The <see cref="System.Array"/> with the data</param>
        /// <param name="shape">The shape for the <see cref="TensorSpan{T}"/></param>
        /// <returns></returns>
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array, params scoped ReadOnlySpan<nint> shape) => new(array, 0, shape, default);
        #endregion

        #region AsReadOnlySpan
        /// <summary>
        /// Extension method to more easily create a TensorSpan from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array</typeparam>
        /// <param name="array">The <see cref="System.Array"/> with the data</param>
        /// <param name="shape">The shape for the <see cref="TensorSpan{T}"/></param>
        /// <returns></returns>
        public static ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan<T>(this T[]? array, params scoped ReadOnlySpan<nint> shape) => new(array, 0, shape, default);
        #endregion

        #region Broadcast
        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the smallest broadcastable shape compatible with <paramref name="lengthsSource"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengthsSource">Other <see cref="Tensor{T}"/> to make shapes broadcastable.</param>
        public static Tensor<T> Broadcast<T>(scoped in ReadOnlyTensorSpan<T> input, scoped in ReadOnlyTensorSpan<T> lengthsSource)
        {
            return Broadcast(input, lengthsSource.Lengths);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the new shape <paramref name="lengths"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// If the shape of the <paramref name="input"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        public static Tensor<T> Broadcast<T>(scoped in ReadOnlyTensorSpan<T> input, scoped ReadOnlySpan<nint> lengths)
        {
            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(input.Lengths, lengths);

            ReadOnlyTensorSpan<T> intermediate = LazyBroadcast(input, newSize);
            Tensor<T> output = Tensor.CreateUninitialized<T>(intermediate.Lengths);
            intermediate.FlattenTo(MemoryMarshal.CreateSpan<T>(ref output._values[0], (int)output.FlattenedLength));
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
        /// <param name="axis">The axis along which the tensors will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
        public static Tensor<T> Concatenate<T>(scoped ReadOnlySpan<Tensor<T>> tensors, int axis = 0)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_ConcatenateTooFewTensors();

            if (axis < -1 || axis > tensors[0].Rank)
                ThrowHelper.ThrowArgument_InvalidAxis();

            // Calculate total space needed.
            nint totalLength = 0;
            for (int i = 0; i < tensors.Length; i++)
                totalLength += TensorSpanHelpers.CalculateTotalLength(tensors[i].Lengths);

            nint sumOfAxis = 0;
            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (axis != -1)
            {
                sumOfAxis = tensors[0].Lengths[axis];
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (tensors[0].Rank != tensors[i].Rank)
                        ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                    for (int j = 0; j < tensors[0].Rank; j++)
                    {
                        if (j != axis)
                        {
                            if (tensors[0].Lengths[j] != tensors[i].Lengths[j])
                                ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                        }
                    }
                    sumOfAxis += tensors[i].Lengths[axis];
                }
            }

            T[] values = tensors[0].IsPinned ? GC.AllocateArray<T>((int)totalLength, tensors[0].IsPinned) : (new T[totalLength]);
            Span<T> dstSpan = MemoryMarshal.CreateSpan(ref values[0], (int)totalLength);
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
                    copyLength = CalculateCopyLength(tensors[i].Lengths, axis);
                    Span<T> srcSpan = MemoryMarshal.CreateSpan(ref tensors[i]._values[srcIndex], (int)copyLength);
                    TensorSpanHelpers.Memmove(dstSpan, srcSpan, copyLength, valuesCopied);
                    valuesCopied += copyLength;
                }
                TensorSpanHelpers.AdjustIndexes(axis - 1, 1, curIndex, tensors[0].Lengths);
            }

            Tensor<T> tensor;
            if (axis == -1)
            {
                tensor = new Tensor<T>(values, [valuesCopied], tensors[0].IsPinned);
            }
            else
            {
                nint[] lengths = new nint[tensors[0].Rank];
                tensors[0].Lengths.CopyTo(lengths);
                lengths[axis] = sumOfAxis;
                tensor = new Tensor<T>(values, lengths, tensors[0].IsPinned);
            }

            if (curIndexArray != null)
                ArrayPool<nint>.Shared.Return(curIndexArray);

            return tensor;
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

        #region ElementwiseEqual
        /// <summary>
        /// Compares the elements of two <see cref="Tensor{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static Tensor<bool> ElementwiseEqual<T>(Tensor<T> left, Tensor<T> right)
            where T : IEqualityOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreLengthsTheSame<T>(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
            {
                result = Tensor.Create<bool>(left.Lengths, false);

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    result._values[i] = left._values[i] == right._values[i];
                }
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                result = Tensor.Create<bool>(newSize, false);
                Tensor<T> broadcastedLeft = LazyBroadcast(left, newSize);
                Tensor<T> broadcastedRight = LazyBroadcast(right, newSize);

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
                    result._values[i] = broadcastedLeft[curIndex] == broadcastedRight[curIndex];
                    TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
                }

                if (curIndexArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexArray);
            }

            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> ElementwiseEqual<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<bool> destination)
            where T : IEqualityOperators<T, T, bool>
        {
            Span<bool> result = MemoryMarshal.CreateSpan<bool>(ref destination._reference, (int)destination._shape._memoryLength);
            if (TensorHelpers.AreLengthsTheSame<T>(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
            {
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, left.Lengths))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;

                if (left.Rank > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(left.Lengths.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[left.Lengths.Length];
                }

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    result[i] = left[curIndex] == right[curIndex];
                    TensorSpanHelpers.AdjustIndexes(left.Rank - 1, 1, curIndex, left.Lengths);
                }

                if (curIndexArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexArray);
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, newSize))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));

                ReadOnlyTensorSpan<T> broadcastedLeft = LazyBroadcast(left, newSize);
                ReadOnlyTensorSpan<T> broadcastedRight = LazyBroadcast(right, newSize);

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
                    result[i] = broadcastedLeft[curIndex] == broadcastedRight[curIndex];
                    TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
                }

                if (curIndexArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexArray);
            }

            return ref destination;
        }
        #endregion

        #region FilteredUpdate
        //  REVIEW: PYTORCH/NUMPY DO THIS.
        //  t0[t0 < 2] = -1;
        //  OR SHOULD THIS BE AN OVERLOAD OF FILL THAT TAKES IN A FUNC TO KNOW WHICH ONE TO UPDATE?
        /// <summary>
        /// Updates the <paramref name="tensor"/> tensor with the <paramref name="value"/> where the <paramref name="filter"/> is true.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="filter">Input filter where if the index is true then it will update the <paramref name="tensor"/>.</param>
        /// <param name="value">Value to update in the <paramref name="tensor"/>.</param>
        public static TensorSpan<T> FilteredUpdate<T>(in this TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<bool> filter, T value)
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

            return tensor;
        }

        /// <summary>
        /// Updates the <paramref name="tensor"/> tensor with the <paramref name="values"/> where the <paramref name="filter"/> is true.
        /// If dmesions are not the same an exception is thrown.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="filter">Input filter where if the index is true then it will update the <paramref name="tensor"/>.</param>
        /// <param name="values">Values to update in the <paramref name="tensor"/>.</param>
        public static TensorSpan<T> FilteredUpdate<T>(in this TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<bool> filter, scoped in ReadOnlyTensorSpan<T> values)
        {
            if (filter.Lengths.Length != tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(filter));
            if (values.Rank != 1)
                ThrowHelper.ThrowArgument_1DTensorRequired(nameof(values));

            nint numTrueElements = TensorHelpers.CountTrueElements(filter);
            if (numTrueElements != values._shape._memoryLength)
                ThrowHelper.ThrowArgument_IncorrectNumberOfFilterItems(nameof(values));

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

            return tensor;
        }
        #endregion

        #region GreaterThan
        /// <summary>
        /// Compares the elements of two <see cref="Tensor{T}"/> to see which elements of <paramref name="left"/> are greater than <paramref name="right"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="left"/> are greater than <paramref name="right"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="left"/> are greater than <paramref name="right"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> GreaterThan<T>(Tensor<T> left, Tensor<T> right)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreLengthsTheSame<T>(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
            {
                result = Tensor.Create<bool>(left.Lengths, false);

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    result._values[i] = left._values[i] > right._values[i];
                }
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                result = Tensor.Create<bool>(newSize, false);
                Tensor<T> broadcastedLeft = LazyBroadcast(left, newSize);
                Tensor<T> broadcastedRight = LazyBroadcast(right, newSize);

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
                    result._values[i] = broadcastedLeft[curIndex] > broadcastedRight[curIndex];
                    TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
                }

                if (curIndexArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexArray);
            }

            return result;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are greater than <paramref name="right"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="left"/> are greater than <paramref name="right"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="left"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right"><typeparamref name="T"/> to compare against <paramref name="left"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="left"/> are greater than <paramref name="right"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThan<T>(Tensor<T> left, T right)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(left.Lengths, false);

            for (int i = 0; i < left._values.Length; i++)
            {
                result._values[i] = left._values[i] > right;
            }
            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="Tensor{T}"/> to see if any elements of <paramref name="left"/> are greater than <paramref name="right"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="left"/> are greater than <paramref name="right"/>.
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="left"/> are greater than <paramref name="right"/>.</returns>
        public static bool GreaterThanAny<T>(Tensor<T> left, Tensor<T> right)
            where T : IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreLengthsTheSame<T>(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
            {

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    if (left._values[i] > right._values[i])
                        return true;
                }
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                Tensor<T> broadcastedLeft = LazyBroadcast(left, newSize);
                Tensor<T> broadcastedRight = LazyBroadcast(right, newSize);

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
            }
            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="Tensor{T}"/> to see if all elements of <paramref name="left"/> are greater than <paramref name="right"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="left"/> are greater than <paramref name="right"/>.
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="left"/> are greater than <paramref name="right"/>.</returns>
        public static bool GreaterThanAll<T>(Tensor<T> left, Tensor<T> right)
            where T : IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreLengthsTheSame<T>(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
            {

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    if (left._values[i] < right._values[i])
                        return false;
                }
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                Tensor<T> broadcastedLeft = LazyBroadcast(left, newSize);
                Tensor<T> broadcastedRight = LazyBroadcast(right, newSize);

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
                        return false;
                    TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
                }

                if (curIndexArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexArray);
            }
            return true;
        }
        #endregion

        #region LessThan
        /// <summary>
        /// Compares the elements of two <see cref="Tensor{T}"/> to see which elements of <paramref name="left"/> are less than <paramref name="right"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="left"/> are less than <paramref name="right"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="left"/> are less than <paramref name="right"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> LessThan<T>(Tensor<T> left, Tensor<T> right)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreLengthsTheSame<T>(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
            {
                result = Tensor.Create<bool>(left.Lengths, false);

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    result._values[i] = left._values[i] < right._values[i];
                }
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                result = Tensor.Create<bool>(newSize, false);
                Tensor<T> broadcastedLeft = LazyBroadcast(left, newSize);
                Tensor<T> broadcastedRight = LazyBroadcast(right, newSize);

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
                    result._values[i] = broadcastedLeft[curIndex] < broadcastedRight[curIndex];
                    TensorSpanHelpers.AdjustIndexes(broadcastedRight.Rank - 1, 1, curIndex, broadcastedRight.Lengths);
                }

                if (curIndexArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexArray);
            }

            return result;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="right"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="left"/> are less than <paramref name="right"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="left"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right"><typeparamref name="T"/> to compare against <paramref name="left"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="left"/> are less than <paramref name="right"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThan<T>(Tensor<T> left, T right)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(left.Lengths, false);

            for (int i = 0; i < left._values.Length; i++)
            {
                result._values[i] = left._values[i] < right;
            }
            return result;
        }

        /// <summary>
        /// Compares the elements of two <see cref="Tensor{T}"/> to see if any elements of <paramref name="left"/> are less than <paramref name="right"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="left"/> are less than <paramref name="right"/>.
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="left"/> are less than <paramref name="right"/>.</returns>
        public static bool LessThanAny<T>(Tensor<T> left, Tensor<T> right)
            where T : IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreLengthsTheSame<T>(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
            {
                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    if (left._values[i] < right._values[i])
                        return true;
                }
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                Tensor<T> broadcastedLeft = LazyBroadcast(left, newSize);
                Tensor<T> broadcastedRight = LazyBroadcast(right, newSize);

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
            }

            return false;
        }

        /// <summary>
        /// Compares the elements of two <see cref="Tensor{T}"/> to see if all elements of <paramref name="left"/> are less than <paramref name="right"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="left"/> are less than <paramref name="right"/>.
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="left"/> are less than <paramref name="right"/>.</returns>
        public static bool LessThanAll<T>(Tensor<T> left, Tensor<T> right)
            where T : IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreLengthsTheSame<T>(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
            {
                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    if (left._values[i] > right._values[i])
                        return false;
                }
            }
            else
            {
                nint[] newSize = Tensor.GetSmallestBroadcastableLengths(left.Lengths, right.Lengths);
                Tensor<T> broadcastedLeft = LazyBroadcast(left, newSize);
                Tensor<T> broadcastedRight = LazyBroadcast(right, newSize);

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
            }
            return true;
        }
        #endregion

        #region Mean
        /// <summary>
        /// Returns the mean of the elements in the <paramref name="input"/> tensor.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the mean of.</param>
        /// <returns><typeparamref name="T"/> representing the mean.</returns>
        public static T Mean<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : IFloatingPoint<T>
        {
            T sum = Sum(input);
            return T.CreateChecked(sum / T.CreateChecked(input._shape._memoryLength));
        }
        #endregion

        #region Permute

        /// <summary>
        /// Swaps the dimensions of the <paramref name="input"/> tensor according to the <paramref name="axis"/> parameter.
        /// If <paramref name="input"/> is a 1D tensor, it will return <paramref name="input"/>. Otherwise it creates a new <see cref="Tensor{T}"/>
        /// with the new axis ordering by allocating new memory.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/></param>
        /// <param name="axis"><see cref="ReadOnlySpan{T}"/> with the new axis ordering.</param>
        public static Tensor<T> Permute<T>(this Tensor<T> input, params ReadOnlySpan<int> axis)
        {
            if (input.Rank == 1)
            {
                return input;
            }
            else
            {
                T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input._flattenedLength) : (new T[input._flattenedLength]);
                nint[] lengths = new nint[input.Rank];
                Tensor<T> tensor;
                TensorSpan<T> ospan;
                TensorSpan<T> ispan;
                ReadOnlySpan<int> permutation;

                if (axis.IsEmpty)
                {
                    lengths = input._lengths.Reverse().ToArray();
                    permutation = Enumerable.Range(0, input.Rank).Reverse().ToArray();
                }
                else
                {
                    if (axis.Length != input.Lengths.Length)
                        ThrowHelper.ThrowArgument_PermuteAxisOrder();
                    for (int i = 0; i < lengths.Length; i++)
                        lengths[i] = input.Lengths[axis[i]];
                    permutation = axis.ToArray();
                }
                tensor = new Tensor<T>(values, lengths, Array.Empty<nint>(), input._isPinned);

                ospan = tensor.AsTensorSpan();
                ispan = input.AsTensorSpan();

                scoped Span<nint> indexes;
                nint[]? indicesArray;
                scoped Span<nint> permutedIndices;
                nint[]? permutedIndicesArray;
                if (tensor.Rank > 6)
                {
                    indicesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                    indexes = indicesArray;
                    permutedIndicesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                    permutedIndices = permutedIndicesArray;
                }
                else
                {
                    indicesArray = null;
                    indexes = stackalloc nint[tensor.Rank];
                    permutedIndicesArray = null;
                    permutedIndices = stackalloc nint[tensor.Rank];
                }

                for (int i = 0; i < input._flattenedLength; i++)
                {
                    TensorHelpers.PermuteIndices(indexes, permutedIndices, permutation);
                    ospan[permutedIndices] = ispan[indexes];
                    TensorSpanHelpers.AdjustIndexes(tensor.Rank - 1, 1, indexes, input._lengths);
                }

                if (indicesArray != null && permutedIndicesArray != null)
                {
                    ArrayPool<nint>.Shared.Return(indicesArray);
                    ArrayPool<nint>.Shared.Return(permutedIndicesArray);
                }

                return tensor;
            }
        }
        #endregion

        #region Reshape
        // REVIEW: SENTINAL VALUE? CONSTANT VALUE FOR -1 WILDCARD?
        /// <summary>
        /// Reshapes the <paramref name="input"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="input"><see cref="Tensor{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static Tensor<T> Reshape<T>(this Tensor<T> input, params ReadOnlySpan<nint> lengths)
        {
            nint[] arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = input._flattenedLength;
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
            if (tempLinear != input.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
            nint[] strides = TensorSpanHelpers.CalculateStrides(arrLengths);
            return new Tensor<T>(input._values, arrLengths, strides);
        }

        /// <summary>
        /// Reshapes the <paramref name="input"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="input"><see cref="TensorSpan{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static TensorSpan<T> Reshape<T>(this in TensorSpan<T> input, params scoped ReadOnlySpan<nint> lengths)
        {
            nint[] arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = input.FlattenedLength;
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
            if (tempLinear != input.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
            nint[] strides = TensorSpanHelpers.CalculateStrides(arrLengths);
            TensorSpan<T> output = new TensorSpan<T>(ref input._reference, arrLengths, strides, input._shape._memoryLength);
            return output;
        }

        /// <summary>
        /// Reshapes the <paramref name="input"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="input"><see cref="TensorSpan{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static ReadOnlyTensorSpan<T> Reshape<T>(this in ReadOnlyTensorSpan<T> input, params scoped ReadOnlySpan<nint> lengths)
        {
            nint[] arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = input.FlattenedLength;
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
            if (tempLinear != input.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
            nint[] strides = TensorSpanHelpers.CalculateStrides(arrLengths);
            ReadOnlyTensorSpan<T> output = new ReadOnlyTensorSpan<T>(ref input._reference, arrLengths, strides, input._shape._memoryLength);
            return output;
        }
        #endregion

        #region Resize
        /// <summary>
        /// Creates a new <see cref="Tensor{T}"/>, allocates new memory, and copies the data from <paramref name="input"/>. If the final shape is smaller all data after
        /// that point is ignored.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        public static Tensor<T> Resize<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
        {
            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)newSize) : (new T[newSize]);
            Tensor<T> output = new Tensor<T>(values, shape, false);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input.AsTensorSpan()._reference, (int)input._values.Length);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsTensorSpan()._reference, (int)output.FlattenedLength);
            if (newSize > input._values.Length)
                TensorSpanHelpers.Memmove(ospan, span, input._values.Length);
            else
                TensorSpanHelpers.Memmove(ospan, span, newSize);

            return output;
        }

        /// <summary>
        /// Copies the data from <paramref name="input"/>. If the final shape is smaller all data after that point is ignored.
        /// If the final shape is bigger it is filled with 0s.
        /// </summary>
        /// <param name="input">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/> with the desired new shape.</param>
        public static ref readonly TensorSpan<T> Resize<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            if (destination._shape._memoryLength > input._shape._memoryLength)
                TensorSpanHelpers.Memmove(ospan, span, input._shape._memoryLength);
            else
                TensorSpanHelpers.Memmove(ospan, span, destination._shape._memoryLength);

            return ref destination;
        }
        #endregion

        #region Reverse
        /// <summary>
        /// Reverse the order of elements in the <paramref name="input"/> along the given axis. The shape of the tensor is preserved, but the elements are reordered.
        /// <paramref name="axis"/> defaults to -1 when not provided, which reverses the entire tensor.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="axis">Axis along which to reverse over. The default, -1, will reverse over all of the axes of the left tensor.</param>
        public static Tensor<T> Reverse<T>(in ReadOnlyTensorSpan<T> input, nint axis = -1)
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Reverse<T>(input, output, axis);

            return output;
        }

        /// <summary>
        /// Reverse the order of elements in the <paramref name="input"/> along the given axis. The shape of the tensor is preserved, but the elements are reordered.
        /// <paramref name="axis"/> defaults to -1 when not provided, which reverses the entire span.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        /// <param name="axis">Axis along which to reverse over. The default, -1, will reverse over all of the axes of the left span.</param>
        public static ref readonly TensorSpan<T> Reverse<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination, nint axis = -1)
        {
            if (axis == -1)
            {
                nint index = input._shape._memoryLength - 1;
                Span<T> inputSpan = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
                Span<T> outputSpan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
                for (int i = 0; i <= input._shape._memoryLength / 2; i++)
                {
                    outputSpan[i] = inputSpan[(int)index];
                    outputSpan[(int)index--] = inputSpan[i];
                }
            }
            else
            {
                nint copyLength = 1;
                for (nint i = axis; i < input.Lengths.Length; i++)
                {
                    copyLength *= input.Lengths[(int)i];
                }
                copyLength /= input.Lengths[(int)axis];

                scoped Span<nint> oIndices;
                nint[]? oIndicesArray;
                scoped Span<nint> iIndices;
                nint[]? iIndicesArray;
                if (input.Rank > 6)
                {
                    oIndicesArray = ArrayPool<nint>.Shared.Rent(input.Rank);
                    oIndices = oIndicesArray;
                    iIndicesArray = ArrayPool<nint>.Shared.Rent(input.Rank);
                    iIndices = iIndicesArray;
                }
                else
                {
                    oIndicesArray = null;
                    oIndices = stackalloc nint[input.Rank];
                    iIndicesArray = null;
                    iIndices = stackalloc nint[input.Rank];
                }

                iIndices[(int)axis] = input.Lengths[(int)axis] - 1;
                nint copiedValues = 0;
                ReadOnlyTensorSpan<T> islice = input.Slice(input.Lengths);

                while (copiedValues < input.FlattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref destination._reference, TensorSpanHelpers.ComputeLinearIndex(oIndices, input.Strides, input.Lengths)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes((int)axis, 1, oIndices, input.Lengths);
                    TensorSpanHelpers.AdjustIndexesDown((int)axis, 1, iIndices, input.Lengths);
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
        public static bool SequenceEqual<T>(this scoped in ReadOnlyTensorSpan<T> span, scoped in ReadOnlyTensorSpan<T> other)
        {
            return span.FlattenedLength == other.FlattenedLength
                && MemoryMarshal.CreateReadOnlySpan(in span.GetPinnableReference(), (int)span._shape._memoryLength).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(in other.GetPinnableReference(), (int)other._shape._memoryLength));
        }
        #endregion

        #region SetSlice
        // REVIEW: WHAT DO WE WANT TO CALL THIS? COPYTO? IT DOES FIT IN WITH THE EXISTING COPY TO CONVENTIONS FOR VECTOR (albeit backwards).
        /// <summary>
        /// Sets a slice of the given <paramref name="tensor"/> with the provided <paramref name="values"/> for the given <paramref name="ranges"/>
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="values">The values you want to set in the <paramref name="tensor"/>.</param>
        /// <param name="ranges">The ranges you want to set.</param>
        public static Tensor<T> SetSlice<T>(this Tensor<T> tensor, in ReadOnlyTensorSpan<T> values, params ReadOnlySpan<NRange> ranges)
        {
            SetSlice<T>((TensorSpan<T>)tensor, values, ranges);

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
        /// Split a <see cref="Tensor{T}"/> into <paramref name="numSplits"/> along the given <paramref name="axis"/>. If the tensor cannot be split
        /// evenly on the given <paramref name="axis"/> an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="numSplits">How many times to split the <paramref name="input"/></param>
        /// <param name="axis">The axis to split on.</param>
        public static Tensor<T>[] Split<T>(scoped in ReadOnlyTensorSpan<T> input, nint numSplits, nint axis)
        {
            if (input.Lengths[(int)axis] % numSplits != 0)
                ThrowHelper.ThrowArgument_SplitNotSplitEvenly();

            Tensor<T>[] outputs = new Tensor<T>[numSplits];

            nint totalToCopy = input.FlattenedLength / numSplits;
            nint copyLength = 1;
            for (nint i = axis; i < input.Lengths.Length; i++)
            {
                copyLength *= input.Lengths[(int)i];
            }
            copyLength /= numSplits;
            nint[] newShape = input.Lengths.ToArray();
            newShape[(int)axis] = newShape[(int)axis] / numSplits;

            scoped Span<nint> oIndices;
            nint[]? oIndicesArray;
            scoped Span<nint> iIndices;
            nint[]? iIndicesArray;
            if (input.Rank > 6)
            {
                oIndicesArray = ArrayPool<nint>.Shared.Rent(input.Rank);
                oIndices = oIndicesArray;
                iIndicesArray = ArrayPool<nint>.Shared.Rent(input.Rank);
                iIndices = iIndicesArray;
            }
            else
            {
                oIndicesArray = null;
                oIndices = stackalloc nint[input.Rank];
                iIndicesArray = null;
                iIndices = stackalloc nint[input.Rank];
            }

            for (int i = 0; i < outputs.Length; i++)
            {
                T[] values = new T[(int)totalToCopy];
                outputs[i] = new Tensor<T>(values, newShape);
                oIndices.Clear();
                iIndices.Clear();

                iIndices[(int)axis] = i;
                ReadOnlyTensorSpan<T> islice = input.Slice(input.Lengths);
                TensorSpan<T> oslice = outputs[i].AsTensorSpan().Slice(outputs[i]._lengths);

                nint copiedValues = 0;
                while (copiedValues < totalToCopy)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, TensorSpanHelpers.ComputeLinearIndex(oIndices, outputs[0].Strides, outputs[0].Lengths)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes((int)axis, 1, oIndices, outputs[i]._lengths);
                    TensorSpanHelpers.AdjustIndexes((int)axis - 1, 1, iIndices, input.Lengths);
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
        // REVIEW: NAME?
        /// <summary>
        /// Removes axis of length one from the <paramref name="input"/>. <paramref name="axis"/> defaults to -1 and will remove all axis with length of 1.
        /// If <paramref name="axis"/> is specified, it will only remove that axis and if it is not of length one it will throw an exception.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to remove. Defaults to -1 which removes all axis of length 1.</param>
        public static Tensor<T> Squeeze<T>(this Tensor<T> input, int axis = -1)
        {
            if (axis >= input.Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            nint[] lengths;
            nint[] strides;

            List<nint> tempLengths = new List<nint>();
            if (axis == -1)
            {
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (input.Lengths[i] != 1)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }
            else
            {
                if (input.Lengths[axis] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (i != axis)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }

            return new Tensor<T>(input._values, lengths, strides);
        }

        /// <summary>
        /// Removes axis of length one from the <paramref name="input"/>. <paramref name="axis"/> defaults to -1 and will remove all axis with length of 1.
        /// If <paramref name="axis"/> is specified, it will only remove that axis and if it is not of length one it will throw an exception.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to remove. Defaults to -1 which removes all axis of length 1.</param>
        public static TensorSpan<T> Squeeze<T>(in this TensorSpan<T> input, int axis = -1)
        {
            if (axis >= input.Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            nint[] lengths;
            nint[] strides;

            List<nint> tempLengths = new List<nint>();
            if (axis == -1)
            {
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (input.Lengths[i] != 1)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }
            else
            {
                if (input.Lengths[axis] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (i != axis)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }

            return new TensorSpan<T>(ref input._reference, lengths, strides, input._shape._memoryLength);
        }

        /// <summary>
        /// Removes axis of length one from the <paramref name="input"/>. <paramref name="axis"/> defaults to -1 and will remove all axis with length of 1.
        /// If <paramref name="axis"/> is specified, it will only remove that axis and if it is not of length one it will throw an exception.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to remove. Defaults to -1 which removes all axis of length 1.</param>
        public static ReadOnlyTensorSpan<T> Squeeze<T>(in this ReadOnlyTensorSpan<T> input, int axis = -1)
        {
            if (axis >= input.Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            nint[] lengths;
            nint[] strides;

            List<nint> tempLengths = new List<nint>();
            if (axis == -1)
            {
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (input.Lengths[i] != 1)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }
            else
            {
                if (input.Lengths[axis] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (i != axis)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }

            return new ReadOnlyTensorSpan<T>(ref input._reference, lengths, strides, input._shape._memoryLength);
        }
        #endregion

        #region Stack
        /// <summary>
        /// Join an array of <see cref="Tensor{T}"/> along a new axis. The axis parameter specifies the index of the new axis in the dimensions of the result and
        /// defaults to 0. All tensors must have the same shape.
        /// </summary>
        /// <param name="input">Array of <see cref="Tensor{T}"/>.</param>
        /// <param name="axis">Index of where the new axis will be. Defaults to 0.</param>
        public static Tensor<T> Stack<T>(ReadOnlySpan<Tensor<T>> input, int axis = 0)
        {
            if (input.Length < 2)
                ThrowHelper.ThrowArgument_StackTooFewTensors();

            for (int i = 1; i < input.Length; i++)
            {
                if (!TensorHelpers.AreLengthsTheSame<T>(input[0], input[i]))
                    ThrowHelper.ThrowArgument_StackShapesNotSame();
            }

            if (axis < 0)
                axis = input[0].Rank - axis;

            Tensor<T>[] outputs = new Tensor<T>[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                outputs[i] = Tensor.Unsqueeze(input[0], axis);
            }
            return Tensor.Concatenate<T>(outputs, axis);
        }
        #endregion

        #region StdDev
        /// <summary>
        /// Returns the standard deviation of the elements in the <paramref name="input"/> tensor.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the standard deviation of.</param>
        /// <returns><typeparamref name="T"/> representing the standard deviation.</returns>
        public static T StdDev<T>(in ReadOnlyTensorSpan<T> input)
            where T : IFloatingPoint<T>, IPowerFunctions<T>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            T mean = Mean(input);
            Span<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> output = new T[input.FlattenedLength];
            TensorPrimitives.Subtract(span, mean, output);
            TensorPrimitives.Abs(output, output);
            TensorPrimitives.Pow((ReadOnlySpan<T>)output, T.CreateChecked(2), output);
            T sum = TensorPrimitives.Sum((ReadOnlySpan<T>)output);
            return T.CreateChecked(sum / T.CreateChecked(input._shape._memoryLength));
        }
        #endregion

        #region ToString
        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="span">The <see cref="TensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="span"/></returns>
        public static string ToString<T>(this in TensorSpan<T> span, params ReadOnlySpan<nint> maximumLengths) => ((ReadOnlyTensorSpan<T>)span).ToString(maximumLengths);

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="ReadOnlyTensorSpan{T}"/>."/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The <see cref="ReadOnlyTensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        public static string ToString<T>(this in ReadOnlyTensorSpan<T> span, params ReadOnlySpan<nint> maximumLengths)
        {
            var sb = new StringBuilder();
            scoped Span<nint> curIndexes;
            nint[]? curIndexesArray;
            if (span.Rank > 6)
            {
                curIndexesArray = ArrayPool<nint>.Shared.Rent(span.Rank);
                curIndexes = curIndexesArray;
            }
            else
            {
                curIndexesArray = null;
                curIndexes = stackalloc nint[span.Rank];
            }

            nint copiedValues = 0;

            T[] values = new T[span.Lengths[span.Rank - 1]];
            while (copiedValues < span.FlattenedLength)
            {
                var sp = new ReadOnlyTensorSpan<T>(ref Unsafe.Add(ref span._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, span.Strides, span.Lengths)), [span.Lengths[span.Rank - 1]], [1], span.Lengths[span.Rank - 1]);
                sb.Append('{');
                sp.FlattenTo(values);
                sb.Append(string.Join(",", values));
                sb.AppendLine("}");

                TensorSpanHelpers.AdjustIndexes(span.Rank - 2, 1, curIndexes, span.Lengths);
                copiedValues += span.Lengths[span.Rank - 1];
            }

            if (curIndexesArray != null)
                ArrayPool<nint>.Shared.Return(curIndexesArray);

            return sb.ToString();
        }
        #endregion

        #region Transpose
        /// <summary>
        /// Swaps the last two dimensions of the <paramref name="input"/> tensor.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Transpose<T>(Tensor<T> input)
        {
            if (input.Lengths.Length < 2)
                ThrowHelper.ThrowArgument_TransposeTooFewDimensions();
            int[] axis = Enumerable.Range(0, input.Rank).ToArray();
            int temp = axis[input.Rank - 1];
            axis[input.Rank - 1] = axis[input.Rank - 2];
            axis[input.Rank - 2] = temp;
            return Permute(input, axis.AsSpan());
        }
        #endregion

        #region TryBroadcastTo
        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(this Tensor<T> input, in TensorSpan<T> destination)
        {
            return TryBroadcastTo((ReadOnlyTensorSpan<T>)input, destination);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(in this TensorSpan<T> input, in TensorSpan<T> destination)
        {
            return TryBroadcastTo((ReadOnlyTensorSpan<T>)input, destination);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="input">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(in this ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
        {
            if (!TensorHelpers.IsBroadcastableTo(input.Lengths, destination.Lengths))
                return false;

            nint[] newSize = Tensor.GetSmallestBroadcastableLengths(input.Lengths, destination.Lengths);
            if (!TensorHelpers.AreLengthsTheSame(destination.Lengths, newSize))
                return false;

            LazyBroadcast(input, newSize).CopyTo(destination);
            return true;
        }
        #endregion

        #region Unsqueeze
        // REVIEW: NAME? NUMPY CALLS THIS expand_dims.
        /// <summary>
        /// Insert a new axis of length 1 that will appear at the axis position.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to add.</param>
        public static Tensor<T> Unsqueeze<T>(this Tensor<T> input, int axis)
        {
            if (axis > input.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (axis < 0)
                axis = input.Rank - axis;

            List<nint> tempLengths = input._lengths.ToList();
            tempLengths.Insert(axis, 1);
            nint[] lengths = tempLengths.ToArray();
            nint[] strides = TensorSpanHelpers.CalculateStrides(lengths);
            return new Tensor<T>(input._values, lengths, strides);
        }

        // REVIEW: NAME? NUMPY CALLS THIS expand_dims.
        /// <summary>
        /// Insert a new axis of length 1 that will appear at the axis position.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to add.</param>
        public static TensorSpan<T> Unsqueeze<T>(in this TensorSpan<T> input, int axis)
        {
            if (axis > input.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (axis < 0)
                axis = input.Rank - axis;

            List<nint> tempLengths = input.Lengths.ToArray().ToList();
            tempLengths.Insert(axis, 1);
            nint[] lengths = tempLengths.ToArray();
            nint[] strides = TensorSpanHelpers.CalculateStrides(lengths);
            return new TensorSpan<T>(ref input._reference, lengths, strides, input._shape._memoryLength);
        }

        // REVIEW: NAME? NUMPY CALLS THIS expand_dims.
        /// <summary>
        /// Insert a new axis of length 1 that will appear at the axis position.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to add.</param>
        public static ReadOnlyTensorSpan<T> Unsqueeze<T>(in this ReadOnlyTensorSpan<T> input, int axis)
        {
            if (axis > input.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (axis < 0)
                axis = input.Rank - axis;

            List<nint> tempLengths = input.Lengths.ToArray().ToList();
            tempLengths.Insert(axis, 1);
            nint[] lengths = tempLengths.ToArray();
            nint[] strides = TensorSpanHelpers.CalculateStrides(lengths);
            return new ReadOnlyTensorSpan<T>(ref input._reference, lengths, strides, input._shape._memoryLength);
        }
        #endregion

        #region TensorPrimitives
        #region Abs
        /// <summary>
        /// Takes the absolute value of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the abs of.</param>
        public static Tensor<T> Abs<T>(Tensor<T> input)
            where T : INumberBase<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Abs<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the absolute value of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the abs of.</param>
        /// <param name="destination">The <see cref="TensorSpan{T}"/> destination.</param>
        public static ref readonly TensorSpan<T> Abs<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : INumberBase<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Abs);
        }
        #endregion

        #region Acos
        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Acos<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Acos<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Acos<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Acos);
        }
        #endregion

        #region Acosh
        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Acosh<T>(Tensor<T> input)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Acosh<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Acosh<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Acosh);
        }
        #endregion

        #region AcosPi
        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AcosPi<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            AcosPi<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AcosPi<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.AcosPi);
        }
        #endregion

        #region Add
        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="right">The second <see cref="Tensor{T}"/> of values to add.</param>
        public static Tensor<T> Add<T>(Tensor<T> left, Tensor<T> right)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Add<T>(left, right, output);
            return output;
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> to add to each element of <paramref name="input"/>.</param>
        public static Tensor<T> Add<T>(Tensor<T> input, T val)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Add<T>(input, val, output);
            return output;
        }

        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> and returns a new <see cref="ReadOnlyTensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="right">The second <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Add<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Add);
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> to add to each element of <paramref name="input"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Add<T>(scoped in ReadOnlyTensorSpan<T> input, T val, in TensorSpan<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(input, val, destination, TensorPrimitives.Add);
        }
        #endregion

        #region Asin
        /// <summary>
        /// Takes the inverse sin of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Asin<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Asin<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Asin<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Asin);
        }
        #endregion

        #region Asinh
        /// <summary>
        /// Takes the inverse hyperbolic sine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Asinh<T>(Tensor<T> input)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Asinh<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Asinh<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Asinh);
        }
        #endregion

        #region AsinPi
        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AsinPi<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            AsinPi<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AsinPi<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.AsinPi);
        }
        #endregion

        #region Atan
        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/></param>
        public static Tensor<T> Atan<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Atan<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Atan);
        }
        #endregion

        #region Atan2
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Atan2<T>(Tensor<T> left, Tensor<T> right)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Atan2<T>(left, right, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Atan2);
        }
        #endregion

        #region Atan2Pi
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="Tensor{T}"/>, divides each element by pi, and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Atan2Pi<T>(Tensor<T> left, Tensor<T> right)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Atan2Pi<T>(left, right, output);
            return output;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2Pi<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Atan2Pi);
        }
        #endregion

        #region Atanh
        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Atanh<T>(Tensor<T> input)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Atanh<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atanh<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Atanh);
        }
        #endregion

        #region AtanPi
        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input<see cref="Tensor{T}"/>.</param>
        public static Tensor<T> AtanPi<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            AtanPi<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input<see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AtanPi<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.AtanPi);
        }
        #endregion

        #region BitwiseAnd
        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> BitwiseAnd<T>(Tensor<T> left, Tensor<T> right)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            BitwiseAnd<T>(left, right, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseAnd<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.BitwiseAnd);
        }
        #endregion

        #region BitwiseOr
        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> BitwiseOr<T>(Tensor<T> left, Tensor<T> right)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            BitwiseOr<T>(left, right, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseOr<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.BitwiseOr);
        }
        #endregion

        #region CubeRoot
        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The left <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Cbrt<T>(Tensor<T> input)
            where T : IRootFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Cbrt<T>(input, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cbrt<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Cbrt);
        }
        #endregion

        #region Ceiling
        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The left <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Ceiling<T>(Tensor<T> input)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Ceiling<T>(input, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ceiling<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Ceiling);
        }
        #endregion

        #region ConvertChecked
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="Tensor{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<TTo> ConvertChecked<TFrom, TTo>(Tensor<TFrom> source)
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
            return ref TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(source, destination, TensorPrimitives.ConvertChecked);
        }
        #endregion

        #region ConvertSaturating
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="Tensor{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<TTo> ConvertSaturating<TFrom, TTo>(Tensor<TFrom> source)
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
            return ref TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(source, destination, TensorPrimitives.ConvertSaturating);
        }
        #endregion

        #region ConvertTruncating
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="Tensor{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<TTo> ConvertTruncating<TFrom, TTo>(Tensor<TFrom> source)
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
            return ref TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(source, destination, TensorPrimitives.ConvertTruncating);
        }
        #endregion

        #region CopySign
        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new tensor with the result.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        public static Tensor<T> CopySign<T>(Tensor<T> input, T sign)
            where T : INumber<T>
        {
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);

            CopySign<T>(input, sign, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="sign">The <see cref="Tensor{T}"/> with the associated signs.</param>
        public static Tensor<T> CopySign<T>(Tensor<T> input, Tensor<T> sign)
            where T : INumber<T>
        {
            Tensor<T> output;
            if (input.Lengths.SequenceEqual(sign.Lengths))
            {
                output = Tensor.Create<T>(input.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(input.Lengths, sign.Lengths));
            }

            CopySign<T>(input, sign, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new tensor with the result.
        /// </summary>
        /// <param name="input">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> CopySign<T>(scoped in ReadOnlyTensorSpan<T> input, T sign, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperSpanInTInSpanOut(input, sign, destination, TensorPrimitives.CopySign);
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The <see cref="ReadOnlyTensorSpan{T}"/> with the associated signs.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> CopySign<T>(scoped in ReadOnlyTensorSpan<T> input, scoped in ReadOnlyTensorSpan<T> sign, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(input, sign, destination, TensorPrimitives.CopySign);
        }
        #endregion

        #region Cos
        /// <summary>
        /// Takes the cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the cosine of.</param>
        public static Tensor<T> Cos<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Cos<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cos<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Cos);
        }
        #endregion

        #region Cosh
        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the cosine of.</param>
        public static Tensor<T> Cosh<T>(Tensor<T> input)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Cosh<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cosh<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Cosh);
        }
        #endregion

        #region CosineSimilarity
        /// <summary>
        /// Compute cosine similarity between <paramref name="left"/> and <paramref name="right"/>.
        /// </summary>
        /// <param name="left">The first <see cref="Tensor{T}"/></param>
        /// <param name="right">The second <see cref="Tensor{T}"/></param>
        public static Tensor<T> CosineSimilarity<T>(Tensor<T> left, Tensor<T> right)
            where T : IRootFunctions<T>
        {
            if (left.Rank != 2)
                ThrowHelper.ThrowArgument_2DTensorRequired(nameof(left));

            if (right.Rank != 2)
                ThrowHelper.ThrowArgument_2DTensorRequired(nameof(right));

            if (left.Lengths[1] != right.Lengths[1])
                ThrowHelper.ThrowArgument_IncompatibleDimensions(left.Lengths[1], right.Lengths[1]);

            nint dim1 = left.Lengths[0];
            nint dim2 = right.Lengths[0];

            T[] values = new T[dim1 * dim2];

            scoped Span<nint> leftIndexes = stackalloc nint[2];
            scoped Span<nint> rightIndexes = stackalloc nint[2];

            int outputOffset = 0;

            ReadOnlySpan<T> lspan;
            ReadOnlySpan<T> rspan;
            int rowLength = (int)left.Lengths[1];
            for (int i = 0; i < dim1; i++)
            {
                for (int j = 0; j < dim2; j++)
                {
                    lspan = MemoryMarshal.CreateSpan(ref left[leftIndexes], rowLength);
                    rspan = MemoryMarshal.CreateSpan(ref right[rightIndexes], rowLength);
                    values[outputOffset++] = TensorPrimitives.CosineSimilarity(lspan, rspan);
                    rightIndexes[0]++;
                }
                rightIndexes[0] = 0;
                leftIndexes[0]++;
            }

            return Tensor.Create<T>(values, [dim1, dim2]);
        }

        public static ref readonly TensorSpan<T> CosineSimilarity<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            if (left.Rank != 2)
                ThrowHelper.ThrowArgument_2DTensorRequired(nameof(left));

            if (right.Rank != 2)
                ThrowHelper.ThrowArgument_2DTensorRequired(nameof(right));

            if (left.Lengths[1] != right.Lengths[1])
                ThrowHelper.ThrowArgument_IncompatibleDimensions(left.Lengths[1], right.Lengths[1]);

            nint dim1 = left.Lengths[0];
            nint dim2 = right.Lengths[0];

            if (destination.Lengths[0] != dim1 || destination.Lengths[1] != dim2)
                ThrowHelper.ThrowArgument_IncompatibleDimensions(left.Lengths[1], right.Lengths[1]);

            Span<T> values = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);

            scoped Span<nint> leftIndexes = stackalloc nint[2];
            scoped Span<nint> rightIndexes = stackalloc nint[2];

            int outputOffset = 0;

            ReadOnlySpan<T> lspan;
            ReadOnlySpan<T> rspan;
            int rowLength = (int)left.Lengths[1];
            for (int i = 0; i < dim1; i++)
            {
                for (int j = 0; j < dim2; j++)
                {
                    lspan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref left._reference, TensorSpanHelpers.ComputeLinearIndex(leftIndexes, left.Strides, left.Lengths)), (int)rowLength);
                    rspan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref right._reference, TensorSpanHelpers.ComputeLinearIndex(rightIndexes, right.Strides, right.Lengths)), (int)rowLength);
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
        /// <param name="input">The input <see cref="Tensor{T}"/></param>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><typeparamref name="T"/>.CosPi(<paramref name="input" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static Tensor<T> CosPi<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            CosPi<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi and returns a new <see cref="TensorSpan{T}"/> with the results.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><typeparamref name="T"/>.CosPi(<paramref name="input" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static ref readonly TensorSpan<T> CosPi<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.CosPi);
        }
        #endregion

        #region DegreesToRadians
        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians and returns a new tensor with the results.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> DegreesToRadians<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            DegreesToRadians<T>(input, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians and returns a new tensor with the results.
        /// </summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> DegreesToRadians<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.DegreesToRadians);
        }
        #endregion

        #region Distance
        /// <summary>
        /// Computes the distance between two points, specified as non-empty, equal-length tensors of numbers, in Euclidean space.
        /// </summary>
        /// <param name="left">The input <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The input <see cref="Tensor{T}"/>.</param>
        public static T Distance<T>(Tensor<T> left, Tensor<T> right)
            where T : IRootFunctions<T>
        {
            return Distance<T>((ReadOnlyTensorSpan<T>)left, right);
        }

        /// <summary>
        /// Computes the distance between two points, specified as non-empty, equal-length tensors of numbers, in Euclidean space.
        /// </summary>
        /// <param name="left">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Distance<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right)
            where T : IRootFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInTOut(left, right, TensorPrimitives.Distance);
        }
        #endregion

        #region Divide
        /// <summary>
        /// Divides each element of <paramref name="input"/> by <paramref name="val"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="val">The divisor</param>
        public static Tensor<T> Divide<T>(Tensor<T> input, T val)
            where T : IDivisionOperators<T, T, T>
        {
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Divide<T>(input, val, output);
            return output;
        }

        /// <summary>
        /// Divides <paramref name="val"/> by each element of <paramref name="input"/> and returns a new <see cref="Tensor{T}"/> with the result."/>
        /// </summary>
        /// <param name="val">The value to be divided.</param>
        /// <param name="input">The <see cref="Tensor{T}"/> divisor.</param>
        public static Tensor<T> Divide<T>(T val, Tensor<T> input)
            where T : IDivisionOperators<T, T, T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths, input.IsPinned);
            Divide<T>(val, input, output);
            return output;
        }

        /// <summary>
        /// Divides each element of <paramref name="left"/> by its corresponding element in <paramref name="right"/> and returns
        /// a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> to be divided.</param>
        /// <param name="right">The <see cref="Tensor{T}"/> divisor.</param>
        public static Tensor<T> Divide<T>(Tensor<T> left, Tensor<T> right)
            where T : IDivisionOperators<T, T, T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Divide<T>(left, right, output);
            return output;
        }

        /// <summary>
        /// Divides each element of <paramref name="input"/> by <paramref name="val"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="val">The divisor</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(scoped in ReadOnlyTensorSpan<T> input, T val, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            if (destination._shape._memoryLength < input._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Divide(span, val, ospan);
            return ref destination;
        }

        /// <summary>
        /// Divides <paramref name="val"/> by each element of <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result."/>
        /// </summary>
        /// <param name="val">The value to be divided.</param>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(T val, scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            if (destination._shape._memoryLength < input._shape._memoryLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Divide(val, span, ospan);
            return ref destination;
        }

        /// <summary>
        /// Divides each element of <paramref name="left"/> by its corresponding element in <paramref name="right"/> and returns
        /// a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="ReadOnlyTensorSpan{T}"/> to be divided.</param>
        /// <param name="right">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Divide);
        }
        #endregion

        #region Dot
        /// <summary>
        /// Computes the dot product of two tensors containing numbers.
        /// </summary>
        /// <param name="left">The input <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The input <see cref="Tensor{T}"/>.</param>
        public static T Dot<T>(Tensor<T> left, Tensor<T> right)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return Dot<T>((ReadOnlyTensorSpan<T>)left, right);
        }

        /// <summary>
        /// Computes the dot product of two tensors containing numbers.
        /// </summary>
        /// <param name="left">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Dot<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInTOut(left, right, TensorPrimitives.Dot);
        }

        #endregion

        #region Exp
        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp<T>(Tensor<T> input)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Exp<T>(input, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Exp);
        }
        #endregion

        #region Exp10
        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp10<T>(Tensor<T> input)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Exp10<T>(input, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp10<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Exp10);
        }
        #endregion

        #region Exp10M1
        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp10M1<T>(Tensor<T> input)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Exp10M1<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp10M1<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Exp10M1);
        }
        #endregion

        #region Exp2
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp2<T>(Tensor<T> input)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Exp2<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp2<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Exp2);
        }
        #endregion

        #region Exp2M1
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp2M1<T>(Tensor<T> input)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Exp2M1<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp2M1<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Exp2M1);
        }
        #endregion

        #region ExpM1
        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> ExpM1<T>(Tensor<T> input)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            ExpM1<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> ExpM1<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.ExpM1);
        }
        #endregion

        #region Floor
        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Floor<T>(Tensor<T> input)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Floor<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Floor<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Floor);
        }
        #endregion

        #region Hypotenuse
        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Hypot<T>(Tensor<T> left, Tensor<T> right)
            where T : IRootFunctions<T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Hypot<T>(left, right, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Hypot<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Hypot);
        }
        #endregion

        #region Ieee754Remainder
        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// <param name="left">Left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Ieee754Remainder<T>(Tensor<T> left, Tensor<T> right)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Ieee754Remainder<T>(left, right, output);
            return output;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// <param name="left">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ieee754Remainder<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Ieee754Remainder);
        }
        #endregion

        #region ILogB
        /// <summary>Computes the element-wise integer logarithm of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<int> ILogB<T>(Tensor<T> input)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<int> output = Tensor.Create<int>(input.Lengths, input.Strides);
            ILogB<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise integer logarithm of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<int> ILogB<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<int> destination)
            where T : IFloatingPointIeee754<T>
        {
            return ref TensorPrimitivesHelperSpanInIntSpanOut(input, destination, TensorPrimitives.ILogB);
        }
        #endregion

        #region IndexOfMax
        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static int IndexOfMax<T>(Tensor<T> input)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._values.Length);
            return TensorPrimitives.IndexOfMax(span);
        }

        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static int IndexOfMax<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            return TensorPrimitives.IndexOfMax(span);
        }
        #endregion

        #region IndexOfMaxMagnitude
        /// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static int IndexOfMaxMagnitude<T>(Tensor<T> input)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._values.Length);
            return TensorPrimitives.IndexOfMaxMagnitude(span);
        }

        /// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static int IndexOfMaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            return TensorPrimitives.IndexOfMaxMagnitude(span);
        }
        #endregion

        #region IndexOfMin
        /// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static int IndexOfMin<T>(Tensor<T> input)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._values.Length);
            return TensorPrimitives.IndexOfMin(span);
        }

        /// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static int IndexOfMin<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            return TensorPrimitives.IndexOfMin(span);
        }
        #endregion

        #region IndexOfMinMagnitude
        /// <summary>
        /// Searches for the index of the number with the smallest magnitude in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static int IndexOfMinMagnitude<T>(Tensor<T> input)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._values.Length);
            return TensorPrimitives.IndexOfMinMagnitude(span);
        }

        /// <summary>
        /// Searches for the index of the number with the smallest magnitude in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static int IndexOfMinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            return TensorPrimitives.IndexOfMinMagnitude(span);
        }
        #endregion

        #region LeadingZeroCount
        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> LeadingZeroCount<T>(Tensor<T> input)
            where T : IBinaryInteger<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            LeadingZeroCount<T>(input, output);
            return output;
        }

        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> LeadingZeroCount<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.LeadingZeroCount);
        }
        #endregion

        #region Log
        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> Log<T>(Tensor<T> input)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Log<T>(input, output);
            return output;
        }


        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Log);
        }
        #endregion

        #region Log10
        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10<T>(Tensor<T> input)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Log10<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log10<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Log10);
        }
        #endregion

        #region Log10P1
        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10P1<T>(Tensor<T> input)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Log10P1<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log10P1<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Log10P1);
        }
        #endregion

        #region Log2
        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2<T>(Tensor<T> input)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Log2<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log2<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Log2);
        }
        #endregion

        #region Log2P1
        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2P1<T>(Tensor<T> input)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Log2P1<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log2P1<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Log2P1);
        }
        #endregion

        #region LogP1
        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> LogP1<T>(Tensor<T> input)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            LogP1<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> LogP1<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.LogP1);
        }
        #endregion

        #region Max
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>..</param>
        public static T Max<T>(Tensor<T> input)
            where T : INumber<T>
        {
            return Max<T>((ReadOnlyTensorSpan<T>)input);
        }

        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>..</param>
        public static T Max<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Max);
        }
        #endregion

        #region MaxMagnitude
        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>..</param>
        public static T MaxMagnitude<T>(Tensor<T> input)
            where T : INumber<T>
        {
            return MaxMagnitude<T>((ReadOnlyTensorSpan<T>)input);
        }

        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>..</param>
        public static T MaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MaxMagnitude);
        }
        #endregion

        #region MaxNumber
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>..</param>
        public static T MaxNumber<T>(Tensor<T> input)
            where T : INumber<T>
        {
            return MaxNumber<T>((ReadOnlyTensorSpan<T>)input);
        }

        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>..</param>
        public static T MaxNumber<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MaxNumber);
        }
        #endregion

        #region Min
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static T Min<T>(Tensor<T> input)
            where T : INumber<T>
        {
            return Min<T>((ReadOnlyTensorSpan<T>)input);
        }

        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Min<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Min);
        }
        #endregion

        #region MinMagnitude
        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static T MinMagnitude<T>(Tensor<T> input)
            where T : INumber<T>
        {
            return MinMagnitude<T>((ReadOnlyTensorSpan<T>)input);
        }

        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T MinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MinMagnitude);
        }
        #endregion

        #region MinNumber
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>..</param>
        public static T MinNumber<T>(Tensor<T> input)
            where T : INumber<T>
        {
            return MinNumber<T>((ReadOnlyTensorSpan<T>)input);
        }

        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>..</param>
        public static T MinNumber<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MinNumber);
        }
        #endregion

        #region Multiply
        /// <summary>
        /// Multiplies each element of <paramref name="input"/> with <paramref name="val"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/></param>
        /// <param name="val"><typeparamref name="T"/> value to multiply by.</param>
        public static Tensor<T> Multiply<T>(Tensor<T> input, T val)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths, input.IsPinned);
            Multiply<T>((ReadOnlyTensorSpan<T>)input, val, output);
            return output;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="left"/> with <paramref name="right"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="Tensor{T}"/> for multiplication.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/> for multiplication.</param>
        public static Tensor<T> Multiply<T>(Tensor<T> left, Tensor<T> right)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Multiply<T>((ReadOnlyTensorSpan<T>)left, right, output);
            return output;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="input"/> with <paramref name="val"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="val"><typeparamref name="T"/> value to multiply by.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Multiply<T>(scoped in ReadOnlyTensorSpan<T> input, T val, in TensorSpan<T> destination)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Multiply(span, val, ospan);
            return ref destination;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="left"/> with <paramref name="right"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        /// <param name="right">Right <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Multiply<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Multiply);
        }
        #endregion

        #region Negate
        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> Negate<T>(Tensor<T> input)
            where T : IUnaryNegationOperators<T, T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Negate<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Negate<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IUnaryNegationOperators<T, T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Negate);
        }
        #endregion

        #region Norm
        /// <summary>
        /// Takes the norm of the <see cref="Tensor{T}"/> and returns the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the norm of.</param>
        public static T Norm<T>(Tensor<T> input)
            where T : IRootFunctions<T>
        {
            return Norm<T>((ReadOnlyTensorSpan<T>)input);
        }


        /// <summary>
        ///  Takes the norm of the <see cref="ReadOnlyTensorSpan{T}"/> and returns the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the norm of.</param>
        public static T Norm<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : IRootFunctions<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            return TensorPrimitives.Norm(span);
        }
        #endregion

        #region OnesComplement
        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> OnesComplement<T>(Tensor<T> input)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            OnesComplement<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> OnesComplement<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.OnesComplement);
        }
        #endregion

        #region PopCount
        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> PopCount<T>(Tensor<T> input)
            where T : IBinaryInteger<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            PopCount<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> PopCount<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.PopCount);
        }
        #endregion

        #region Pow
        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="left">The input <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The second input <see cref="Tensor{T}"/></param>
        public static Tensor<T> Pow<T>(Tensor<T> left, Tensor<T> right)
            where T : IPowerFunctions<T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Pow<T>(left, right, output);
            return output;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="left">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Pow<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IPowerFunctions<T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Pow);
        }
        #endregion

        #region Product
        /// <summary>Computes the product of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static T Product<T>(Tensor<T> input)
            where T : IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return Product<T>((ReadOnlyTensorSpan<T>)input);
        }

        /// <summary>Computes the product of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Product<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Product);
        }
        #endregion

        #region RadiansToDegrees
        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> RadiansToDegrees<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            RadiansToDegrees<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> RadiansToDegrees<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.RadiansToDegrees);
        }
        #endregion

        #region Reciprocal
        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Reciprocal<T>(Tensor<T> input)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Reciprocal<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Reciprocal<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Reciprocal);
        }
        #endregion

        #region Round
        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Round<T>(Tensor<T> input)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Round<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Round);
        }
        #endregion

        #region Sigmoid
        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Sigmoid<T>(Tensor<T> input)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Sigmoid<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sigmoid<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Sigmoid);
        }
        #endregion

        #region Sin
        /// <summary>
        /// Takes the sin of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Sin<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Sin<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sin<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Sin);
        }
        #endregion

        #region Sinh
        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Sinh<T>(Tensor<T> input)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Sinh<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sinh<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Sinh);
        }
        #endregion

        #region SinPi
        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SinPi<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            SinPi<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> SinPi<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.SinPi);
        }
        #endregion

        #region SoftMax
        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SoftMax<T>(Tensor<T> input)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            SoftMax<T>(input, output);
            return output;
        }

        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> SoftMax<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.SoftMax);
        }
        #endregion

        #region Sqrt
        /// <summary>
        /// Takes the square root of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the square root of.</param>
        public static Tensor<T> Sqrt<T>(Tensor<T> input)
            where T : IRootFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Sqrt<T>(input, output);
            return output;
        }

        /// <summary>
        /// Takes the square root of each element of the <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the square root of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sqrt<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Sqrt);
        }
        #endregion

        #region Subtract
        /// <summary>
        /// Subtracts <paramref name="val"/> from each element of <paramref name="input"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/>.</param>
        /// <param name="val">The <typeparamref name="T"/> to subtract.</param>
        public static Tensor<T> Subtract<T>(Tensor<T> input, T val)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Subtract<T>(input, val, output);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="input"/> from <paramref name="val"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="val">The <typeparamref name="T"/> to be subtracted from.</param>
        /// <param name="input">The <see cref="Tensor{T}"/> of values to subtract.</param>
        public static Tensor<T> Subtract<T>(T val, Tensor<T> input)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Subtract<T>(val, input, output);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="left"/> from <paramref name="right"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> with values to be subtracted from.</param>
        /// <param name="right">The <see cref="Tensor{T}"/> with values to subtract.</param>
        public static Tensor<T> Subtract<T>(Tensor<T> left, Tensor<T> right)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Subtract<T>(left, right, output);
            return output;
        }

        /// <summary>
        /// Subtracts <paramref name="val"/> from each element of <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> with values to be subtracted from.</param>
        /// <param name="val">The <typeparamref name="T"/> value to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(scoped in ReadOnlyTensorSpan<T> input, T val, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return ref destination;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="input"/> from <paramref name="val"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="val">The <typeparamref name="T"/> value to be subtracted from.</param>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> values to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(T val, scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            TensorPrimitives.Subtract(val, span, ospan);
            return ref destination;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="left"/> from <paramref name="right"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="ReadOnlyTensorSpan{T}"/> of values to be subtracted from.</param>
        /// <param name="right">The <see cref="ReadOnlyTensorSpan{T}"/>of values to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Subtract);
        }
        #endregion

        #region Sum
        public static T Sum<T>(Tensor<T> input)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return Sum<T>((ReadOnlyTensorSpan<T>)input);
        }

        public static T Sum<T>(scoped in ReadOnlyTensorSpan<T> input)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            return TensorPrimitives.Sum(span);
        }
        #endregion

        #region Tan
        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Tan<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Tan<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Tan<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Tan);
        }
        #endregion

        #region Tanh
        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Tanh<T>(Tensor<T> input)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Tanh<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Tanh<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Tanh);
        }
        #endregion

        #region TanPi
        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> TanPi<T>(Tensor<T> input)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            TanPi<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> TanPi<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.TanPi);
        }
        #endregion

        #region TrailingZeroCount
        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> TrailingZeroCount<T>(Tensor<T> input)
            where T : IBinaryInteger<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            TrailingZeroCount<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> TrailingZeroCount<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.TrailingZeroCount);
        }
        #endregion

        #region Truncate
        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Truncate<T>(Tensor<T> input)
            where T : IFloatingPoint<T>
        {
            Tensor<T> output = Tensor.Create<T>(input.Lengths);
            Truncate<T>(input, output);
            return output;
        }

        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Truncate<T>(scoped in ReadOnlyTensorSpan<T> input, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            return ref TensorPrimitivesHelperSpanInSpanOut(input, destination, TensorPrimitives.Truncate);
        }
        #endregion

        #region Xor
        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Xor<T>(Tensor<T> left, Tensor<T> right)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> output;
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                output = Tensor.Create<T>(left.Lengths);
            }
            else
            {
                output = Tensor.Create<T>(GetSmallestBroadcastableLengths(left.Lengths, right.Lengths));
            }

            Xor<T>(left, right, output);
            return output;
        }

        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="left">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Xor<T>(scoped in ReadOnlyTensorSpan<T> left, scoped in ReadOnlyTensorSpan<T> right, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            return ref TensorPrimitivesHelperTwoSpanInSpanOut(left, right, destination, TensorPrimitives.Xor);
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
            if (TensorHelpers.AreLengthsTheSame(left, right) && TensorHelpers.IsUnderlyingStorageSameSize<T>(left, right))
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            performCalculation(span, ospan);
            return ref destination;
        }

        private static ref readonly TensorSpan<T> TensorPrimitivesHelperSpanInTInSpanOut<T>(scoped in ReadOnlyTensorSpan<T> input, T value, in TensorSpan<T> destination, PerformCalculationSpanInTInSpanOut<T> performCalculation)
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._shape._memoryLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape._memoryLength);
            performCalculation(span, value, ospan);
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
