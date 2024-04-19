// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        #region Resize
        public static Tensor<T> Resize<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var newSize = SpanHelpers.CalculateTotalLength(shape);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)newSize, input.IsPinned) : (new T[newSize]);
            Tensor<T> output = new Tensor<T>(values, shape.ToArray(), input.IsPinned);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input.AsSpan()._reference, (int)input.LinearLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsSpan()._reference, (int)output.LinearLength);
            if (newSize > input.LinearLength)
                SpanHelpers.Memmove(ospan, span, input.LinearLength);
            else
                SpanHelpers.Memmove(ospan, span, newSize);

            return output;
        }

        public static SpanND<T> Resize<T>(SpanND<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var newSize = SpanHelpers.CalculateTotalLength(shape);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)newSize, input.IsPinned) : (new T[newSize]);
            SpanND<T> output = new SpanND<T>(values, shape, input.IsPinned);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            if (newSize > input.LinearLength)
                SpanHelpers.Memmove(ospan, span, input.LinearLength);
            else
                SpanHelpers.Memmove(ospan, span, newSize);

            return output;
        }
        #endregion

        #region Broadcast
        public static Tensor<T> BroadcastTo<T>(Tensor<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (!AreShapesBroadcastToCompatible(input.Lengths, shape))
                throw new Exception("Shapes are not broadcast compatible.");

            var newSize = SpanHelpers.CalculateTotalLength(shape);

            if (newSize == input.LinearLength)
                return Reshape(input, shape);

            var intermediateShape = GetIntermediateShape(input.Lengths, shape.Length);
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

        public static bool AreShapesBroadcastToCompatible<T>(Tensor<T> tensor1, Tensor<T> tensor2)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => AreShapesBroadcastToCompatible(tensor1.Lengths, tensor2.Lengths);


        public static bool AreShapesBroadcastToCompatible(ReadOnlySpan<nint> shape1, ReadOnlySpan<nint> shape2)
        {
            var shape1Index = shape1.Length - 1;
            var shape2Index = shape2.Length - 1;

            bool areCompatible = true;

            nint s1;
            nint s2;

            while (shape1Index >= 0 || shape2Index >= 0)
            {
                // if a dimension is missing in one of the shapes, it is considered to be 1
                if (shape1Index < 0)
                    s1 = 1;
                else
                    s1 = shape1[shape1Index--];

                if (shape2Index < 0)
                    s2 = 1;
                else
                    s2 = shape2[shape2Index--];

                if (s1 == s2 || (s1 == 1 && s2 != 1) || (s1 == 1 && s2 != 1)) { }
                else
                {
                    areCompatible = false;
                    break;
                }
            }

            return areCompatible;
        }

        public static nint[] GetIntermediateShape(ReadOnlySpan<nint> shape1, int shape2Length)
        {
            var shape1Index = shape1.Length - 1;
            var newShapeIndex = Math.Max(shape1.Length, shape2Length) - 1;
            nint[] newShape = new nint[Math.Max(shape1.Length, shape2Length)];

            while (newShapeIndex >= 0)
            {
                // if a dimension is missing in one of the shapes, it is considered to be 1
                if (shape1Index < 0)
                    newShape[newShapeIndex--] = 1;
                else
                    newShape[newShapeIndex--] = shape1[shape1Index--];
            }

            return newShape;
        }
        #endregion

        #region Reverse
        public static Tensor<T> Reverse<T>(Tensor<T> input, nint axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input._linearLength, input.IsPinned) : (new T[input._linearLength]);
            Tensor<T> output = new Tensor<T>(values, input.Lengths.ToArray(), input.Strides.ToArray(), input.IsPinned);
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
                for (nint i = axis; i < input.Lengths.Length; i++)
                {
                    copyLength *= input.Lengths[(int)i];
                }
                copyLength /= input.Lengths[(int)axis];

                var oIndices = new nint[input.Rank];
                var iIndices = new nint[input.Rank];

                iIndices[axis] = input.Lengths[(int)axis] - 1;
                nint copiedValues = 0;
                var islice = input.AsSpan().Slice(input.Lengths);
                var oslice = output.AsSpan().Slice(output._lengths);
                while (copiedValues < input._linearLength)
                {
                    SpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, SpanHelpers.GetIndex(oIndices, input.Strides, input.Lengths)), ref Unsafe.Add(ref islice._reference, SpanHelpers.GetIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    SpanHelpers.AdjustIndices((int)axis, 1, ref oIndices, input._lengths);
                    SpanHelpers.AdjustIndicesDown((int)axis, 1, ref iIndices, input._lengths);
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
                for (nint i = axis; i < input.Lengths.Length; i++)
                {
                    copyLength *= input.Lengths[(int)i];
                }
                copyLength /= input.Lengths[(int)axis];

                var oIndices = new nint[input.Rank];
                var iIndices = new nint[input.Rank];

                iIndices[axis] = input.Lengths[(int)axis] - 1;
                nint copiedValues = 0;
                var islice = input.Slice(input.Lengths);
                while (copiedValues < input.LinearLength)
                {
                    SpanHelpers.Memmove(ref Unsafe.Add(ref values, SpanHelpers.GetIndex(oIndices, input.Strides, input.Lengths)), ref Unsafe.Add(ref islice._reference, SpanHelpers.GetIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    SpanHelpers.AdjustIndices((int)axis, 1, ref oIndices, input.Lengths);
                    SpanHelpers.AdjustIndicesDown((int)axis, 1, ref iIndices, input.Lengths);
                    copiedValues += copyLength;
                }
                SpanHelpers.Memmove(ref input._reference, ref values[0], input.LinearLength);
            }

            return input;
        }

        #endregion

        #region Split
        public static Tensor<T>[] Split<T>(Tensor<T> input, nint numSplits, nint axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Lengths[(int)axis] % numSplits != 0)
                throw new Exception("The number of splits must perfectly divide the dimension.");

            Tensor<T>[] outputs = new Tensor<T>[numSplits];

            nint totalToCopy = input.LinearLength / numSplits;
            nint copyLength = 1;
            for (nint i = axis; i < input.Lengths.Length; i++)
            {
                copyLength *= input.Lengths[(int)i];
            }
            copyLength /= numSplits;
            nint[] newShape = input.Lengths.ToArray();
            newShape[(int)axis] = newShape[(int)axis] / numSplits;

            for (int i = 0; i < outputs.Length; i++)
            {
                T[] values = input.IsPinned ? GC.AllocateArray<T>((int)totalToCopy, input.IsPinned) : (new T[(int)totalToCopy]);
                outputs[i] = new Tensor<T>(values, newShape, input.IsPinned);

                var oIndices = new nint[input.Rank];
                var iIndices = new nint[input.Rank];
                iIndices[axis] = i;
                var islice = input.AsSpan().Slice(input.Lengths);
                var oslice = outputs[i].AsSpan().Slice(outputs[i]._lengths);

                nint copiedValues = 0;
                while (copiedValues < totalToCopy)
                {
                    SpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, SpanHelpers.GetIndex(oIndices, outputs[0].Strides, outputs[0].Lengths)), ref Unsafe.Add(ref islice._reference, SpanHelpers.GetIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    SpanHelpers.AdjustIndices((int)axis, 1, ref oIndices, outputs[i]._lengths);
                    SpanHelpers.AdjustIndices((int)axis - 1, 1, ref iIndices, input._lengths);
                    copiedValues += copyLength;
                }
            }

            return outputs;
        }

        //public static SpanND<T>[] Split<T>(SpanND<T> input, nint numSplits, nint axis)
        //    where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        //{
        //    if (input.Lengths[(int)axis] % numSplits != 0)
        //        throw new Exception("The number of splits must perfectly divide the dimension.");

        //    SpanND<T>[] outputs = new SpanND<T>[numSplits];

        //    nint totalToCopy = input.LinearLength / numSplits;
        //    nint copyLength = 1;
        //    for (nint i = axis; i < input.Lengths.Length; i++)
        //    {
        //        copyLength *= input.Lengths[(int)i];
        //    }
        //    copyLength /= numSplits;
        //    nint[] newShape = input.Lengths.ToArray();
        //    newShape[(int)axis] = newShape[(int)axis] / numSplits;

        //    for (int i = 0; i < outputs.Length; i++)
        //    {
        //        T[] values = input.IsPinned ? GC.AllocateArray<T>((int)totalToCopy, input.IsPinned) : (new T[(int)totalToCopy]);
        //        outputs[i] = new SpanND<T>(values, newShape, input.IsPinned);

        //        var oIndices = new nint[input.Rank];
        //        var iIndices = new nint[input.Rank];
        //        iIndices[axis] = i;
        //        var islice = input.Slice(input.Lengths);
        //        var oslice = outputs[i].Slice(outputs[i].Lengths);

        //        nint copiedValues = 0;
        //        while (copiedValues < totalToCopy)
        //        {
        //            SpanHelpers.Memmove(ref Unsafe.Add(ref oslice._reference, SpanHelpers.GetIndex(oIndices, outputs[0].Strides, outputs[0].Lengths)), ref Unsafe.Add(ref islice._reference, SpanHelpers.GetIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
        //            SpanHelpers.AdjustIndices((int)axis, 1, ref oIndices, outputs[i].Lengths);
        //            SpanHelpers.AdjustIndices((int)axis - 1, 1, ref iIndices, input.Lengths);
        //            copiedValues += copyLength;
        //        }
        //    }

        //    return outputs;
        //}
        #endregion

        #region SetSlice
        // REVIEW: NOT IN DESIGN DOC BUT NEEDED FOR NIKLAS NOTEBOOK.
        // REVIEW: WHAT DO WE WANT TO CALL THIS? COPYTO? IT DOES FIT IN WITH THE EXISTING COPY TO CONVENTIONS FOR VECTOR.
        public static Tensor<T> SetSlice<T>(this Tensor<T> tensor, Tensor<T> values, params NativeRange[] ranges)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            SpanND<T> srcSpan;
            if (ranges == Array.Empty<NativeRange>())
            {
                if (!tensor.Lengths.SequenceEqual(values.Lengths))
                    throw new ArgumentException("When no ranges are specified the values tensor must be equal in size as the input tensor.", nameof(values));
                srcSpan = tensor.AsSpan().Slice(tensor.Lengths);
            }
            else
                srcSpan = tensor.AsSpan().Slice(ranges);

            if (!srcSpan.Lengths.SequenceEqual(values.Lengths))
                throw new ArgumentException("Provided values must have the same shape as the input tensor.", nameof(values));

            values.AsSpan().CopyTo(srcSpan);

            return tensor;
        }
        #endregion

        #region FilteredUpdate
        // REVIEW: NOT IN DESIGN DOC BUT NEEDED FOR NIKLAS NOTEBOOK.
        // REVIEW: PYTORCH/NUMPY DO THIS.
        //  t0[t0 < 2] = -1;
        //  OR SHOULD THIS BE AN OVERLOAD OF FILL THAT TAKES IN A FUNC TO KNOW WHICH ONE TO UPDATE?
        public static Tensor<T> FilteredUpdate<T>(Tensor<T> left, Tensor<bool> filter, T value)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (filter.Lengths.Length != left.Lengths.Length)
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
            if (filter.Lengths.Length != left.Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(filter), "Number of dimensions to slice does not equal the number of dimensions in the span");
            if (values.Rank != 1)
                throw new ArgumentOutOfRangeException(nameof(values), "Must be a 1d tensor");

            var numTrueElements = SpanHelpers.CountTrueElements(filter);
            if (numTrueElements != values._linearLength)
                throw new ArgumentOutOfRangeException(nameof(values), "Number of elements provided does not match the number of filters.");

            var srcSpan = MemoryMarshal.CreateSpan(ref left._values[0], (int)left._linearLength);
            var filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)left._linearLength);
            var valuesSpan = MemoryMarshal.CreateSpan(ref values._values[0], (int)values._linearLength);

            var index = 0;
            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                {
                    srcSpan[i] = valuesSpan[index++];
                }
            }

            return left;
        }
        #endregion

        #region SequenceEqual
        // REVIEW: THIS NEEDS TO SUPPORT BROADCASTING AND ADD APPROPRIATE CHECKING.
        public static Tensor<bool> SequenceEqual<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] == right._values[i];
            }
            return result;
        }
        #endregion

        #region LessThan
        // REVIEW: ALL OF THESE NEED TO SUPPORT BROADCASTING AND ADD APPROPRIATE CHECKING.
        public static Tensor<bool> LessThan<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] < right._values[i];
            }
            return result;
        }

        public static Tensor<bool> LessThan<T>(Tensor<T> left, T right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] < right;
            }
            return result;
        }

        public static bool LessThanAny<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            for (int i = 0; i < left.LinearLength; i++)
            {
                if (left._values[i] < right._values[i])
                    return true;
            }
            return false;
        }

        public static bool LessThanAll<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            for (int i = 0; i < left.LinearLength; i++)
            {
                if (left._values[i] > right._values[i])
                    return false;
            }
            return true;
        }
        #endregion

        #region GreaterThan
        // REVIEW: ALL OF THESE NEED TO SUPPORT BROADCASTING AND ADD APPROPRIATE CHECKING.
        public static Tensor<bool> GreaterThan<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] > right._values[i];
            }
            return result;
        }

        public static Tensor<bool> GreaterThan<T>(Tensor<T> left, T right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] > right;
            }
            return result;
        }

        public static bool GreaterThanAny<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            for (int i = 0; i < left.LinearLength; i++)
            {
                if (left._values[i] > right._values[i])
                    return true;
            }
            return false;
        }

        public static bool GreaterThanAll<T>(Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            for (int i = 0; i < left.LinearLength; i++)
            {
                if (left._values[i] < right._values[i])
                    return false;
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
        public static Tensor<T> Reshape<T>(this Tensor<T> input, params nint[] lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => Reshape(input, lengths.AsSpan());

        public static Tensor<T> Reshape<T>(this Tensor<T> input, ReadOnlySpan<nint> lengths)
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

            var tempLinear = SpanHelpers.CalculateTotalLength(ref arrLengths);
            if (tempLinear != input.LinearLength)
                throw new ArgumentException("Provided dimensions are not valid for reshaping");
            var strides = SpanHelpers.CalculateStrides(arrLengths.Length, arrLengths);
            return new Tensor<T>(input._values, arrLengths, strides.ToArray(), input.IsPinned);
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
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (input.Lengths[i] != 1)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = SpanHelpers.CalculateStrides(lengths.Length, lengths);
            }
            else
            {
                if (input.Lengths[axis] != 1)
                {
                    throw new ArgumentException("Cannot select an axis to squeeze which has size not equal to one");
                }
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (i != axis)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = SpanHelpers.CalculateStrides(lengths.Length, lengths);
            }

            return new Tensor<T>(input._values, lengths, strides, input.IsPinned);
        }
        #endregion

        #region Unsqueeze
        // REVIEW: NAME? NUMPY CALLS THIS expand_dims.
        public static Tensor<T> Unsqueeze<T>(Tensor<T> input, int axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (axis > input.Lengths.Length)
                throw new ArgumentException("Cannot select an axis less greater than the current Rank");
            if (axis < 0)
                axis = input.Rank - axis;

            List<nint> tempLengths = input._lengths.ToList();
            tempLengths.Insert(axis, 1);
            var lengths = tempLengths.ToArray();
            var strides = SpanHelpers.CalculateStrides(lengths.Length, lengths);
            return new Tensor<T>(input._values, lengths, strides, input.IsPinned);
        }
        #endregion

        #region Concatenate
        //REVIEW: SHOULD AXIS BE NULLABLE INT SO NULL CAN BE PROVIDED INSTEAD OF -1? SENTINAL VALUE?
        /// <summary>
        /// Join a sequence of arrays along an existing axis.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tensors">The arrays must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="axis">The axis along which the arrays will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
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
                totalLength += SpanHelpers.CalculateTotalLength(tensors[i].Lengths);

            nint sumOfAxis = 0;
            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (axis != -1)
            {
                sumOfAxis = tensors[0].Lengths[axis];
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (tensors[0].Rank != tensors[i].Rank)
                        throw new ArgumentException("The arrays must have the same shape, except in the dimension corresponding to axis.");
                    for (int j = 0; j < tensors[0].Rank; j++)
                    {
                        if (j != axis)
                        {
                            if (tensors[0].Lengths[j] != tensors[i].Lengths[j])
                                throw new ArgumentException("The arrays must have the same shape, except in the dimension corresponding to axis.");
                        }
                    }
                    sumOfAxis += tensors[i].Lengths[axis];
                }
            }

            T[] values = tensors[0].IsPinned ? GC.AllocateArray<T>((int)totalLength, tensors[0].IsPinned) : (new T[totalLength]);
            var dstSpan = MemoryMarshal.CreateSpan(ref values[0], (int)totalLength);
            nint valuesCopied = 0;
            nint[] indices = new nint[tensors[0].Rank];
            nint srcIndex;
            nint copyLength;

            while (valuesCopied < totalLength)
            {
                for (int i = 0; i < tensors.Length; i++)
                {
                    srcIndex = SpanHelpers.GetIndex(indices, tensors[i].Strides, tensors[i].Lengths);
                    copyLength = CalculateCopyLength(tensors[i].Lengths, axis);
                    var srcSpan = MemoryMarshal.CreateSpan(ref tensors[i]._values[srcIndex], (int)copyLength);
                    SpanHelpers.Memmove(dstSpan, srcSpan, copyLength, valuesCopied);
                    valuesCopied += copyLength;
                }
                SpanHelpers.AdjustIndices(axis - 1, 1, ref indices, tensors[0].Lengths);
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
            return T.CreateChecked(sum / T.CreateChecked(input.LinearLength));
        }

        public static TResult StdDev<T, TResult>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Tensor.Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.LinearLength));
        }
        #endregion

        #region Mean
        // REVIEW: OTHER MATH OPERATIONS LIKE MEDIAN/MODE.
        public static T Mean<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>

        {
            T sum = Tensor.Sum(input);
            return T.CreateChecked(sum / T.CreateChecked(input.LinearLength));
        }

        public static Tensor<T> Mean<T>(Tensor<T> input, int axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T> => throw new NotImplementedException();

        public static TResult Mean<T, TResult>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Tensor.Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.LinearLength));
        }

        public static Tensor<TResult> Mean<T, TResult>(Tensor<T> input, int axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult> => throw new NotImplementedException();
        #endregion

        #region Permute/Transpose
        public static Tensor<T> Transpose<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Lengths.Length < 2)
                throw new ArgumentException("Must provide a tensor with at least 2 dimensions to transpose it.");
            var axis = Enumerable.Range(0, input.Rank).ToArray();
            var temp = axis[input.Rank - 1];
            axis[input.Rank - 1] = axis[input.Rank - 2];
            axis[input.Rank - 2] = temp;
            return Permute(input, axis.AsSpan());
        }

        public static Tensor<T> Permute<T>(Tensor<T> input, params int[] axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => Permute(input, axis.AsSpan());

        public static Tensor<T> Permute<T>(Tensor<T> input, ReadOnlySpan<int> axis)
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
                nint[] indices;
                int[] permutation;

                if (axis.IsEmpty)
                {
                    lengths = input._lengths.Reverse().ToArray();
                    permutation = Enumerable.Range(0, input.Rank).Reverse().ToArray();
                }
                else
                {
                    if (axis.Length != input.Lengths.Length)
                        throw new ArgumentException("Must provide an axis order for each axis");
                    for (int i = 0; i < lengths.Length; i++)
                        lengths[i] = input.Lengths[axis[i]];
                    permutation = axis.ToArray();
                }
                tensor = new Tensor<T>(values, lengths, Array.Empty<nint>(), input._isPinned);
                nint[] permutedIndices = new nint[tensor.Rank];

                ospan = tensor.AsSpan();
                ispan = input.AsSpan();
                indices = new nint[tensor.Rank];
                for (int i = 0; i < input._linearLength; i++)
                {
                    PermuteIndices(ref indices, ref permutedIndices, ref permutation);
                    ospan[permutedIndices] = ispan[indices];
                    SpanHelpers.AdjustIndices(tensor.Rank - 1, 1, ref indices, input._lengths);
                }

                return tensor;
            }
        }

        private static void PermuteIndices(ref nint[] indices, ref nint[] permutedIndices, ref int[] permutation)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                permutedIndices[i] = indices[permutation[i]];
            }
        }
        #endregion

        #region TensorPrimitives
        #region Multiply
        public static Tensor<T> Multiply<T>(Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            Tensor<T> output = Create<T>(input.IsPinned, input.Lengths);
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

        public static Tensor<T> Multiply<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Multiply);
        }

        public static Tensor<T> MultiplyInPlace<T>(Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            return TensorPrimitivesHelperT1T2(input, other, TensorPrimitives.Multiply, true);
        }

        public static SpanND<T> Multiply<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            SpanND<T> output = new SpanND<T>(values, input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Multiply(span, val, ospan);
            return output;
        }

        public static SpanND<T> MultiplyInPlace<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
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
            Tensor<T> output = Create<T>(input.IsPinned, input.Lengths);
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
            Tensor<T> output = Create<T>(input.IsPinned, input.Lengths);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            SpanND<T> output = new SpanND<T>(values, input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        public static SpanND<T> DivideInPlace<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Divide(span, val, ospan);
            return output;
        }

        public static SpanND<T> Divide<T>(T val, SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            SpanND<T> output = new SpanND<T>(values, input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Divide(val, span, ospan);
            return output;
        }

        public static SpanND<T> DivideInPlace<T>(T val, SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
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
            Tensor<T> output = Create<T>(input.IsPinned, input.Lengths);
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
            Tensor<T> output = Create<T>(input.IsPinned, input.Lengths);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            SpanND<T> output = new SpanND<T>(values, input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        public static SpanND<T> SubtractInPlace<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Subtract(span, val, ospan);
            return output;
        }

        public static SpanND<T> Subtract<T>(T val, SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            SpanND<T> output = new SpanND<T>(values, input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Subtract(val, span, ospan);
            return output;
        }

        public static SpanND<T> SubtractInPlace<T>(T val, SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
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
        public static Tensor<T> Sum<T>(Tensor<T> input, int axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T> => throw new NotImplementedException();

        public static T Sum<T>(Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            return TensorPrimitives.Sum(span);
        }

        public static SpanND<T> Sum<T>(SpanND<T> input, int axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T> => throw new NotImplementedException();

        public static T Sum<T>(SpanND<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
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
            Tensor<T> output = Create<T>(input.IsPinned, input.Lengths);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input.LinearLength, input.IsPinned) : (new T[input.LinearLength]);
            SpanND<T> output = new SpanND<T>(values, input.Lengths, input.IsPinned);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            TensorPrimitives.Add(span, val, ospan);
            return output;
        }

        public static SpanND<T> AddInPlace<T>(SpanND<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            SpanND<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
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
            Tensor<T> output = inPlace ? input : Create<T>(input.IsPinned, input.Lengths);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            performCalculation(span, ospan);
            return output;
        }

        private static SpanND<T> TensorPrimitivesHelperT1<T>(SpanND<T> input, PerformCalculationT1<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            SpanND<T> output = inPlace ? input : Create<T>(input.IsPinned, input.Lengths);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            performCalculation(span, ospan);
            return output;
        }

        private static Tensor<T> TensorPrimitivesHelperT1T2<T>(Tensor<T> input, Tensor<T> inputTwo, PerformCalculationT1T2<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref inputTwo._values[0], (int)inputTwo._linearLength);
            Tensor<T> output = inPlace ? input : Create<T>(input.IsPinned, input.Lengths);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._values[0], (int)output._linearLength);
            performCalculation(span, rspan, ospan);
            return output;
        }

        private static SpanND<T> TensorPrimitivesHelperT1T2<T>(SpanND<T> input, SpanND<T> inputTwo, PerformCalculationT1T2<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.LinearLength);
            ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref inputTwo._reference, (int)inputTwo.LinearLength);
            SpanND<T> output = inPlace ? input : Create<T>(input.IsPinned, input.Lengths);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.LinearLength);
            performCalculation(span, rspan, ospan);
            return output;
        }
        #endregion
        #endregion
    }
}
