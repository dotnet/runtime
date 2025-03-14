// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class ReadOnlyTensorSpanTests
    {
        [Fact]
        public static void ReadOnlyTensorSpan_SystemArrayConstructorTests()
        {
            // When using System.Array constructor make sure the type of the array matches T[]
            Assert.Throws<ArrayTypeMismatchException>(() => new ReadOnlyTensorSpan<double>(array: new[] { 1 }));

            string[] stringArray = { "a", "b", "c" };
            Assert.Throws<ArrayTypeMismatchException>(() => new ReadOnlyTensorSpan<object>(array: stringArray));

            // Make sure basic T[,] constructor works
            int[,] a = new int[,] { { 91, 92, -93, 94 } };
            scoped ReadOnlyTensorSpan<int> spanInt = new ReadOnlyTensorSpan<int>(a);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(4, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[0, 2]);
            Assert.Equal(94, spanInt[0, 3]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure null works
            // Should be a tensor with 0 elements and Rank 0 and no strides or lengths
            int[,] n = null;
            spanInt = new ReadOnlyTensorSpan<int>(n);
            Assert.Equal(0, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths.Length);
            Assert.Equal(0, spanInt.Strides.Length);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure empty array works
            // Should be a Tensor with 0 elements but Rank 2 with dimension 0 length 0
            int[,] b = { { } };
            spanInt = new ReadOnlyTensorSpan<int>(b);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.Lengths[1]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);
            Assert.Equal(0, spanInt.Strides[1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
            // Make sure it still throws on index 0, 0
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(b);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[0, 0];
            });

            // Make sure 2D array works
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [2, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure can use only some of the array
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[0, -1];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[-1, 0];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 1], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 2], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 3], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure we catch that there aren't enough elements in the array for the lengths
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 3], [1, 2], default);
            });

            // Make sure 2D array works with basic strides
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [2, 2], [2, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 2], [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], [-1, 0]);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], [0, -1]);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [-1, 2], []);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, -2], []);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[], [2, 2], [2, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(91, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(-93, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[], [2, 2], [1, 1]);
            });

            a = new int[,] { { 91, 92 }, { -93, 94 } };
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[1, 1], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            //Make sure it works with NIndex
            spanInt = new ReadOnlyTensorSpan<int>(a, (NIndex[])[1, 1], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            //Make sure it works with NIndex
            spanInt = new ReadOnlyTensorSpan<int>(a, (NIndex[])[^1, ^1], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
        }

        [Fact]
        public static void ReadOnlyTensorSpan_ArrayConstructorTests()
        {
            // Make sure exception is thrown if lengths and strides would let you go past the end of the array
            Assert.Throws<ArgumentException>(() => new ReadOnlyTensorSpan<double>(new double[0], lengths: new IntPtr[] { 2 }, strides: new IntPtr[] { 1 }));
            Assert.Throws<ArgumentException>(() => new ReadOnlyTensorSpan<double>(new double[1], lengths: new IntPtr[] { 2 }, strides: new IntPtr[] { 1 }));
            Assert.Throws<ArgumentException>(() => new ReadOnlyTensorSpan<double>(new double[2], lengths: new IntPtr[] { 2 }, strides: new IntPtr[] { 2 }));

            // Make sure basic T[] constructor works
            int[] a = { 91, 92, -93, 94 };
            scoped ReadOnlyTensorSpan<int> spanInt = new ReadOnlyTensorSpan<int>(a);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure null works
            // Should be a tensor with 0 elements and Rank 0 and no strides or lengths
            spanInt = new ReadOnlyTensorSpan<int>(null);
            Assert.Equal(0, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths.Length);
            Assert.Equal(0, spanInt.Strides.Length);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure empty array works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            int[] b = { };
            spanInt = new ReadOnlyTensorSpan<int>(b);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
            // Make sure it still throws on index 0
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(b);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[0];
            });

            // Make sure empty array works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            spanInt = new ReadOnlyTensorSpan<int>(b, 0, [], default);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works
            spanInt = new ReadOnlyTensorSpan<int>(a, new Index(0), [2, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure can use only some of the array
            spanInt = new ReadOnlyTensorSpan<int>(a, new Index(0), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, new Index(0), [1, 2], default);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, new Index(0), [1, 2], default);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a, new Index(1), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a, new Index(2), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure we catch that there aren't enough elements in the array for the lengths
            Assert.Throws<ArgumentException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, new Index(3), [1, 2], default);
            });

            // Make sure 2D array works with basic strides
            spanInt = new ReadOnlyTensorSpan<int>(a, new Index(0), [2, 2], [2, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, new Index(0), [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, new Index(2), [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new ReadOnlyTensorSpan<int>(a, new Index(3), [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, new Index(3), [1, 2], [-1, 0]);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, new Index(3), [1, 2], [0, -1]);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, new Index(3), [-1, 2], []);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, new Index(3), [1, -2], []);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            spanInt = new ReadOnlyTensorSpan<int>(a, 0, [2, 2], [2, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(91, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(-93, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 0, [2, 2], [1, 1]);
            });
        }

        [Fact]
        public static void ReadOnlyTensorSpan_SpanConstructorTests()
        {
            // Make sure basic T[] constructor works
            Span<int> a = [91, 92, -93, 94];
            scoped ReadOnlyTensorSpan<int> spanInt = new ReadOnlyTensorSpan<int>(a);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure empty span works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            Span<int> b = [];
            spanInt = new ReadOnlyTensorSpan<int>(b);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
            // Make sure it still throws on index 0
            Assert.Throws<IndexOutOfRangeException>(() => {
                Span<int> b = [];
                var spanInt = new ReadOnlyTensorSpan<int>(b);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[0];
            });

            // Make sure 2D array works
            spanInt = new ReadOnlyTensorSpan<int>(a, [2, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure can use only some of the array
            spanInt = new ReadOnlyTensorSpan<int>(a, [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
            Assert.Throws<IndexOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [1, 2], default);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [1, 2], default);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a.Slice(1), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a.Slice(2), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure we catch that there aren't enough elements in the array for the lengths
            Assert.Throws<ArgumentException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a.Slice(3), [1, 2], default);
            });

            // Make sure 2D array works with basic strides
            spanInt = new ReadOnlyTensorSpan<int>(a, [2, 2], [2, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a.Slice(2), [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new ReadOnlyTensorSpan<int>(a.Slice(3), [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [1, 2], [-1, 0]);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [1, 2], [0, -1]);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [-1, 2], []);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [1, -2], []);
            });

            // Make sure 2D array works with strides to hit element 0,0,2,2
            spanInt = new ReadOnlyTensorSpan<int>(a, [2, 2], [2, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(91, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(-93, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [2, 2], [1, 1]);
            });
        }

        [Fact]
        public static unsafe void ReadOnlyTensorSpan_PointerConstructorTests()
        {
            // Make sure basic T[] constructor works
            Span<int> a = [91, 92, -93, 94];
            ReadOnlyTensorSpan<int> spanInt;
            fixed (int* p = a)
            {
                spanInt = new ReadOnlyTensorSpan<int>(p, 4);
                Assert.Equal(1, spanInt.Rank);
                Assert.Equal(4, spanInt.Lengths[0]);
                Assert.Equal(91, spanInt[0]);
                Assert.Equal(92, spanInt[1]);
                Assert.Equal(-93, spanInt[2]);
                Assert.Equal(94, spanInt[3]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
            }

            // Make sure empty span works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            Span<int> b = [];
            fixed (int* p = b)
            {
                spanInt = new ReadOnlyTensorSpan<int>(p, 0);
                Assert.Equal(1, spanInt.Rank);
                Assert.Equal(0, spanInt.Lengths[0]);
                Assert.Equal(0, spanInt.FlattenedLength);
                Assert.Equal(0, spanInt.Strides[0]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                // Make sure it still throws on index 0
                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    Span<int> b = [];
                    fixed (int* p = b)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 0);
                        ReadOnlyTensorSpan_TestEnumerator(spanInt);
                        var x = spanInt[0];
                    }
                });
            }


            // Make sure 2D array works
            fixed (int* p = a)
            {
                spanInt = new ReadOnlyTensorSpan<int>(p, 4, [2, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);

                // Make sure can use only some of the array
                spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(1, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);
                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, 2], default);
                        ReadOnlyTensorSpan_TestEnumerator(spanInt);
                        var x = spanInt[1, 1];
                    }
                });

                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, 2], default);
                        ReadOnlyTensorSpan_TestEnumerator(spanInt);
                        var x = spanInt[1, 0];
                    }
                });

                // Make sure Index offset works correctly
                spanInt = new ReadOnlyTensorSpan<int>(p + 1, 3, [1, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(1, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(92, spanInt[0, 0]);
                Assert.Equal(-93, spanInt[0, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);

                // Make sure Index offset works correctly
                spanInt = new ReadOnlyTensorSpan<int>(p + 2, 2, [1, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(1, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(-93, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);

                // Make sure we catch that there aren't enough elements in the array for the lengths
                Assert.Throws<ArgumentException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p + 3, 1, [1, 2], default);
                    }
                });

                // Make sure 2D array works with basic strides
                spanInt = new ReadOnlyTensorSpan<int>(p, 4, [2, 2], [2, 1]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);

                // Make sure 2D array works with stride of 0 to loop over first 2 elements again
                spanInt = new ReadOnlyTensorSpan<int>(p, 4, [2, 2], [0, 1]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Equal(91, spanInt[1, 0]);
                Assert.Equal(92, spanInt[1, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);

                // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
                spanInt = new ReadOnlyTensorSpan<int>(p + 2, 2, [2, 2], [0, 1]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(-93, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);

                // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
                spanInt = new ReadOnlyTensorSpan<int>(p + 3, 1, [2, 2], [0, 0]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(94, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);
                Assert.Equal(94, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);

                // Make sure strides can't be negative
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, 2], [-1, 0]);
                    }
                });
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, 2], [0, -1]);
                    }
                });

                // Make sure lengths can't be negative
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [-1, 2], []);
                    }
                });
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, -2], []);
                    }
                });

                // Make sure can't use negative data length amount
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, -1, [1, -2], []);
                    }
                });

                // Make sure 2D array works with strides to hit element 0,0,2,2
                spanInt = new ReadOnlyTensorSpan<int>(p, 4, [2, 2], [2, 0]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(91, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(-93, spanInt[1, 1]);
                ReadOnlyTensorSpan_TestEnumerator(spanInt);

                // Make sure you can't overlap elements using strides
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [2, 2], [1, 1]);
                    }
                });
            }
        }

        [Fact]
        public static void ReadOnlyTensorSpan_LargeDimensionsTests()
        {
            int[] a = { 91, 92, -93, 94, 95, -96 };
            int[] results = new int[6];
            ReadOnlyTensorSpan<int> spanInt = a.AsReadOnlyTensorSpan(1, 1, 1, 1, 1, 6);
            Assert.Equal(6, spanInt.Rank);

            Assert.Equal(6, spanInt.Lengths.Length);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(1, spanInt.Lengths[1]);
            Assert.Equal(1, spanInt.Lengths[2]);
            Assert.Equal(1, spanInt.Lengths[3]);
            Assert.Equal(1, spanInt.Lengths[4]);
            Assert.Equal(6, spanInt.Lengths[5]);
            Assert.Equal(6, spanInt.Strides.Length);
            Assert.Equal(6, spanInt.Strides[0]);
            Assert.Equal(6, spanInt.Strides[1]);
            Assert.Equal(6, spanInt.Strides[2]);
            Assert.Equal(6, spanInt.Strides[3]);
            Assert.Equal(6, spanInt.Strides[4]);
            Assert.Equal(1, spanInt.Strides[5]);
            Assert.Equal(91, spanInt[0, 0, 0, 0, 0, 0]);
            Assert.Equal(92, spanInt[0, 0, 0, 0, 0, 1]);
            Assert.Equal(-93, spanInt[0, 0, 0, 0, 0, 2]);
            Assert.Equal(94, spanInt[0, 0, 0, 0, 0, 3]);
            Assert.Equal(95, spanInt[0, 0, 0, 0, 0, 4]);
            Assert.Equal(-96, spanInt[0, 0, 0, 0, 0, 5]);
            spanInt.FlattenTo(results);
            Assert.Equal(a, results);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            a = [91, 92, -93, 94, 95, -96, -91, -92, 93, -94, -95, 96];
            results = new int[12];
            spanInt = a.AsReadOnlyTensorSpan(1, 2, 2, 1, 1, 3);
            Assert.Equal(6, spanInt.Lengths.Length);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(2, spanInt.Lengths[2]);
            Assert.Equal(1, spanInt.Lengths[3]);
            Assert.Equal(1, spanInt.Lengths[4]);
            Assert.Equal(3, spanInt.Lengths[5]);
            Assert.Equal(6, spanInt.Strides.Length);
            Assert.Equal(12, spanInt.Strides[0]);
            Assert.Equal(6, spanInt.Strides[1]);
            Assert.Equal(3, spanInt.Strides[2]);
            Assert.Equal(3, spanInt.Strides[3]);
            Assert.Equal(3, spanInt.Strides[4]);
            Assert.Equal(1, spanInt.Strides[5]);
            Assert.Equal(91, spanInt[0, 0, 0, 0, 0, 0]);
            Assert.Equal(92, spanInt[0, 0, 0, 0, 0, 1]);
            Assert.Equal(-93, spanInt[0, 0, 0, 0, 0, 2]);
            Assert.Equal(94, spanInt[0, 0, 1, 0, 0, 0]);
            Assert.Equal(95, spanInt[0, 0, 1, 0, 0, 1]);
            Assert.Equal(-96, spanInt[0, 0, 1, 0, 0, 2]);
            Assert.Equal(-91, spanInt[0, 1, 0, 0, 0, 0]);
            Assert.Equal(-92, spanInt[0, 1, 0, 0, 0, 1]);
            Assert.Equal(93, spanInt[0, 1, 0, 0, 0, 2]);
            Assert.Equal(-94, spanInt[0, 1, 1, 0, 0, 0]);
            Assert.Equal(-95, spanInt[0, 1, 1, 0, 0, 1]);
            Assert.Equal(96, spanInt[0, 1, 1, 0, 0, 2]);
            spanInt.FlattenTo(results);
            Assert.Equal(a, results);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
        }

        [Fact]
        public static void ReadOnlyTensorSpan_IntArrayAs()
        {
            int[] a = { 91, 92, -93, 94 };
            int[] results = new int[4];
            ReadOnlyTensorSpan<int> spanInt = a.AsReadOnlyTensorSpan(4);
            Assert.Equal(1, spanInt.Rank);

            Assert.Equal(1, spanInt.Lengths.Length);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(1, spanInt.Strides.Length);
            Assert.Equal(1, spanInt.Strides[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);
            spanInt.FlattenTo(results);
            Assert.Equal(a, results);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            a[0] = 91;
            a[1] = 92;
            a[2] = -93;
            a[3] = 94;
            spanInt = a.AsReadOnlyTensorSpan(2, 2);
            spanInt.FlattenTo(results);
            Assert.Equal(a, results);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths.Length);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(2, spanInt.Strides.Length);
            Assert.Equal(2, spanInt.Strides[0]);
            Assert.Equal(1, spanInt.Strides[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
        }

        [Fact]
        public static void ReadOnlyTensorSpan_CopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            ReadOnlyTensorSpan<int> leftSpan = leftData.AsReadOnlyTensorSpan(3, 3);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan(3, 3);
            leftSpan.CopyTo(rightSpan);
            var leftEnum = leftSpan.GetEnumerator();
            var rightEnum = rightSpan.GetEnumerator();
            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            //Make sure its a copy
            rightSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);

            // Can't copy if data is not same shape or broadcastable to.
            Assert.Throws<ArgumentException>(() =>
            {
                leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
                rightData = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
                ReadOnlyTensorSpan<int> leftSpan = leftData.AsReadOnlyTensorSpan(9);
                TensorSpan<int> tensor = rightData.AsTensorSpan(rightData.Length);
                ReadOnlyTensorSpan_TestEnumerator(leftSpan);
                leftSpan.CopyTo(tensor);
            }
            );
            ReadOnlyTensorSpan_TestEnumerator(leftSpan);

            leftData = [.. Enumerable.Range(0, 27)];
            rightData = [.. Enumerable.Range(0, 27)];
            leftSpan = leftData.AsReadOnlyTensorSpan(3, 3, 3);
            rightSpan = rightData.AsTensorSpan(3, 3, 3);
            leftSpan.CopyTo(rightSpan);

            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            Assert.Throws<ArgumentException>(() =>
            {
                ReadOnlyTensorSpan<int> l = leftData.AsReadOnlyTensorSpan(3, 3, 3);
                ReadOnlyTensorSpan_TestEnumerator(l);
                var r = new TensorSpan<int>();
                l.CopyTo(r);
            });
            ReadOnlyTensorSpan_TestEnumerator(leftSpan);
        }

        [Fact]
        public static void ReadOnlyTensorSpan_TryCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            ReadOnlyTensorSpan<int> leftSpan = leftData.AsReadOnlyTensorSpan(3, 3);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan(3, 3);
            var success = leftSpan.TryCopyTo(rightSpan);
            Assert.True(success);
            var leftEnum = leftSpan.GetEnumerator();
            var rightEnum = rightSpan.GetEnumerator();
            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            //Make sure its a copy
            rightSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);
            ReadOnlyTensorSpan_TestEnumerator(leftSpan);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            rightData = new int[15];
            leftSpan = leftData.AsReadOnlyTensorSpan(9);
            rightSpan = rightData.AsTensorSpan(15);
            success = leftSpan.TryCopyTo(rightSpan);
            Assert.False(success);
            ReadOnlyTensorSpan_TestEnumerator(leftSpan);

            leftData = [.. Enumerable.Range(0, 27)];
            rightData = [.. Enumerable.Range(0, 27)];
            leftSpan = leftData.AsReadOnlyTensorSpan(3, 3, 3);
            rightSpan = rightData.AsTensorSpan(3, 3, 3);
            success = leftSpan.TryCopyTo(rightSpan);
            Assert.True(success);

            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }
            ReadOnlyTensorSpan_TestEnumerator(leftSpan);

            ReadOnlyTensorSpan<int> l = leftData.AsReadOnlyTensorSpan(3, 3, 3);
            var r = new TensorSpan<int>();
            success = l.TryCopyTo(r);
            Assert.False(success);
            ReadOnlyTensorSpan_TestEnumerator(l);

            success = new ReadOnlyTensorSpan<double>(new double[1]).TryCopyTo(Array.Empty<double>());
            Assert.False(success);
            ReadOnlyTensorSpan_TestEnumerator(new ReadOnlyTensorSpan<double>(new double[1]));
        }

        [Fact]
        public static void ReadOnlyTensorSpan_SliceTest()
        {
            // Make sure slicing an empty ReadOnlyTensorSpan works
            ReadOnlyTensorSpan<int> emptyTensorSpan = new ReadOnlyTensorSpan<int>(Array.Empty<int>()).Slice(new NRange[] { .. });
            Assert.Equal([0], emptyTensorSpan.Lengths);
            Assert.Equal(1, emptyTensorSpan.Rank);
            Assert.Equal(0, emptyTensorSpan.FlattenedLength);
            ReadOnlyTensorSpan_TestEnumerator(emptyTensorSpan);

            // Make sure slicing a multi-dimensional empty ReadOnlyTensorSpan works
            int[,] empty2dArray = new int[2, 0];
            emptyTensorSpan = new ReadOnlyTensorSpan<int>(empty2dArray);
            ReadOnlyTensorSpan<int> slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { .., .. });
            Assert.Equal([2, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(2, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);
            ReadOnlyTensorSpan_TestEnumerator(emptyTensorSpan);
            ReadOnlyTensorSpan_TestEnumerator(slicedEmptyTensorSpan);

            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { 0..1, .. });
            Assert.Equal([1, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(2, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);
            ReadOnlyTensorSpan_TestEnumerator(slicedEmptyTensorSpan);

            // Make sure slicing a multi-dimensional empty ReadOnlyTensorSpan works
            int[,,,] empty4dArray = new int[2, 5, 1, 0];
            emptyTensorSpan = new ReadOnlyTensorSpan<int>(empty4dArray);
            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { .., .., .., .. });
            Assert.Equal([2, 5, 1, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(4, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);
            ReadOnlyTensorSpan_TestEnumerator(emptyTensorSpan);
            ReadOnlyTensorSpan_TestEnumerator(slicedEmptyTensorSpan);

            emptyTensorSpan = new ReadOnlyTensorSpan<int>(empty4dArray);
            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { 0..1, .., .., .. });
            Assert.Equal([1, 5, 1, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(4, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);
            ReadOnlyTensorSpan_TestEnumerator(emptyTensorSpan);
            ReadOnlyTensorSpan_TestEnumerator(slicedEmptyTensorSpan);

            emptyTensorSpan = new ReadOnlyTensorSpan<int>(empty4dArray);
            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { 0..1, 2..3, .., .. });
            Assert.Equal([1, 1, 1, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(4, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);
            ReadOnlyTensorSpan_TestEnumerator(emptyTensorSpan);
            ReadOnlyTensorSpan_TestEnumerator(slicedEmptyTensorSpan);

            empty4dArray = new int[2, 0, 1, 5];
            emptyTensorSpan = new ReadOnlyTensorSpan<int>(empty4dArray);
            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { .., .., .., .. });
            Assert.Equal([2, 0, 1, 5], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(4, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);
            ReadOnlyTensorSpan_TestEnumerator(emptyTensorSpan);
            ReadOnlyTensorSpan_TestEnumerator(slicedEmptyTensorSpan);

            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] results = new int[9];
            ReadOnlyTensorSpan<int> spanInt = a.AsReadOnlyTensorSpan(3, 3);

            Assert.Throws<IndexOutOfRangeException>(() => a.AsReadOnlyTensorSpan(2, 3).Slice(0..1));
            Assert.Throws<IndexOutOfRangeException>(() => a.AsReadOnlyTensorSpan(2, 3).Slice(1..2));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsReadOnlyTensorSpan(2, 3).Slice(0..1, 5..6));
            ReadOnlyTensorSpan_TestEnumerator(spanInt);

            var sp = spanInt.Slice(1..3, 1..3);
            Assert.Equal(5, sp[0, 0]);
            Assert.Equal(6, sp[0, 1]);
            Assert.Equal(8, sp[1, 0]);
            Assert.Equal(9, sp[1, 1]);
            int[] slice = [5, 6, 8, 9];
            results = new int[4];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            var enumerator = sp.GetEnumerator();
            var index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
            ReadOnlyTensorSpan_TestEnumerator(sp);

            sp = spanInt.Slice(0..3, 0..3);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(3, sp[0, 2]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            Assert.Equal(6, sp[1, 2]);
            Assert.Equal(7, sp[2, 0]);
            Assert.Equal(8, sp[2, 1]);
            Assert.Equal(9, sp[2, 2]);
            results = new int[9];
            sp.FlattenTo(results);
            Assert.Equal(a, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(a[index++], enumerator.Current);
            }
            ReadOnlyTensorSpan_TestEnumerator(sp);

            sp = spanInt.Slice(0..1, 0..1);
            Assert.Equal(1, sp[0, 0]);
            Assert.Throws<IndexOutOfRangeException>(() => a.AsReadOnlyTensorSpan(3, 3).Slice(0..1, 0..1)[0, 1]);
            slice = [1];
            results = new int[1];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
            ReadOnlyTensorSpan_TestEnumerator(sp);

            sp = spanInt.Slice(0..2, 0..2);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            slice = [1, 2, 4, 5];
            results = new int[4];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
            ReadOnlyTensorSpan_TestEnumerator(sp);

            int[] numbers = [.. Enumerable.Range(0, 27)];
            spanInt = numbers.AsReadOnlyTensorSpan(3, 3, 3);
            sp = spanInt.Slice(1..2, 1..2, 1..2);
            Assert.Equal(13, sp[0, 0, 0]);
            slice = [13];
            results = new int[1];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
            ReadOnlyTensorSpan_TestEnumerator(sp);

            sp = spanInt.Slice(1..3, 1..3, 1..3);
            Assert.Equal(13, sp[0, 0, 0]);
            Assert.Equal(14, sp[0, 0, 1]);
            Assert.Equal(16, sp[0, 1, 0]);
            Assert.Equal(17, sp[0, 1, 1]);
            Assert.Equal(22, sp[1, 0, 0]);
            Assert.Equal(23, sp[1, 0, 1]);
            Assert.Equal(25, sp[1, 1, 0]);
            Assert.Equal(26, sp[1, 1, 1]);
            slice = [13, 14, 16, 17, 22, 23, 25, 26];
            results = new int[8];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
            ReadOnlyTensorSpan_TestEnumerator(sp);

            numbers = [.. Enumerable.Range(0, 16)];
            spanInt = numbers.AsReadOnlyTensorSpan(2, 2, 2, 2);
            sp = spanInt.Slice(1..2, 0..2, 1..2, 0..2);
            Assert.Equal(10, sp[0, 0, 0, 0]);
            Assert.Equal(11, sp[0, 0, 0, 1]);
            Assert.Equal(14, sp[0, 1, 0, 0]);
            Assert.Equal(15, sp[0, 1, 0, 1]);
            slice = [10, 11, 14, 15];
            results = new int[4];
            sp.FlattenTo(results);
            Assert.Equal(slice, results);
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
            ReadOnlyTensorSpan_TestEnumerator(spanInt);
            ReadOnlyTensorSpan_TestEnumerator(sp);
        }

        [Fact]
        public static void ReadOnlyTensorSpan_LongArrayAs()
        {
            long[] b = { 91, -92, 93, 94, -95 };
            ReadOnlyTensorSpan<long> spanLong = b.AsReadOnlyTensorSpan(5);
            Assert.Equal(91, spanLong[0]);
            Assert.Equal(-92, spanLong[1]);
            Assert.Equal(93, spanLong[2]);
            Assert.Equal(94, spanLong[3]);
            Assert.Equal(-95, spanLong[4]);
            ReadOnlyTensorSpan_TestEnumerator(spanLong);
        }

        [Fact]
        public static void ReadOnlyTensorSpan_NullArrayAs()
        {
            int[] a = null;
            ReadOnlyTensorSpan<int> span = a.AsReadOnlyTensorSpan();
            ReadOnlyTensorSpan<int> d = default;
            Assert.True(span == d);
            ReadOnlyTensorSpan_TestEnumerator(span);
            ReadOnlyTensorSpan_TestEnumerator(d);
        }

        private static void ReadOnlyTensorSpan_TestEnumerator<T>(ReadOnlyTensorSpan<T> span)
            where T: INumberBase<T>
        {
            Span<nint> curIndexes = new nint[span.Rank];
            if (span.Rank > 0)
                curIndexes[span.Rank - 1] = -1;
            var enumerator = span.GetEnumerator();
            for (var i = 0; i < span.FlattenedLength; i++)
            {
                TensorSpanHelpers_AdjustIndexes(span.Rank - 1, 1, curIndexes, span.Lengths);
                Assert.True(enumerator.MoveNext());
                ref readonly var current = ref enumerator.Current;

                Assert.Equal(span[curIndexes], current);
                Unsafe.AsRef(in span[curIndexes])++;
                Assert.Equal(span[curIndexes], current);
                Unsafe.AsRef(in span[curIndexes])--;
                Assert.Equal(span[curIndexes], current);
            }
            Assert.False(enumerator.MoveNext());

            TestGI(span.GetEnumerator(), span);
            TestI(span.GetEnumerator(), span);

            static void TestGI<TEnumerator>(TEnumerator enumerator, ReadOnlyTensorSpan<T> span)
                where TEnumerator : IEnumerator<T>, allows ref struct
            {
                Test(enumerator, span);
                _ = enumerator.MoveNext();
                enumerator.Reset();
                Test(enumerator, span);

                static void Test(TEnumerator enumerator, ReadOnlyTensorSpan<T> span)
                {
                    Span<nint> curIndexes = new nint[span.Rank];
                    if (span.Rank > 0)
                        curIndexes[span.Rank - 1] = -1;
                    for (var i = 0; i < span.FlattenedLength; i++)
                    {
                        TensorSpanHelpers_AdjustIndexes(span.Rank - 1, 1, curIndexes, span.Lengths);
                        Assert.True(enumerator.MoveNext());
                        Assert.Equal(span[curIndexes], enumerator.Current);
                        enumerator.Dispose();
                        Assert.Equal(span[curIndexes], enumerator.Current);
                    }
                    Assert.False(enumerator.MoveNext());
                    enumerator.Dispose();
                    Assert.False(enumerator.MoveNext());
                }
            }

            static void TestI<TEnumerator>(TEnumerator enumerator, ReadOnlyTensorSpan<T> span)
                where TEnumerator : IEnumerator, allows ref struct
            {
                Test(enumerator, span);
                _ = enumerator.MoveNext();
                enumerator.Reset();
                Test(enumerator, span);

                static void Test(TEnumerator enumerator, ReadOnlyTensorSpan<T> span)
                {
                    Span<nint> curIndexes = new nint[span.Rank];
                    if (span.Rank > 0)
                        curIndexes[span.Rank - 1] = -1;
                    for (var i = 0; i < span.FlattenedLength; i++)
                    {
                        TensorSpanHelpers_AdjustIndexes(span.Rank - 1, 1, curIndexes, span.Lengths);
                        Assert.True(enumerator.MoveNext());
                        Assert.Equal(span[curIndexes], enumerator.Current);
                    }
                    Assert.False(enumerator.MoveNext());
                }
            }
        }

        private static void TensorSpanHelpers_AdjustIndexes(int curIndex, nint addend, Span<nint> curIndexes, scoped ReadOnlySpan<nint> length)
        {
            if (addend <= 0 || curIndex < 0 || length[curIndex] <= 0)
                return;
            curIndexes[curIndex] += addend;

            (nint Quotient, nint Remainder) result = Math.DivRem(curIndexes[curIndex], length[curIndex]);

            TensorSpanHelpers_AdjustIndexes(curIndex - 1, result.Quotient, curIndexes, length);
            curIndexes[curIndex] = result.Remainder;
        }
    }
}
