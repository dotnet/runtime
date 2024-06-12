// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics.Tensors
{
    public static class TensorSpan
    {
        #region SequenceEqual
        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this ReadOnlyTensorSpan<T> span, in ReadOnlyTensorSpan<T> other) where T : IEquatable<T>?
        {
            return span.FlattenedLength == other.FlattenedLength
                && MemoryMarshal.CreateReadOnlySpan(in span.GetPinnableReference(), (int)span.FlattenedLength).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(in other.GetPinnableReference(), (int)other.FlattenedLength));
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this TensorSpan<T> span, in TensorSpan<T> other) where T : IEquatable<T>?
        {
            return ((ReadOnlyTensorSpan<T>)span).SequenceEqual((ReadOnlyTensorSpan<T>)other);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this TensorSpan<T> span, in ReadOnlyTensorSpan<T> other) where T : IEquatable<T>?
        {
            return ((ReadOnlyTensorSpan<T>)span).SequenceEqual(other);
        }
        #endregion

        #region AsTensorSpan
        /// <summary>
        /// Extension method to more easily create a TensorSpan from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array</typeparam>
        /// <param name="array">The <see cref="System.Array"/> with the data</param>
        /// <param name="shape">The shape for the <see cref="TensorSpan{T}"/></param>
        /// <returns></returns>
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array, params ReadOnlySpan<nint> shape) => new(array, 0, shape, default);
        #endregion

        #region ToString
        // REVIEW: WHAT SHOULD WE NAME THIS? WHERE DO WE WANT IT TO LIVE?
        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="span">The <see cref="TensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="span"/></returns>
        public static string ToString<T>(this TensorSpan<T> span, params ReadOnlySpan<nint> maximumLengths) => ((ReadOnlyTensorSpan<T>)span).ToString(maximumLengths);

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="ReadOnlyTensorSpan{T}"/>."/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The <see cref="ReadOnlyTensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        public static string ToString<T>(this ReadOnlyTensorSpan<T> span, params ReadOnlySpan<nint> maximumLengths)
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
            while (copiedValues < span._flattenedLength)
            {
                var sp = new ReadOnlyTensorSpan<T>(ref Unsafe.Add(ref span._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, span.Strides, span.Lengths)), [span.Lengths[span.Rank - 1]], [1], span.Lengths[span.Rank - 1]);
                sb.Append('{');
                sp.FlattenTo(values);
                sb.Append(string.Join(",", values));
                sb.AppendLine("}");

                TensorSpanHelpers.AdjustIndexes(span.Rank - 2, 1, curIndexes, span._lengths);
                copiedValues += span.Lengths[span.Rank - 1];
            }

            if (curIndexesArray != null)
                ArrayPool<nint>.Shared.Return(curIndexesArray);

            return sb.ToString();
        }
        #endregion

        #region Broadcast
        /// <summary>
        /// Broadcast the data from <paramref name="left"/> to the smallest broadcastable shape compatible with <paramref name="right"/>. Creates a new <see cref="TensorSpan{T}"/> and allocates new memory.
        /// </summary>
        /// <param name="left">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">Other <see cref="TensorSpan{T}"/> to make shapes broadcastable.</param>
        public static TensorSpan<T> Broadcast<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            nint[] newSize = TensorHelpers.GetSmallestBroadcastableSize(left.Lengths, right.Lengths);

            TensorSpan<T> intermediate = BroadcastTo(left, newSize);
            T[] data = new T[intermediate.FlattenedLength];
            intermediate.FlattenTo(data);
            return new TensorSpan<T>(data, 0, intermediate.Lengths, []);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the new shape <paramref name="shape"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// If the shape of the <paramref name="input"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        public static TensorSpan<T> Broadcast<T>(TensorSpan<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            TensorSpan<T> intermediate = BroadcastTo(input, shape);
            T[] data = new T[intermediate.FlattenedLength];
            intermediate.FlattenTo(data);
            return new TensorSpan<T>(data, 0, intermediate.Lengths, []);
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
            if (input.Lengths.SequenceEqual(shape))
                return new TensorSpan<T>(ref input._reference, shape, input.Strides, input._memoryLength);

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

            TensorSpan<T> output = new TensorSpan<T>(ref input._reference, shape, strides, input._memoryLength);

            return output;
        }
        #endregion

        #region Reverse
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
                nint index = input.FlattenedLength - 1;
                Span<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
                T temp;
                for (int i = 0; i <= input.FlattenedLength / 2; i++)
                {
                    temp = span[(int)index];
                    span[(int)index] = span[i];
                    span[i] = temp;
                }
            }
            else
            {
                T[] values = new T[input.FlattenedLength];

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
                TensorSpan<T> islice = input.Slice(input.Lengths);
                while (copiedValues < input.FlattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref values, TensorSpanHelpers.ComputeLinearIndex(oIndices, input.Strides, input.Lengths)), ref Unsafe.Add(ref islice._reference, TensorSpanHelpers.ComputeLinearIndex(iIndices, islice.Strides, islice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes((int)axis, 1, oIndices, input.Lengths);
                    TensorSpanHelpers.AdjustIndexesDown((int)axis, 1, iIndices, input.Lengths);
                    copiedValues += copyLength;
                }
                TensorSpanHelpers.Memmove(ref input._reference, ref values[0], input.FlattenedLength);

                if (oIndicesArray != null && iIndicesArray != null)
                {
                    ArrayPool<nint>.Shared.Return(oIndicesArray);
                    ArrayPool<nint>.Shared.Return(iIndicesArray);
                }
            }

            return input;
        }
        #endregion

        #region Reshape
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
            return new TensorSpan<T>(ref input._reference, arrLengths, strides, input._memoryLength);
        }
        #endregion

        #region Resize
        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/>, allocates new managed memory, and copies the data from <paramref name="input"/>. If the final shape is smaller all data after
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        public static TensorSpan<T> Resize<T>(TensorSpan<T> input, ReadOnlySpan<nint> shape)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            nint newSize = TensorSpanHelpers.CalculateTotalLength(shape);
            T[] values = new T[newSize];
            TensorSpan<T> output = new TensorSpan<T>(values, 0, shape, default);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
            if (newSize > input.FlattenedLength)
                TensorSpanHelpers.Memmove(ospan, span, input.FlattenedLength);
            else
                TensorSpanHelpers.Memmove(ospan, span, newSize);

            return output;
        }
        #endregion

        #region Squeeze
        // REVIEW: NAME?
        /// <summary>
        /// Removes axis of length one from the <paramref name="input"/>. <paramref name="axis"/> defaults to -1 and will remove all axis with length of 1.
        /// If <paramref name="axis"/> is specified, it will only remove that axis and if it is not of length one it will throw an exception.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to remove. Defaults to -1 which removes all axis of length 1.</param>
        public static TensorSpan<T> Squeeze<T>(TensorSpan<T> input, int axis = -1)
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

            return new TensorSpan<T>(ref input._reference, lengths, strides, input._memoryLength);
        }
        #endregion

        #region Unsqueeze
        // REVIEW: NAME? NUMPY CALLS THIS expand_dims.
        /// <summary>
        /// Insert a new axis of length 1 that will appear at the axis position.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to remove axis of length 1.</param>
        /// <param name="axis">The axis to add.</param>
        public static TensorSpan<T> Unsqueeze<T>(TensorSpan<T> input, int axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (axis > input.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (axis < 0)
                axis = input.Rank - axis;

            List<nint> tempLengths = input._lengths.ToArray().ToList();
            tempLengths.Insert(axis, 1);
            nint[] lengths = tempLengths.ToArray();
            nint[] strides = TensorSpanHelpers.CalculateStrides(lengths);
            return new TensorSpan<T>(ref input._reference, lengths, strides, input._memoryLength);
        }
        #endregion

        #region StdDev
        /// <summary>
        /// Returns the standard deviation of the elements in the <paramref name="input"/> tensor.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the standard deviation of.</param>
        /// <returns><typeparamref name="T"/> representing the standard deviation.</returns>
        public static T StdDev<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>, IPowerFunctions<T>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>

        {
            T mean = Mean(input);
            Span<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
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
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the standard deviation of.</param>
        /// <returns><typeparamref name="TResult"/> representing the standard deviation.</returns>
        public static TResult StdDev<T, TResult>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>, IFloatingPoint<T>, IPowerFunctions<T>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T mean = Mean(input);
            Span<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
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
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the mean of.</param>
        /// <returns><typeparamref name="T"/> representing the mean.</returns>
        public static T Mean<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>

        {
            T sum = Sum(input);
            return T.CreateChecked(sum / T.CreateChecked(input.FlattenedLength));
        }

        /// <summary>
        /// Return the mean of the elements in the <paramref name="input"/> tensor. Casts the return value to <typeparamref name="TResult"/>.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the mean of.</param>
        /// <returns><typeparamref name="TResult"/> representing the mean.</returns>
        public static TResult Mean<T, TResult>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.FlattenedLength));
        }

        #endregion

        #region TensorPrimitives
        #region Abs
        /// <summary>
        /// Takes the absolute value of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Abs<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumberBase<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Abs);
        }

        /// <summary>
        /// Takes the absolute of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AbsInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumberBase<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Abs, true);
        }
        #endregion

        #region Acos
        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Acos<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Acos);
        }

        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AcosInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Acos, true);
        }
        #endregion

        #region Acosh
        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Acosh<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Acosh);
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="Tensor{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AcoshInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Acosh, true);
        }
        #endregion

        #region AcosPi
        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AcosPi<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AcosPi);
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AcosPiInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AcosPi, true);
        }
        #endregion

        #region Add
        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The <see cref="TensorSpan{T}"/> of values to add.</param>
        /// <param name="right">The second <see cref="TensorSpan{T}"/> of values to add.</param>
        public static TensorSpan<T> Add<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Add);
        }

        /// <summary>
        /// Adds each element of <paramref name="left"/> to each element of <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="TensorSpan{T}"/> of values to add.</param>
        /// <param name="right">The second <see cref="TensorSpan{T}"/> of values to add.</param>
        public static TensorSpan<T> AddInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Add, true);
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> to add to each element of <paramref name="input"/>.</param>
        public static TensorSpan<T> Add<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperSpanInTInSpanOut(input, val, TensorPrimitives.Add);
        }

        /// <summary>
        /// Adds <paramref name="val"/> to each element of <paramref name="input"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> of values to add.</param>
        /// <param name="val">The <typeparamref name="T"/> to add to each element of <paramref name="input"/>.</param>
        public static TensorSpan<T> AddInPlace<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            return TensorPrimitivesHelperSpanInTInSpanOut(input, val, TensorPrimitives.Add, true);

        }
        #endregion

        #region Asin
        /// <summary>
        /// Takes the inverse sin of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Asin<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Asin);
        }

        /// <summary>
        /// Takes the inverse sine each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AsinInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Asin, true);
        }
        #endregion

        #region Asinh
        /// <summary>
        /// Takes the inverse hyperbolic sine of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Asinh<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Asinh);
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AsinhInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Asinh, true);
        }
        #endregion

        #region AsinPi
        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AsinPi<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AsinPi);
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> AsinPiInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AsinPi, true);
        }
        #endregion

        #region Atan
        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> Atan<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Atan);
        }

        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> AtanInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Atan, true);
        }
        #endregion

        #region Atan2
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Atan2<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Atan2);
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Atan2InPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Atan2, true);
        }
        #endregion

        #region Atan2Pi
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="TensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Atan2Pi<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Atan2Pi);
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="TensorSpan{T}"/>, divides each element by pi in place.
        /// </summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Atan2PiInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Atan2Pi, true);
        }
        #endregion

        #region Atanh
        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Atanh<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Atanh);
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> AtanhInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Atanh, true);
        }
        #endregion

        #region AtanPi
        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The input<see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> AtanPi<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AtanPi);
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The input<see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> AtanPiInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.AtanPi, true);
        }
        #endregion

        #region BitwiseAnd
        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> BitwiseAnd<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.BitwiseAnd);
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> BitwiseAndInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.BitwiseAnd, true);
        }
        #endregion

        #region BitwiseOr
        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> BitwiseOr<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.BitwiseOr);
        }

        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> BitwiseOrInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.BitwiseOr, true);
        }
        #endregion

        #region CubeRoot
        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The left <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> CubeRoot<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cbrt);
        }

        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The left <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> CubeRootInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cbrt, true);
        }
        #endregion

        #region Ceiling
        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The left <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Ceiling<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Ceiling);
        }

        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The left <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> CeilingInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Ceiling, true);
        }
        #endregion

        #region ConvertChecked
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TTo}"/>.</param>
        public static TensorSpan<TTo> ConvertChecked<TFrom, TTo>(TensorSpan<TFrom> source)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            return TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(source, TensorPrimitives.ConvertChecked);
        }
        #endregion

        #region ConvertSaturating
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TTo}"/>.</param>
        public static TensorSpan<TTo> ConvertSaturating<TFrom, TTo>(TensorSpan<TFrom> source)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            return TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(source, TensorPrimitives.ConvertSaturating);
        }
        #endregion

        #region ConvertTruncating
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TTo}"/>.</param>
        public static TensorSpan<TTo> ConvertTruncating<TFrom, TTo>(TensorSpan<TFrom> source)
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
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        public static TensorSpan<T> CopySign<T>(TensorSpan<T> input, T sign)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTInSpanOut(input, sign, TensorPrimitives.CopySign);
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors in place.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        public static TensorSpan<T> CopySignInPlace<T>(TensorSpan<T> input, T sign)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTInSpanOut(input, sign, TensorPrimitives.CopySign, true);
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="sign">The <see cref="TensorSpan{T}"/> with the associated signs.</param>
        public static TensorSpan<T> CopySign<T>(TensorSpan<T> input, TensorSpan<T> sign)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(input, sign, TensorPrimitives.CopySign);
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors in place.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="sign">The <see cref="TensorSpan{T}"/> with the associated signs.</param>
        public static TensorSpan<T> CopySignInPlace<T>(TensorSpan<T> input, TensorSpan<T> sign)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(input, sign, TensorPrimitives.CopySign, true);
        }
        #endregion

        #region Cos
        /// <summary>
        /// Takes the cosine of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the cosine of.</param>
        public static TensorSpan<T> Cos<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cos);
        }

        /// <summary>
        /// Takes the cosine of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the cosine of.</param>
        public static TensorSpan<T> CosInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cos, true);
        }
        #endregion

        #region Cosh
        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the cosine of.</param>
        public static TensorSpan<T> Cosh<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cosh);
        }

        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the cosine of.</param>
        public static TensorSpan<T> CoshInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Cosh, true);
        }
        #endregion

        #region CosineSimilarity
        /// <summary>
        /// Compute cosine similarity between <paramref name="left"/> and <paramref name="right"/>.
        /// </summary>
        /// <param name="left">The first <see cref="TensorSpan{T}"/></param>
        /// <param name="right">The second <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> CosineSimilarity<T>(TensorSpan<T> left, TensorSpan<T> right)
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

            return new TensorSpan<T>(values, 0, [dim1, dim2], []);

        }
        #endregion

        #region CosPi
        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi and returns a new <see cref="TensorSpan{T}"/> with the results.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/></param>
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
        public static TensorSpan<T> CosPi<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.CosPi);
        }

        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi in place.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/></param>
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
        public static TensorSpan<T> CosPiInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.CosPi, true);
        }
        #endregion

        #region DegreesToRadians
        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians and returns a new tensor with the results.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> DegreesToRadians<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.DegreesToRadians);
        }

        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians in place.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> DegreesToRadiansInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.DegreesToRadians, true);
        }
        #endregion

        #region Distance
        /// <summary>
        /// Computes the distance between two points, specified as non-empty, equal-length tensors of numbers, in Euclidean space.
        /// </summary>
        /// <param name="left">The input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The input <see cref="TensorSpan{T}"/>.</param>
        public static T Distance<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInTOut(left, right, TensorPrimitives.Distance);
        }

        #endregion

        #region Divide
        /// <summary>
        /// Divides each element of <paramref name="input"/> by <paramref name="val"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="val">The divisor</param>
        public static TensorSpan<T> Divide<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            T[] values = new T[input.FlattenedLength];
            TensorSpan<T> output = new TensorSpan<T>(values, 0, input.Lengths, default);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            T[] values = new T[input.FlattenedLength];
            TensorSpan<T> output = new TensorSpan<T>(values, 0, input.Lengths, default);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Divide);
        }

        /// <summary>
        /// Divides each element of <paramref name="left"/> by its corresponding element in <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="TensorSpan{T}"/> to be divided.</param>
        /// <param name="right">The <see cref="TensorSpan{T}"/> divisor.</param>
        public static TensorSpan<T> DivideInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Divide, true);
        }
        #endregion

        #region Dot
        /// <summary>
        /// Computes the dot product of two tensors containing numbers.
        /// </summary>
        /// <param name="left">The input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The input <see cref="TensorSpan{T}"/>.</param>
        public static T Dot<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInTOut(left, right, TensorPrimitives.Dot);
        }

        #endregion

        #region Exp
        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp);
        }

        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> ExpInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp, true);
        }
        #endregion

        #region Exp10
        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp10<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp10);
        }

        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp10InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp10, true);
        }
        #endregion

        #region Exp10M1
        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp10M1<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp10M1);
        }

        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp10M1InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp10M1, true);
        }
        #endregion

        #region Exp2
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp2<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp2);
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp2InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp2, true);
        }
        #endregion

        #region Exp2M1
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp2M1<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp2M1);
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Exp2M1InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Exp2M1, true);
        }
        #endregion

        #region ExpM1
        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> ExpM1<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.ExpM1);
        }

        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> ExpM1InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.ExpM1, true);
        }
        #endregion

        #region Floor
        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Floor<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Floor);
        }

        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> FloorInPlace<T>(TensorSpan<T> input)
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
        /// <param name="left">Left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">Right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Hypotenuse<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Hypot);
        }

        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">Right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> HypotenuseInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Hypot, true);
        }
        #endregion

        #region Ieee754Remainder
        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// <param name="left">Left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">Right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Ieee754Remainder<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Ieee754Remainder);
        }

        /// <summary>
        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="left">Left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">Right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Ieee754RemainderInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Ieee754Remainder, true);
        }
        #endregion

        #region ILogB
        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<int> ILogB<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPointIeee754<T>
        {
            return TensorPrimitivesHelperSpanInIntSpanOut(input, TensorPrimitives.ILogB);
        }
        #endregion

        #region IndexOfMax
        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static int IndexOfMax<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>

        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
            return TensorPrimitives.IndexOfMax(span);
        }
        #endregion

        #region IndexOfMaxMagnitude
        /// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static int IndexOfMaxMagnitude<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>

        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
            return TensorPrimitives.IndexOfMaxMagnitude(span);
        }
        #endregion

        #region IndexOfMin
        /// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static int IndexOfMin<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>

        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
            return TensorPrimitives.IndexOfMin(span);
        }
        #endregion

        #region IndexOfMinMagnitude
        /// <summary>
        /// Searches for the index of the number with the smallest magnitude in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static int IndexOfMinMagnitude<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>

        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
            return TensorPrimitives.IndexOfMinMagnitude(span);
        }
        #endregion

        #region LeadingZeroCount
        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> LeadingZeroCount<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.LeadingZeroCount);
        }

        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> LeadingZeroCountInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.LeadingZeroCount, true);
        }
        #endregion

        #region Log

        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the natural logarithm of.</param>
        public static TensorSpan<T> Log<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log);
        }

        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the natural logarithm of.</param>
        public static TensorSpan<T> LogInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log, true);
        }
        #endregion

        #region Log10

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static TensorSpan<T> Log10<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log10);
        }

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static TensorSpan<T> Log10InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log10, true);
        }
        #endregion

        #region Log10P1
        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static TensorSpan<T> Log10P1<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log10P1);
        }

        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static TensorSpan<T> Log10P1InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log10P1, true);
        }
        #endregion

        #region Log2
        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static TensorSpan<T> Log2<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log2);
        }

        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static TensorSpan<T> Log2InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log2, true);
        }
        #endregion

        #region Log2P1
        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static TensorSpan<T> Log2P1<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log2P1);
        }

        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static TensorSpan<T> Log2P1InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Log2P1, true);
        }
        #endregion

        #region LogP1
        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the natural logarithm of.</param>
        public static TensorSpan<T> LogP1<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.LogP1);
        }

        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the natural logarithm of.</param>
        public static TensorSpan<T> LogP1InPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.LogP1, true);
        }
        #endregion

        #region Max
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>..</param>
        public static T Max<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Max);
        }
        #endregion

        #region MaxMagnitude
        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>..</param>
        public static T MaxMagnitude<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MaxMagnitude);
        }
        #endregion

        #region MaxNumber
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>..</param>
        public static T MaxNumber<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MaxNumber);
        }
        #endregion

        #region Min
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static T Min<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Min);
        }
        #endregion

        #region MinMagnitude
        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static T MinMagnitude<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MinMagnitude);
        }
        #endregion

        #region MinNumber
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>..</param>
        public static T MinNumber<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.MinNumber);
        }
        #endregion

        #region Multiply
        /// <summary>
        /// Multiplies each element of <paramref name="input"/> with <paramref name="val"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/></param>
        /// <param name="val"><typeparamref name="T"/> value to multiply by.</param>
        public static TensorSpan<T> Multiply<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            T[] values = new T[input.FlattenedLength];
            TensorSpan<T> output = new TensorSpan<T>(values, 0, input.Lengths, default);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Multiply);
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
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Multiply, true);
        }
        #endregion

        #region Negate
        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> Negate<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IUnaryNegationOperators<T, T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Negate);
        }

        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> NegateInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IUnaryNegationOperators<T, T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Negate, true);
        }
        #endregion

        #region Norm

        /// <summary>
        ///  Takes the norm of the <see cref="TensorSpan{T}"/> and returns the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the norm of.</param>
        public static T Norm<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            return TensorPrimitives.Norm(span);
        }
        #endregion

        #region OnesComplement
        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> OnesComplement<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.OnesComplement);
        }

        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> OnesComplementInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.OnesComplement, true);
        }
        #endregion

        #region PopCount
        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> PopCount<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.PopCount);
        }

        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> PopCountInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.PopCount, true);
        }
        #endregion

        #region Pow
        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="left">The input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The second input <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> Pow<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IPowerFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Pow);
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="left">The input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The second input <see cref="TensorSpan{T}"/></param>
        public static TensorSpan<T> PowInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IPowerFunctions<T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Pow, true);
        }
        #endregion

        #region Product
        /// <summary>Computes the product of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static T Product<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            return TensorPrimitivesHelperSpanInTOut(input, TensorPrimitives.Product);
        }
        #endregion

        #region RadiansToDegrees
        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> RadiansToDegrees<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.RadiansToDegrees);
        }

        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> RadiansToDegreesInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.RadiansToDegrees, true);
        }
        #endregion

        #region Reciprocal
        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Reciprocal<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Reciprocal);
        }

        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> ReciprocalInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Reciprocal, true);
        }
        #endregion

        #region Round
        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Round<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Round);
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> RoundInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Round, true);
        }
        #endregion

        #region Sigmoid
        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Sigmoid<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sigmoid);
        }

        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> SigmoidInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sigmoid, true);
        }
        #endregion

        #region Sin

        /// <summary>
        /// Takes the sin of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Sin<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sin);
        }

        /// <summary>
        /// Takes the sin of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> SinInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sin, true);
        }
        #endregion

        #region Sinh
        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Sinh<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sinh);
        }

        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> SinhInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sinh, true);
        }
        #endregion

        #region SinPi
        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> SinPi<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.SinPi);
        }

        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> SinPiInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.SinPi, true);
        }
        #endregion

        #region SoftMax
        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> SoftMax<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.SoftMax);
        }

        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> SoftMaxInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IExponentialFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.SoftMax, true);
        }
        #endregion

        #region Sqrt
        /// <summary>
        /// Takes the square root of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the square root of.</param>
        public static TensorSpan<T> Sqrt<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sqrt);
        }

        /// <summary>
        /// Takes the square root of each element of the <see cref="TensorSpan{T}"/> in place.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the square root of.</param>
        public static TensorSpan<T> SqrtInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Sqrt, true);
        }
        #endregion

        #region Subtract
        /// <summary>
        /// Subtracts <paramref name="val"/> from each element of <paramref name="input"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> with values to be subtracted from.</param>
        /// <param name="val">The <typeparamref name="T"/> value to subtract.</param>
        public static TensorSpan<T> Subtract<T>(TensorSpan<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            T[] values = new T[input.FlattenedLength];
            TensorSpan<T> output = new TensorSpan<T>(values, 0, input.Lengths, default);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            T[] values = new T[input.FlattenedLength];
            TensorSpan<T> output = new TensorSpan<T>(values, 0, input.Lengths, default);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Subtract);
        }

        /// <summary>
        /// Subtracts each element of <paramref name="left"/> from <paramref name="right"/> in place.
        /// </summary>
        /// <param name="left">The <see cref="TensorSpan{T}"/> of values to be subtracted from.</param>
        /// <param name="right">The <see cref="TensorSpan{T}"/>of values to subtract.</param>
        public static TensorSpan<T> SubtractInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Subtract, true);
        }
        #endregion

        #region Sum
        /// <summary>
        /// Sums all the elements of the <see cref="TensorSpan{T}"/> and returns the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to sum.</param>
        public static T Sum<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            return TensorPrimitives.Sum(span);
        }
        #endregion

        #region Tan
        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Tan<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Tan);
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> TanInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Tan, true);
        }
        #endregion

        #region Tanh
        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Tanh<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Tanh);
        }

        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> TanhInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IHyperbolicFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Tanh, true);
        }
        #endregion

        #region TanPi
        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> TanPi<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.TanPi);
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> TanPiInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.TanPi, true);
        }
        #endregion

        #region TrailingZeroCount
        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> TrailingZeroCount<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.TrailingZeroCount);
        }

        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> TrailingZeroCountInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBinaryInteger<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.TrailingZeroCount, true);
        }
        #endregion

        #region Truncate
        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Truncate<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Truncate);
        }

        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="input">The input <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> TruncateInPlace<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            return TensorPrimitivesHelperSpanInSpanOut(input, TensorPrimitives.Truncate, true);
        }
        #endregion

        #region Xor
        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> Xor<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Xor);
        }

        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="left">The left <see cref="TensorSpan{T}"/>.</param>
        /// <param name="right">The right <see cref="TensorSpan{T}"/>.</param>
        public static TensorSpan<T> XorInPlace<T>(TensorSpan<T> left, TensorSpan<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        {
            return TensorPrimitivesHelperTwoSpanInSpanOut(left, right, TensorPrimitives.Xor, true);
        }
        #endregion

        #region TensorPrimitivesHelpers
        private delegate void PerformCalculationSpanInSpanOut<T>(ReadOnlySpan<T> input, Span<T> output)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate void PerformCalculationSpanInTInSpanOut<T>(ReadOnlySpan<T> input, T value, Span<T> output)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate void PerformCalculationTwoSpanInSpanOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo, Span<T> output)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate void PerformCalculationTFromSpanInTToSpanOut<TFrom, TTo>(ReadOnlySpan<TFrom> input, Span<TTo> output)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>;

        private delegate T PerformCalculationTwoSpanInTOut<T>(ReadOnlySpan<T> input, ReadOnlySpan<T> inputTwo)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate void PerformCalculationSpanInIntSpanOut<T>(ReadOnlySpan<T> input, Span<int> output)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private delegate T PerformCalculationSpanInTOut<T>(ReadOnlySpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>;

        private static T TensorPrimitivesHelperSpanInTOut<T>(TensorSpan<T> input, PerformCalculationSpanInTOut<T> performCalculation)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
            return performCalculation(span);
        }

        private static TensorSpan<int> TensorPrimitivesHelperSpanInIntSpanOut<T>(TensorSpan<T> input, PerformCalculationSpanInIntSpanOut<T> performCalculation)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
            int[] data = new int[input.FlattenedLength];
            performCalculation(span, data);
            return new TensorSpan<int>(data, 0, input.Lengths, input.Strides);
        }

        private static T TensorPrimitivesHelperTwoSpanInTOut<T>(TensorSpan<T> left, TensorSpan<T> right, PerformCalculationTwoSpanInTOut<T> performCalculation)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            // If not in place but sizes are the same.
            if (left.Lengths.SequenceEqual(right.Lengths))
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left.FlattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.FlattenedLength);
                return performCalculation(span, rspan);
            }
            // Not in place and broadcasting needs to happen.
            else
            {
                // Have a couple different possible cases here.
                // 1 - Both tensors have row contiguous memory (i.e. a 1x5 being broadcast to a 5x5)
                // 2 - One tensor has row contiguous memory and the right has column contiguous memory (i.e. a 1x5 and a 5x1)
                // Because we are returning a single T though we need to actual realize the broadcasts at this point to perform the calculations.

                var broadcastedLeft = Broadcast(left, right);
                var broadcastedRight = Broadcast(right, left);

                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref broadcastedLeft._reference, (int)broadcastedLeft.FlattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref broadcastedRight._reference, (int)broadcastedRight.FlattenedLength);
                return performCalculation(span, rspan);
            }
        }

        private static TensorSpan<T> TensorPrimitivesHelperSpanInSpanOut<T>(TensorSpan<T> input, PerformCalculationSpanInSpanOut<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = inPlace ? input : new TensorSpan<T>(new T[input.FlattenedLength], 0, input.Lengths, input.Strides);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
            performCalculation(span, ospan);
            return output;
        }

        private static TensorSpan<T> TensorPrimitivesHelperSpanInTInSpanOut<T>(TensorSpan<T> input, T value, PerformCalculationSpanInTInSpanOut<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = inPlace ? input : new TensorSpan<T>(new T[input.FlattenedLength], 0, input.Lengths, input.Strides);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
            performCalculation(span, value, ospan);
            return output;
        }

        private static TensorSpan<TTo> TensorPrimitivesHelperTFromSpanInTToSpanOut<TFrom, TTo>(TensorSpan<TFrom> input, PerformCalculationTFromSpanInTToSpanOut<TFrom, TTo> performCalculation)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            ReadOnlySpan<TFrom> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input._flattenedLength);
            TTo[] data = new TTo[(int)input.FlattenedLength];
            TensorSpan<TTo> output = new TensorSpan<TTo>(data, 0, input.Lengths, input.Strides);
            Span<TTo> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output._flattenedLength);
            performCalculation(span, ospan);
            return output;
        }

        private static TensorSpan<T> TensorPrimitivesHelperTwoSpanInSpanOut<T>(TensorSpan<T> left, TensorSpan<T> right, PerformCalculationTwoSpanInSpanOut<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (inPlace && !left.Lengths.SequenceEqual(right.Lengths))
                ThrowHelper.ThrowArgument_InPlaceInvalidShape();

            TensorSpan<T> output;
            if (inPlace)
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left.FlattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.FlattenedLength);
                output = left;
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
                performCalculation(span, rspan, ospan);
            }
            // If not in place but sizes are the same.
            else if (left.Lengths.SequenceEqual(right.Lengths))
            {
                ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref left._reference, (int)left.FlattenedLength);
                ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.FlattenedLength);
                output = new TensorSpan<T>(new T[left.FlattenedLength], 0, left.Lengths, left.Strides);
                Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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

                TensorSpan<T> broadcastedLeft = TensorSpan.BroadcastTo(left, newSize);
                TensorSpan<T> broadcastedRight = TensorSpan.BroadcastTo(right, newSize);

                output = new TensorSpan<T>(new T[TensorSpanHelpers.CalculateTotalLength(newSize)], newSize, default);
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
                    while (outputOffset < output.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        buffer.Fill(broadcastedLeft[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndex], (int)rowLength);
                        performCalculation(buffer, ispan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndex, broadcastedLeft.Lengths);
                    }
                }
                // right not row contiguous
                else if (broadcastedRight.Strides[^1] == 0)
                {
                    while (outputOffset < output.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        buffer.Fill(broadcastedRight[curIndex]);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndex], (int)rowLength);
                        performCalculation(ispan, buffer, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndex, broadcastedLeft.Lengths);
                    }
                }
                // both row contiguous
                else
                {
                    Span<T> rspan;
                    while (outputOffset < output.FlattenedLength)
                    {
                        ospan = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref output._reference, outputOffset), (int)rowLength);
                        ispan = MemoryMarshal.CreateSpan(ref broadcastedLeft[curIndex], (int)rowLength);
                        rspan = MemoryMarshal.CreateSpan(ref broadcastedRight[curIndex], (int)rowLength);
                        performCalculation(ispan, rspan, ospan);
                        outputOffset += (int)rowLength;
                        TensorSpanHelpers.AdjustIndexes(broadcastedLeft.Rank - 2, 1, curIndex, broadcastedLeft.Lengths);
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
