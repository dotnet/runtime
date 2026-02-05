// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("F8A2C5D1-9B7E-4A3C-8F5D-2E1B9C4A7F6E")]
    internal partial interface INullArrayCases
    {
        // Basic case: single null array with non-zero length
        void SingleNullArrayWithLength(
            int length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In, Out] int[]? array);

        // Multiple arrays sharing same length, some null, some not
        void MultipleArraysSharedLength(
            int length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In, Out] int[]? array1,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In, Out] int[]? array2,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In, Out] int[]? array3);

        // Non-blittable types with null arrays
        void NonBlittableNullArray(
            int length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In, Out] IntStructWrapper[]? array);

        // Different parameter directions
        void InOnlyNullArray(
            int length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In] int[]? array);

        void OutOnlyNullArray(
            int length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] int[]? array);

        // Reference arrays with null (different from value type arrays)
        void ReferenceArrayNullCase(
            int length,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0), In, Out] string[]? array);

        // Span<T> with null cases (ContiguousCollectionMarshaller)
        void SpanNullCase(
            int length,
            [MarshalUsing(CountElementName = nameof(length))] ref Span<int> span);

        // Span<T> with non-blittable types
        void SpanNonBlittableNullCase(
            int length,
            [MarshalUsing(CountElementName = nameof(length))] ref Span<IntStructWrapper> span);
    }

    [GeneratedComClass]
    internal partial class INullArrayCasesImpl : INullArrayCases
    {
        public void SingleNullArrayWithLength(int length, int[]? array)
        {
            // Should handle null array gracefully regardless of length
            if (array != null)
            {
                for (int i = 0; i < Math.Min(length, array.Length); i++)
                {
                    array[i] = i * 2;
                }
            }
        }

        public void MultipleArraysSharedLength(int length, int[]? array1, int[]? array2, int[]? array3)
        {
            // Should only process non-null arrays
            if (array1 != null)
            {
                for (int i = 0; i < length; i++)
                {
                    array1[i] = i;
                }
            }
            if (array2 != null)
            {
                for (int i = 0; i < length; i++)
                {
                    array2[i] = i * 10;
                }
            }
            if (array3 != null)
            {
                for (int i = 0; i < length; i++)
                {
                    array3[i] = i * 100;
                }
            }
        }

        public void NonBlittableNullArray(int length, IntStructWrapper[]? array)
        {
            if (array != null)
            {
                for (int i = 0; i < length; i++)
                {
                    array[i] = new IntStructWrapper { Value = i * 3 };
                }
            }
        }

        public void InOnlyNullArray(int length, int[]? array)
        {
            // Input-only: just read from array if not null
            if (array != null)
            {
                int sum = 0;
                for (int i = 0; i < length; i++)
                {
                    sum += array[i];
                }
                // Could store sum somewhere, but for test we just ensure no crash
            }
        }

        public void OutOnlyNullArray(int length, int[]? array)
        {
            // Output-only: write to array if not null
            if (array != null)
            {
                for (int i = 0; i < length; i++)
                {
                    array[i] = i + 1000;
                }
            }
        }

        public void ReferenceArrayNullCase(int length, string[]? array)
        {
            if (array != null)
            {
                for (int i = 0; i < length; i++)
                {
                    array[i] = $"Item {i}";
                }
            }
        }

        public void SpanNullCase(int length, ref Span<int> span)
        {
            // Should handle empty/default span gracefully regardless of length
            if (!span.IsEmpty)
            {
                for (int i = 0; i < length; i++)
                {
                    span[i] = i * 5;
                }
            }
        }

        public void SpanNonBlittableNullCase(int length, ref Span<IntStructWrapper> span)
        {
            // Should handle empty/default span gracefully regardless of length
            if (!span.IsEmpty)
            {
                for (int i = 0; i < length; i++)
                {
                    span[i] = new IntStructWrapper { Value = i * 7 };
                }
            }
        }
    }
}
