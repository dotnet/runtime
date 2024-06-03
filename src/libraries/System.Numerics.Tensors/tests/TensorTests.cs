// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class TensorTests
    {
        [Fact]
        public static void TensorSequenceEqualTests()
        {
            Tensor<int> t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            Tensor<int> t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            Tensor<bool> equal = Tensor.SequenceEqual(t0, t1);

            Assert.Equal([3], equal.Lengths.ToArray());
            Assert.True(equal[0]);
            Assert.True(equal[1]);
            Assert.True(equal[2]);

            t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(1, 3);
            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            equal = Tensor.SequenceEqual(t0, t1);

            Assert.Equal([1, 3], equal.Lengths.ToArray());
            Assert.True(equal[0, 0]);
            Assert.True(equal[0, 1]);
            Assert.True(equal[0, 2]);

            t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(1, 1, 3);
            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            equal = Tensor.SequenceEqual(t0, t1);

            Assert.Equal([1, 1, 3], equal.Lengths.ToArray());
            Assert.True(equal[0, 0, 0]);
            Assert.True(equal[0, 0, 1]);
            Assert.True(equal[0, 0, 2]);

            t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(1, 3);
            equal = Tensor.SequenceEqual(t0, t1);

            Assert.Equal([1, 3], equal.Lengths.ToArray());
            Assert.True(equal[0, 0]);
            Assert.True(equal[0, 1]);
            Assert.True(equal[0, 2]);

            t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(3, 1);
            equal = Tensor.SequenceEqual(t0, t1);

            Assert.Equal([3, 3], equal.Lengths.ToArray());
            Assert.True(equal[0, 0]);
            Assert.False(equal[0, 1]);
            Assert.False(equal[0, 2]);
            Assert.False(equal[1, 0]);
            Assert.True(equal[1, 1]);
            Assert.False(equal[1, 2]);
            Assert.False(equal[2, 0]);
            Assert.False(equal[2, 1]);
            Assert.True(equal[2, 2]);

            t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(1, 3);
            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(3, 1);
            equal = Tensor.SequenceEqual(t0, t1);

            Assert.Equal([3, 3], equal.Lengths.ToArray());
            Assert.True(equal[0, 0]);
            Assert.False(equal[0, 1]);
            Assert.False(equal[0, 2]);
            Assert.False(equal[1, 0]);
            Assert.True(equal[1, 1]);
            Assert.False(equal[1, 2]);
            Assert.False(equal[2, 0]);
            Assert.False(equal[2, 1]);
            Assert.True(equal[2, 2]);

            t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 4));
            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            Assert.Throws<Exception>(() => Tensor.SequenceEqual(t0, t1));
        }

        [Fact]
        public static void TensorMultiplyTests()
        {
            Tensor<int> t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            Tensor<int> t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(3, 1);
            Tensor<int> t2 = Tensor.Multiply(t0, t1);

            Assert.Equal([3,3], t2.Lengths.ToArray());
            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(0, t2[0, 1]);
            Assert.Equal(0, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(2, t2[2, 1]);
            Assert.Equal(4, t2[2, 2]);

            t2 = Tensor.Multiply(t1, t0);

            Assert.Equal([3, 3], t2.Lengths.ToArray());
            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(0, t2[0, 1]);
            Assert.Equal(0, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(2, t2[2, 1]);
            Assert.Equal(4, t2[2, 2]);

            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 9)).Reshape(3, 3);
            t2 = Tensor.Multiply(t0, t1);

            Assert.Equal([3, 3], t2.Lengths.ToArray());
            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(1, t2[0, 1]);
            Assert.Equal(4, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(4, t2[1, 1]);
            Assert.Equal(10, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(7, t2[2, 1]);
            Assert.Equal(16, t2[2, 2]);




        }

        [Fact]
        public static void TensorBroadcastTests()
        {
            Tensor<int> t0 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)), 1, 3, 1, 1, 1);
            Tensor<int> t1 = Tensor.Broadcast(t0, [1, 3, 1, 2, 1]);

            Assert.Equal([1, 3, 1, 2, 1], t1.Lengths.ToArray());

            Assert.Equal(0, t1[0, 0, 0, 0, 0]);
            Assert.Equal(0, t1[0, 0, 0, 1, 0]);
            Assert.Equal(1, t1[0, 1, 0, 0, 0]);
            Assert.Equal(1, t1[0, 1, 0, 1, 0]);
            Assert.Equal(2, t1[0, 2, 0, 0, 0]);
            Assert.Equal(2, t1[0, 2, 0, 1, 0]);

            t1 = Tensor.Broadcast(t0, [1, 3, 2, 1, 1]);
            Assert.Equal([1, 3, 2, 1, 1], t1.Lengths.ToArray());

            Assert.Equal(0, t1[0, 0, 0, 0, 0]);
            Assert.Equal(0, t1[0, 0, 1, 0, 0]);
            Assert.Equal(1, t1[0, 1, 0, 0, 0]);
            Assert.Equal(1, t1[0, 1, 1, 0, 0]);
            Assert.Equal(2, t1[0, 2, 0, 0, 0]);
            Assert.Equal(2, t1[0, 2, 1, 0, 0]);

            t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(1, 3);
            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(3, 1);
            var t2 = Tensor.Broadcast(t0, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths.ToArray());

            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(1, t2[0, 1]);
            Assert.Equal(2, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(1, t2[2, 1]);
            Assert.Equal(2, t2[2, 2]);

            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3)).Reshape(3, 1);
            t2 = Tensor.Broadcast(t1, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths.ToArray());

            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(0, t2[0, 1]);
            Assert.Equal(0, t2[0, 2]);
            Assert.Equal(1, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(1, t2[1, 2]);
            Assert.Equal(2, t2[2, 0]);
            Assert.Equal(2, t2[2, 1]);
            Assert.Equal(2, t2[2, 2]);

            var s1 = t2.AsTensorSpan();
            Assert.Equal(0, s1[0, 0]);
            Assert.Equal(0, s1[0, 1]);
            Assert.Equal(0, s1[0, 2]);
            Assert.Equal(1, s1[1, 0]);
            Assert.Equal(1, s1[1, 1]);
            Assert.Equal(1, s1[1, 2]);
            Assert.Equal(2, s1[2, 0]);
            Assert.Equal(2, s1[2, 1]);
            Assert.Equal(2, s1[2, 2]);

            var t3 = t2.Slice(0..1, ..);
            Assert.Equal([1, 3], t3.Lengths.ToArray());

            t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 3));
            t2 = Tensor.Broadcast(t1, [3, 3]);
            Assert.Equal([3, 3], t2.Lengths.ToArray());

            Assert.Equal(0, t2[0, 0]);
            Assert.Equal(1, t2[0, 1]);
            Assert.Equal(2, t2[0, 2]);
            Assert.Equal(0, t2[1, 0]);
            Assert.Equal(1, t2[1, 1]);
            Assert.Equal(2, t2[1, 2]);
            Assert.Equal(0, t2[2, 0]);
            Assert.Equal(1, t2[2, 1]);
            Assert.Equal(2, t2[2, 2]);
        }

        //// Needs internals visible
        //[Fact]
        //public static void TensorBroadcastToShapeCompatibleTests()
        //{
        //    Tensor<int> t0 = Tensor.Reshape(Tensor.FillRange(Enumerable.Range(0, 8)), 8);
        //    Tensor<int> t1 = Tensor.Reshape(Tensor.FillRange(Enumerable.Range(0, 8)), 1, 8);

        //    Assert.True(Tensor.AreShapesBroadcastToCompatible(t0.Lengths, t1.Lengths));
        //    Assert.True(Tensor.AreShapesBroadcastToCompatible(t0, t1));

        //    t1 = Tensor.Reshape(Tensor.FillRange(Enumerable.Range(0, 8)), 2, 4);
        //    Assert.False(Tensor.AreShapesBroadcastToCompatible(t0, t1));

        //    t0 = Tensor.FillRange(Enumerable.Range(0, 3));

        //    Assert.False(Tensor.AreShapesBroadcastToCompatible(t0.Lengths, [1,3,1,1,1]));

        //}

        [Fact]
        public static void TensorResizeTests()
        {
            Tensor<int> t0 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(0, 8)), 2, 2, 2);
            var t1 = Tensor.Resize(t0, [1]);
            Assert.Equal([1], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0]);

            t1 = Tensor.Resize(t0, [1, 1]);
            Assert.Equal([1, 1], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0, 0]);

            t1 = Tensor.Resize(t0, [6]);
            Assert.Equal([6], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0]);
            Assert.Equal(1, t1[1]);
            Assert.Equal(2, t1[2]);
            Assert.Equal(3, t1[3]);
            Assert.Equal(4, t1[4]);
            Assert.Equal(5, t1[5]);

            t1 = Tensor.Resize(t0, [10]);
            Assert.Equal([10], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0]);
            Assert.Equal(1, t1[1]);
            Assert.Equal(2, t1[2]);
            Assert.Equal(3, t1[3]);
            Assert.Equal(4, t1[4]);
            Assert.Equal(5, t1[5]);
            Assert.Equal(6, t1[6]);
            Assert.Equal(7, t1[7]);
            Assert.Equal(0, t1[8]);
            Assert.Equal(0, t1[9]);

            t1 = Tensor.Resize(t0, [2, 5]);
            Assert.Equal([2, 5], t1.Lengths.ToArray());
            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(1, t1[0, 1]);
            Assert.Equal(2, t1[0, 2]);
            Assert.Equal(3, t1[0, 3]);
            Assert.Equal(4, t1[0, 4]);
            Assert.Equal(5, t1[1, 0]);
            Assert.Equal(6, t1[1, 1]);
            Assert.Equal(7, t1[1, 2]);
            Assert.Equal(0, t1[1, 3]);
            Assert.Equal(0, t1[1, 4]);
        }

        [Fact]
        public static void TensorSplitTests()
        {
            Tensor<int> t0 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(0, 8)), 2, 2, 2);
            var t1 = Tensor.Split(t0, 2, 0);
            Assert.Equal([1, 2, 2], t1[0].Lengths.ToArray());
            Assert.Equal([1, 2, 2], t1[1].Lengths.ToArray());
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(1, t1[0][0, 0, 1]);
            Assert.Equal(2, t1[0][0, 1, 0]);
            Assert.Equal(3, t1[0][0, 1, 1]);
            Assert.Equal(4, t1[1][0, 0, 0]);
            Assert.Equal(5, t1[1][0, 0, 1]);
            Assert.Equal(6, t1[1][0, 1, 0]);
            Assert.Equal(7, t1[1][0, 1, 1]);

            t1 = Tensor.Split(t0, 2, 1);
            Assert.Equal([2, 1, 2], t1[0].Lengths.ToArray());
            Assert.Equal([2, 1, 2], t1[1].Lengths.ToArray());
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(1, t1[0][0, 0, 1]);
            Assert.Equal(4, t1[0][1, 0, 0]);
            Assert.Equal(5, t1[0][1, 0, 1]);
            Assert.Equal(2, t1[1][0, 0, 0]);
            Assert.Equal(3, t1[1][0, 0, 1]);
            Assert.Equal(6, t1[1][1, 0, 0]);
            Assert.Equal(7, t1[1][1, 0, 1]);

            t1 = Tensor.Split(t0, 2, 2);
            Assert.Equal([2, 2, 1], t1[0].Lengths.ToArray());
            Assert.Equal([2, 2, 1], t1[1].Lengths.ToArray());
            Assert.Equal(0, t1[0][0, 0, 0]);
            Assert.Equal(2, t1[0][0, 1, 0]);
            Assert.Equal(4, t1[0][1, 0, 0]);
            Assert.Equal(6, t1[0][1, 1, 0]);
            Assert.Equal(1, t1[1][0, 0, 0]);
            Assert.Equal(3, t1[1][0, 1, 0]);
            Assert.Equal(5, t1[1][1, 0, 0]);
            Assert.Equal(7, t1[1][1, 1, 0]);
        }

        [Fact]
        public static void TensorReverseTests()
        {
            Tensor<int> t0 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(0, 8)), 2, 2, 2);
            var t1 = Tensor.Reverse(t0);
            Assert.Equal(7, t1[0, 0, 0]);
            Assert.Equal(6, t1[0, 0, 1]);
            Assert.Equal(5, t1[0, 1, 0]);
            Assert.Equal(4, t1[0, 1, 1]);
            Assert.Equal(3, t1[1, 0, 0]);
            Assert.Equal(2, t1[1, 0, 1]);
            Assert.Equal(1, t1[1, 1, 0]);
            Assert.Equal(0, t1[1, 1, 1]);

            t1 = Tensor.Reverse(t0, 0);
            Assert.Equal(4, t1[0, 0, 0]);
            Assert.Equal(5, t1[0, 0, 1]);
            Assert.Equal(6, t1[0, 1, 0]);
            Assert.Equal(7, t1[0, 1, 1]);
            Assert.Equal(0, t1[1, 0, 0]);
            Assert.Equal(1, t1[1, 0, 1]);
            Assert.Equal(2, t1[1, 1, 0]);
            Assert.Equal(3, t1[1, 1, 1]);

            t1 = Tensor.Reverse(t0, 1);
            Assert.Equal(2, t1[0, 0, 0]);
            Assert.Equal(3, t1[0, 0, 1]);
            Assert.Equal(0, t1[0, 1, 0]);
            Assert.Equal(1, t1[0, 1, 1]);
            Assert.Equal(6, t1[1, 0, 0]);
            Assert.Equal(7, t1[1, 0, 1]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(5, t1[1, 1, 1]);

            t1 = Tensor.Reverse(t0, 2);
            Assert.Equal(1, t1[0, 0, 0]);
            Assert.Equal(0, t1[0, 0, 1]);
            Assert.Equal(3, t1[0, 1, 0]);
            Assert.Equal(2, t1[0, 1, 1]);
            Assert.Equal(5, t1[1, 0, 0]);
            Assert.Equal(4, t1[1, 0, 1]);
            Assert.Equal(7, t1[1, 1, 0]);
            Assert.Equal(6, t1[1, 1, 1]);
        }

        [Fact]
        public static void TensorSetSliceTests()
        {
            Tensor<int> t0 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(0, 10)), 2, 5);
            Tensor<int> t1 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(10, 10)), 2, 5);
            Tensor.SetSlice(t0, t1);

            Assert.Equal(10, t0[0, 0]);
            Assert.Equal(11, t0[0, 1]);
            Assert.Equal(12, t0[0, 2]);
            Assert.Equal(13, t0[0, 3]);
            Assert.Equal(14, t0[0, 4]);
            Assert.Equal(15, t0[1, 0]);
            Assert.Equal(16, t0[1, 1]);
            Assert.Equal(17, t0[1, 2]);
            Assert.Equal(18, t0[1, 3]);
            Assert.Equal(19, t0[1, 4]);

            t0 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(0, 10)), 2, 5);
            t1 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(10, 5)), 1, 5);
            t0.SetSlice(t1, 0..1, ..);

            Assert.Equal(10, t0[0, 0]);
            Assert.Equal(11, t0[0, 1]);
            Assert.Equal(12, t0[0, 2]);
            Assert.Equal(13, t0[0, 3]);
            Assert.Equal(14, t0[0, 4]);
            Assert.Equal(5, t0[1, 0]);
            Assert.Equal(6, t0[1, 1]);
            Assert.Equal(7, t0[1, 2]);
            Assert.Equal(8, t0[1, 3]);
            Assert.Equal(9, t0[1, 4]);

            t0 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(0, 10)), 2, 5);
            t1 = Tensor.Reshape(Tensor.CreateFromEnumerable(Enumerable.Range(10, 5)), 1, 5);
            Tensor.SetSlice(t0, t1, 1..2, ..);

            Assert.Equal(0, t0[0, 0]);
            Assert.Equal(1, t0[0, 1]);
            Assert.Equal(2, t0[0, 2]);
            Assert.Equal(3, t0[0, 3]);
            Assert.Equal(4, t0[0, 4]);
            Assert.Equal(10, t0[1, 0]);
            Assert.Equal(11, t0[1, 1]);
            Assert.Equal(12, t0[1, 2]);
            Assert.Equal(13, t0[1, 3]);
            Assert.Equal(14, t0[1, 4]);
        }
        [Fact]
        public static void TensorStackTests()
        {
            Tensor<int> t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 10)).Reshape(2, 5);
            Tensor<int> t1 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 10)).Reshape(2, 5);

            var resultTensor = Tensor.Stack([t0, t1]);
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(5, resultTensor.Lengths[2]);

            Assert.Equal(0, resultTensor[0, 0, 0]);
            Assert.Equal(1, resultTensor[0, 0, 1]);
            Assert.Equal(2, resultTensor[0, 0, 2]);
            Assert.Equal(3, resultTensor[0, 0, 3]);
            Assert.Equal(4, resultTensor[0, 0, 4]);
            Assert.Equal(5, resultTensor[0, 1, 0]);
            Assert.Equal(6, resultTensor[0, 1, 1]);
            Assert.Equal(7, resultTensor[0, 1, 2]);
            Assert.Equal(8, resultTensor[0, 1, 3]);
            Assert.Equal(9, resultTensor[0, 1, 4]);
            Assert.Equal(0, resultTensor[1, 0, 0]);
            Assert.Equal(1, resultTensor[1, 0, 1]);
            Assert.Equal(2, resultTensor[1, 0, 2]);
            Assert.Equal(3, resultTensor[1, 0, 3]);
            Assert.Equal(4, resultTensor[1, 0, 4]);
            Assert.Equal(5, resultTensor[1, 1, 0]);
            Assert.Equal(6, resultTensor[1, 1, 1]);
            Assert.Equal(7, resultTensor[1, 1, 2]);
            Assert.Equal(8, resultTensor[1, 1, 3]);
            Assert.Equal(9, resultTensor[1, 1, 4]);

            resultTensor = Tensor.Stack([t0, t1], axis:1);
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(5, resultTensor.Lengths[2]);

            Assert.Equal(0, resultTensor[0, 0, 0]);
            Assert.Equal(1, resultTensor[0, 0, 1]);
            Assert.Equal(2, resultTensor[0, 0, 2]);
            Assert.Equal(3, resultTensor[0, 0, 3]);
            Assert.Equal(4, resultTensor[0, 0, 4]);
            Assert.Equal(0, resultTensor[0, 1, 0]);
            Assert.Equal(1, resultTensor[0, 1, 1]);
            Assert.Equal(2, resultTensor[0, 1, 2]);
            Assert.Equal(3, resultTensor[0, 1, 3]);
            Assert.Equal(4, resultTensor[0, 1, 4]);
            Assert.Equal(5, resultTensor[1, 0, 0]);
            Assert.Equal(6, resultTensor[1, 0, 1]);
            Assert.Equal(7, resultTensor[1, 0, 2]);
            Assert.Equal(8, resultTensor[1, 0, 3]);
            Assert.Equal(9, resultTensor[1, 0, 4]);
            Assert.Equal(5, resultTensor[1, 1, 0]);
            Assert.Equal(6, resultTensor[1, 1, 1]);
            Assert.Equal(7, resultTensor[1, 1, 2]);
            Assert.Equal(8, resultTensor[1, 1, 3]);
            Assert.Equal(9, resultTensor[1, 1, 4]);

            resultTensor = Tensor.Stack([t0, t1], axis: 2);
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(5, resultTensor.Lengths[1]);
            Assert.Equal(2, resultTensor.Lengths[2]);

            Assert.Equal(0, resultTensor[0, 0, 0]);
            Assert.Equal(0, resultTensor[0, 0, 1]);
            Assert.Equal(1, resultTensor[0, 1, 0]);
            Assert.Equal(1, resultTensor[0, 1, 1]);
            Assert.Equal(2, resultTensor[0, 2, 0]);
            Assert.Equal(2, resultTensor[0, 2, 1]);
            Assert.Equal(3, resultTensor[0, 3, 0]);
            Assert.Equal(3, resultTensor[0, 3, 1]);
            Assert.Equal(4, resultTensor[0, 4, 0]);
            Assert.Equal(4, resultTensor[0, 4, 1]);
            Assert.Equal(5, resultTensor[1, 0, 0]);
            Assert.Equal(5, resultTensor[1, 0, 1]);
            Assert.Equal(6, resultTensor[1, 1, 0]);
            Assert.Equal(6, resultTensor[1, 1, 1]);
            Assert.Equal(7, resultTensor[1, 2, 0]);
            Assert.Equal(7, resultTensor[1, 2, 1]);
            Assert.Equal(8, resultTensor[1, 3, 0]);
            Assert.Equal(8, resultTensor[1, 3, 1]);
            Assert.Equal(9, resultTensor[1, 4, 0]);
            Assert.Equal(9, resultTensor[1, 4, 1]);
        }

        [Fact]
        public static void TensorStdDevTests()
        {
            Tensor<float> t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 4).Select(i => (float)i))).Reshape(2, 2);
            //Tensor.Sum(t0)

            Assert.Equal(StdDev([0, 1, 2, 3]), Tensor.StdDev(t0), .1);


        }

        public static float StdDev(float[] values)
        {
            float mean = Mean(values);
            float sum = 0;
            for(int i = 0; i < values.Length; i++)
            {
                sum += MathF.Pow(values[i] - mean, 2);
            }
            return sum / values.Length;
        }

        [Fact]
        public static void TensorMeanTests()
        {
            Tensor<float> t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 4).Select(i => (float)i))).Reshape(2, 2);

            Assert.Equal(Mean([0, 1, 2, 3]), Tensor.Mean(t0), .1);
        }

        public static float Mean(float[] values)
        {
            float sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }
            return sum/values.Length;
        }

        [Fact]
        public static void TensorConcatenateTests()
        {
            Tensor<float> t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 4).Select(i => (float)i))).Reshape(2, 2);
            Tensor<float> t1 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 4).Select(i => (float)i))).Reshape(2, 2);
            var resultTensor = Tensor.Concatenate([t0, t1]);

            Assert.Equal(2, resultTensor.Rank);
            Assert.Equal(4, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(0, resultTensor[0, 0]);
            Assert.Equal(1, resultTensor[0, 1]);
            Assert.Equal(2, resultTensor[1, 0]);
            Assert.Equal(3, resultTensor[1, 1]);
            Assert.Equal(0, resultTensor[2, 0]);
            Assert.Equal(1, resultTensor[2, 1]);
            Assert.Equal(2, resultTensor[3, 0]);
            Assert.Equal(3, resultTensor[3, 1]);

            resultTensor = Tensor.Concatenate([t0, t1], 1);
            Assert.Equal(2, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(4, resultTensor.Lengths[1]);
            Assert.Equal(0, resultTensor[0, 0]);
            Assert.Equal(1, resultTensor[0, 1]);
            Assert.Equal(0, resultTensor[0, 2]);
            Assert.Equal(1, resultTensor[0, 3]);
            Assert.Equal(2, resultTensor[1, 0]);
            Assert.Equal(3, resultTensor[1, 1]);
            Assert.Equal(2, resultTensor[1, 2]);
            Assert.Equal(3, resultTensor[1, 3]);

            resultTensor = Tensor.Concatenate([t0, t1], -1);
            Assert.Equal(1, resultTensor.Rank);
            Assert.Equal(8, resultTensor.Lengths[0]);
            Assert.Equal(0, resultTensor[0]);
            Assert.Equal(1, resultTensor[1]);
            Assert.Equal(2, resultTensor[2]);
            Assert.Equal(3, resultTensor[3]);
            Assert.Equal(0, resultTensor[4]);
            Assert.Equal(1, resultTensor[5]);
            Assert.Equal(2, resultTensor[6]);
            Assert.Equal(3, resultTensor[7]);

            Tensor<float> t2 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 4).Select(i => (float)i))).Reshape(2, 2);
            resultTensor = Tensor.Concatenate([t0, t1, t2]);

            Assert.Equal(2, resultTensor.Rank);
            Assert.Equal(6, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(0, resultTensor[0, 0]);
            Assert.Equal(1, resultTensor[0, 1]);
            Assert.Equal(2, resultTensor[1, 0]);
            Assert.Equal(3, resultTensor[1, 1]);
            Assert.Equal(0, resultTensor[2, 0]);
            Assert.Equal(1, resultTensor[2, 1]);
            Assert.Equal(2, resultTensor[3, 0]);
            Assert.Equal(3, resultTensor[3, 1]);
            Assert.Equal(0, resultTensor[4, 0]);
            Assert.Equal(1, resultTensor[4, 1]);
            Assert.Equal(2, resultTensor[5, 0]);
            Assert.Equal(3, resultTensor[5, 1]);

            resultTensor = Tensor.Concatenate([t0, t1, t2], -1);

            Assert.Equal(1, resultTensor.Rank);
            Assert.Equal(12, resultTensor.Lengths[0]);
            Assert.Equal(0, resultTensor[0]);
            Assert.Equal(1, resultTensor[1]);
            Assert.Equal(2, resultTensor[2]);
            Assert.Equal(3, resultTensor[3]);
            Assert.Equal(0, resultTensor[4]);
            Assert.Equal(1, resultTensor[5]);
            Assert.Equal(2, resultTensor[6]);
            Assert.Equal(3, resultTensor[7]);
            Assert.Equal(0, resultTensor[8]);
            Assert.Equal(1, resultTensor[9]);
            Assert.Equal(2, resultTensor[10]);
            Assert.Equal(3, resultTensor[11]);

            resultTensor = Tensor.Concatenate([t0, t1, t2], 1);

            Assert.Equal(2, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(6, resultTensor.Lengths[1]);
            Assert.Equal(0, resultTensor[0, 0]);
            Assert.Equal(1, resultTensor[0, 1]);
            Assert.Equal(0, resultTensor[0, 2]);
            Assert.Equal(1, resultTensor[0, 3]);
            Assert.Equal(0, resultTensor[0, 4]);
            Assert.Equal(1, resultTensor[0, 5]);
            Assert.Equal(2, resultTensor[1, 0]);
            Assert.Equal(3, resultTensor[1, 1]);
            Assert.Equal(2, resultTensor[1, 2]);
            Assert.Equal(3, resultTensor[1, 3]);
            Assert.Equal(2, resultTensor[1, 4]);
            Assert.Equal(3, resultTensor[1, 5]);

            t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(2, 3, 2);
            t1 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(2, 3, 2);
            t2 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 8).Select(i => (float)i))).Reshape(2, 2, 2);
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2]));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 2));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 5));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], -2));
            resultTensor = Tensor.Concatenate([t0, t1, t2], -1);
            float[] result = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7];
            Assert.Equal(1, resultTensor.Rank);
            Assert.Equal(32, resultTensor.Lengths[0]);
            Assert.Equal(result, resultTensor.ToArray());

            resultTensor = Tensor.Concatenate([t0, t1, t2], 1);
            result = [0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 6, 7, 8, 9, 10, 11, 6, 7, 8, 9, 10, 11, 4, 5, 6, 7];
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(8, resultTensor.Lengths[1]);
            Assert.Equal(2, resultTensor.Lengths[2]);
            Assert.Equal(result, resultTensor.ToArray());
            nint[] indices = new nint[resultTensor.Rank];
            for(int i  = 0; i < result.Length; i++)
            {
                Assert.Equal(result[i], resultTensor[indices]);
                Helpers.AdjustIndices(resultTensor.Rank - 1, 1, ref indices, resultTensor.Lengths);
            }

            t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(2, 2, 3);
            t1 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(2, 2, 3);
            t2 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 8).Select(i => (float)i))).Reshape(2, 2, 2);
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 0));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 1));
            resultTensor = Tensor.Concatenate([t0, t1, t2], 2);
            result = [0, 1, 2, 0, 1, 2, 0, 1, 3, 4, 5, 3, 4, 5, 2, 3, 6, 7, 8, 6, 7, 8, 4, 5, 9, 10, 11, 9, 10, 11, 6, 7];
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(2, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(8, resultTensor.Lengths[2]);
            Assert.Equal(result, resultTensor.ToArray());
            indices = new nint[resultTensor.Rank];
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(result[i], resultTensor[indices]);
                Helpers.AdjustIndices(resultTensor.Rank - 1, 1, ref indices, resultTensor.Lengths);
            }

            t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(3, 2, 2);
            t1 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(3, 2, 2);
            t2 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 8).Select(i => (float)i))).Reshape(2, 2, 2);
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 1));
            Assert.Throws<ArgumentException>(() => Tensor.Concatenate([t0, t1, t2], 2));
            resultTensor = Tensor.Concatenate([t0, t1, t2]);
            result = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4, 5, 6, 7];
            Assert.Equal(3, resultTensor.Rank);
            Assert.Equal(8, resultTensor.Lengths[0]);
            Assert.Equal(2, resultTensor.Lengths[1]);
            Assert.Equal(2, resultTensor.Lengths[2]);
            Assert.Equal(result, resultTensor.ToArray());
            indices = new nint[resultTensor.Rank];
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(result[i], resultTensor[indices]);
                Helpers.AdjustIndices(resultTensor.Rank - 1, 1, ref indices, resultTensor.Lengths);
            }
        }

        [Fact]
        public static void TensorTransposeTests()
        {
            Tensor<float> t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 4).Select(i => (float)i))).Reshape(2, 2);
            var t1 = Tensor.Permute(t0);

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(2, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(3, t1[1, 1]);

            t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 6).Select(i => (float)i))).Reshape(2, 3);
            t1 = Tensor.Permute(t0);

            Assert.Equal(3, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(3, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(4, t1[1, 1]);
            Assert.Equal(2, t1[2, 0]);
            Assert.Equal(5, t1[2, 1]);

            t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 6).Select(i => (float)i))).Reshape(1, 2, 3);
            t1 = Tensor.Permute(t0);

            Assert.Equal(3, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(1, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(3, t1[0, 1, 0]);
            Assert.Equal(1, t1[1, 0, 0]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(2, t1[2, 0, 0]);
            Assert.Equal(5, t1[2, 1, 0]);

            t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(2, 2, 3);
            t1 = Tensor.Permute(t0);

            Assert.Equal(3, t1.Lengths[0]);
            Assert.Equal(2, t1.Lengths[1]);
            Assert.Equal(2, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(6, t1[0, 0, 1]);
            Assert.Equal(3, t1[0, 1, 0]);
            Assert.Equal(9, t1[0, 1, 1]);
            Assert.Equal(1, t1[1, 0, 0]);
            Assert.Equal(7, t1[1, 0, 1]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(10, t1[1, 1, 1]);
            Assert.Equal(2, t1[2, 0, 0]);
            Assert.Equal(8, t1[2, 0, 1]);
            Assert.Equal(5, t1[2, 1, 0]);
            Assert.Equal(11, t1[2, 1, 1]);

            t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(2, 2, 3);
            t1 = Tensor.Permute(t0, 1, 2, 0);

            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(3, t1.Lengths[1]);
            Assert.Equal(2, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(6, t1[0, 0, 1]);
            Assert.Equal(1, t1[0, 1, 0]);
            Assert.Equal(7, t1[0, 1, 1]);
            Assert.Equal(2, t1[0, 2, 0]);
            Assert.Equal(8, t1[0, 2, 1]);
            Assert.Equal(3, t1[1, 0, 0]);
            Assert.Equal(9, t1[1, 0, 1]);
            Assert.Equal(4, t1[1, 1, 0]);
            Assert.Equal(10, t1[1, 1, 1]);
            Assert.Equal(5, t1[1, 2, 0]);
            Assert.Equal(11, t1[1, 2, 1]);
        }

        [Fact]
        public static void TensorPermuteTests()
        {
            Tensor<float> t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 4).Select(i => (float)i))).Reshape(2, 2);
            var t1 = Tensor.Transpose(t0);

            Assert.Equal(0, t1[0, 0]);
            Assert.Equal(2, t1[0, 1]);
            Assert.Equal(1, t1[1, 0]);
            Assert.Equal(3, t1[1, 1]);

            t0 = Tensor.CreateFromEnumerable<float>((Enumerable.Range(0, 12).Select(i => (float)i))).Reshape(2, 2, 3);
            t1 = Tensor.Transpose(t0);

            Assert.Equal(2, t1.Lengths[0]);
            Assert.Equal(3, t1.Lengths[1]);
            Assert.Equal(2, t1.Lengths[2]);
            Assert.Equal(0, t1[0, 0, 0]);
            Assert.Equal(3, t1[0, 0, 1]);
            Assert.Equal(1, t1[0, 1, 0]);
            Assert.Equal(4, t1[0, 1, 1]);
            Assert.Equal(2, t1[0, 2, 0]);
            Assert.Equal(5, t1[0, 2, 1]);
            Assert.Equal(6, t1[1, 0, 0]);
            Assert.Equal(9, t1[1, 0, 1]);
            Assert.Equal(7, t1[1, 1, 0]);
            Assert.Equal(10, t1[1, 1, 1]);
            Assert.Equal(8, t1[1, 2, 0]);
            Assert.Equal(11, t1[1, 2, 1]);
        }

        [Fact]
        public static void IntArrayAsTensor()
        {
            int[] a = [91, 92, -93, 94];
            TensorSpan<int> spanInt = a.AsTensorSpan(4);
            nint[] dims = [4];
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            spanInt.CopyTo(tensor);
            Assert.Equal(1, tensor.Rank);

            Assert.Equal(1, tensor.Lengths.Length);
            Assert.Equal(4, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Strides.Length);
            Assert.Equal(1, tensor.Strides[0]);
            Assert.Equal(91, tensor[0]);
            Assert.Equal(92, tensor[1]);
            Assert.Equal(-93, tensor[2]);
            Assert.Equal(94, tensor[3]);
            Assert.Equal(a, tensor.ToArray());
            tensor[0] = 100;
            tensor[1] = 101;
            tensor[2] = -102;
            tensor[3] = 103;

            Assert.Equal(100, tensor[0]);
            Assert.Equal(101, tensor[1]);
            Assert.Equal(-102, tensor[2]);
            Assert.Equal(103, tensor[3]);

            a[0] = 91;
            a[1] = 92;
            a[2] = -93;
            a[3] = 94;
            spanInt = a.AsTensorSpan(2, 2);
            dims = [2, 2];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            spanInt.CopyTo(tensor);
            Assert.Equal(a, tensor.ToArray());
            Assert.Equal(2, tensor.Rank);
            //Assert.Equal(4, spanInt.FlattenedLength);
            Assert.Equal(2, tensor.Lengths.Length);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(2, tensor.Strides.Length);
            Assert.Equal(2, tensor.Strides[0]);
            Assert.Equal(1, tensor.Strides[1]);
            Assert.Equal(91, tensor[0, 0]);
            Assert.Equal(92, tensor[0, 1]);
            Assert.Equal(-93, tensor[1, 0]);
            Assert.Equal(94, tensor[1, 1]);

            tensor[0, 0] = 100;
            tensor[0, 1] = 101;
            tensor[1, 0] = -102;
            tensor[1, 1] = 103;

            Assert.Equal(100, tensor[0, 0]);
            Assert.Equal(101, tensor[0, 1]);
            Assert.Equal(-102, tensor[1, 0]);
            Assert.Equal(103, tensor[1, 1]);
        }

        [Fact]
        public static void TensorFillTest()
        {
            nint[] dims = [3, 3];
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            var enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            tensor.Fill(int.MinValue);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(int.MinValue, enumerator.Current);
            }

            tensor.Fill(int.MaxValue);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(int.MaxValue, enumerator.Current);
            }

            dims = [9];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 3, 3];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 2, 2];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [2, 2, 2, 2];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            dims = [3, 2, 2, 2];
            tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            tensor.Fill(-1);
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }
        }

        [Fact]
        public static void TensorClearTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            TensorSpan<int> spanInt = a.AsTensorSpan(3, 3);
            var tensor = Tensor.CreateUninitialized<int>([3, 3], false);
            spanInt.CopyTo(tensor);
            var slice = tensor.Slice(0..2, 0..2);
            slice.Clear();
            Assert.Equal(0, slice[0, 0]);
            Assert.Equal(0, slice[0, 1]);
            Assert.Equal(0, slice[1, 0]);
            Assert.Equal(0, slice[1, 1]);

            // Since Tensor.Slice does a copy the original tensor shouldn't be modified
            Assert.Equal(1, tensor[0, 0]);
            Assert.Equal(2, tensor[0, 1]);
            Assert.Equal(3, tensor[0, 2]);
            Assert.Equal(4, tensor[1, 0]);
            Assert.Equal(5, tensor[1, 1]);
            Assert.Equal(6, tensor[1, 2]);
            Assert.Equal(7, tensor[2, 0]);
            Assert.Equal(8, tensor[2, 1]);
            Assert.Equal(9, tensor[2, 2]);


            tensor.Clear();
            var enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            spanInt = a.AsTensorSpan(9);
            tensor = Tensor.CreateUninitialized<int>([9], false);
            spanInt.CopyTo(tensor);
            slice = tensor.Slice(0..1);
            slice.Clear();
            Assert.Equal(0, slice[0]);

            // Since Tensor.Slice does a copy the original tensor shouldn't be modified
            Assert.Equal(1, tensor[0]);
            Assert.Equal(2, tensor[1]);
            Assert.Equal(3, tensor[2]);
            Assert.Equal(4, tensor[3]);
            Assert.Equal(5, tensor[4]);
            Assert.Equal(6, tensor[5]);
            Assert.Equal(7, tensor[6]);
            Assert.Equal(8, tensor[7]);
            Assert.Equal(9, tensor[8]);


            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 27)];
            spanInt = a.AsTensorSpan(3, 3, 3);
            tensor = Tensor.CreateUninitialized<int>([3, 3, 3], false);
            spanInt.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 12)];
            spanInt = a.AsTensorSpan(3, 2, 2);
            tensor = Tensor.CreateUninitialized<int>([3, 2, 2], false);
            spanInt.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 16)];
            spanInt = a.AsTensorSpan(2, 2, 2, 2);
            tensor = Tensor.CreateUninitialized<int>([2, 2, 2, 2], false);
            spanInt.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 24)];
            spanInt = a.AsTensorSpan(3, 2, 2, 2);
            tensor = Tensor.CreateUninitialized<int>([3, 2, 2, 2], false);
            spanInt.CopyTo(tensor);
            tensor.Clear();
            enumerator = tensor.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            // Make sure clearing a slice of a SPan doesn't clear the whole thing.
            a = [.. Enumerable.Range(0, 9)];
            spanInt = a.AsTensorSpan(3, 3);
            var spanSlice = spanInt.Slice(0..1, 0..3);
            spanSlice.Clear();
            var spanEnumerator = spanSlice.GetEnumerator();
            while (spanEnumerator.MoveNext())
            {
                Assert.Equal(0, spanEnumerator.Current);
            }

            Assert.Equal(0, spanInt[0, 0]);
            Assert.Equal(0, spanInt[0, 1]);
            Assert.Equal(0, spanInt[0, 2]);
            Assert.Equal(3, spanInt[1, 0]);
            Assert.Equal(4, spanInt[1, 1]);
            Assert.Equal(5, spanInt[1, 2]);
            Assert.Equal(6, spanInt[2, 0]);
            Assert.Equal(7, spanInt[2, 1]);
            Assert.Equal(8, spanInt[2, 2]);

            // Make sure clearing a slice from the middle of a SPan doesn't clear the whole thing.
            a = [.. Enumerable.Range(0, 9)];
            spanInt = a.AsTensorSpan(3, 3);
            spanSlice = spanInt.Slice(1..2, 0..3);
            spanSlice.Clear();
            spanEnumerator = spanSlice.GetEnumerator();
            while (spanEnumerator.MoveNext())
            {
                Assert.Equal(0, spanEnumerator.Current);
            }

            Assert.Equal(0, spanInt[0, 0]);
            Assert.Equal(1, spanInt[0, 1]);
            Assert.Equal(2, spanInt[0, 2]);
            Assert.Equal(0, spanInt[1, 0]);
            Assert.Equal(0, spanInt[1, 1]);
            Assert.Equal(0, spanInt[1, 2]);
            Assert.Equal(6, spanInt[2, 0]);
            Assert.Equal(7, spanInt[2, 1]);
            Assert.Equal(8, spanInt[2, 2]);

            // Make sure clearing a slice from the end of a SPan doesn't clear the whole thing.
            a = [.. Enumerable.Range(0, 9)];
            spanInt = a.AsTensorSpan(3, 3);
            spanSlice = spanInt.Slice(2..3, 0..3);
            spanSlice.Clear();
            spanEnumerator = spanSlice.GetEnumerator();
            while (spanEnumerator.MoveNext())
            {
                Assert.Equal(0, spanEnumerator.Current);
            }

            Assert.Equal(0, spanInt[0, 0]);
            Assert.Equal(1, spanInt[0, 1]);
            Assert.Equal(2, spanInt[0, 2]);
            Assert.Equal(3, spanInt[1, 0]);
            Assert.Equal(4, spanInt[1, 1]);
            Assert.Equal(5, spanInt[1, 2]);
            Assert.Equal(0, spanInt[2, 0]);
            Assert.Equal(0, spanInt[2, 1]);
            Assert.Equal(0, spanInt[2, 2]);

            // Make sure it works with reference types.
            object[] o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            TensorSpan<object> spanObj = o.AsTensorSpan(3, 3);
            spanObj.Clear();

            var oSpanEnumerator = spanObj.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            // Make sure clearing a slice of a SPan with references it doesn't clear the whole thing.
            o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            spanObj = o.AsTensorSpan(3, 3);
            var oSpanSlice = spanObj.Slice(0..1, 0..3);
            oSpanSlice.Clear();
            oSpanEnumerator = oSpanSlice.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            Assert.Null(spanObj[0, 0]);
            Assert.Null(spanObj[0, 1]);
            Assert.Null(spanObj[0, 2]);
            Assert.NotNull(spanObj[1, 0]);
            Assert.NotNull(spanObj[1, 1]);
            Assert.NotNull(spanObj[1, 2]);
            Assert.NotNull(spanObj[2, 0]);
            Assert.NotNull(spanObj[2, 1]);
            Assert.NotNull(spanObj[2, 2]);

            // Make sure clearing a slice of a SPan with references it doesn't clear the whole thing.
            o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            spanObj = o.AsTensorSpan(3, 3);
            oSpanSlice = spanObj.Slice(1..2, 0..3);
            oSpanSlice.Clear();
            oSpanEnumerator = oSpanSlice.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            Assert.NotNull(spanObj[0, 0]);
            Assert.NotNull(spanObj[0, 1]);
            Assert.NotNull(spanObj[0, 2]);
            Assert.Null(spanObj[1, 0]);
            Assert.Null(spanObj[1, 1]);
            Assert.Null(spanObj[1, 2]);
            Assert.NotNull(spanObj[2, 0]);
            Assert.NotNull(spanObj[2, 1]);
            Assert.NotNull(spanObj[2, 2]);

            // Make sure clearing a slice of a SPan with references it doesn't clear the whole thing.
            o = [new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()];
            spanObj = o.AsTensorSpan(3, 3);
            oSpanSlice = spanObj.Slice(2..3, 0..3);
            oSpanSlice.Clear();
            oSpanEnumerator = oSpanSlice.GetEnumerator();
            while (oSpanEnumerator.MoveNext())
            {
                Assert.Null(oSpanEnumerator.Current);
            }

            Assert.NotNull(spanObj[0, 0]);
            Assert.NotNull(spanObj[0, 1]);
            Assert.NotNull(spanObj[0, 2]);
            Assert.NotNull(spanObj[1, 0]);
            Assert.NotNull(spanObj[1, 1]);
            Assert.NotNull(spanObj[1, 2]);
            Assert.Null(spanObj[2, 0]);
            Assert.Null(spanObj[2, 1]);
            Assert.Null(spanObj[2, 2]);
        }

        [Fact]
        public static void TensorCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            nint[] dims = [3, 3];
            TensorSpan<int> leftSpan = leftData.AsTensorSpan(3, 3);
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan(3, 3);
            leftSpan.CopyTo(tensor);
            var leftEnum = leftSpan.GetEnumerator();
            var tensorEnum = tensor.GetEnumerator();
            while (leftEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, tensorEnum.Current);
            }
            tensor.CopyTo(rightSpan);
            var rightEnum = rightSpan.GetEnumerator();
            tensorEnum = tensor.GetEnumerator();
            while (rightEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(rightEnum.Current, tensorEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);
            Assert.NotEqual(leftSpan[0, 0], tensor[0, 0]);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            dims = [15];
            leftSpan = leftData.AsTensorSpan(9);
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            leftSpan.CopyTo(tensor);
            leftEnum = leftSpan.GetEnumerator();
            tensorEnum = tensor.GetEnumerator();
            // Make sure the first 9 spots are equal after copy
            while (leftEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, tensorEnum.Current);
            }
            // The rest of the slots shouldn't have been touched.
            while (tensorEnum.MoveNext())
            {
                Assert.Equal(0, tensorEnum.Current);
            }

            Assert.Throws<ArgumentException>(() =>
            {
                var l = leftData.AsTensorSpan(3, 3, 3);
                var r = new TensorSpan<int>();
                l.CopyTo(r);
            });
        }

        [Fact]
        public static void TensorTryCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            TensorSpan<int> leftSpan = leftData.AsTensorSpan(3, 3);
            nint[] dims = [3, 3];
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            TensorSpan<int> rightSpan = rightData.AsTensorSpan(3, 3);
            var success = leftSpan.TryCopyTo(tensor);
            Assert.True(success);
            success = tensor.TryCopyTo(rightSpan);
            Assert.True(success);

            var leftEnum = leftSpan.GetEnumerator();
            var tensorEnum = tensor.GetEnumerator();
            while (leftEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, tensorEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);
            Assert.NotEqual(leftSpan[0, 0], tensor[0, 0]);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            dims = [15];
            leftSpan = leftData.AsTensorSpan(9);
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            success = leftSpan.TryCopyTo(tensor);
            leftEnum = leftSpan.GetEnumerator();
            tensorEnum = tensor.GetEnumerator();
            Assert.True(success);
            // Make sure the first 9 spots are equal after copy
            while (leftEnum.MoveNext() && tensorEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, tensorEnum.Current);
            }
            // The rest of the slots shouldn't have been touched.
            while (tensorEnum.MoveNext())
            {
                Assert.Equal(0, tensorEnum.Current);
            }

            leftData = [.. Enumerable.Range(0, 27)];
            var l = leftData.AsTensorSpan(3, 3, 3);
            dims = [2, 2];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            var r = new TensorSpan<int>();
            success = l.TryCopyTo(tensor);
            Assert.False(success);
            success = tensor.TryCopyTo(r);
            Assert.False(success);
        }

        [Fact]
        public static void TensorSliceTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            var tensor = Tensor.CreateUninitialized<int>([3, 3], false);

            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(0..1));
            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(1..2));
            //Assert.Throws<ArgumentOutOfRangeException>(() => tensor.Slice(0..1, 5..6));
            var intSpan = a.AsTensorSpan(3, 3);
            intSpan.CopyTo(tensor.AsTensorSpan());

            var sp = tensor.Slice(1..3, 1..3);
            Assert.Equal(5, sp[0, 0]);
            Assert.Equal(6, sp[0, 1]);
            Assert.Equal(8, sp[1, 0]);
            Assert.Equal(9, sp[1, 1]);
            int[] slice = [5, 6, 8, 9];
            Assert.Equal(slice, sp.ToArray());
            var enumerator = sp.GetEnumerator();
            var index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = tensor.Slice(0..3, 0..3);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(3, sp[0, 2]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            Assert.Equal(6, sp[1, 2]);
            Assert.Equal(7, sp[2, 0]);
            Assert.Equal(8, sp[2, 1]);
            Assert.Equal(9, sp[2, 2]);
            Assert.Equal(a, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(a[index++], enumerator.Current);
            }

            sp = tensor.Slice(0..1, 0..1);
            Assert.Equal(1, sp[0, 0]);
            Assert.Throws<IndexOutOfRangeException>(() => a.AsTensorSpan(3, 3).Slice(0..1, 0..1)[0, 1]);
            slice = [1];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = tensor.Slice(0..2, 0..2);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            slice = [1, 2, 4, 5];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            int[] numbers = [.. Enumerable.Range(0, 27)];
            intSpan = numbers.AsTensorSpan(3, 3, 3);
            tensor = Tensor.CreateUninitialized<int>([3, 3, 3], false);
            intSpan.CopyTo(tensor.AsTensorSpan());
            sp = tensor.Slice(1..2, 1..2, 1..2);
            Assert.Equal(13, sp[0, 0, 0]);
            slice = [13];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = tensor.Slice(1..3, 1..3, 1..3);
            Assert.Equal(13, sp[0, 0, 0]);
            Assert.Equal(14, sp[0, 0, 1]);
            Assert.Equal(16, sp[0, 1, 0]);
            Assert.Equal(17, sp[0, 1, 1]);
            Assert.Equal(22, sp[1, 0, 0]);
            Assert.Equal(23, sp[1, 0, 1]);
            Assert.Equal(25, sp[1, 1, 0]);
            Assert.Equal(26, sp[1, 1, 1]);
            slice = [13, 14, 16, 17, 22, 23, 25, 26];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            numbers = [.. Enumerable.Range(0, 16)];
            intSpan = numbers.AsTensorSpan(2, 2, 2, 2);
            tensor = Tensor.CreateUninitialized<int>([2, 2, 2, 2], false);
            intSpan.CopyTo(tensor.AsTensorSpan());
            sp = tensor.Slice(1..2, 0..2, 1..2, 0..2);
            Assert.Equal(10, sp[0, 0, 0, 0]);
            Assert.Equal(11, sp[0, 0, 0, 1]);
            Assert.Equal(14, sp[0, 1, 0, 0]);
            Assert.Equal(15, sp[0, 1, 0, 1]);
            slice = [10, 11, 14, 15];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
        }

        [Fact]
        public static void TensorReshapeTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            nint[] dims = [9];
            var tensor = Tensor.CreateUninitialized<int>(dims.AsSpan(), false);
            var span = a.AsTensorSpan(dims);
            span.CopyTo(tensor);

            Assert.Equal(1, tensor.Rank);
            Assert.Equal(9, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Strides.Length);
            Assert.Equal(1, tensor.Strides[0]);
            Assert.Equal(1, tensor[0]);
            Assert.Equal(2, tensor[1]);
            Assert.Equal(3, tensor[2]);
            Assert.Equal(4, tensor[3]);
            Assert.Equal(5, tensor[4]);
            Assert.Equal(6, tensor[5]);
            Assert.Equal(7, tensor[6]);
            Assert.Equal(8, tensor[7]);
            Assert.Equal(9, tensor[8]);

            dims = [3, 3];
            tensor = Tensor.Reshape(tensor, dims);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(3, tensor.Lengths[0]);
            Assert.Equal(3, tensor.Lengths[1]);
            Assert.Equal(2, tensor.Strides.Length);
            Assert.Equal(3, tensor.Strides[0]);
            Assert.Equal(1, tensor.Strides[1]);
            Assert.Equal(1, tensor[0, 0]);
            Assert.Equal(2, tensor[0, 1]);
            Assert.Equal(3, tensor[0, 2]);
            Assert.Equal(4, tensor[1, 0]);
            Assert.Equal(5, tensor[1, 1]);
            Assert.Equal(6, tensor[1, 2]);
            Assert.Equal(7, tensor[2, 0]);
            Assert.Equal(8, tensor[2, 1]);
            Assert.Equal(9, tensor[2, 2]);

            Assert.Throws<ArgumentException>(() => Tensor.Reshape(tensor, [1, 2, 3, 4, 5]));
        }

        [Fact]
        public static void TensorSqueezeTest()
        {
            nint[] dims = [1, 2];
            var tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);

            tensor = Tensor.Squeeze(tensor);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            dims = [1, 2, 1];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.Squeeze(tensor);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            dims = [1, 2, 1];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.Squeeze(tensor, 0);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);

            dims = [1, 2, 1];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            tensor = Tensor.Squeeze(tensor, 2);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);

            dims = [1, 2, 1];
            tensor = Tensor.Create<int>(dims.AsSpan(), false);
            Assert.Equal(3, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);
            Assert.Equal(1, tensor.Lengths[2]);

            Assert.Throws<ArgumentException>(() => tensor = Tensor.Squeeze(tensor, 1));
            Assert.Throws<ArgumentException>(() => tensor = Tensor.Squeeze(tensor, 3));
        }

        [Fact]
        public static void TensorUnsqueezeTest()
        {
            var tensor = Tensor.Create<int>([2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            tensor = Tensor.Unsqueeze(tensor, 0);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(1, tensor.Lengths[0]);
            Assert.Equal(2, tensor.Lengths[1]);

            tensor = Tensor.Create<int>([2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            tensor = Tensor.Unsqueeze(tensor, 1);
            Assert.Equal(2, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);
            Assert.Equal(1, tensor.Lengths[1]);

            tensor = Tensor.Create<int>([2], false);
            Assert.Equal(1, tensor.Rank);
            Assert.Equal(2, tensor.Lengths[0]);

            Assert.Throws<ArgumentOutOfRangeException>(() => Tensor.Unsqueeze<int>(tensor, -1));
            Assert.Throws<ArgumentException>(() => Tensor.Unsqueeze<int>(tensor, 2));

            Tensor<int> t0 = Tensor.CreateFromEnumerable(Enumerable.Range(0, 2));
            t0 = Tensor.Unsqueeze(t0, 1);
            Assert.Equal(0, t0[0, 0]);
            Assert.Equal(1, t0[1, 0]);
        }
    }
}
