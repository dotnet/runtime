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

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        #region Resize
        /// <summary>
        /// Creates a new <see cref="Tensor{T}"/>, allocates new memory, and copies the data from <paramref name="input"/>. If the final shape is smaller all data after
        /// that point is ignored.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        public static Tensor<T> Resize<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)newSize) : (new T[newSize]);
            Tensor<T> output = new Tensor<T>(values, shape, false);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input.AsTensorSpan()._reference, (int)input.FlattenedLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsTensorSpan()._reference, (int)output.FlattenedLength);
            if (newSize > input.FlattenedLength)
                TensorSpanHelpers.Memmove(ospan, span, input.FlattenedLength);
            else
                TensorSpanHelpers.Memmove(ospan, span, newSize);

            return output;
        }

        #endregion

        #region Broadcast
        /// <summary>
        /// Broadcast the data from <paramref name="left"/> to the smallest broadcastable shape compatible with <paramref name="right"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// </summary>
        /// <param name="left">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="right">Other <see cref="Tensor{T}"/> to make shapes broadcastable.</param>
        public static Tensor<T> Broadcast<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);

            Tensor<T> intermediate = BroadcastTo(left, newSize);
            return Tensor.Create(intermediate.ToArray(), intermediate.Lengths);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the new shape <paramref name="shape"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// If the shape of the <paramref name="input"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        public static Tensor<T> Broadcast<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            Tensor<T> intermediate = BroadcastTo(input, shape);
            return Tensor.Create(intermediate.ToArray(), intermediate.Lengths);
        }

        // Lazy/non-copy broadcasting, internal only for now.
        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the new shape <paramref name="shape"/>. Creates a new <see cref="Tensor{T}"/>
        /// but no memory is allocated. It manipulates the strides to achieve this affect.
        /// If the shape of the <paramref name="input"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        internal static Tensor<T> BroadcastTo<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
        where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Lengths.SequenceEqual(shape))
                return new Tensor<T>(input._values, shape, false);

            if (!TensorHelpers.AreShapesBroadcastCompatible(input.Lengths, shape))
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

            Tensor<T> output = new Tensor<T>(input._values, shape, strides);

            return output;
        }

        #endregion

        #region Reverse
        /// <summary>
        /// Reverse the order of elements in the <paramref name="input"/> along the given axis. The shape of the tensor is preserved, but the elements are reordered.
        /// <paramref name="axis"/> defaults to -1 when not provided, which reverses the entire tensor.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="axis">Axis along which to reverse over. The default, -1, will reverse over all of the axes of the left tensor.</param>
        public static Tensor<T> Reverse<T>(Tensor<T> input, nint axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input._flattenedLength) : (new T[input._flattenedLength]);
            Tensor<T> output = new Tensor<T>(values, input.Lengths.ToArray(), input.Strides.ToArray());
            if (axis == -1)
            {
                int index = 0;
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input.FlattenedLength);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output.FlattenedLength);
                for (int i = (int)input.FlattenedLength - 1; i >= 0; i--)
                {
                    ospan[index++] = span[i];
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
                TensorSpan<T> islice = input.AsTensorSpan().Slice(input.Lengths);
                TensorSpan<T> oslice = output.AsTensorSpan().Slice(output._lengths);
                while (copiedValues < input._flattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, TensorSpanHelpers.ComputeLinearIndex(oIndices, input.Strides, input.Lengths)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes((int)axis, 1, oIndices, input._lengths);
                    TensorSpanHelpers.AdjustIndexesDown((int)axis, 1, iIndices, input._lengths);
                    copiedValues += copyLength;
                }

                if (oIndicesArray != null && iIndicesArray != null)
                {
                    ArrayPool<nint>.Shared.Return(oIndicesArray);
                    ArrayPool<nint>.Shared.Return(iIndicesArray);
                }
            }

            return output;
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
        public static Tensor<T>[] Split<T>(Tensor<T> input, nint numSplits, nint axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
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
                T[] values = input.IsPinned ? GC.AllocateArray<T>((int)totalToCopy) : (new T[(int)totalToCopy]);
                outputs[i] = new Tensor<T>(values, newShape);
                oIndices.Clear();
                iIndices.Clear();

                iIndices[(int)axis] = i;
                TensorSpan<T> islice = input.AsTensorSpan().Slice(input.Lengths);
                TensorSpan<T> oslice = outputs[i].AsTensorSpan().Slice(outputs[i]._lengths);

                nint copiedValues = 0;
                while (copiedValues < totalToCopy)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, TensorSpanHelpers.ComputeLinearIndex(oIndices, outputs[0].Strides, outputs[0].Lengths)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes((int)axis, 1, oIndices, outputs[i]._lengths);
                    TensorSpanHelpers.AdjustIndexes((int)axis - 1, 1, iIndices, input._lengths);
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

        #region SetSlice
        // REVIEW: WHAT DO WE WANT TO CALL THIS? COPYTO? IT DOES FIT IN WITH THE EXISTING COPY TO CONVENTIONS FOR VECTOR (albeit backwards).
        /// <summary>
        /// Sets a slice of the given <paramref name="tensor"/> with the provided <paramref name="values"/> for the given <paramref name="ranges"/>
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="values">The values you want to set in the <paramref name="tensor"/>.</param>
        /// <param name="ranges">The ranges you want to set.</param>
        public static Tensor<T> SetSlice<T>(this Tensor<T> tensor, Tensor<T> values, params ReadOnlySpan<NRange> ranges)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            TensorSpan<T> srcSpan;
            if (ranges == ReadOnlySpan<NRange>.Empty)
            {
                if (!tensor.Lengths.SequenceEqual(values.Lengths))
                    ThrowHelper.ThrowArgument_SetSliceNoRange(nameof(values));
                srcSpan = tensor.AsTensorSpan().Slice(tensor.Lengths);
            }
            else
                srcSpan = tensor.AsTensorSpan().Slice(ranges);

            if (!srcSpan.Lengths.SequenceEqual(values.Lengths))
                ThrowHelper.ThrowArgument_SetSliceInvalidShapes(nameof(values));

            values.AsTensorSpan().CopyTo(srcSpan);

            return tensor;
        }
        #endregion

        #region FilteredUpdate
        // REVIEW: PYTORCH/NUMPY DO THIS.
        //  t0[t0 < 2] = -1;
        //  OR SHOULD THIS BE AN OVERLOAD OF FILL THAT TAKES IN A FUNC TO KNOW WHICH ONE TO UPDATE?
        /// <summary>
        /// Updates the <paramref name="tensor"/> tensor with the <paramref name="value"/> where the <paramref name="filter"/> is true.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="filter">Input filter where if the index is true then it will update the <paramref name="tensor"/>.</param>
        /// <param name="value">Value to update in the <paramref name="tensor"/>.</param>
        public static Tensor<T> FilteredUpdate<T>(Tensor<T> tensor, Tensor<bool> filter, T value)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (filter.Lengths.Length != tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(filter));

            Span<T> srcSpan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._flattenedLength);
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)tensor._flattenedLength);

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
        public static Tensor<T> FilteredUpdate<T>(Tensor<T> tensor, Tensor<bool> filter, Tensor<T> values)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (filter.Lengths.Length != tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(filter));
            if (values.Rank != 1)
                ThrowHelper.ThrowArgument_1DTensorRequired(nameof(values));

            nint numTrueElements = TensorHelpers.CountTrueElements(filter);
            if (numTrueElements != values._flattenedLength)
                ThrowHelper.ThrowArgument_IncorrectNumberOfFilterItems(nameof(values));

            Span<T> dstSpan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._flattenedLength);
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)tensor._flattenedLength);
            Span<T> valuesSpan = MemoryMarshal.CreateSpan(ref values._values[0], (int)values._flattenedLength);

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

        #region SequenceEqual
        /// <summary>
        /// Compares the elements of two <see cref="Tensor{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="left">First <see cref="Tensor{T}"/> to compare.</param>
        /// <param name="right">Second <see cref="Tensor{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static Tensor<bool> SequenceEqual<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                result = Tensor.Create<bool>(left.Lengths, false);

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    result._values[i] = left._values[i] == right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);
                result = Tensor.Create<bool>(newSize, false);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                result = Tensor.Create<bool>(left.Lengths, false);

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    result._values[i] = left._values[i] < right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);
                result = Tensor.Create<bool>(newSize, false);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(left.Lengths, false);

            for (int i = 0; i < left.FlattenedLength; i++)
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    if (left._values[i] < right._values[i])
                        return true;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    if (left._values[i] > right._values[i])
                        return false;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                result = Tensor.Create<bool>(left.Lengths, false);

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    result._values[i] = left._values[i] > right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);
                result = Tensor.Create<bool>(newSize, false);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(left.Lengths, false);

            for (int i = 0; i < left.FlattenedLength; i++)
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreShapesTheSame(left, right))
            {

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    if (left._values[i] > right._values[i])
                        return true;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreShapesTheSame(left, right))
            {

                for (int i = 0; i < left.FlattenedLength; i++)
                {
                    if (left._values[i] < right._values[i])
                        return false;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

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

        #region Stack
        // REVIEW: NEEDS A DIFFERENT NAME?
        // JUST AN OVERLOAD FOR CONCATENATE?
        /// <summary>
        /// Join an array of <see cref="Tensor{T}"/> along a new axis. The axis parameter specifies the index of the new axis in the dimensions of the result and
        /// defaults to 0. All tensors must have the same shape.
        /// </summary>
        /// <param name="input">Array of <see cref="Tensor{T}"/>.</param>
        /// <param name="axis">Index of where the new axis will be. Defaults to 0.</param>
        public static Tensor<T> Stack<T>(Tensor<T>[] input, int axis = 0)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Length < 2)
                ThrowHelper.ThrowArgument_StackTooFewTensors();
            if (axis < 0)
                axis = input.Rank - axis;

            Tensor<T>[] outputs = new Tensor<T>[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                outputs[i] = Tensor.Unsqueeze(input[0], axis);
            }
            return Tensor.Concatenate<T>(outputs, axis);
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
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
        #endregion

        #region Squeeze
        // REVIEW: NAME?
        /// <summary>
        /// Removes axis of length one from the <paramref name="input"/>. <paramref name="axis"/> defaults to -1 and will remove all axis with length of 1.
        /// If <paramref name="axis"/> is specified, it will only remove that axis and if it is not of length one it will throw an exception.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to remove. Defaults to -1 which removes all axis of length 1.</param>
        public static Tensor<T> Squeeze<T>(Tensor<T> input, int axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
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
        #endregion

        #region Unsqueeze
        // REVIEW: NAME? NUMPY CALLS THIS expand_dims.
        /// <summary>
        /// Insert a new axis of length 1 that will appear at the axis position.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to add.</param>
        public static Tensor<T> Unsqueeze<T>(Tensor<T> input, int axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
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
        #endregion

        #region Concatenate
        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="axis">The axis along which the tensors will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
        public static Tensor<T> Concatenate<T>(ReadOnlySpan<Tensor<T>> tensors, int axis = 0)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
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

        #region StdDev
        /// <summary>
        /// Returns the standard deviation of the elements in the <paramref name="input"/> tensor.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the standard deviation of.</param>
        /// <returns><typeparamref name="T"/> representing the standard deviation.</returns>
        public static T StdDev<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>, IPowerFunctions<T>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>

        {
            T mean = Mean(input);
            Span<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Span<T> output = new T[input._flattenedLength].AsSpan();
            TensorPrimitives.Subtract(span, mean, output);
            TensorPrimitives.Abs(output, output);
            TensorPrimitives.Pow((ReadOnlySpan<T>)output, T.CreateChecked(2), output);
            T sum = TensorPrimitives.Sum((ReadOnlySpan<T>)output);
            return T.CreateChecked(sum / T.CreateChecked(input.FlattenedLength));
        }

        /// <summary>
        /// Return the standard deviation of the elements in the <paramref name="input"/> tensor. Casts the return value to <typeparamref name="TResult"/>.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the standard deviation of.</param>
        /// <returns><typeparamref name="TResult"/> representing the standard deviation.</returns>
        public static TResult StdDev<T, TResult>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>, IFloatingPoint<T>, IPowerFunctions<T>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T mean = Mean(input);
            Span<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Span<T> output = new T[input._flattenedLength].AsSpan();
            TensorPrimitives.Subtract(span, mean, output);
            TensorPrimitives.Abs(output, output);
            TensorPrimitives.Pow((ReadOnlySpan<T>)output, T.CreateChecked(2), output);
            T sum = TensorPrimitives.Sum((ReadOnlySpan<T>)output);
            return TResult.CreateChecked(sum / T.CreateChecked(input.FlattenedLength));
        }

        #endregion

        #region Mean
        /// <summary>
        /// Returns the mean of the elements in the <paramref name="input"/> tensor.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the mean of.</param>
        /// <returns><typeparamref name="T"/> representing the mean.</returns>
        public static T Mean<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>

        {
            T sum = Sum(input);
            return T.CreateChecked(sum / T.CreateChecked(input.FlattenedLength));
        }

        /// <summary>
        /// Return the mean of the elements in the <paramref name="input"/> tensor. Casts the return value to <typeparamref name="TResult"/>.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the mean of.</param>
        /// <returns><typeparamref name="TResult"/> representing the mean.</returns>
        public static TResult Mean<T, TResult>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.FlattenedLength));
        }

        #endregion

        #region Permute/Transpose
        /// <summary>
        /// Swaps the last two dimensions of the <paramref name="input"/> tensor.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Transpose<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Lengths.Length < 2)
                ThrowHelper.ThrowArgument_TransposeTooFewDimensions();
            int[] axis = Enumerable.Range(0, input.Rank).ToArray();
            int temp = axis[input.Rank - 1];
            axis[input.Rank - 1] = axis[input.Rank - 2];
            axis[input.Rank - 2] = temp;
            return Permute(input, axis.AsSpan());
        }

        /// <summary>
        /// Swaps the dimensions of the <paramref name="input"/> tensor according to the <paramref name="axis"/> parameter.
        /// If <paramref name="input"/> is a 1D tensor, it will return <paramref name="input"/>. Otherwise it creates a new <see cref="Tensor{T}"/>
        /// with the new axis ordering by allocating new memory.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/></param>
        /// <param name="axis"><see cref="ReadOnlySpan{T}"/> with the new axis ordering.</param>
        public static Tensor<T> Permute<T>(Tensor<T> input, params ReadOnlySpan<int> axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
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

        #region TensorPrimitives
        #region Abs
        /// <summary>
        /// Takes the absolute value of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Abs<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumberBase<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Abs);
        }

        /// <summary>
        /// Takes the absolute of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AbsInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumberBase<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Abs, true);
        }
        #endregion

        #region Acos
        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Acos<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Acos);
        }

        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AcosInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Acos, true);
        }
        #endregion

        #region Acosh
        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Acosh<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Acosh);
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AcoshInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Acosh, true);
        }
        #endregion

        #region AcosPi
        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AcosPi<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AcosPi);
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AcosPiInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AcosPi, true);
        }
        #endregion

        #region Add
        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="right">The second <see cref="Tensor{T}"/> of values to add.</param>
        public static Tensor<T> Add<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Add);
        }

        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="right">The second <see cref="Tensor{T}"/> of values to add.</param>
        public static Tensor<T> AddInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Add, true);
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> to add to each element of <paramref name="input"/>.</param>
        public static Tensor<T> Add<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperSpanInTInSpanOut(input, val, TensorPrimitives.Add);
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> to add to each element of <paramref name="input"/>.</param>
        public static Tensor<T> AddInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperSpanInTInSpanOut(input, val, TensorPrimitives.Add, true);

        }
        #endregion

        #region Asin
        /// <summary>
        /// Takes the inverse sin of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Asin<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Asin);
        }

        /// <summary>
        /// Takes the inverse sine each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AsinInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Asin, true);
        }
        #endregion

        #region Asinh
        /// <summary>
        /// Takes the inverse hyperbolic sine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Asinh<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Asinh);
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AsinhInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Asinh, true);
        }
        #endregion

        #region AsinPi
        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AsinPi<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AsinPi);
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> AsinPiInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AsinPi, true);
        }
        #endregion

        #region Atan
        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/></param>
        public static Tensor<T> Atan<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Atan);
        }

        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/></param>
        public static Tensor<T> AtanInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Atan, true);
        }
        #endregion

        #region Atan2
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Atan2<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Atan2);
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Atan2InPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Atan2, true);
        }
        #endregion

        #region Atan2Pi
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="Tensor{T}"/>, divides each element by pi, and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Atan2Pi<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Atan2Pi);
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="Tensor{T}"/>, divides each element by pi in place.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Atan2PiInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Atan2Pi, true);
        }
        #endregion

        #region Atanh
        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Atanh<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Atanh);
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> AtanhInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Atanh, true);
        }
        #endregion

        #region AtanPi
        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input<see cref="Tensor{T}"/>.</param>
        public static Tensor<T> AtanPi<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AtanPi);
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The input<see cref="Tensor{T}"/>.</param>
        public static Tensor<T> AtanPiInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AtanPi, true);
        }
        #endregion

        #region BitwiseAnd
        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> BitwiseAnd<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.BitwiseAnd);
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> BitwiseAndInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.BitwiseAnd, true);
        }
        #endregion

        #region BitwiseOr
        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> BitwiseOr<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.BitwiseOr);
        }

        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> BitwiseOrInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.BitwiseOr, true);
        }
        #endregion

        #region CubeRoot
        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The left <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> CubeRoot<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cbrt);
        }

        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The left <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> CubeRootInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cbrt, true);
        }
        #endregion

        #region Ceiling
        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The left <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Ceiling<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Ceiling);
        }

        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The left <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> CeilingInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Ceiling, true);
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
            return TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(source, TensorPrimitives.ConvertChecked);
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
            return TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(source, TensorPrimitives.ConvertSaturating);
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
            return TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(source, TensorPrimitives.ConvertTruncating);
        }
        #endregion

        #region CopySign
        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new tensor with the result.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        public static Tensor<T> CopySign<T>(Tensor<T> input, T sign)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.CopySign(span, sign, ospan);
            return output;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors in place.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        public static Tensor<T> CopySignInPlace<T>(Tensor<T> input, T sign)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            Span<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            TensorPrimitives.CopySign(span, sign, span);
            return input;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="sign">The <see cref="Tensor{T}"/> with the associated signs.</param>
        public static Tensor<T> CopySign<T>(Tensor<T> input, Tensor<T> sign)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(input, sign, TensorPrimitives.CopySign);
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors in place.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="sign">The <see cref="Tensor{T}"/> with the associated signs.</param>
        public static Tensor<T> CopySignInPlace<T>(Tensor<T> input, Tensor<T> sign)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(input, sign, TensorPrimitives.CopySign, true);
        }
        #endregion

        #region Cos
        /// <summary>
        /// Takes the cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the cosine of.</param>
        public static Tensor<T> Cos<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cos);
        }

        /// <summary>
        /// Takes the cosine of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the cosine of.</param>
        public static Tensor<T> CosInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cos, true);
        }
        #endregion

        #region Cosh
        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the cosine of.</param>
        public static Tensor<T> Cosh<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cosh);
        }

        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the cosine of.</param>
        public static Tensor<T> CoshInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cosh, true);
        }
        #endregion

        #region CosineSimilarity
        /// <summary>
        /// Compute cosine similarity between <paramref name="left"/> and <paramref name="right"/>.
        /// </summary>
        /// <param name="left">The first <see cref="Tensor{T}"/></param>
        /// <param name="right">The second <see cref="Tensor{T}"/></param>
        public static Tensor<T> CosineSimilarity<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.CosPi);
        }

        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi in place.</summary>
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
        public static Tensor<T> CosPiInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.CosPi, true);
        }
        #endregion

        #region DegreesToRadians
        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians and returns a new tensor with the results.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> DegreesToRadians<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.DegreesToRadians);
        }

        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians in place.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> DegreesToRadiansInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.DegreesToRadians, true);
        }
        #endregion

        #region Distance
        /// <summary>
        /// Computes the distance between two points, specified as non-empty, equal-length tensors of numbers, in Euclidean space.
        /// </summary>
        /// <param name="left">The input <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The input <see cref="Tensor{T}"/>.</param>
        public static T Distance<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Divides each element of <paramref name="input"/> by <paramref name="val"/> in place.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="val">The divisor.</param>
        public static Tensor<T> DivideInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Divides <paramref name="val"/> by each element of <paramref name="input"/> and returns a new <see cref="Tensor{T}"/> with the result."/>
        /// </summary>
        /// <param name="val">The value to be divided.</param>
        /// <param name="input">The <see cref="Tensor{T}"/> divisor.</param>
        public static Tensor<T> Divide<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        /// <summary>
        /// Divides <paramref name="val"/> by each element of <paramref name="input"/> in place.
        /// </summary>
        /// <param name="val">The value to be divided.</param>
        /// <param name="input">The <see cref="Tensor{T}"/> divisor.</param>
        public static Tensor<T> DivideInPlace<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        /// <summary>
        /// Divides each element of <paramref name="left"/> by its corresponding element in <paramref name="right"/> and returns
        /// a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> to be divided.</param>
        /// <param name="right">The <see cref="Tensor{T}"/> divisor.</param>
        public static Tensor<T> Divide<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Divide);
        }

        /// <summary>
        /// Divides each element of <paramref name="left"/> by its corresponding element in <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> to be divided.</param>
        /// <param name="right">The <see cref="Tensor{T}"/> divisor.</param>
        public static Tensor<T> DivideInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Divide, true);
        }
        #endregion

        #region Dot
        /// <summary>
        /// Computes the dot product of two tensors containing numbers.
        /// </summary>
        /// <param name="left">The input <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The input <see cref="Tensor{T}"/>.</param>
        public static T Dot<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp);
        }

        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> ExpInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp, true);
        }
        #endregion

        #region Exp10
        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp10<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp10);
        }

        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp10InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp10, true);
        }
        #endregion

        #region Exp10M1
        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp10M1<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp10M1);
        }

        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp10M1InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp10M1, true);
        }
        #endregion

        #region Exp2
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp2<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp2);
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp2InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp2, true);
        }
        #endregion

        #region Exp2M1
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp2M1<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp2M1);
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Exp2M1InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp2M1, true);
        }
        #endregion

        #region ExpM1
        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> ExpM1<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.ExpM1);
        }

        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> ExpM1InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.ExpM1, true);
        }
        #endregion

        #region Floor
        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Floor<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Floor);
        }

        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> FloorInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Floor, true);
        }
        #endregion

        #region Hypotenuse
        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Hypotenuse<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Hypot);
        }

        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> HypotenuseInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Hypot, true);
        }
        #endregion

        #region Ieee754Remainder
        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// <param name="left">Left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Ieee754Remainder<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Ieee754Remainder);
        }

        /// <summary>
        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Ieee754RemainderInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Ieee754Remainder, true);
        }
        #endregion

        #region ILogB
        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<int> ILogB<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperSpanInIntSpanOut(input, TensorPrimitives.ILogB);
        }
        #endregion

        #region IndexOfMax
        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static int IndexOfMax<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>

        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            return TensorPrimitives.IndexOfMax(span);
        }
        #endregion

        #region IndexOfMaxMagnitude
        /// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static int IndexOfMaxMagnitude<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>

        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            return TensorPrimitives.IndexOfMaxMagnitude(span);
        }
        #endregion

        #region IndexOfMin
        /// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static int IndexOfMin<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>

        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            return TensorPrimitives.IndexOfMin(span);
        }
        #endregion

        #region IndexOfMinMagnitude
        /// <summary>
        /// Searches for the index of the number with the smallest magnitude in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static int IndexOfMinMagnitude<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>

        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            return TensorPrimitives.IndexOfMinMagnitude(span);
        }
        #endregion

        #region LeadingZeroCount
        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> LeadingZeroCount<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.LeadingZeroCount);
        }

        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> LeadingZeroCountInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.LeadingZeroCount, true);
        }
        #endregion

        #region Log
        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> Log<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log);
        }

        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> LogInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log, true);
        }
        #endregion

        #region Log10
        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log10);
        }

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log10, true);
        }
        #endregion

        #region Log10P1
        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10P1<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log10P1);
        }

        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10P1InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log10P1, true);
        }
        #endregion

        #region Log2
        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log2);
        }

        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log2, true);
        }

        #endregion

        #region Log2P1
        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2P1<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log2P1);
        }

        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2P1InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log2P1, true);
        }
        #endregion

        #region LogP1
        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> LogP1<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.LogP1);
        }

        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> LogP1InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.LogP1, true);
        }
        #endregion

        #region Max
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>..</param>
        public static T Max<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Max);
        }
        #endregion

        #region MaxMagnitude
        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>..</param>
        public static T MaxMagnitude<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MaxMagnitude);
        }
        #endregion

        #region MaxNumber
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>..</param>
        public static T MaxNumber<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MaxNumber);
        }
        #endregion

        #region Min
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static T Min<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Min);
        }
        #endregion

        #region MinMagnitude
        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static T MinMagnitude<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MinMagnitude);
        }
        #endregion

        #region MinNumber
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>..</param>
        public static T MinNumber<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="input"/> with <paramref name="val"/> in place.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/></param>
        /// <param name="val"><typeparamref name="T"/> value to multiply by.</param>
        public static Tensor<T> MultiplyInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="left"/> with <paramref name="right"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="Tensor{T}"/> for multiplication.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/> for multiplication.</param>
        public static Tensor<T> Multiply<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Multiply);
        }

        /// <summary>
        /// Multiplies each element of <paramref name="left"/> with <paramref name="right"/> in place.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="Tensor{T}"/> for multiplication.</param>
        /// <param name="right">Right <see cref="Tensor{T}"/> for multiplication.</param>
        public static Tensor<T> MultiplyInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Multiply, true);
        }
        #endregion

        #region Negate
        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> Negate<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IUnaryNegationOperators<T, T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Negate);
        }

        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> NegateInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IUnaryNegationOperators<T, T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Negate, true);
        }
        #endregion

        #region Norm
        /// <summary>
        /// Takes the norm of the <see cref="Tensor{T}"/> and returns the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the norm of.</param>
        public static T Norm<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Norm);
        }
        #endregion

        #region OnesComplement
        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> OnesComplement<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.OnesComplement);
        }

        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> OnesComplementInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.OnesComplement, true);
        }
        #endregion

        #region PopCount
        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> PopCount<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.PopCount);
        }

        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/></param>
        public static Tensor<T> PopCountInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.PopCount, true);
        }
        #endregion

        #region Pow
        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="left">The input <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The second input <see cref="Tensor{T}"/></param>
        public static Tensor<T> Pow<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IPowerFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Pow);
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="left">The input <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The second input <see cref="Tensor{T}"/></param>
        public static Tensor<T> PowInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IPowerFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Pow, true);
        }
        #endregion

        #region Product
        /// <summary>Computes the product of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static T Product<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Product);
        }
        #endregion

        #region RadiansToDegrees
        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> RadiansToDegrees<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.RadiansToDegrees);
        }

        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> RadiansToDegreesInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.RadiansToDegrees, true);
        }
        #endregion

        #region Reciprocal
        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Reciprocal<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Reciprocal);
        }

        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> ReciprocalInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Reciprocal, true);
        }
        #endregion

        #region Round
        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Round<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Round);
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> RoundInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Round, true);
        }
        #endregion

        #region Sigmoid
        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Sigmoid<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sigmoid);
        }

        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> SigmoidInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sigmoid, true);
        }
        #endregion

        #region Sin
        /// <summary>
        /// Takes the sin of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Sin<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sin);
        }

        /// <summary>
        /// Takes the sin of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SinInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sin, true);
        }
        #endregion

        #region Sinh
        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Sinh<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sinh);
        }

        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SinhInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sinh, true);
        }
        #endregion

        #region SinPi
        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SinPi<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.SinPi);
        }

        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SinPiInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.SinPi, true);
        }
        #endregion

        #region SoftMax
        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SoftMax<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.SoftMax);
        }

        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SoftMaxInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.SoftMax, true);
        }
        #endregion

        #region Sqrt
        /// <summary>
        /// Takes the square root of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the square root of.</param>
        public static Tensor<T> Sqrt<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sqrt);
        }

        /// <summary>
        /// Takes the square root of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the square root of.</param>
        public static Tensor<T> SqrtInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sqrt, true);
        }
        #endregion

        #region Subtract
        /// <summary>
        /// Subtracts <paramref name="val"/> from each element of <paramref name="input"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/>.</param>
        /// <param name="val">The <typeparamref name="T"/> to subtract.</param>
        public static Tensor<T> Subtract<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Subtracts <paramref name="val"/> from each element of <paramref name="input"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/>.</param>
        /// <param name="val">The <typeparamref name="T"/> to subtract.</param>
        public static Tensor<T> SubtractInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="input"/> from <paramref name="val"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="val">The <typeparamref name="T"/> to be subtracted from.</param>
        /// <param name="input">The <see cref="Tensor{T}"/> of values to subtract.</param>
        public static Tensor<T> Subtract<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = Create<T>(input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="input"/> from <paramref name="val"/> in place.
        /// </summary>
        /// <param name="val">The <typeparamref name="T"/> to be subtracted from.</param>
        /// <param name="input">The <see cref="Tensor{T}"/> of values to subtract.</param>
        public static Tensor<T> SubtractInPlace<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="left"/> from <paramref name="right"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> with values to be subtracted from.</param>
        /// <param name="right">The <see cref="Tensor{T}"/> with values to subtract.</param>
        public static Tensor<T> Subtract<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Subtract);
        }

        /// <summary>
        /// Subtracts each element of <paramref name="left"/> from <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> with values to be subtracted from.</param>
        /// <param name="right">The <see cref="Tensor{T}"/> with values to subtract.</param>
        public static Tensor<T> SubtractInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Subtract, true);
        }
        #endregion

        #region Sum

        /// <summary>
        /// Sums all the elements of the <see cref="Tensor{T}"/> and returns the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to sum.</param>
        public static T Sum<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            return TensorPrimitives.Sum(span);
        }

        #endregion

        #region Tan
        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Tan<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Tan);
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> TanInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Tan, true);
        }
        #endregion

        #region Tanh
        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> Tanh<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Tanh);
        }

        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> TanhInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Tanh, true);
        }
        #endregion

        #region TanPi
        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> TanPi<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.TanPi);
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> TanPiInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.TanPi, true);
        }
        #endregion

        #region TrailingZeroCount
        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> TrailingZeroCount<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.TrailingZeroCount);
        }

        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> TrailingZeroCountInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.TrailingZeroCount, true);
        }
        #endregion

        #region Truncate
        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Truncate<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Truncate);
        }

        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> TruncateInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Truncate, true);
        }
        #endregion

        #region Xor
        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Xor<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Xor);
        }

        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="left">The left <see cref="Tensor{T}"/>.</param>
        /// <param name="right">The right <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> XorInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Xor, true);
        }
        #endregion

        #region TensorPrimitivesHelpers
        private delegate void PerformCalculationTFromSpanInTToSpanOut<TFrom, TTo>(ReadOnlySpan<TFrom> input, Span<TTo> output)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>;

        private delegate void PerformCalculationSpanInTInSpanOut<T>(ReadOnlySpan<T> input, T value, Span<T> output)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate void PerformCalculationSpanInSpanOut<T>(ReadOnlySpan<T> input, Span<T> output)
             where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate void PerformCalculationSpanInIntSpanOut<T>(ReadOnlySpan<T> input, Span<int> output)
             where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate T PerformCalculationTwoSpanInTOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate T PerformCalculationSpanInTOut<T>(ReadOnlySpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate void PerformCalculationTwoSpanInSpanOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo, Span<T> output)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private static Tensor<T> TensorPrimitivesHelperSpanInTInSpanOut<T>(Tensor<T> input, T value, PerformCalculationSpanInTInSpanOut<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = inPlace ? input : Create<T>(input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            performCalculation(span, value, ospan);
            return output;
        }

        private static Tensor<int> TensorPrimitivesHelperSpanInIntSpanOut<T>(Tensor<T> input, PerformCalculationSpanInIntSpanOut<T> performCalculation)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<int> output = Create<int>(input.Lengths, input.IsPinned);
            Span<int> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            performCalculation(span, ospan);
            return output;
        }

        private static Tensor<T> TensorPrimitivesHelperSpanInSpanOut<T>(Tensor<T> input, PerformCalculationSpanInSpanOut<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<T> output = inPlace ? input : Create<T>(input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            performCalculation(span, ospan);
            return output;
        }

        private static T TensorPrimitivesHelperSpanInTOut<T>(Tensor<T> input, PerformCalculationSpanInTOut<T> performCalculation)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            return performCalculation(span);
        }

        private static Tensor<TTo> TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(Tensor<TFrom> input, PerformCalculationTFromSpanInTToSpanOut<TFrom, TTo> performCalculation)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            ReadOnlySpan<TFrom> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._flattenedLength);
            Tensor<TTo> output = Create<TTo>(input.Lengths, input.IsPinned);
            Span<TTo> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
            performCalculation(span, ospan);
            return output;
        }

        private static Tensor<T> TensorPrimitivesHelperTwoSpanInSpanOut<T>(Tensor<T> left, Tensor<T> right, PerformCalculationTwoSpanInSpanOut<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (inPlace && !left.Lengths.SequenceEqual(right.Lengths))
                ThrowHelper.ThrowArgument_InPlaceInvalidShape();

            Tensor<T> output;
            if (inPlace)
            {

                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._values[0], (int)left._flattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._values[0], (int)right._flattenedLength);
                output = left;
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._flattenedLength);
                performCalculation(span, rspan, ospan);
            }
            // If not in place but sizes are the same.
            else if (left.Lengths.SequenceEqual(right.Lengths))
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left.AsTensorSpan()._reference, (int)left.FlattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right.AsTensorSpan()._reference, (int)right.FlattenedLength);
                output = Create<T>(left.Lengths, left.IsPinned);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsTensorSpan()._reference, (int)output.FlattenedLength);
                performCalculation(span, rspan, ospan);
                return output;
            }
            // Not in place and broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the right has column contiguous memory (i.e. a 1x5 and a 5x1)

                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);

                var broadcastedLeft = Tensor.BroadcastTo(left, newSize);
                var broadcastedRight = Tensor.BroadcastTo(right, newSize);

                output = Create<T>(newSize, left.IsPinned);
                nint rowLength = newSize[^1];
                Span<T> ospan;
                Span<T> ispan;
                Span<T> buffer = new T[rowLength];

                scoped Span<nint> curIndexes;
                nint[]? curIndexesArray;
                if (newSize.Length> 6)
                {
                    curIndexesArray = ArrayPool<nint>.Shared.Rent(newSize.Length);
                    curIndexes = curIndexesArray;
                }
                else
                {
                    curIndexesArray = null;
                    curIndexes = stackalloc nint[newSize.Length];
                }

                int outputOffset = 0;
                // tensor not row contiguous
                if (broadcastedLeft.Strides[^1] == 0)
                {
                    while (outputOffset < output.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        buffer.Fill(broadcastedLeft[curIndexes]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndexes], (int)rowLength);
                        performCalculation(buffer, ispan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndexes, broadcastedLeft.Lengths);
                    }
                }
                // right now row contiguous
                else if (broadcastedRight.Strides[^1] == 0)
                {
                    while (outputOffset < output.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        buffer.Fill(broadcastedRight[curIndexes]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndexes], (int)rowLength);
                        performCalculation(ispan, buffer, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndexes, broadcastedLeft.Lengths);
                    }
                }
                // both row contiguous
                else
                {
                    Span<T> rspan;
                    while (outputOffset < output.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndexes], (int)rowLength);
                        rspan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndexes], (int)rowLength);
                        performCalculation(ispan, rspan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndexes, broadcastedLeft.Lengths);
                    }
                }

                if (curIndexesArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexesArray);
            }

            return output;
        }

        private static T TensorPrimitivesHelperTwoSpanInTOut<T>(Tensor<T> left, Tensor<T> right, PerformCalculationTwoSpanInTOut<T> performCalculation)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            // If not in place but sizes are the same.
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left.AsTensorSpan()._reference, (int)left.FlattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right.AsTensorSpan()._reference, (int)right.FlattenedLength);
                return performCalculation(span, rspan);
            }
            // Not in place and broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the right has column contiguous memory (i.e. a 1x5 and a 5x1)
                // Because we are returning a single T though we need to actual realize the broadcasts at this point to perform the calculations.

                var broadcastedLeft = Tensor.Broadcast(left, right);
                var broadcastedRight = Tensor.Broadcast(right, left);

                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref broadcastedLeft.AsTensorSpan()._reference, (int)broadcastedLeft.FlattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref broadcastedRight.AsTensorSpan()._reference, (int)broadcastedRight.FlattenedLength);
                return performCalculation(span, rspan);
            }
        }
        #endregion
        #endregion
    }
}
