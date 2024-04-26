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

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        #region ToString
        // REVIEW: WHAT SHOULD WE NAME THIS? WHERE DO WE WANT IT TO LIVE?
        public static string ToString<T>(this SpanND<T> span, int maxRows, int maxColumns) => ((ReadOnlySpanND<T>)span).ToString(maxRows, maxColumns);

        public static string ToString<T>(this ReadOnlySpanND<T> span, int maxRows, int maxColumns)
        {
            var sb = new StringBuilder();
            Span<nint> curIndices = stackalloc nint[span.Rank];
            nint copiedValues = 0;
            while (copiedValues < span._linearLength)
            {
                var sp = new SpanND<T>(ref Unsafe.Add(ref span._reference, SpanNDHelpers.GetIndex(curIndices, span.Strides, span.Shape)), [span.Shape[span.Rank - 1]], [1], span.IsPinned);
                sb.Append('{');
                sb.Append(string.Join(",", sp.ToArray()));
                sb.AppendLine("}");

                SpanNDHelpers.AdjustIndices(span.Rank - 2, 1, ref curIndices, span._lengths);
                copiedValues += span.Shape[span.Rank - 1];
            }

            return sb.ToString();
        }
        #endregion

        #region Resize
        public static Tensor<T> Resize<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var newSize = SpanNDHelpers.CalculateTotalLength(shape);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)newSize, input.IsPinned) : (new T[newSize]);
            Tensor<T> output = new Tensor<T>(values, shape.ToArray(), input.IsPinned);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input.AsSpanND()._reference, (int)input.Length);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsSpanND()._reference, (int)output.Length);
            if (newSize > input.Length)
                SpanNDHelpers.Memmove(ospan, span, input.Length);
            else
                SpanNDHelpers.Memmove(ospan, span, newSize);

            return output;
        }

        public static SpanND<T> Resize<T>(SpanND<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var newSize = SpanNDHelpers.CalculateTotalLength(shape);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)newSize, input.IsPinned) : (new T[newSize]);
            SpanND<T> output = new SpanND<T>(values, shape, input.IsPinned);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            if (newSize > input.Length)
                SpanNDHelpers.Memmove(ospan, span, input.Length);
            else
                SpanNDHelpers.Memmove(ospan, span, newSize);

            return output;
        }
        #endregion

        #region Broadcast

        public static Tensor<T> Broadcast<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            Tensor<T> intermediate = BroadcastTo(input, shape);
            return Tensor.Create(intermediate.ToArray(), intermediate.Shape);
        }

        // Lazy/non-copy broadcasting, internal only for now.
        internal static Tensor<T> BroadcastTo<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
        where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Shape.SequenceEqual(shape))
                return new Tensor<T>(input._values, shape.ToArray(), input.IsPinned);

            if (!TensorHelpers.AreShapesBroadcastCompatible(input.Shape, shape))
                throw new Exception("Shapes are not broadcast compatible.");

            var newSize = SpanNDHelpers.CalculateTotalLength(shape);

            if (newSize == input.Length)
                return Reshape(input, shape);

            var intermediateShape = TensorHelpers.GetIntermediateShape(input.Shape, shape.Length);
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

        internal static SpanND<T> BroadcastTo<T>(SpanND<T> input, ReadOnlySpan<nint> shape)
        where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Shape.SequenceEqual(shape))
                return new SpanND<T>(ref input._reference, shape, input.Strides, input.IsPinned);

            if (!TensorHelpers.AreShapesBroadcastCompatible(input.Shape, shape))
                throw new Exception("Shapes are not broadcast compatible.");

            var newSize = SpanNDHelpers.CalculateTotalLength(shape);

            if (newSize == input.Length)
                return Reshape(input, shape);

            var intermediateShape = TensorHelpers.GetIntermediateShape(input.Shape, shape.Length);
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

            SpanND<T> output = new SpanND<T>(ref input._reference, shape, strides, input.IsPinned);

            return output;
        }
        #endregion

        #region Reverse
        public static Tensor<T> Reverse<T>(Tensor<T> input, nint axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input._linearLength, input.IsPinned) : (new T[input._linearLength]);
            Tensor<T> output = new Tensor<T>(values, input.Shape.ToArray(), input.Strides.ToArray(), input.IsPinned);
            if (axis == -1)
            {
                int index = 0;
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input.Length);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output.Length);
                for (int i = (int)input.Length - 1; i >= 0; i--)
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

                Span<nint> oIndices = stackalloc nint[input.Rank];
                Span<nint> iIndices = stackalloc nint[input.Rank];

                iIndices[(int)axis] = input.Shape[(int)axis] - 1;
                nint copiedValues = 0;
                var islice = input.AsSpanND().Slice(input.Shape);
                var oslice = output.AsSpanND().Slice(output._lengths);
                while (copiedValues < input._linearLength)
                {
                    SpanNDHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, SpanNDHelpers.GetIndex(oIndices, input.Strides, input.Shape)), ref Unsafe.Add(ref islice._reference, SpanNDHelpers.GetIndex(iIndices, islice.Strides, islice.Shape)), copyLength);
                    SpanNDHelpers.AdjustIndices((int)axis, 1, ref oIndices, input._lengths);
                    SpanNDHelpers.AdjustIndicesDown((int)axis, 1, ref iIndices, input._lengths);
                    copiedValues += copyLength;
                }
            }

            return output;
        }

        public static SpanND<T> Reverse<T>(SpanND<T> input, nint axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (axis == -1)
            {
                nint index = input.Length - 1;
                Span<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
                T temp;
                for (int i = 0; i <= input.Length / 2; i++)
                {
                    temp = span[(int)index];
                    span[(int)index] = span[i];
                    span[i] = temp;
                }
            }
            else
            {
                T[] values = new T[input.Length];

                nint copyLength = 1;
                for (nint i = axis; i < input.Shape.Length; i++)
                {
                    copyLength *= input.Shape[(int)i];
                }
                copyLength /= input.Shape[(int)axis];

                Span<nint> oIndices = stackalloc nint[input.Rank];
                Span<nint> iIndices = stackalloc nint[input.Rank];

                iIndices[(int)axis] = input.Shape[(int)axis] - 1;
                nint copiedValues = 0;
                var islice = input.Slice(input.Shape);
                while (copiedValues < input.Length)
                {
                    SpanNDHelpers.Memmove(ref Unsafe.Add(ref values, SpanNDHelpers.GetIndex(oIndices, input.Strides, input.Shape)), ref Unsafe.Add(ref islice._reference, SpanNDHelpers.GetIndex(iIndices, islice.Strides, islice.Shape)), copyLength);
                    SpanNDHelpers.AdjustIndices((int)axis, 1, ref oIndices, input.Shape);
                    SpanNDHelpers.AdjustIndicesDown((int)axis, 1, ref iIndices, input.Shape);
                    copiedValues += copyLength;
                }
                SpanNDHelpers.Memmove(ref input._reference, ref values[0], input.Length);
            }

            return input;
        }

        #endregion

        #region Split
        public static Tensor<T>[] Split<T>(Tensor<T> input, nint numSplits, nint axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Shape[(int)axis] % numSplits != 0)
                throw new Exception("The number of splits must perfectly divide the dimension.");

            Tensor<T>[] outputs = new Tensor<T>[numSplits];

            nint totalToCopy = input.Length / numSplits;
            nint copyLength = 1;
            for (nint i = axis; i < input.Shape.Length; i++)
            {
                copyLength *= input.Shape[(int)i];
            }
            copyLength /= numSplits;
            nint[] newShape = input.Shape.ToArray();
            newShape[(int)axis] = newShape[(int)axis] / numSplits;

            Span<nint> oIndices = stackalloc nint[input.Rank];
            Span<nint> iIndices = stackalloc nint[input.Rank];

            for (int i = 0; i < outputs.Length; i++)
            {
                T[] values = input.IsPinned ? GC.AllocateArray<T>((int)totalToCopy, input.IsPinned) : (new T[(int)totalToCopy]);
                outputs[i] = new Tensor<T>(values, newShape, input.IsPinned);
                oIndices.Clear();
                iIndices.Clear();

                iIndices[(int)axis] = i;
                var islice = input.AsSpanND().Slice(input.Shape);
                var oslice = outputs[i].AsSpanND().Slice(outputs[i]._lengths);

                nint copiedValues = 0;
                while (copiedValues < totalToCopy)
                {
                    SpanNDHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, SpanNDHelpers.GetIndex(oIndices, outputs[0].Strides, outputs[0].Shape)), ref Unsafe.Add(ref islice._reference, SpanNDHelpers.GetIndex(iIndices, islice.Strides, islice.Shape)), copyLength);
                    SpanNDHelpers.AdjustIndices((int)axis, 1, ref oIndices, outputs[i]._lengths);
                    SpanNDHelpers.AdjustIndices((int)axis - 1, 1, ref iIndices, input._lengths);
                    copiedValues += copyLength;
                }
            }

            return outputs;
        }
        #endregion

        #region SetSlice
        // REVIEW: WHAT DO WE WANT TO CALL THIS? COPYTO? IT DOES FIT IN WITH THE EXISTING COPY TO CONVENTIONS FOR VECTOR (albeit backwards).
        public static Tensor<T> SetSlice<T>(this Tensor<T> tensor, Tensor<T> values, params ReadOnlySpan<NativeRange> ranges)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            SpanND<T> srcSpan;
            if (ranges == ReadOnlySpan<NativeRange>.Empty)
            {
                if (!tensor.Shape.SequenceEqual(values.Shape))
                    throw new ArgumentException("When no ranges are specified the values tensor must be equal in size as the input tensor.", nameof(values));
                srcSpan = tensor.AsSpanND().Slice(tensor.Shape);
            }
            else
                srcSpan = tensor.AsSpanND().Slice(ranges);

            if (!srcSpan.Shape.SequenceEqual(values.Shape))
                throw new ArgumentException("Provided values must have the same shape as the input tensor.", nameof(values));

            values.AsSpanND().CopyTo(srcSpan);

            return tensor;
        }
        #endregion

        #region FilteredUpdate
        // REVIEW: PYTORCH/NUMPY DO THIS.
        //  t0[t0 < 2] = -1;
        //  OR SHOULD THIS BE AN OVERLOAD OF FILL THAT TAKES IN A FUNC TO KNOW WHICH ONE TO UPDATE?
        public static Tensor<T> FilteredUpdate<T>(Tensor<T> left, Tensor<bool> filter, T value)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (filter.Shape.Length != left.Shape.Length)
                throw new ArgumentOutOfRangeException(nameof(filter), "Number of dimensions to slice does not equal the number of dimensions in the span");

            var srcSpan = MemoryMarshal.CreateSpan(ref left._values[0], (int)left._linearLength);
            var filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)left._linearLength);

            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                {
                    srcSpan[i] = value;
                }
            }

            return left;
        }

        public static Tensor<T> FilteredUpdate<T>(Tensor<T> left, Tensor<bool> filter, Tensor<T> values)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (filter.Shape.Length != left.Shape.Length)
                throw new ArgumentOutOfRangeException(nameof(filter), "Number of dimensions to slice does not equal the number of dimensions in the span");
            if (values.Rank != 1)
                throw new ArgumentOutOfRangeException(nameof(values), "Must be a 1d tensor");

            nint numTrueElements = SpanNDHelpers.CountTrueElements(filter);
            if (numTrueElements != values._linearLength)
                throw new ArgumentOutOfRangeException(nameof(values), "Number of elements provided does not match the number of filters.");

            Span<T> dstSpan = MemoryMarshal.CreateSpan(ref left._values[0], (int)left._linearLength);
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)left._linearLength);
            Span<T> valuesSpan = MemoryMarshal.CreateSpan(ref values._values[0], (int)values._linearLength);

            int index = 0;
            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                {
                    dstSpan[i] = valuesSpan[index++];
                }
            }

            return left;
        }
        #endregion

        #region SequenceEqual
        public static Tensor<bool> SequenceEqual<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                result = Tensor.Create<bool>(false, left.Shape);

                for (int i = 0; i < left.Length; i++)
                {
                    result._values[i] = left._values[i] == right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                result = Tensor.Create<bool>(false, newSize);
                var broadcastedLeft = BroadcastTo(left, newSize);
                var broadcastedRight = BroadcastTo(right, newSize);
                Span<nint> curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                for (int i = 0; i < broadcastedLeft.Length; i++)
                {
                    result._values[i] = broadcastedLeft[curIndex] == broadcastedRight[curIndex];
                    SpanNDHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
                }
            }

            return result;
        }
        #endregion

        #region LessThan
        public static Tensor<bool> LessThan<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                result = Tensor.Create<bool>(false, left.Shape);

                for (int i = 0; i < left.Length; i++)
                {
                    result._values[i] = left._values[i] < right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                result = Tensor.Create<bool>(false, newSize);
                var broadcastedLeft = BroadcastTo(left, newSize);
                var broadcastedRight = BroadcastTo(right, newSize);
                Span<nint> curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                for (int i = 0; i < broadcastedLeft.Length; i++)
                {
                    result._values[i] = broadcastedLeft[curIndex] < broadcastedRight[curIndex];
                    SpanNDHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
                }
            }

            return result;
        }

        public static Tensor<bool> LessThan<T>(Tensor<T> left, T right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Shape);

            for (int i = 0; i < left.Length; i++)
            {
                result._values[i] = left._values[i] < right;
            }
            return result;
        }

        public static bool LessThanAny<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                for (int i = 0; i < left.Length; i++)
                {
                    if (left._values[i] < right._values[i])
                        return true;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                var broadcastedLeft = BroadcastTo(left, newSize);
                var broadcastedRight = BroadcastTo(right, newSize);
                Span<nint> curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                for (int i = 0; i < broadcastedLeft.Length; i++)
                {
                    if (broadcastedLeft[curIndex] < broadcastedRight[curIndex])
                        return true;
                    SpanNDHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
                }
            }

            return false;
        }

        public static bool LessThanAll<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                for (int i = 0; i < left.Length; i++)
                {
                    if (left._values[i] > right._values[i])
                        return false;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                var broadcastedLeft = BroadcastTo(left, newSize);
                var broadcastedRight = BroadcastTo(right, newSize);
                Span<nint> curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                for (int i = 0; i < broadcastedLeft.Length; i++)
                {
                    if (broadcastedLeft[curIndex] > broadcastedRight[curIndex])
                        return false;
                    SpanNDHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
                }
            }
            return true;
        }
        #endregion

        #region GreaterThan
        public static Tensor<bool> GreaterThan<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result;
            if (TensorHelpers.AreShapesTheSame(left, right))
            {
                result = Tensor.Create<bool>(false, left.Shape);

                for (int i = 0; i < left.Length; i++)
                {
                    result._values[i] = left._values[i] > right._values[i];
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                result = Tensor.Create<bool>(false, newSize);
                var broadcastedLeft = BroadcastTo(left, newSize);
                var broadcastedRight = BroadcastTo(right, newSize);
                Span<nint> curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                for (int i = 0; i < broadcastedLeft.Length; i++)
                {
                    result._values[i] = broadcastedLeft[curIndex] > broadcastedRight[curIndex];
                    SpanNDHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
                }
            }

            return result;
        }

        public static Tensor<bool> GreaterThan<T>(Tensor<T> left, T right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Shape);

            for (int i = 0; i < left.Length; i++)
            {
                result._values[i] = left._values[i] > right;
            }
            return result;
        }

        public static bool GreaterThanAny<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreShapesTheSame(left, right))
            {

                for (int i = 0; i < left.Length; i++)
                {
                    if (left._values[i] > right._values[i])
                        return true;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                var broadcastedLeft = BroadcastTo(left, newSize);
                var broadcastedRight = BroadcastTo(right, newSize);
                Span<nint> curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                for (int i = 0; i < broadcastedLeft.Length; i++)
                {
                    if (broadcastedLeft[curIndex] > broadcastedRight[curIndex])
                        return true;
                    SpanNDHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
                }
            }
            return false;
        }

        public static bool GreaterThanAll<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            if (TensorHelpers.AreShapesTheSame(left, right))
            {

                for (int i = 0; i < left.Length; i++)
                {
                    if (left._values[i] < right._values[i])
                        return false;
                }
            }
            else
            {
                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);
                var broadcastedLeft = BroadcastTo(left, newSize);
                var broadcastedRight = BroadcastTo(right, newSize);
                Span<nint> curIndex = stackalloc nint[broadcastedRight.Shape.Length];
                for (int i = 0; i < broadcastedLeft.Length; i++)
                {
                    if (broadcastedLeft[curIndex] < broadcastedRight[curIndex])
                        return false;
                    SpanNDHelpers.AdjustIndices(broadcastedRight.Rank - 1, 1, ref curIndex, broadcastedRight.Shape);
                }
            }
            return true;
        }
        #endregion

        #region Stack
        // REVIEW: NEEDS A DIFFERENT NAME?
        // JUST AN OVERLOAD FOR CONCATENATE?
        public static Tensor<T> Stack<T>(Tensor<T>[] input, int axis = 0)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Length < 2)
                throw new ArgumentException("Must provide at least 2 tensors to Stack.");
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
        public static Tensor<T> Reshape<T>(this Tensor<T> input, params ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    throw new ArgumentException("Provided dimensions can only include 1 wildcard.");
                var tempTotal = input._linearLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                arrLengths[lengths.IndexOf(-1)] = tempTotal;

            }

            var tempLinear = SpanNDHelpers.CalculateTotalLength(ref arrLengths);
            if (tempLinear != input.Length)
                throw new ArgumentException("Provided dimensions are not valid for reshaping");
            var strides = SpanNDHelpers.CalculateStrides(arrLengths.Length, arrLengths);
            return new Tensor<T>(input._values, arrLengths, strides.ToArray(), input.IsPinned);
        }

        public static SpanND<T> Reshape<T>(this SpanND<T> input, params ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    throw new ArgumentException("Provided dimensions can only include 1 wildcard.");
                var tempTotal = input.Length;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                arrLengths[lengths.IndexOf(-1)] = tempTotal;

            }

            var tempLinear = SpanNDHelpers.CalculateTotalLength(ref arrLengths);
            if (tempLinear != input.Length)
                throw new ArgumentException("Provided dimensions are not valid for reshaping");
            var strides = SpanNDHelpers.CalculateStrides(arrLengths.Length, arrLengths);
            return new SpanND<T>(ref input._reference, arrLengths, strides, input.IsPinned);
        }
        #endregion

        #region Squeeze
        // REVIEW: NAME?
        public static Tensor<T> Squeeze<T>(Tensor<T> input, int axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (axis >= input.Rank)
                throw new ArgumentException("Cannot select an axis greater than the current Rank");

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
                strides = SpanNDHelpers.CalculateStrides(lengths.Length, lengths);
            }
            else
            {
                if (input.Shape[axis] != 1)
                {
                    throw new ArgumentException("Cannot select an axis to squeeze which has size not equal to one");
                }
                for (int i = 0; i < input.Shape.Length; i++)
                {
                    if (i != axis)
                    {
                        tempLengths.Add(input.Shape[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = SpanNDHelpers.CalculateStrides(lengths.Length, lengths);
            }

            return new Tensor<T>(input._values, lengths, strides, input.IsPinned);
        }
        #endregion

        #region Unsqueeze
        // REVIEW: NAME? NUMPY CALLS THIS expand_dims.
        public static Tensor<T> Unsqueeze<T>(Tensor<T> input, int axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (axis > input.Shape.Length)
                throw new ArgumentException("Cannot select an axis less greater than the current Rank");
            if (axis < 0)
                axis = input.Rank - axis;

            List<nint> tempLengths = input._lengths.ToList();
            tempLengths.Insert(axis, 1);
            var lengths = tempLengths.ToArray();
            var strides = SpanNDHelpers.CalculateStrides(lengths.Length, lengths);
            return new Tensor<T>(input._values, lengths, strides, input.IsPinned);
        }
        #endregion

        #region Concatenate
        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="axis">The axis along which the tensors will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
        /// <returns></returns>
        public static Tensor<T> Concatenate<T>(ReadOnlySpan<Tensor<T>> tensors, int axis = 0)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (tensors.Length < 2)
                throw new ArgumentException("Must provide at least 2 tensors to concatenate");

            if (axis < -1 || axis > tensors[0].Rank)
                throw new ArgumentException("Invalid axis provided");

            // Calculate total space needed.
            nint totalLength = 0;
            for (int i = 0; i < tensors.Length; i++)
                totalLength += SpanNDHelpers.CalculateTotalLength(tensors[i].Shape);

            nint sumOfAxis = 0;
            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (axis != -1)
            {
                sumOfAxis = tensors[0].Shape[axis];
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (tensors[0].Rank != tensors[i].Rank)
                        throw new ArgumentException("The arrays must have the same shape, except in the dimension corresponding to axis.");
                    for (int j = 0; j < tensors[0].Rank; j++)
                    {
                        if (j != axis)
                        {
                            if (tensors[0].Shape[j] != tensors[i].Shape[j])
                                throw new ArgumentException("The arrays must have the same shape, except in the dimension corresponding to axis.");
                        }
                    }
                    sumOfAxis += tensors[i].Shape[axis];
                }
            }

            T[] values = tensors[0].IsPinned ? GC.AllocateArray<T>((int)totalLength, tensors[0].IsPinned) : (new T[totalLength]);
            var dstSpan = MemoryMarshal.CreateSpan(ref values[0], (int)totalLength);
            nint valuesCopied = 0;
            Span<nint> indices = stackalloc nint[tensors[0].Rank];
            nint srcIndex;
            nint copyLength;

            while (valuesCopied < totalLength)
            {
                for (int i = 0; i < tensors.Length; i++)
                {
                    srcIndex = SpanNDHelpers.GetIndex(indices, tensors[i].Strides, tensors[i].Shape);
                    copyLength = CalculateCopyLength(tensors[i].Shape, axis);
                    var srcSpan = MemoryMarshal.CreateSpan(ref tensors[i]._values[srcIndex], (int)copyLength);
                    SpanNDHelpers.Memmove(dstSpan, srcSpan, copyLength, valuesCopied);
                    valuesCopied += copyLength;
                }
                SpanNDHelpers.AdjustIndices(axis - 1, 1, ref indices, tensors[0].Shape);
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
        // REVIEW: ADD IN ONES THAT TAKE AXIS.
        public static T StdDev<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>, IPowerFunctions<T>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>

        {
            T mean = Mean(input);
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var output = new T[input._linearLength].AsSpan();
            TensorPrimitives.Subtract(span, mean, output);
            TensorPrimitives.Abs(output, output);
            TensorPrimitives.Pow((ReadOnlySpan<T>)output, T.CreateChecked(2), output);
            T sum = TensorPrimitives.Sum((ReadOnlySpan<T>)output);
            return T.CreateChecked(sum / T.CreateChecked(input.Length));
        }

        public static TResult StdDev<T, TResult>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Tensor.Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.Length));
        }

        #endregion

        #region Mean
        // REVIEW: OTHER MATH OPERATIONS LIKE MEDIAN/MODE.
        public static T Mean<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>

        {
            T sum = Tensor.Sum(input);
            return T.CreateChecked(sum / T.CreateChecked(input.Length));
        }

        public static TResult Mean<T, TResult>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Tensor.Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.Length));
        }

        #endregion

        #region Permute/Transpose
        public static Tensor<T> Transpose<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Shape.Length < 2)
                throw new ArgumentException("Must provide a tensor with at least 2 dimensions to transpose it.");
            var axis = Enumerable.Range(0, input.Rank).ToArray();
            var temp = axis[input.Rank - 1];
            axis[input.Rank - 1] = axis[input.Rank - 2];
            axis[input.Rank - 2] = temp;
            return Permute(input, axis.AsSpan());
        }

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
                SpanND<T> ospan;
                SpanND<T> ispan;
                ReadOnlySpan<int> permutation;

                if (axis.IsEmpty)
                {
                    lengths = input._lengths.Reverse().ToArray();
                    permutation = Enumerable.Range(0, input.Rank).Reverse().ToArray();
                }
                else
                {
                    if (axis.Length != input.Shape.Length)
                        throw new ArgumentException("Must provide an axis order for each axis");
                    for (int i = 0; i < lengths.Length; i++)
                        lengths[i] = input.Shape[axis[i]];
                    permutation = axis.ToArray();
                }
                tensor = new Tensor<T>(values, lengths, Array.Empty<nint>(), input._isPinned);
                Span<nint> permutedIndices = stackalloc nint[tensor.Rank];

                ospan = tensor.AsSpanND();
                ispan = input.AsSpanND();
                Span<nint> indices = stackalloc nint[tensor.Rank];
                for (int i = 0; i < input._linearLength; i++)
                {
                    TensorHelpers.PermuteIndices(indices, permutedIndices, permutation);
                    ospan[permutedIndices] = ispan[indices];
                    SpanNDHelpers.AdjustIndices(tensor.Rank - 1, 1, ref indices, input._lengths);
                }

                return tensor;
            }
        }
        #endregion

        #region TensorPrimitives
        #region Multiply
        public static Tensor<T> Multiply<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        public static Tensor<T> MultiplyInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        public static Tensor<T> Multiply<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(left, right, TensorPrimitives.Multiply);
        }

        public static Tensor<T> MultiplyInPlace<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Multiply, true);
        }

        public static SpanND<T> Multiply<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.Length, input.IsPinned) : (new T[input.Length]);
            SpanND<T> output = new SpanND<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        public static SpanND<T> MultiplyInPlace<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        public static SpanND<T> Multiply<T>(SpanND<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Multiply);
        }

        public static SpanND<T> MultiplyInPlace<T>(SpanND<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Multiply, true);
        }
        #endregion

        #region Divide
        public static Tensor<T> Divide<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        public static Tensor<T> DivideInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        public static Tensor<T> Divide<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        public static Tensor<T> DivideInPlace<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        public static Tensor<T> Divide<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Divide);
        }

        public static Tensor<T> DivideInPlace<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Divide, true);
        }

        public static SpanND<T> Divide<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.Length, input.IsPinned) : (new T[input.Length]);
            SpanND<T> output = new SpanND<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        public static SpanND<T> DivideInPlace<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        public static SpanND<T> Divide<T>(T val, SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.Length, input.IsPinned) : (new T[input.Length]);
            SpanND<T> output = new SpanND<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        public static SpanND<T> DivideInPlace<T>(T val, SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        public static SpanND<T> Divide<T>(SpanND<T> input, SpanND<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Divide);
        }

        public static SpanND<T> DivideInPlace<T>(SpanND<T> input, SpanND<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Divide, true);
        }

        #endregion

        #region Subtract
        public static Tensor<T> Subtract<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        public static Tensor<T> SubtractInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        public static Tensor<T> Subtract<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        public static Tensor<T> SubtractInPlace<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        public static Tensor<T> Subtract<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Subtract);
        }

        public static Tensor<T> SubtractInPlace<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Subtract, true);
        }

        public static SpanND<T> Subtract<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.Length, input.IsPinned) : (new T[input.Length]);
            SpanND<T> output = new SpanND<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        public static SpanND<T> SubtractInPlace<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        public static SpanND<T> Subtract<T>(T val, SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.Length, input.IsPinned) : (new T[input.Length]);
            SpanND<T> output = new SpanND<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        public static SpanND<T> SubtractInPlace<T>(T val, SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        public static SpanND<T> Subtract<T>(SpanND<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Subtract);
        }

        public static SpanND<T> SubtractInPlace<T>(SpanND<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Subtract, true);
        }

        #endregion

        #region Sum

        public static T Sum<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            return TensorPrimitives.Sum(span);
        }

        public static T Sum<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            return TensorPrimitives.Sum(span);
        }

        #endregion

        #region Add
        public static Tensor<T> Add<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Add);
        }

        public static Tensor<T> AddInPlace<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Add, true);
        }

        public static Tensor<T> Add<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Add(span, val, ospan);
            return output;
        }

        public static Tensor<T> AddInPlace<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            TensorPrimitives.Add(span, val, ospan);
            return output;
        }

        public static SpanND<T> Add<T>(SpanND<T> input, SpanND<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Add);
        }

        public static SpanND<T> AddInPlace<T>(SpanND<T> input, SpanND<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Add, true);
        }

        public static SpanND<T> Add<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.Length, input.IsPinned) : (new T[input.Length]);
            SpanND<T> output = new SpanND<T>(values, input.Shape, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Add(span, val, ospan);
            return output;
        }

        public static SpanND<T> AddInPlace<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            TensorPrimitives.Add(span, val, ospan);
            return output;
        }

        #endregion

        #region Norm
        public static T Norm<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            return TensorPrimitives.Norm(span);
        }

        public static T Norm<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            return TensorPrimitives.Norm(span);
        }

        #endregion

        #region Cos
        public static Tensor<T> Cos<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Cos);
        }

        public static SpanND<T> Cos<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Cos);
        }

        public static Tensor<T> CosInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Cos, true);
        }

        public static SpanND<T> CosInPlace<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Cos, true);
        }

        #endregion

        #region Sin
        public static Tensor<T> Sin<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin);
        }

        public static SpanND<T> Sin<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin);
        }

        public static Tensor<T> SinInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin, true);
        }

        public static SpanND<T> SinInPlace<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin, true);
        }

        #endregion

        #region Sqrt
        public static Tensor<T> Sqrt<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt);
        }

        public static SpanND<T> Sqrt<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt);
        }

        public static Tensor<T> SqrtInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt, true);
        }

        public static SpanND<T> SqrtInPlace<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt, true);
        }

        #endregion

        #region Log
        public static Tensor<T> Log<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log);
        }

        public static SpanND<T> Log<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log);
        }

        public static Tensor<T> LogInPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log, true);
        }

        public static SpanND<T> LogInPlace<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log, true);
        }

        #endregion

        #region Log10
        public static Tensor<T> Log10<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10);
        }

        public static SpanND<T> Log10<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10);
        }

        public static Tensor<T> Log10InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10, true);
        }

        public static SpanND<T> Log10InPlace<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10, true);
        }
        #endregion

        #region Log2
        public static Tensor<T> Log2<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log2);
        }

        public static SpanND<T> Log2<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log2);
        }

        public static Tensor<T> Log2InPlace<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log2, true);
        }

        public static SpanND<T> Log2InPlace<T>(SpanND<T> input)
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

        private static SpanND<T> TensorPrimitivesHelperT1<T>(SpanND<T> input, PerformCalculationT1<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.Length);
            SpanND<T> output = inPlace ? input : Create<T>(input.IsPinned, input.Shape);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
            performCalculation(span, ospan);
            return output;
        }

        private static Tensor<T> TensorPrimitivesHelperT1T2<T>(Tensor<T> left, Tensor<T> right, PerformCalculationT1T2<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (inPlace && left.Shape != right.Shape)
                throw new ArgumentException("In place operations require the same shape for both tensors");

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
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left.AsSpanND()._reference, (int)left.Length);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right.AsSpanND()._reference, (int)right.Length);
                output = Create<T>(left.IsPinned, left.Shape);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsSpanND()._reference, (int)output.Length);
                performCalculation(span, rspan, ospan);
                return output;
            }
            // Not in place and broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the other has column contiguous memory (i.e. a 1x5 and a 5x1)

                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);

                var broadcastedLeft = Tensor.BroadcastTo(left, newSize);
                var broadcastedRight = Tensor.BroadcastTo(right, newSize);

                output = Create<T>(left.IsPinned, newSize);
                var rowLength = newSize[^1];
                Span<T> ospan;
                Span<T> ispan;
                Span<T> buffer = new T[rowLength];
                Span<nint> curIndex = stackalloc nint[newSize.Length];
                int outputOffset = 0;
                // left not row contiguous
                if (broadcastedLeft.Strides[^1] == 0)
                {
                    while (outputOffset < output.Length)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        buffer.Fill(broadcastedLeft[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndex], (int)rowLength);
                        performCalculation(buffer, ispan, ospan);
                        outputOffset += (int)rowLength;
                        SpanNDHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }
                // right now row contiguous
                else if (broadcastedRight.Strides[^1] == 0)
                {
                    while (outputOffset < output.Length)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        buffer.Fill(broadcastedRight[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndex], (int)rowLength);
                        performCalculation(ispan, buffer, ospan);
                        outputOffset += (int)rowLength;
                        SpanNDHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }
                // both row contiguous
                else
                {
                    Span<T> rspan;
                    while (outputOffset < output.Length)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref output._values[outputOffset], (int)rowLength);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndex], (int)rowLength);
                        rspan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndex], (int)rowLength);
                        performCalculation(ispan, rspan, ospan);
                        outputOffset += (int)rowLength;
                        SpanNDHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }
            }
            return output;
        }

        private static SpanND<T> TensorPrimitivesHelperT1T2<T>(SpanND<T> left, SpanND<T> right, PerformCalculationT1T2<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (inPlace && left.Shape != right.Shape)
                throw new ArgumentException("In place operations require the same shape for both spans");

            //ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left.LinearLength);
            //ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.LinearLength);
            //SpanND<T> output = inPlace ? left : Create<T>(left.IsPinned, left.Lengths);
            //Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            //performCalculation(span, rspan, ospan);
            //return output;

            SpanND<T> output;
            if (inPlace)
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left.Length);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.Length);
                output = left;
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
                performCalculation(span, rspan, ospan);
            }
            // If not in place but sizes are the same.
            else if (left.Shape.SequenceEqual(right.Shape))
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left.Length);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.Length);
                output = Create<T>(left.IsPinned, left.Shape);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.Length);
                performCalculation(span, rspan, ospan);
                return output;
            }
            // Not in place and broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the other has column contiguous memory (i.e. a 1x5 and a 5x1)

                nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Shape, right.Shape);

                var broadcastedLeft = Tensor.BroadcastTo(left, newSize);
                var broadcastedRight = Tensor.BroadcastTo(right, newSize);

                output = Create<T>(left.IsPinned, newSize);
                var rowLength = newSize[^1];
                Span<T> ospan;
                Span<T> ispan;
                Span<T> buffer = new T[rowLength];
                Span<nint> curIndex = stackalloc nint[newSize.Length];
                int outputOffset = 0;
                // left not row contiguous
                if (broadcastedLeft.Strides[^1] == 0)
                {
                    while (outputOffset < output.Length)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        buffer.Fill(broadcastedLeft[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndex], (int)rowLength);
                        performCalculation(buffer, ispan, ospan);
                        outputOffset += (int)rowLength;
                        SpanNDHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }
                // right now row contiguous
                else if (broadcastedRight.Strides[^1] == 0)
                {
                    while (outputOffset < output.Length)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        buffer.Fill(broadcastedRight[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndex], (int)rowLength);
                        performCalculation(ispan, buffer, ospan);
                        outputOffset += (int)rowLength;
                        SpanNDHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }
                // both row contiguous
                else
                {
                    Span<T> rspan;
                    while (outputOffset < output.Length)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndex], (int)rowLength);
                        rspan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndex], (int)rowLength);
                        performCalculation(ispan, rspan, ospan);
                        outputOffset += (int)rowLength;
                        SpanNDHelpers.AdjustIndices(broadcastedLeft.Rank - 2, 1, ref curIndex, broadcastedLeft.Shape);
                    }
                }
            }
            return output;
        }
        #endregion
        #endregion
    }
}
