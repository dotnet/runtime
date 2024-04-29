// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal static partial class SpanNDHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint CalculateTotalLength(ref nint[] lengths)
        {
            nint totalLength = 1;
            for (int i = 0; i < lengths.Length; i++)
            {
                totalLength *= lengths[i];
            }

            return totalLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint CalculateTotalLength(ReadOnlySpan<nint> lengths)
        {
            nint totalLength = 1;
            for (int i = 0; i < lengths.Length; i++)
            {
                totalLength *= lengths[i];
            }

            return totalLength;
        }

        /// <summary>
        /// Gets the set of strides that can be used to calculate the offset of n-dimensions in a 1-dimensional layout
        /// </summary>
        /// <returns></returns>
        public static nint[] CalculateStrides(int rank, ReadOnlySpan<nint> lengths)
        {
            nint[] strides = new nint[rank];

            nint stride = 1;

            for (int i = strides.Length - 1; i >= 0; i--)
            {
                strides[i] = stride;
                stride *= lengths[i];
            }

            return strides;
        }

        /// <summary>
        /// Calculates the 1-d index for n-d indices in layout specified by strides.
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="strides"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static nint GetIndex(ReadOnlySpan<nint> indices, ReadOnlySpan<nint> strides, ReadOnlySpan<nint> lengths)
        {
            Debug.Assert(strides.Length == indices.Length);

            nint index = 0;
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] >= lengths[i])
                    ThrowHelper.ThrowIndexOutOfRangeException();
                index += strides[i] * (indices[i]);
            }

            return index;
        }

        /// <summary>
        /// Takes the span holding the current index and increments it by the addend. If the length of the current spot is greater than the
        /// length of that dimension then it rolls that over to the next dimension.
        /// </summary>
        /// <param name="curIndex">The current index from the indices we are on.</param>
        /// <param name="addend">How much we are adding to the <paramref name="curIndex"/></param>
        /// <param name="curIndices">The current indices</param>
        /// <param name="shape">The shape of the SpanND we are iterating over.</param>
        public static void AdjustIndices(int curIndex, nint addend, ref Span<nint> curIndices, ReadOnlySpan<nint> shape)
        {
            if (addend <= 0 || curIndex < 0)
                return;
            curIndices[curIndex] += addend;

            (nint Quotient, nint Remainder) result = Math.DivRem(curIndices[curIndex], shape[curIndex]);

            AdjustIndices(curIndex - 1, result.Quotient, ref curIndices, shape);
            curIndices[curIndex] = result.Remainder;
        }

        /// <summary>
        /// Takes the span holding the current index and increments it by the addend. If the length of the current spot is greater than the
        /// length of that dimension then it rolls that over to the next dimension.
        /// </summary>
        /// <param name="curIndex">The current index from the indices we are on.</param>
        /// <param name="addend">How much we are adding to the <paramref name="curIndex"/></param>
        /// <param name="curIndices">The current indices</param>
        /// <param name="shape">The shape of the SpanND we are iterating over.</param>
        public static void AdjustIndices(int curIndex, nint addend, ref nint[] curIndices, ReadOnlySpan<nint> shape)
        {
            if (addend <= 0 || curIndex < 0)
                return;
            curIndices[curIndex] += addend;

            (nint Quotient, nint Remainder) result = Math.DivRem(curIndices[curIndex], shape[curIndex]);

            AdjustIndices(curIndex - 1, result.Quotient, ref curIndices, shape);
            curIndices[curIndex] = result.Remainder;
        }

        /// <summary>
        /// Takes the span holding the current index and decrements it by the addend. If the length of the current spot is greater than the
        /// length of that dimension then it rolls that over to the next dimension.
        /// </summary>
        /// <param name="curIndex">The current index from the indices we are on.</param>
        /// <param name="addend">How much we are subtracting from the <paramref name="curIndex"/></param>
        /// <param name="curIndices">The current indices</param>
        /// <param name="shape">The shape of the SpanND we are iterating over.</param>
        public static void AdjustIndicesDown(int curIndex, nint addend, ref Span<nint> curIndices, ReadOnlySpan<nint> shape)
        {
            if (addend <= 0 || curIndex < 0)
                return;
            curIndices[curIndex] -= addend;
            if (curIndices[curIndex] < 0)
            {
                curIndices[curIndex] = shape[curIndex] - 1;
                AdjustIndices(curIndex - 1, 1, ref curIndices, shape);
            }
        }
    }
}
