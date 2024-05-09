// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.VisualBasic;
using System.Text;
using System.Buffers;
using System.Xml.Linq;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        #region ToString
        // REVIEW: WHAT SHOULD WE NAME THIS? WHERE DO WE WANT IT TO LIVE?
        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="span">The <see cref="TensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maxRows">Max rows to show.</param>
        /// <param name="maxColumns">Max columns to show.</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="span"/></returns>
        public static string ToString<T>(this TensorSpan<T> span, int maxRows, int maxColumns) => ((ReadOnlyTensorSpan<T>)span).ToString(maxRows, maxColumns);

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="ReadOnlyTensorSpan{T}"/>."/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The <see cref="ReadOnlyTensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maxRows">Max rows to show.</param>
        /// <param name="maxColumns">Max columns to show.</param>
        public static string ToString<T>(this ReadOnlyTensorSpan<T> span, int maxRows, int maxColumns)
        {
            var sb = new StringBuilder();
            scoped Span<nint> curIndices;
            nint[]? curIndicesArray;
            if (span.Rank > 6)
            {
                curIndicesArray = ArrayPool<nint>.Shared.Rent(span.Rank);
                curIndices = curIndicesArray;
            }
            else
            {
                curIndicesArray = null;
                curIndices = stackalloc nint[span.Rank];
            }

            nint copiedValues = 0;
            while (copiedValues < span._linearLength)
            {
                var sp = new TensorSpan<T>(ref Unsafe.Add(ref span._reference, TensorSpanHelpers.ComputeLinearIndex(curIndices, span.Strides, span.Shape)), [span.Shape[span.Rank - 1]], [1], span.IsPinned);
                sb.Append('{');
                sb.Append(string.Join(",", sp.ToArray()));
                sb.AppendLine("}");

                TensorSpanHelpers.AdjustIndices(span.Rank - 2, 1, ref curIndices, span._lengths);
                copiedValues += span.Shape[span.Rank - 1];
            }

            if (curIndicesArray != null)
                ArrayPool<nint>.Shared.Return(curIndicesArray);

            return sb.ToString();
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)newSize, input.IsPinned) : (new T[newSize]);
            Tensor<T> output = new Tensor<T>(values, shape.ToArray(), input.IsPinned);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input.AsTensorSpan()._reference, (int)input.LinearLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsTensorSpan()._reference, (int)output.LinearLength);
            if (newSize > input.LinearLength)
                TensorSpanHelpers.Memmove(ospan, span, input.LinearLength);
            else
                TensorSpanHelpers.Memmove(ospan, span, newSize);

            return output;
        }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/>, allocates new managed memory, and copies the data from <paramref name="input"/>. If the final shape is smaller all data after
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        public static TensorSpan<T> Resize<T>(TensorSpan<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)newSize, input.IsPinned) : (new T[newSize]);
            TensorSpan<T> output = new TensorSpan<T>(values, shape, input.IsPinned);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            if (newSize > input.LinearLength)
                TensorSpanHelpers.Memmove(ospan, span, input.LinearLength);
            else
                TensorSpanHelpers.Memmove(ospan, span, newSize);

            return output;
        }
        #endregion

        #region Broadcast
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
            return Tensor.Create(intermediate.ToArray(), intermediate.Shape);
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
            if (input.Shape.SequenceEqual(shape))
                return new Tensor<T>(input._values, shape.ToArray(), input.IsPinned);

            if (!TensorHelpers.AreShapesBroadcastCompatible(input.Shape, shape))
                ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);

            if (newSize == input.LinearLength)
                return Reshape(input, shape);

            nint[] intermediateShape = TensorHelpers.GetIntermediateShape(input.Shape, shape.Length);
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

            Tensor<T> output = new Tensor<T>(input._values, shape.ToArray(), strides.ToArray(), input.IsPinned);

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
        internal static TensorSpan<T> BroadcastTo<T>(TensorSpan<T> input, ReadOnlySpan<nint> shape)
        where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Shape.SequenceEqual(shape))
                return new TensorSpan<T>(ref input._reference, shape, input.Strides, input.IsPinned);

            if (!TensorHelpers.AreShapesBroadcastCompatible(input.Shape, shape))
                ThrowHelper.ThrowArgument_ShapesNotBroadcastCompatible();

            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);

            if (newSize == input.LinearLength)
                return Reshape(input, shape);

            nint[] intermediateShape = TensorHelpers.GetIntermediateShape(input.Shape, shape.Length);
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

            TensorSpan<T> output = new TensorSpan<T>(ref input._reference, shape, strides, input.IsPinned);

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
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input._linearLength, input.IsPinned) : (new T[input._linearLength]);
            Tensor<T> output = new Tensor<T>(values, input.Shape.ToArray(), input.Strides.ToArray(), input.IsPinned);
            if (axis == -1)
            {
                int index = 0;
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input.LinearLength);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output.LinearLength);
                for (int i = (int)input.LinearLength - 1; i >= 0; i--)
                {
                    ospan[index++] = span[i];
                }
            }
            else
            {
                nint copyLength = 1;
                for (nint i = axis; i < input.Shape.Length; i++)
                {
                    copyLength *= input.Shape[(int)i];
                }
                copyLength /= input.Shape[(int)axis];

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

                iIndices[(int)axis] = input.Shape[(int)axis] - 1;
                nint copiedValues = 0;
                TensorSpan<T> islice = input.AsTensorSpan().Slice(input.Shape);
                TensorSpan<T> oslice = output.AsTensorSpan().Slice(output._lengths);
                while (copiedValues < input._linearLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, TensorSpanHelpers.ComputeLinearIndex(oIndices, input.Strides, input.Shape)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Shape)), copyLength);
                    TensorSpanHelpers.AdjustIndices((int)axis, 1, ref oIndices, input._lengths);
                    TensorSpanHelpers.AdjustIndicesDown((int)axis, 1, ref iIndices, input._lengths);
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

        /// <summary>
        /// Reverse the order of elements in the <paramref name="input"/> along the given axis. The shape of the tensor is preserved, but the elements are reordered.
        /// <paramref name="axis"/> defaults to -1 when not provided, which reverses the entire span.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="axis">Axis along which to reverse over. The default, -1, will reverse over all of the axes of the left span.</param>
        public static TensorSpan<T> Reverse<T>(TensorSpan<T> input, nint axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (axis == -1)
            {
                nint index = input.LinearLength - 1;
                Span<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
                T temp;
                for (int i = 0; i <= input.LinearLength / 2; i++)
                {
                    temp = span[(int)index];
                    span[(int)index] = span[i];
                    span[i] = temp;
                }
            }
            else
            {
                T[] values = new T[input.LinearLength];

                nint copyLength = 1;
                for (nint i = axis; i < input.Shape.Length; i++)
                {
                    copyLength *= input.Shape[(int)i];
                }
                copyLength /= input.Shape[(int)axis];

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

                iIndices[(int)axis] = input.Shape[(int)axis] - 1;
                nint copiedValues = 0;
                TensorSpan<T> islice = input.Slice(input.Shape);
                while (copiedValues < input.LinearLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref values, TensorSpanHelpers.ComputeLinearIndex(oIndices, input.Strides, input.Shape)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Shape)), copyLength);
                    TensorSpanHelpers.AdjustIndices((int)axis, 1, ref oIndices, input.Shape);
                    TensorSpanHelpers.AdjustIndicesDown((int)axis, 1, ref iIndices, input.Shape);
                    copiedValues += copyLength;
                }
                TensorSpanHelpers.Memmove(ref input._reference, ref values[0], input.LinearLength);

                if (oIndicesArray != null && iIndicesArray != null)
                {
                    ArrayPool<nint>.Shared.Return(oIndicesArray);
                    ArrayPool<nint>.Shared.Return(iIndicesArray);
                }
            }

            return input;
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
            if (input.Shape[(int)axis] % numSplits != 0)
                ThrowHelper.ThrowArgument_SplitNotSplitEvenly();

            Tensor<T>[] outputs = new Tensor<T>[numSplits];

            nint totalToCopy = input.LinearLength / numSplits;
            nint copyLength = 1;
            for (nint i = axis; i < input.Shape.Length; i++)
            {
                copyLength *= input.Shape[(int)i];
            }
            copyLength /= numSplits;
            nint[] newShape = input.Shape.ToArray();
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
                T[] values = input.IsPinned ? GC.AllocateArray<T>((int)totalToCopy, input.IsPinned) : (new T[(int)totalToCopy]);
                outputs[i] = new Tensor<T>(values, newShape, input.IsPinned);
                oIndices.Clear();
                iIndices.Clear();

                iIndices[(int)axis] = i;
                TensorSpan<T> islice = input.AsTensorSpan().Slice(input.Shape);
                TensorSpan<T> oslice = outputs[i].AsTensorSpan().Slice(outputs[i]._lengths);

                nint copiedValues = 0;
                while (copiedValues < totalToCopy)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, TensorSpanHelpers.ComputeLinearIndex(oIndices, outputs[0].Strides, outputs[0].Shape)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Shape)), copyLength);
                    TensorSpanHelpers.AdjustIndices((int)axis, 1, ref oIndices, outputs[i]._lengths);
                    TensorSpanHelpers.AdjustIndices((int)axis - 1, 1, ref iIndices, input._lengths);
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
                if (!tensor.Shape.SequenceEqual(values.Shape))
                    ThrowHelper.ThrowArgument_SetSliceNoRange(nameof(values));
                srcSpan = tensor.AsTensorSpan().Slice(tensor.Shape);
            }
            else
                srcSpan = tensor.AsTensorSpan().Slice(ranges);

            if (!srcSpan.Shape.SequenceEqual(values.Shape))
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
            if (filter.Shape.Length != tensor.Shape.Length)
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(filter));

            Span<T> srcSpan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)tensor._linearLength);

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
            if (filter.Shape.Length != tensor.Shape.Length)
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(filter));
            if (values.Rank != 1)
                ThrowHelper.ThrowArgument_1DTensorRequired(nameof(values));

            nint numTrueElements = TensorHelpers.CountTrueElements(filter);
            if (numTrueElements != values._linearLength)
                ThrowHelper.ThrowArgument_IncorrectNumberOfFilterItems(nameof(values));

            Span<T> dstSpan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)tensor._linearLength);
            Span<T> valuesSpan = MemoryMarshal.CreateSpan(ref values._values[0], (int)values._linearLength);

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
                result = Tensor.Create<bool>(false, left.Shape);

                for (int i = 0; i < left.LinearLength; i++)
                {
                    result._values[i] = left._values[i] == right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                result = Tensor.Create<bool>(false, newSize);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;

                if (broadcastedRight.Shape.Length > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Shape.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                }

                for (int i = 0; i < broadcastedLeft.LinearLength; i++)
                {
                    result._values[i] = broadcastedLeft[curIndex] == broadcastedRight[curIndex];
                    TensorSpanHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
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
                result = Tensor.Create<bool>(false, left.Shape);

                for (int i = 0; i < left.LinearLength; i++)
                {
                    result._values[i] = left._values[i] < right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                result = Tensor.Create<bool>(false, newSize);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;

                if (broadcastedRight.Shape.Length > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Shape.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                }

                for (int i = 0; i < broadcastedLeft.LinearLength; i++)
                {
                    result._values[i] = broadcastedLeft[curIndex] < broadcastedRight[curIndex];
                    TensorSpanHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
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
            Tensor<bool> result = Tensor.Create<bool>(false, left.Shape);

            for (int i = 0; i < left.LinearLength; i++)
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
                for (int i = 0; i < left.LinearLength; i++)
                {
                    if (left._values[i] < right._values[i])
                        return true;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;

                if (broadcastedRight.Shape.Length > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Shape.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                }

                for (int i = 0; i < broadcastedLeft.LinearLength; i++)
                {
                    if (broadcastedLeft[curIndex] < broadcastedRight[curIndex])
                        return true;
                    TensorSpanHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
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
                for (int i = 0; i < left.LinearLength; i++)
                {
                    if (left._values[i] > right._values[i])
                        return false;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;

                if (broadcastedRight.Shape.Length > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Shape.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                }

                for (int i = 0; i < broadcastedLeft.LinearLength; i++)
                {
                    if (broadcastedLeft[curIndex] > broadcastedRight[curIndex])
                        return false;
                    TensorSpanHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
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
                result = Tensor.Create<bool>(false, left.Shape);

                for (int i = 0; i < left.LinearLength; i++)
                {
                    result._values[i] = left._values[i] > right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                result = Tensor.Create<bool>(false, newSize);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;

                if (broadcastedRight.Shape.Length > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Shape.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                }

                for (int i = 0; i < broadcastedLeft.LinearLength; i++)
                {
                    result._values[i] = broadcastedLeft[curIndex] > broadcastedRight[curIndex];
                    TensorSpanHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
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
            Tensor<bool> result = Tensor.Create<bool>(false, left.Shape);

            for (int i = 0; i < left.LinearLength; i++)
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

                for (int i = 0; i < left.LinearLength; i++)
                {
                    if (left._values[i] > right._values[i])
                        return true;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;

                if (broadcastedRight.Shape.Length > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Shape.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                }

                for (int i = 0; i < broadcastedLeft.LinearLength; i++)
                {
                    if (broadcastedLeft[curIndex] > broadcastedRight[curIndex])
                        return true;
                    TensorSpanHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
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

                for (int i = 0; i < left.LinearLength; i++)
                {
                    if (left._values[i] < right._values[i])
                        return false;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                Tensor<T> broadcastedLeft = BroadcastTo(left, newSize);
                Tensor<T> broadcastedRight = BroadcastTo(right, newSize);

                scoped Span<nint> curIndex;
                nint[]? curIndexArray;

                if (broadcastedRight.Shape.Length > 6)
                {
                    curIndexArray = ArrayPool<nint>.Shared.Rent(broadcastedRight.Shape.Length);
                    curIndex = curIndexArray;
                }
                else
                {
                    curIndexArray = null;
                    curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                }

                for (int i = 0; i < broadcastedLeft.LinearLength; i++)
                {
                    if (broadcastedLeft[curIndex] < broadcastedRight[curIndex])
                        return false;
                    TensorSpanHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
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
                nint tempTotal = input._linearLength;
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
            if (tempLinear != input.LinearLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
            nint[] strides = TensorSpanHelpers.CalculateStrides(arrLengths);
            return new Tensor<T>(input._values, arrLengths, strides, input.IsPinned);
        }

        /// <summary>
        /// Reshapes the <paramref name="input"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="input"><see cref="TensorSpan{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static TensorSpan<T> Reshape<T>(this TensorSpan<T> input, params ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            nint[] arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = input.LinearLength;
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
            if (tempLinear != input.LinearLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
            nint[] strides = TensorSpanHelpers.CalculateStrides(arrLengths);
            return new TensorSpan<T>(ref input._reference, arrLengths, strides, input.IsPinned);
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
                for (int i = 0; i < input.Shape.Length; i++)
                {
                    if (input.Shape[i] != 1)
                    {
                        tempLengths.Add(input.Shape[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }
            else
            {
                if (input.Shape[axis] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                for (int i = 0; i < input.Shape.Length; i++)
                {
                    if (i != axis)
                    {
                        tempLengths.Add(input.Shape[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = TensorSpanHelpers.CalculateStrides(lengths);
            }

            return new Tensor<T>(input._values, lengths, strides, input.IsPinned);
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
            if (axis > input.Shape.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (axis < 0)
                axis = input.Rank - axis;

            List<nint> tempLengths = input._lengths.ToList();
            tempLengths.Insert(axis, 1);
            nint[] lengths = tempLengths.ToArray();
            nint[] strides = TensorSpanHelpers.CalculateStrides(lengths);
            return new Tensor<T>(input._values, lengths, strides, input.IsPinned);
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
                totalLength += TensorSpanHelpers.CalculateTotalLength(tensors[i].Shape);

            nint sumOfAxis = 0;
            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (axis != -1)
            {
                sumOfAxis = tensors[0].Shape[axis];
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (tensors[0].Rank != tensors[i].Rank)
                        ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                    for (int j = 0; j < tensors[0].Rank; j++)
                    {
                        if (j != axis)
                        {
                            if (tensors[0].Shape[j] != tensors[i].Shape[j])
                                ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                        }
                    }
                    sumOfAxis += tensors[i].Shape[axis];
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
                    srcIndex = TensorSpanHelpers.ComputeLinearIndex(curIndex, tensors[i].Strides, tensors[i].Shape);
                    copyLength = CalculateCopyLength(tensors[i].Shape, axis);
                    Span<T> srcSpan = MemoryMarshal.CreateSpan(ref tensors[i]._values[srcIndex], (int)copyLength);
                    TensorSpanHelpers.Memmove(dstSpan, srcSpan, copyLength, valuesCopied);
                    valuesCopied += copyLength;
                }
                TensorSpanHelpers.AdjustIndices(axis - 1, 1, ref curIndex, tensors[0].Shape);
            }

            Tensor<T> tensor;
            if (axis == -1)
            {
                tensor = new Tensor<T>(values, [valuesCopied], tensors[0].IsPinned);
            }
            else
            {
                nint[] lengths = new nint[tensors[0].Rank];
                tensors[0].Shape.CopyTo(lengths);
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
            Span<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Span<T> output = new T[input._linearLength].AsSpan();
            TensorPrimitives.Subtract(span, mean, output);
            TensorPrimitives.Abs(output, output);
            TensorPrimitives.Pow((ReadOnlySpan<T>)output, T.CreateChecked(2), output);
            T sum = TensorPrimitives.Sum((ReadOnlySpan<T>)output);
            return T.CreateChecked(sum / T.CreateChecked(input.LinearLength));
        }

        /// <summary>
        /// Return the standard deviation of the elements in the <paramref name="input"/> tensor. Casts the return value to <typeparamref name="TResult"/>.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the standard deviation of.</param>
        /// <returns><typeparamref name="TResult"/> representing the standard deviation.</returns>
        public static TResult StdDev<T, TResult>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Tensor.Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.LinearLength));
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
            T sum = Tensor.Sum(input);
            return T.CreateChecked(sum / T.CreateChecked(input.LinearLength));
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
            T sum = Tensor.Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.LinearLength));
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
            if (input.Shape.Length < 2)
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
                T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input._linearLength, input.IsPinned) : (new T[input._linearLength]);
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
                    if (axis.Length != input.Shape.Length)
                        ThrowHelper.ThrowArgument_PermuteAxisOrder();
                    for (int i = 0; i < lengths.Length; i++)
                        lengths[i] = input.Shape[axis[i]];
                    permutation = axis.ToArray();
                }
                tensor = new Tensor<T>(values, lengths, Array.Empty<nint>(), input._isPinned);

                ospan = tensor.AsTensorSpan();
                ispan = input.AsTensorSpan();

                scoped Span<nint> indices;
                nint[]? indicesArray;
                scoped Span<nint> permutedIndices;
                nint[]? permutedIndicesArray;
                if (tensor.Rank > 6)
                {
                    indicesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                    indices = indicesArray;
                    permutedIndicesArray = ArrayPool<nint>.Shared.Rent(tensor.Rank);
                    permutedIndices = permutedIndicesArray;
                }
                else
                {
                    indicesArray = null;
                    indices = stackalloc nint[tensor.Rank];
                    permutedIndicesArray = null;
                    permutedIndices = stackalloc nint[tensor.Rank];
                }

                for (int i = 0; i < input._linearLength; i++)
                {
                    TensorHelpers.PermuteIndices(indices, permutedIndices, permutation);
                    ospan[permutedIndices] = ispan[indices];
                    TensorSpanHelpers.AdjustIndices(tensor.Rank - 1, 1, ref indices, input._lengths);
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
        #region Multiply
        /// <summary>
        /// Multiplies each element of <paramref name="input"/> with <paramref name="val"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="Tensor{T}"/></param>
        /// <param name="val"><typeparamref name="T"/> value to multiply by.</param>
        public static Tensor<T> Multiply<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Multiply);
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
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Multiply, true);
        }

        /// <summary>
        /// Multiplies each element of <paramref name="input"/> with <paramref name="val"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/></param>
        /// <param name="val"><typeparamref name="T"/> value to multiply by.</param>
        public static TensorSpan<T> Multiply<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            TensorSpan<T> output = new TensorSpan<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="input"/> with <paramref name="val"/> in place.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/></param>
        /// <param name="val"><typeparamref name="T"/> value to multiply by.</param>
        public static TensorSpan<T> MultiplyInPlace<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="left"/> with <paramref name="right"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="TensorSpan{T}"/> for multiplication.</param>
        /// <param name="right">Right <see cref="TensorSpan{T}"/> for multiplication.</param>
        public static TensorSpan<T> Multiply<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Multiply);
        }

        /// <summary>
        /// Multiplies each element of <paramref name="left"/> with <paramref name="right"/> in place.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="TensorSpan{T}"/> for multiplication.</param>
        /// <param name="right">Right <see cref="TensorSpan{T}"/> for multiplication.</param>
        public static TensorSpan<T> MultiplyInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Multiply, true);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Divide);
        }

        /// <summary>
        /// Divides each element of <paramref name="left"/> by its corresponding element in <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> to be divided.</param>
        /// <param name="right">The <see cref="Tensor{T}"/> divisor.</param>
        public static Tensor<T> DivideInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Divide, true);
        }

        /// <summary>
        /// Divides each element of <paramref name="input"/> by <paramref name="val"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="val">The divisor</param>
        public static TensorSpan<T> Divide<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            TensorSpan<T> output = new TensorSpan<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Divides each element of <paramref name="input"/> by <paramref name="val"/> in place.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="val">The divisor</param>
        public static TensorSpan<T> DivideInPlace<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Divides <paramref name="val"/> by each element of <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result."/>
        /// </summary>
        /// <param name="val">The value to be divided.</param>
        /// <param name="input">The <see cref="TensorSpan{T}"/> divisor.</param>
        public static TensorSpan<T> Divide<T>(T val, TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            TensorSpan<T> output = new TensorSpan<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        /// <summary>
        /// Divides <paramref name="val"/> by each element of <paramref name="input"/> in place.
        /// </summary>
        /// <param name="val">The value to be divided.</param>
        /// <param name="input">The <see cref="TensorSpan{T}"/> divisor.</param>
        public static TensorSpan<T> DivideInPlace<T>(T val, TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        /// <summary>
        /// Divides each element of <paramref name="left"/> by its corresponding element in <paramref name="right"/> and returns
        /// a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="TensorSpan{T}"/> to be divided.</param>
        /// <param name="right">The <see cref="TensorSpan{T}"/> divisor.</param>
        public static TensorSpan<T> Divide<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Divide);
        }

        /// <summary>
        /// Divides each element of <paramref name="left"/> by its corresponding element in <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="TensorSpan{T}"/> to be divided.</param>
        /// <param name="right">The <see cref="TensorSpan{T}"/> divisor.</param>
        public static TensorSpan<T> DivideInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Divide, true);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
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
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Subtract);
        }

        /// <summary>
        /// Subtracts each element of <paramref name="left"/> from <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> with values to be subtracted from.</param>
        /// <param name="right">The <see cref="Tensor{T}"/> with values to subtract.</param>
        public static Tensor<T> SubtractInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Subtract, true);
        }

        /// <summary>
        /// Subtracts <paramref name="val"/> from each element of <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> with values to be subtracted from.</param>
        /// <param name="val">The <typeparamref name="T"/> value to subtract.</param>
        public static TensorSpan<T> Subtract<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            TensorSpan<T> output = new TensorSpan<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Subtracts <paramref name="val"/> from each element of <paramref name="input"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> with values to be subtracted from.</param>
        /// <param name="val">The <typeparamref name="T"/> value to subtract.</param>
        public static TensorSpan<T> SubtractInPlace<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="input"/> from <paramref name="val"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="val">The <typeparamref name="T"/> value to be subtracted from.</param>
        /// <param name="input">The <see cref="TensorSpan{T}"/> values to subtract.</param>
        public static TensorSpan<T> Subtract<T>(T val, TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            TensorSpan<T> output = new TensorSpan<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="input"/> from <paramref name="val"/> in place.
        /// </summary>
        /// <param name="val">The <typeparamref name="T"/> value to be subtracted from.</param>
        /// <param name="input">The <see cref="TensorSpan{T}"/> values to subtract.</param>
        public static TensorSpan<T> SubtractInPlace<T>(T val, TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="left"/> from <paramref name="right"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="TensorSpan{T}"/> of values to be subtracted from.</param>
        /// <param name="right">The <see cref="TensorSpan{T}"/>of values to subtract.</param>
        public static TensorSpan<T> Subtract<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Subtract);
        }

        /// <summary>
        /// Subtracts each element of <paramref name="left"/> from <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="TensorSpan{T}"/> of values to be subtracted from.</param>
        /// <param name="right">The <see cref="TensorSpan{T}"/>of values to subtract.</param>
        public static TensorSpan<T> SubtractInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Subtract, true);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            return TensorPrimitives.Sum(span);
        }

        /// <summary>
        /// Sums all the elements of the <see cref="TensorSpan{T}"/> and returns the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to sum.</param>
        public static T Sum<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            return TensorPrimitives.Sum(span);
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
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Add);
        }

        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="right">The second <see cref="Tensor{T}"/> of values to add.</param>
        public static Tensor<T> AddInPlace<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Add, true);
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> to add to each element of <paramref name="input"/>.</param>
        public static Tensor<T> Add<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Add(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> to add to each element of <paramref name="input"/>.</param>
        public static Tensor<T> AddInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Add(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The first <see cref="TensorSpan{T}"/> of elements to add.</param>
        /// <param name="right">The second <see cref="TensorSpan{T}"/> of elements to add.</param>
        public static TensorSpan<T> Add<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Add);
        }

        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The first <see cref="TensorSpan{T}"/> of values to add.</param>
        /// <param name="right">The second <see cref="TensorSpan{T}"/> of values to add.</param>
        public static TensorSpan<T> AddInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Add, true);
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> value to add to each element of <paramref name="input"/>.</param>
        public static TensorSpan<T> Add<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            TensorSpan<T> output = new TensorSpan<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Add(span, val, ospan);
            return output;
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> value to add to each element of <paramref name="input"/>.</param>
        public static TensorSpan<T> AddInPlace<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Add(span, val, ospan);
            return output;
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            return TensorPrimitives.Norm(span);
        }

        /// <summary>
        ///  Takes the norm of the <see cref="TensorSpan{T}"/> and returns the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the norm of.</param>
        public static T Norm<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            return TensorPrimitives.Norm(span);
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
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Cos);
        }

        /// <summary>
        /// Takes the cosine of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the cosine of.</param>
        public static TensorSpan<T> Cos<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Cos);
        }

        /// <summary>
        /// Takes the cosine of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the cosine of.</param>
        public static Tensor<T> CosInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Cos, true);
        }

        /// <summary>
        /// Takes the cosine of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the cosine of.</param>
        public static TensorSpan<T> CosInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Cos, true);
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
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin);
        }

        /// <summary>
        /// Takes the sin of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Sin<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin);
        }

        /// <summary>
        /// Takes the sin of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static Tensor<T> SinInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin, true);
        }

        /// <summary>
        /// Takes the sin of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> SinInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin, true);
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
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt);
        }

        /// <summary>
        /// Takes the square root of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the square root of.</param>
        public static TensorSpan<T> Sqrt<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt);
        }

        /// <summary>
        /// Takes the square root of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the square root of.</param>
        public static Tensor<T> SqrtInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt, true);
        }

        /// <summary>
        /// Takes the square root of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the square root of.</param>
        public static TensorSpan<T> SqrtInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt, true);
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
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log);
        }

        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the natural logarithm of.</param>
        public static TensorSpan<T> Log<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log);
        }

        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> LogInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log, true);
        }

        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the natural logarithm of.</param>
        public static TensorSpan<T> LogInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log, true);
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
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10);
        }

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static TensorSpan<T> Log10<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10);
        }

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10, true);
        }

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static TensorSpan<T> Log10InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10, true);
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
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log2);
        }

        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static TensorSpan<T> Log2<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log2);
        }

        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="Tensor{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log2, true);
        }

        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static TensorSpan<T> Log2InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log2, true);
        }

        #endregion

        #region TensorPrimitivesHelpers
        private delegate void PerformCalculationT1<T>(ReadOnlySpan<T> input, Span<T> output)
             where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate void PerformCalculationT1T2<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo, Span<T> output)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private static Tensor<T> TensorPrimitivesHelperT1<T>(Tensor<T> input, PerformCalculationT1<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = inPlace ? input : Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            performCalculation(span, ospan);
            return output;
        }

        private static TensorSpan<T> TensorPrimitivesHelperT1<T>(TensorSpan<T> input, PerformCalculationT1<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            TensorSpan<T> output = inPlace ? input : Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            performCalculation(span, ospan);
            return output;
        }

        private static Tensor<T> TensorPrimitivesHelperT1T2<T>(Tensor<T> left, Tensor<T> right, PerformCalculationT1T2<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (inPlace && left.Shape != right.Shape)
                ThrowHelper.ThrowArgument_InPlaceInvalidShape();

            Tensor<T> output;
            if (inPlace)
            {

                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._values[0], (int)left._linearLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._values[0], (int)right._linearLength);
                output = left;
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
                performCalculation(span, rspan, ospan);
            }
            // If not in place but sizes are the same.
            else if (left.Shape.SequenceEqual(right.Shape))
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left.AsTensorSpan()._reference, (int)left.LinearLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right.AsTensorSpan()._reference, (int)right.LinearLength);
                output = Create<T>(left.IsPinned, left.Shape);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsTensorSpan()._reference, (int)output.LinearLength);
                performCalculation(span, rspan, ospan);
                return output;
            }
            // Not in place and broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the right has column contiguous memory (i.e. a 1x5 and a 5x1)

                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);

                var broadcastedLeft = Tensor.BroadcastTo(left, newSize);
                var broadcastedRight = Tensor.BroadcastTo(right, newSize);

                output = Create<T>(left.IsPinned, newSize);
                nint rowLength = newSize[^1];
                Span<T> ospan;
                Span<T> ispan;
                Span<T> buffer = new T[rowLength];

                scoped Span<nint> curIndices;
                nint[]? curIndicesArray;
                if (newSize.Length> 6)
                {
                    curIndicesArray = ArrayPool<nint>.Shared.Rent(newSize.Length);
                    curIndices = curIndicesArray;
                }
                else
                {
                    curIndicesArray = null;
                    curIndices = stackalloc nint[newSize.Length];
                }

                int outputOffset = 0;
                // tensor not row contiguous
                if (broadcastedLeft.Strides[^1] == 0)
                {
                    while (outputOffset < output.LinearLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        buffer.Fill(broadcastedLeft[curIndices]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndices], (int)rowLength);
                        performCalculation(buffer, ispan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndices, broadcastedLeft.Shape);
                    }
                }
                // right now row contiguous
                else if (broadcastedRight.Strides[^1] == 0)
                {
                    while (outputOffset < output.LinearLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        buffer.Fill(broadcastedRight[curIndices]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndices], (int)rowLength);
                        performCalculation(ispan, buffer, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndices, broadcastedLeft.Shape);
                    }
                }
                // both row contiguous
                else
                {
                    Span<T> rspan;
                    while (outputOffset < output.LinearLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndices], (int)rowLength);
                        rspan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndices], (int)rowLength);
                        performCalculation(ispan, rspan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndices, broadcastedLeft.Shape);
                    }
                }

                if (curIndicesArray != null)
                    ArrayPool<nint>.Shared.Return(curIndicesArray);
            }

            return output;
        }

        private static TensorSpan<T> TensorPrimitivesHelperT1T2<T>(TensorSpan<T> left, TensorSpan<T> right, PerformCalculationT1T2<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (inPlace && left.Shape != right.Shape)
                ThrowHelper.ThrowArgument_InPlaceInvalidShape();

            //ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref tensor._reference, (int)tensor.LinearLength);
            //ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.LinearLength);
            //TensorSpan<T> output = inPlace ? tensor : Create<T>(tensor.IsPinned, tensor.Lengths);
            //Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            //performCalculation(span, rspan, ospan);
            //return output;

            TensorSpan<T> output;
            if (inPlace)
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left.LinearLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.LinearLength);
                output = left;
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
                performCalculation(span, rspan, ospan);
            }
            // If not in place but sizes are the same.
            else if (left.Shape.SequenceEqual(right.Shape))
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left.LinearLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.LinearLength);
                output = Create<T>(left.IsPinned, left.Shape);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
                performCalculation(span, rspan, ospan);
                return output;
            }
            // Not in place and broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the right has column contiguous memory (i.e. a 1x5 and a 5x1)

                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);

                TensorSpan<T> broadcastedLeft = Tensor.BroadcastTo(left, newSize);
                TensorSpan<T> broadcastedRight = Tensor.BroadcastTo(right, newSize);

                output = Create<T>(left.IsPinned, newSize);
                nint rowLength = newSize[^1];
                Span<T> ospan;
                Span<T> ispan;
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
                    while (outputOffset < output.LinearLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        buffer.Fill(broadcastedLeft[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndex], (int)rowLength);
                        performCalculation(buffer, ispan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }
                // right not row contiguous
                else if (broadcastedRight.Strides[^1] == 0)
                {
                    while (outputOffset < output.LinearLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        buffer.Fill(broadcastedRight[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndex], (int)rowLength);
                        performCalculation(ispan, buffer, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }
                // both row contiguous
                else
                {
                    Span<T> rspan;
                    while (outputOffset < output.LinearLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndex], (int)rowLength);
                        rspan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndex], (int)rowLength);
                        performCalculation(ispan, rspan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }

                if (curIndexArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexArray);
            }
            return output;
        }
        #endregion
        #endregion
    }
}
