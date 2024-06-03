// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics.Tensors
{
    public static class TensorSpan
    {
        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool SequenceEqual<T>(this TensorSpan<T> span, TensorSpan<T> other) where T : IEquatable<T>? => span.FlattenedLength == other.FlattenedLength && span.Lengths.SequenceEqual(other.Lengths) && TensorSpanHelpers.SequenceEqual(ref span.GetPinnableReference(), ref other.GetPinnableReference(), (nuint)span.FlattenedLength);

        // Doing a copy here for shape because otherwise I get a CS8347 about potentially exposing it beyond its lifetime. In this case that would never happen
        // because we were always doing a copy of the shape in the constructor anyways, but I couldn't figure out another way around it.
        /// <summary>
        /// Extension method to more easily create a TensorSpan from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array</typeparam>
        /// <param name="array">The <see cref="System.Array"/> with the data</param>
        /// <param name="shape">The shape for the <see cref="TensorSpan{T}"/></param>
        /// <returns></returns>
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array, params scoped ReadOnlySpan<nint> shape) => new(array, 0, shape, default);

        #region ToString
        // REVIEW: WHAT SHOULD WE NAME THIS? WHERE DO WE WANT IT TO LIVE?
        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="span">The <see cref="TensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="span"/></returns>
        public static string ToString<T>(this TensorSpan<T> span, params scoped ReadOnlySpan<nint> maximumLengths) => ((ReadOnlyTensorSpan<T>)span).ToString(maximumLengths);

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="ReadOnlyTensorSpan{T}"/>."/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The <see cref="ReadOnlyTensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        public static string ToString<T>(this ReadOnlyTensorSpan<T> span, params scoped ReadOnlySpan<nint> maximumLengths)
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

        // Lazy/non-copy broadcasting, internal only for now.
        /// <summary>
        /// Broadcast the data from <paramref name="input"/> to the new shape <paramref name="shape"/>. Creates a new <see cref="Tensor{T}"/>
        /// but no memory is allocated. It manipulates the strides to achieve this affect.
        /// If the shape of the <paramref name="input"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="input">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="shape"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">Thrown when the shapes are not broadcast compatible.</exception>
        internal static TensorSpan<T> BroadcastTo<T>(TensorSpan<T> input, scoped ReadOnlySpan<nint> shape)
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

        /// <summary>
        /// Reshapes the <paramref name="input"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="input"><see cref="TensorSpan{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static TensorSpan<T> Reshape<T>(this TensorSpan<T> input, params scoped ReadOnlySpan<nint> lengths)
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

        #region TensorPrimitives
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

        #region Add
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            T[] values = new T[input.FlattenedLength];
            TensorSpan<T> output = new TensorSpan<T>(values, 0, input.Lengths, default);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
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
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = input;
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
            TensorPrimitives.Add(span, val, ospan);
            return output;
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

        #region Cos
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
        /// Takes the sin of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        public static TensorSpan<T> Sin<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sin);
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
        /// Takes the square root of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the square root of.</param>
        public static TensorSpan<T> Sqrt<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Sqrt);
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
        /// Takes the natural logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the natural logarithm of.</param>
        public static TensorSpan<T> Log<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log);
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
        /// Takes the base 10 logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static TensorSpan<T> Log10<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log10);
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
        /// Takes the base 2 logarithm of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="input">The <see cref="TensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static TensorSpan<T> Log2<T>(TensorSpan<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            return TensorPrimitivesHelperT1(input, TensorPrimitives.Log2);
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

        private static TensorSpan<T> TensorPrimitivesHelperT1<T>(TensorSpan<T> input, PerformCalculationT1<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._reference, (int)input.FlattenedLength);
            TensorSpan<T> output = inPlace ? input : new TensorSpan<T>(new T[input.FlattenedLength], 0, input.Lengths, input.Strides);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
            performCalculation(span, ospan);
            return output;
        }

        private static TensorSpan<T> TensorPrimitivesHelperT1T2<T>(TensorSpan<T> left, TensorSpan<T> right, PerformCalculationT1T2<T> performCalculation, bool inPlace = false)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (inPlace && left.Lengths != right.Lengths)
                ThrowHelper.ThrowArgument_InPlaceInvalidShape();

            //ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref tensor._reference, (int)tensor.FlattenedLength);
            //ReadOnlySpan<T> rspan = MemoryMarshal.CreateSpan(ref right._reference, (int)right.FlattenedLength);
            //TensorSpan<T> output = inPlace ? tensor : Create<T>(tensor.IsPinned, tensor.Lengths);
            //Span<T> ospan = MemoryMarshal.CreateSpan(ref output._reference, (int)output.FlattenedLength);
            //performCalculation(span, rspan, ospan);
            //return output;

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
