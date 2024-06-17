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
        public static bool SequenceEqual<T>(this scoped in ReadOnlyTensorSpan<T> span, scoped in ReadOnlyTensorSpan<T> other) where T : IEquatable<T>?
        {
            return span.FlattenedLength == other.FlattenedLength
                && MemoryMarshal.CreateReadOnlySpan(in span.GetPinnableReference(), (int)span.FlattenedLength).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(in other.GetPinnableReference(), (int)other.FlattenedLength));
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this scoped in TensorSpan<T> span, scoped in TensorSpan<T> other) where T : IEquatable<T>?
        {
            return ((ReadOnlyTensorSpan<T>)span).SequenceEqual((ReadOnlyTensorSpan<T>)other);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this scoped in TensorSpan<T> span, scoped in ReadOnlyTensorSpan<T> other) where T : IEquatable<T>?
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

        #region AsReadOnlySpan
        /// <summary>
        /// Extension method to more easily create a TensorSpan from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array</typeparam>
        /// <param name="array">The <see cref="System.Array"/> with the data</param>
        /// <param name="shape">The shape for the <see cref="TensorSpan{T}"/></param>
        /// <returns></returns>
        public static ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan<T>(this T[]? array, params ReadOnlySpan<nint> shape) => new(array, 0, shape, default);
        #endregion

        #region ToString
        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="span">The <see cref="TensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="span"/></returns>
        public static string ToString<T>(this scoped in TensorSpan<T> span, params ReadOnlySpan<nint> maximumLengths) => ((ReadOnlyTensorSpan<T>)span).ToString(maximumLengths);

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="ReadOnlyTensorSpan{T}"/>."/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span">The <see cref="ReadOnlyTensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        public static string ToString<T>(this scoped in ReadOnlyTensorSpan<T> span, params ReadOnlySpan<nint> maximumLengths)
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
    }
}
