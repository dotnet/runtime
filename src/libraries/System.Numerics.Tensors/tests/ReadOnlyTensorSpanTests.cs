// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class ReadOnlyTensorSpanTests
    {
        [Fact]
        public static void ReadOnlyTensorSpanSystemArrayConstructorTests()
        {
            // When using System.Array constructor make sure the type of the array matches T[]
            Assert.Throws<ArrayTypeMismatchException>(() => new TensorSpan<double>(array: new[] { 1 }));

            string[] stringArray = { "a", "b", "c" };
            Assert.Throws<ArrayTypeMismatchException>(() => new TensorSpan<object>(array: stringArray));

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

            // Make sure null works
            // Should be a tensor with 0 elements and Rank 0 and no strides or lengths
            int[,] n = null;
            spanInt = new ReadOnlyTensorSpan<int>(n);
            Assert.Equal(0, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths.Length);
            Assert.Equal(0, spanInt.Strides.Length);

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
            // Make sure it still throws on index 0, 0
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(b);
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

            // Make sure can use only some of the array
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                var x = spanInt[0, -1];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                var x = spanInt[-1, 0];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [1, 2], default);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 1], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 2], [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 3], [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

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

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 0], [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, (int[])[0, 2], [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

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

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentException>(() =>
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

            // // TODO: Make sure it works with NIndex
            // spanInt = new ReadOnlyTensorSpan<int>(a, (NIndex[])[1, 1], [2, 2], [0, 0]);
            // Assert.Equal(2, spanInt.Rank);
            // Assert.Equal(2, spanInt.Lengths[0]);
            // Assert.Equal(2, spanInt.Lengths[1]);
            // Assert.Equal(94, spanInt[0, 0]);
            // Assert.Equal(94, spanInt[0, 1]);
            // Assert.Equal(94, spanInt[1, 0]);
            // Assert.Equal(94, spanInt[1, 1]);

            // // TODO: Make sure it works with NIndex
            // spanInt = new ReadOnlyTensorSpan<int>(a, (NIndex[])[^1, ^1], [2, 2], [0, 0]);
            // Assert.Equal(2, spanInt.Rank);
            // Assert.Equal(2, spanInt.Lengths[0]);
            // Assert.Equal(2, spanInt.Lengths[1]);
            // Assert.Equal(94, spanInt[0, 0]);
            // Assert.Equal(94, spanInt[0, 1]);
            // Assert.Equal(94, spanInt[1, 0]);
            // Assert.Equal(94, spanInt[1, 1]);
        }

        [Fact]
        public static void ReadOnlyTensorSpanArrayConstructorTests()
        {
            // Make sure exception is thrown if lengths and strides would let you go past the end of the array
            Assert.Throws<ArgumentOutOfRangeException>(() => new TensorSpan<double>(new double[0], lengths: new IntPtr[] { 2 }, strides: new IntPtr[] { 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TensorSpan<double>(new double[1], lengths: new IntPtr[] { 2 }, strides: new IntPtr[] { 1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TensorSpan<double>(new double[2], lengths: new IntPtr[] { 2 }, strides: new IntPtr[] { 2 }));

            // Make sure basic T[] constructor works
            int[] a = { 91, 92, -93, 94 };
            scoped ReadOnlyTensorSpan<int> spanInt = new ReadOnlyTensorSpan<int>(a);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);

            // Make sure null works
            // Should be a tensor with 0 elements and Rank 0 and no strides or lengths
            spanInt = new ReadOnlyTensorSpan<int>(null);
            Assert.Equal(0, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths.Length);
            Assert.Equal(0, spanInt.Strides.Length);

            // Make sure empty array works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            int[] b = { };
            spanInt = new ReadOnlyTensorSpan<int>(b);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(1, spanInt.Strides[0]);
            // Make sure it still throws on index 0
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(b);
                var x = spanInt[0];
            });

            // Make sure empty array works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            spanInt = new ReadOnlyTensorSpan<int>(b, 0, [], default);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths[0]);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides[0]);

            // Make sure 2D array works
            spanInt = new ReadOnlyTensorSpan<int>(a, 0, [2,2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0,0]);
            Assert.Equal(92, spanInt[0,1]);
            Assert.Equal(-93, spanInt[1,0]);
            Assert.Equal(94, spanInt[1,1]);

            // Make sure can use only some of the array
            spanInt = new ReadOnlyTensorSpan<int>(a, 0, [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 0, [1, 2], default);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 0, [1, 2], default);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a, 1, [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a, 2, [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);

            // Make sure we catch that there aren't enough elements in the array for the lengths
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 3, [1, 2], default);
            });

            // Make sure 2D array works with basic strides
            spanInt = new ReadOnlyTensorSpan<int>(a, 0, [2, 2], [2, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, 0, [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, 2, [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new ReadOnlyTensorSpan<int>(a, 3, [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure strides can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 3, [1, 2], [-1, 0]);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 3, [1, 2], [0, -1]);
            });

            // Make sure lengths can't be negative
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 3, [-1, 2], []);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 3, [1, -2], []);
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

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentException>(() => {
                var spanInt = new ReadOnlyTensorSpan<int>(a, 0, [2, 2], [1, 1]);
            });
        }

        [Fact]
        public static void ReadOnlyTensorSpanSpanConstructorTests()
        {
            // Make sure basic T[] constructor works
            Span<int> a = [ 91, 92, -93, 94 ];
            scoped ReadOnlyTensorSpan<int> spanInt = new ReadOnlyTensorSpan<int>(a);
            Assert.Equal(1, spanInt.Rank);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);

            // Make sure empty span works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            Span<int> b = [];
            spanInt = new ReadOnlyTensorSpan<int>(b);
            Assert.Equal(0, spanInt.Rank);
            Assert.Equal(0, spanInt.Lengths.Length);
            Assert.Equal(0, spanInt.FlattenedLength);
            Assert.Equal(0, spanInt.Strides.Length);
            // Make sure it still throws on index 0
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                Span<int> b = [];
                var spanInt = new ReadOnlyTensorSpan<int>(b);
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

            // Make sure can use only some of the array
            spanInt = new ReadOnlyTensorSpan<int>(a, [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [1, 2], default);
                var x = spanInt[1, 1];
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [1, 2], default);
                var x = spanInt[1, 0];
            });

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a.Slice(1), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(92, spanInt[0, 0]);
            Assert.Equal(-93, spanInt[0, 1]);

            // Make sure Index offset works correctly
            spanInt = new ReadOnlyTensorSpan<int>(a.Slice(2), [1, 2], default);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);

            // Make sure we catch that there aren't enough elements in the array for the lengths
            Assert.Throws<ArgumentOutOfRangeException>(() => {
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

            // Make sure 2D array works with stride of 0 to loop over first 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a, [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(91, spanInt[1, 0]);
            Assert.Equal(92, spanInt[1, 1]);

            // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
            spanInt = new ReadOnlyTensorSpan<int>(a.Slice(2), [2, 2], [0, 1]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(-93, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
            spanInt = new ReadOnlyTensorSpan<int>(a.Slice(3), [2, 2], [0, 0]);
            Assert.Equal(2, spanInt.Rank);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(94, spanInt[0, 0]);
            Assert.Equal(94, spanInt[0, 1]);
            Assert.Equal(94, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

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

            // Make sure you can't overlap elements using strides
            Assert.Throws<ArgumentException>(() => {
                Span<int> a = [91, 92, -93, 94];
                var spanInt = new ReadOnlyTensorSpan<int>(a, [2, 2], [1, 1]);
            });
        }

        [Fact]
        public static unsafe void ReadOnlyTensorSpanPointerConstructorTests()
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
            }

            // Make sure empty span works
            // Should be a Tensor with 0 elements but Rank 1 with dimension 0 length 0
            Span<int> b = [];
            fixed (int* p = b)
            {
                spanInt = new ReadOnlyTensorSpan<int>(p, 0);
                Assert.Equal(0, spanInt.Rank);
                Assert.Equal(0, spanInt.Lengths.Length);
                Assert.Equal(0, spanInt.FlattenedLength);
                Assert.Equal(0, spanInt.Strides.Length);
                // Make sure it still throws on index 0
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    Span<int> b = [];
                    fixed (int* p = b)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 0);
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

                // Make sure can use only some of the array
                spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(1, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, 2], default);
                        var x = spanInt[1, 1];
                    }
                });

                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    Span<int> a = [91, 92, -93, 94];
                    fixed (int* p = a)
                    {
                        var spanInt = new ReadOnlyTensorSpan<int>(p, 4, [1, 2], default);
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

                // Make sure Index offset works correctly
                spanInt = new ReadOnlyTensorSpan<int>(p + 2, 2, [1, 2], default);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(1, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(-93, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);

                // Make sure we catch that there aren't enough elements in the array for the lengths
                Assert.Throws<ArgumentOutOfRangeException>(() =>
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

                // Make sure 2D array works with stride of 0 to loop over first 2 elements again
                spanInt = new ReadOnlyTensorSpan<int>(p, 4, [2, 2], [0, 1]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(91, spanInt[0, 0]);
                Assert.Equal(92, spanInt[0, 1]);
                Assert.Equal(91, spanInt[1, 0]);
                Assert.Equal(92, spanInt[1, 1]);

                // Make sure 2D array works with stride of 0 and initial offset to loop over last 2 elements again
                spanInt = new ReadOnlyTensorSpan<int>(p + 2, 2, [2, 2], [0, 1]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(-93, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);
                Assert.Equal(-93, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);

                // Make sure 2D array works with strides of all 0 and initial offset to loop over last element again
                spanInt = new ReadOnlyTensorSpan<int>(p + 3, 1, [2, 2], [0, 0]);
                Assert.Equal(2, spanInt.Rank);
                Assert.Equal(2, spanInt.Lengths[0]);
                Assert.Equal(2, spanInt.Lengths[1]);
                Assert.Equal(94, spanInt[0, 0]);
                Assert.Equal(94, spanInt[0, 1]);
                Assert.Equal(94, spanInt[1, 0]);
                Assert.Equal(94, spanInt[1, 1]);

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

                // Make sure you can't overlap elements using strides
                Assert.Throws<ArgumentException>(() =>
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
        public static void ReadOnlyTensorSpanLargeDimensionsTests()
        {
            int[] a = { 91, 92, -93, 94, 95, -96 };
            int[] results = new int[6];
            ReadOnlyTensorSpan<int> spanInt = a.AsTensorSpan([1, 1, 1, 1, 1, 6]);
            Assert.Equal(6, spanInt.Rank);

            Assert.Equal(6, spanInt.Lengths.Length);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(1, spanInt.Lengths[1]);
            Assert.Equal(1, spanInt.Lengths[2]);
            Assert.Equal(1, spanInt.Lengths[3]);
            Assert.Equal(1, spanInt.Lengths[4]);
            Assert.Equal(6, spanInt.Lengths[5]);
            Assert.Equal(6, spanInt.Strides.Length);
            Assert.Equal(0, spanInt.Strides[0]);
            Assert.Equal(0, spanInt.Strides[1]);
            Assert.Equal(0, spanInt.Strides[2]);
            Assert.Equal(0, spanInt.Strides[3]);
            Assert.Equal(0, spanInt.Strides[4]);
            Assert.Equal(1, spanInt.Strides[5]);
            Assert.Equal(91, spanInt[0, 0, 0, 0, 0, 0]);
            Assert.Equal(92, spanInt[0, 0, 0, 0, 0, 1]);
            Assert.Equal(-93, spanInt[0, 0, 0, 0, 0, 2]);
            Assert.Equal(94, spanInt[0, 0, 0, 0, 0, 3]);
            Assert.Equal(95, spanInt[0, 0, 0, 0, 0, 4]);
            Assert.Equal(-96, spanInt[0, 0, 0, 0, 0, 5]);
            spanInt.FlattenTo(results);
            Assert.Equal(a, results);

            a = [91, 92, -93, 94, 95, -96, -91, -92, 93, -94, -95, 96];
            results = new int[12];
            spanInt = a.AsTensorSpan([1, 2, 2, 1, 1, 3]);
            Assert.Equal(6, spanInt.Lengths.Length);
            Assert.Equal(1, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(2, spanInt.Lengths[2]);
            Assert.Equal(1, spanInt.Lengths[3]);
            Assert.Equal(1, spanInt.Lengths[4]);
            Assert.Equal(3, spanInt.Lengths[5]);
            Assert.Equal(6, spanInt.Strides.Length);
            Assert.Equal(0, spanInt.Strides[0]);
            Assert.Equal(6, spanInt.Strides[1]);
            Assert.Equal(3, spanInt.Strides[2]);
            Assert.Equal(0, spanInt.Strides[3]);
            Assert.Equal(0, spanInt.Strides[4]);
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
        }

        [Fact]
        public static void IntArrayAsReadOnlyTensorSpan()
        {
            int[] a = { 91, 92, -93, 94 };
            int[] results = new int[4];
            ReadOnlyTensorSpan<int> spanInt = a.AsTensorSpan([4]);
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

            a[0] = 91;
            a[1] = 92;
            a[2] = -93;
            a[3] = 94;
            spanInt = a.AsTensorSpan([2, 2]);
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
        }

        [Fact]
        public static void ReadOnlyTensorSpanCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            ReadOnlyTensorSpan<int> leftSpan = leftData.AsTensorSpan([3, 3]);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan([3, 3]);
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
                TensorSpan<int> leftSpan = leftData.AsTensorSpan([9]);
                TensorSpan<int> tensor = rightData.AsTensorSpan([rightData.Length]);
                leftSpan.CopyTo(tensor);
            }
            );

            leftData = [.. Enumerable.Range(0, 27)];
            rightData = [.. Enumerable.Range(0, 27)];
            leftSpan = leftData.AsTensorSpan([3, 3, 3]);
            rightSpan = rightData.AsTensorSpan([3, 3, 3]);
            leftSpan.CopyTo(rightSpan);

            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            Assert.Throws<ArgumentException>(() =>
            {
                var l = leftData.AsTensorSpan([3, 3, 3]);
                var r = new TensorSpan<int>();
                l.CopyTo(r);
            });
        }

        [Fact]
        public static void ReadOnlyTensorSpanTryCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            ReadOnlyTensorSpan<int> leftSpan = leftData.AsTensorSpan([3, 3]);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan([3, 3]);
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

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            rightData = new int[15];
            leftSpan = leftData.AsTensorSpan([9]);
            rightSpan = rightData.AsTensorSpan([15]);
            success = leftSpan.TryCopyTo(rightSpan);
            Assert.False(success);

            leftData = [.. Enumerable.Range(0, 27)];
            rightData = [.. Enumerable.Range(0, 27)];
            leftSpan = leftData.AsTensorSpan([3, 3, 3]);
            rightSpan = rightData.AsTensorSpan([3, 3, 3]);
            success = leftSpan.TryCopyTo(rightSpan);
            Assert.True(success);

            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            var l = leftData.AsTensorSpan([3, 3, 3]);
            var r = new TensorSpan<int>();
            success = l.TryCopyTo(r);
            Assert.False(success);

            success = new ReadOnlyTensorSpan<double>(new double[1]).TryCopyTo(Array.Empty<double>());
            Assert.False(success);
        }

        [Fact]
        public static void ReadOnlyTensorSpanSliceTest()
        {
            // Make sure slicing an empty TensorSpan works
            TensorSpan<int> emptyTensorSpan = new TensorSpan<int>(Array.Empty<int>()).Slice(new NRange[] { .. });
            Assert.Equal([0], emptyTensorSpan.Lengths);
            Assert.Equal(1, emptyTensorSpan.Rank);
            Assert.Equal(0, emptyTensorSpan.FlattenedLength);

            // Make sure slicing a multi-dimensional empty TensorSpan works
            int[,] empty2dArray = new int[2, 0];
            emptyTensorSpan = new TensorSpan<int>(empty2dArray);
            TensorSpan<int> slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { .., .. });
            Assert.Equal([2, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(2, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);

            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { 0..1, .. });
            Assert.Equal([1, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(2, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);

            // Make sure slicing a multi-dimensional empty TensorSpan works
            int[,,,] empty4dArray = new int[2, 5, 1, 0];
            emptyTensorSpan = new TensorSpan<int>(empty4dArray);
            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { .., .., .., .. });
            Assert.Equal([2, 5, 1, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(4, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);

            emptyTensorSpan = new TensorSpan<int>(empty4dArray);
            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { 0..1, .., .., .. });
            Assert.Equal([1, 5, 1, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(4, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);

            emptyTensorSpan = new TensorSpan<int>(empty4dArray);
            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { 0..1, 2..3, .., .. });
            Assert.Equal([1, 1, 1, 0], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(4, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);

            empty4dArray = new int[2, 0, 1, 5];
            emptyTensorSpan = new TensorSpan<int>(empty4dArray);
            slicedEmptyTensorSpan = emptyTensorSpan.Slice(new NRange[] { .., .., .., .. });
            Assert.Equal([2, 0, 1, 5], slicedEmptyTensorSpan.Lengths);
            Assert.Equal(4, slicedEmptyTensorSpan.Rank);
            Assert.Equal(0, slicedEmptyTensorSpan.FlattenedLength);

            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] results = new int[9];
            ReadOnlyTensorSpan<int> spanInt = a.AsTensorSpan([3, 3]);

            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsTensorSpan([2, 3]).Slice(0..1));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsTensorSpan([2, 3]).Slice(1..2));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsTensorSpan([2, 3]).Slice(0..1, 5..6));

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

            sp = spanInt.Slice(0..1, 0..1);
            Assert.Equal(1, sp[0, 0]);
            Assert.Throws<IndexOutOfRangeException>(() => a.AsTensorSpan([3, 3]).Slice(0..1, 0..1)[0, 1]);
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

            int[] numbers = [.. Enumerable.Range(0, 27)];
            spanInt = numbers.AsTensorSpan([3, 3, 3]);
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

            numbers = [.. Enumerable.Range(0, 16)];
            spanInt = numbers.AsTensorSpan([2, 2, 2, 2]);
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
        }

        [Fact]
        public static void LongArrayAsReadOnlyTensorSpan()
        {
            long[] b = { 91, -92, 93, 94, -95 };
            ReadOnlyTensorSpan<long> spanLong = b.AsTensorSpan([5]);
            Assert.Equal(91, spanLong[0]);
            Assert.Equal(-92, spanLong[1]);
            Assert.Equal(93, spanLong[2]);
            Assert.Equal(94, spanLong[3]);
            Assert.Equal(-95, spanLong[4]);
        }

        [Fact]
        public static void NullArrayAsReadOnlyTensorSpan()
        {
            int[] a = null;
            ReadOnlyTensorSpan<int> span = a.AsTensorSpan();
            ReadOnlyTensorSpan<int> d = default;
            Assert.True(span == d);
        }

        [Fact]
        public static void GetSpanTest()
        {
            ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);

            ReadOnlySpan<int> span = ReadOnlyTensorSpan.GetSpan([0, 0], 16);
            Assert.Equal(16, span.Length);
            Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], span);

            span = ReadOnlyTensorSpan.GetSpan([1, 1], 3);
            Assert.Equal(3, span.Length);
            Assert.Equal([5, 6, 7], span);

            span = ReadOnlyTensorSpan.GetSpan([3, 0], 4);
            Assert.Equal(4, span.Length);
            Assert.Equal([12, 13, 14, 15], span);

            span = ReadOnlyTensorSpan.GetSpan([0, 3], 1);
            Assert.Equal(1, span.Length);
            Assert.Equal([3], span);

            span = ReadOnlyTensorSpan.GetSpan([3, 3], 1);
            Assert.Equal(1, span.Length);
            Assert.Equal([15], span);
        }

        [Fact]
        public static void GetSpanThrowsForInvalidIndexesTest()
        {
            Assert.Throws<IndexOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([4, 0], 17);
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([0, 4], 17);
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([4, 4], 17);
            });
        }

        [Fact]
        public static void GetSpanThrowsForInvalidLengthsTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([0, 0], -1);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([0, 0], 17);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([1, 1], 4);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([3, 0], 5);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([0, 3], 2);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.GetSpan([3, 3], 2);
            });
        }

        [Fact]
        public static void TryGetSpanTest()
        {
            ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);

            Assert.True(ReadOnlyTensorSpan.TryGetSpan([0, 0], 16, out ReadOnlySpan<int> span));
            Assert.Equal(16, span.Length);
            Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], span);

            Assert.True(ReadOnlyTensorSpan.TryGetSpan([1, 1], 3, out span));
            Assert.Equal(3, span.Length);
            Assert.Equal([5, 6, 7], span);

            Assert.True(ReadOnlyTensorSpan.TryGetSpan([3, 0], 4, out span));
            Assert.Equal(4, span.Length);
            Assert.Equal([12, 13, 14, 15], span);

            Assert.True(ReadOnlyTensorSpan.TryGetSpan([0, 3], 1, out span));
            Assert.Equal(1, span.Length);
            Assert.Equal([3], span);

            Assert.True(ReadOnlyTensorSpan.TryGetSpan([3, 3], 1, out span));
            Assert.Equal(1, span.Length);
            Assert.Equal([15], span);
        }

        [Fact]
        public static void TryGetSpanThrowsForInvalidIndexesTest()
        {
            Assert.Throws<IndexOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.TryGetSpan([4, 0], 17, out _);
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.TryGetSpan([0, 4], 17, out _);
            });

            Assert.Throws<IndexOutOfRangeException>(() => {
                ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);
                _ = ReadOnlyTensorSpan.TryGetSpan([4, 4], 17, out _);
            });
        }

        [Fact]
        public static void TryGetSpanFailsForInvalidLengthsTest()
        {
            ReadOnlyTensorSpan<int> ReadOnlyTensorSpan = new ReadOnlyTensorSpan<int>(Enumerable.Range(0, 16).ToArray(), [4, 4]);

            Assert.False(ReadOnlyTensorSpan.TryGetSpan([0, 0], -1, out ReadOnlySpan<int> span));
            Assert.Equal(0, span.Length);

            Assert.False(ReadOnlyTensorSpan.TryGetSpan([0, 0], 17, out span));
            Assert.Equal(0, span.Length);

            Assert.False(ReadOnlyTensorSpan.TryGetSpan([1, 1], 4, out span));
            Assert.Equal(0, span.Length);

            Assert.False(ReadOnlyTensorSpan.TryGetSpan([3, 0], 5, out span));
            Assert.Equal(0, span.Length);

            Assert.False(ReadOnlyTensorSpan.TryGetSpan([0, 3], 2, out span));
            Assert.Equal(0, span.Length);

            Assert.False(ReadOnlyTensorSpan.TryGetSpan([3, 3], 2, out span));
            Assert.Equal(0, span.Length);
        }

        [Fact]
        public static void ToStringTest()
        {
            ReadOnlyTensorSpan<int> tensor = Tensor.Create<int>([1, 2, 3, 4, 5], lengths: [5]);
            string expected = "System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[5]";
            Assert.Equal(expected, tensor.ToString());

            tensor = Tensor.Create<int>([1, 2, 3, 4], lengths: [2, 2]);
            expected = "System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[2, 2]";
            Assert.Equal(expected, tensor.ToString());

            tensor = Tensor.Create<int>(Enumerable.Range(1, 27).ToArray(), lengths: [3, 3, 3]);
            expected = "System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[3, 3, 3]";
            Assert.Equal(expected, tensor.ToString());
        }

        [Fact]
        public static void ToStringAllDataTest()
        {
            ReadOnlyTensorSpan<int> tensor = Tensor.Create<int>([1, 2, 3, 4, 5], lengths: [5]);
            string expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[5] {
                  [1, 2, 3, 4, 5]
                }
                """;
            Assert.Equal(expected, tensor.ToString([5]));

            tensor = Tensor.Create<int>([1, 2, 3, 4], lengths: [2, 2]);
            expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[2, 2] {
                  [1, 2],
                  [3, 4]
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 2]));

            tensor = Tensor.Create<int>(Enumerable.Range(1, 27).ToArray(), lengths: [3, 3, 3]);
            expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[3, 3, 3] {
                  [
                    [1, 2, 3],
                    [4, 5, 6],
                    [7, 8, 9]
                  ],
                  [
                    [10, 11, 12],
                    [13, 14, 15],
                    [16, 17, 18]
                  ],
                  [
                    [19, 20, 21],
                    [22, 23, 24],
                    [25, 26, 27]
                  ]
                }
                """;
            Assert.Equal(expected, tensor.ToString([3, 3, 3]));
        }

        [Fact]
        public static void ToStringPartialDataTest()
        {
            ReadOnlyTensorSpan<int> tensor = Tensor.Create<int>([1, 2, 3, 4, 5], lengths: [5]);
            string expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[5] {
                  [1, 2, 3, ..]
                }
                """;
            Assert.Equal(expected, tensor.ToString([3]));

            tensor = Tensor.Create<int>([1, 2, 3, 4], lengths: [2, 2]);
            expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[2, 2] {
                  [1, ..],
                  [3, ..]
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 1]));

            tensor = Tensor.Create<int>(Enumerable.Range(1, 27).ToArray(), lengths: [3, 3, 3]);
            expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[3, 3, 3] {
                  [
                    [1, 2, ..],
                    [4, 5, ..],
                    ..
                  ],
                  [
                    [10, 11, ..],
                    [13, 14, ..],
                    ..
                  ],
                  ..
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 2, 2]));
        }

        [Fact]
        public static void ToStringZeroDataTest()
        {
            ReadOnlyTensorSpan<int> tensor = Tensor.Create<int>([1, 2, 3, 4, 5], lengths: [5]);
            string expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[5] {
                  [..]
                }
                """;
            Assert.Equal(expected, tensor.ToString([0]));

            tensor = Tensor.Create<int>([1, 2, 3, 4], lengths: [2, 2]);
            expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[2, 2] {
                  [..],
                  [..]
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 0]));

            tensor = Tensor.Create<int>(Enumerable.Range(1, 27).ToArray(), lengths: [3, 3, 3]);
            expected = """
                System.Numerics.Tensors.ReadOnlyTensorSpan<Int32>[3, 3, 3] {
                  [
                    ..
                  ],
                  [
                    ..
                  ],
                  ..
                }
                """;
            Assert.Equal(expected, tensor.ToString([2, 0, 2]));
        }
    }
}
