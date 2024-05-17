// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Microsoft.VisualBasic;

#pragma warning disable 8500 // sizeof of managed types

namespace System.Numerics.Tensors
{
    internal static partial class TensorSpanHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint CalculateTotalLength(ReadOnlySpan<nint> lengths)
        {
            if (lengths.IsEmpty)
                return 0;
            nint totalLength = 1;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                totalLength *= lengths[i];
            }

            if (totalLength < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return totalLength;
        }

        /// <summary>
        /// Gets the set of strides that can be used to calculate the offset of n-dimensions in a 1-dimensional layout
        /// </summary>
        /// <returns></returns>
        public static nint[] CalculateStrides(ReadOnlySpan<nint> lengths)
        {
            nint[] strides = new nint[lengths.Length];

            nint stride = 1;

            for (int i = strides.Length - 1; i >= 0; i--)
            {
                strides[i] = stride;
                stride *= lengths[i];
            }

            return strides;
        }

        /// <summary>
        /// Calculates the 1-d index for n-d indexes in layout specified by strides.
        /// </summary>
        /// <param name="indexes"></param>
        /// <param name="strides"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static nint ComputeLinearIndex(ReadOnlySpan<nint> indexes, ReadOnlySpan<nint> strides, ReadOnlySpan<nint> lengths)
        {
            Debug.Assert(strides.Length == indexes.Length);

            nint index = 0;
            for (int i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] >= lengths[i] || indexes[i] < 0)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                index += strides[i] * indexes[i];
            }

            return index;
        }

        /// <summary>
        /// Calculates the 1-d index for n-d indexes in layout specified by strides.
        /// </summary>
        /// <param name="indexes"></param>
        /// <param name="strides"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static nint ComputeLinearIndex(ReadOnlySpan<int> indexes, ReadOnlySpan<nint> strides, ReadOnlySpan<nint> lengths)
        {
            Debug.Assert(strides.Length == indexes.Length);

            nint index = 0;
            for (int i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] >= lengths[i] || indexes[i] < 0)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                index += strides[i] * indexes[i];
            }

            return index;
        }

        /// <summary>
        /// Calculates the 1-d index for n-d indexes in layout specified by strides.
        /// </summary>
        /// <param name="indexes"></param>
        /// <param name="strides"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static nint ComputeLinearIndex(ReadOnlySpan<NIndex> indexes, ReadOnlySpan<nint> strides, ReadOnlySpan<nint> lengths)
        {
            Debug.Assert(strides.Length == indexes.Length);

            nint index = 0;
            for (int i = 0; i < indexes.Length; i++)
            {
                nint offset = indexes[i].GetOffset(lengths[i]);
                if (offset >= lengths[i] || offset < 0)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                index += strides[i] * offset;
            }

            return index;
        }

        public static nint ComputeMaxElementCount(ReadOnlySpan<nint> strides, ReadOnlySpan<nint> lengths)
        {
            Debug.Assert(strides.Length == lengths.Length);

            nint index = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (strides[i] < 0)
                    ThrowHelper.ThrowArgument_StrideLessThan0();
                index *= strides[i] * lengths[i];
            }

            return index;
        }

        /// <summary>
        /// Takes the span holding the current index and increments it by the addend. If the length of the current spot is greater than the
        /// length of that dimension then it rolls that over to the next dimension.
        /// </summary>
        /// <param name="curIndex">The current index from the indexes we are on.</param>
        /// <param name="addend">How much we are adding to the <paramref name="curIndex"/></param>
        /// <param name="curIndexes">The current indexes</param>
        /// <param name="length">The length of the TensorSpan we are iterating over.</param>
        public static void AdjustIndexes(int curIndex, nint addend, Span<nint> curIndexes, scoped ReadOnlySpan<nint> length)
        {
            if (addend <= 0 || curIndex < 0)
                return;
            curIndexes[curIndex] += addend;

            (nint Quotient, nint Remainder) result = Math.DivRem(curIndexes[curIndex], length[curIndex]);

            AdjustIndexes(curIndex - 1, result.Quotient, curIndexes, length);
            curIndexes[curIndex] = result.Remainder;
        }

        /// <summary>
        /// Takes the span holding the current index and increments it by the addend. If the length of the current spot is greater than the
        /// length of that dimension then it rolls that over to the next dimension.
        /// </summary>
        /// <param name="curIndex">The current index from the indexes we are on.</param>
        /// <param name="addend">How much we are adding to the <paramref name="curIndex"/></param>
        /// <param name="curIndexes">The current indexes</param>
        /// <param name="shape">The length of the TensorSpan we are iterating over.</param>
        public static void AdjustIndexes(int curIndex, nint addend, ref nint[] curIndexes, ReadOnlySpan<nint> shape)
        {
            if (addend <= 0 || curIndex < 0)
                return;
            curIndexes[curIndex] += addend;

            (nint Quotient, nint Remainder) result = Math.DivRem(curIndexes[curIndex], shape[curIndex]);

            AdjustIndexes(curIndex - 1, result.Quotient, ref curIndexes, shape);
            curIndexes[curIndex] = result.Remainder;
        }

        /// <summary>
        /// Takes the span holding the current index and decrements it by the addend. If the length of the current spot is greater than the
        /// length of that dimension then it rolls that over to the next dimension.
        /// </summary>
        /// <param name="curIndex">The current index from the indexes we are on.</param>
        /// <param name="addend">How much we are subtracting from the <paramref name="curIndex"/></param>
        /// <param name="curIndexes">The current indexes</param>
        /// <param name="shape">The length of the TensorSpan we are iterating over.</param>
        public static void AdjustIndexesDown(int curIndex, nint addend, Span<nint> curIndexes, ReadOnlySpan<nint> shape)
        {
            if (addend <= 0 || curIndex < 0)
                return;
            curIndexes[curIndex] -= addend;
            if (curIndexes[curIndex] < 0)
            {
                curIndexes[curIndex] = shape[curIndex] - 1;
                AdjustIndexes(curIndex - 1, 1, curIndexes, shape);
            }
        }
    }
}
