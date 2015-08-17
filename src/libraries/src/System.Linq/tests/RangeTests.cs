// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class RangeTests
    {
        [Fact]
        public void Range_ProduceCorrectSequence()
        {
            var rangeSequence = Enumerable.Range(1, 100);
            int expected = 0;
            foreach (var val in rangeSequence)
            {
                expected++;
                Assert.Equal(expected, val);
            }

            Assert.Equal(100, expected);
        }

        [Fact]
        public void Range_ToArray_ProduceCorrectResult()
        {
            var array = Enumerable.Range(1, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Equal(i + 1, array[i]);
        }


        [Fact]
        public void Range_ZeroCountLeadToEmptySequence()
        {
            var array = Enumerable.Range(1, 0).ToArray();
            var array2 = Enumerable.Range(int.MinValue, 0).ToArray();
            var array3 = Enumerable.Range(int.MaxValue, 0).ToArray();
            Assert.Equal(array.Length, 0);
            Assert.Equal(array2.Length, 0);
            Assert.Equal(array3.Length, 0);
        }

        [Fact]
        public void Range_ThrowExceptionOnNegativeCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1, int.MinValue));
        }

        [Fact]
        public void Range_ThrowExceptionOnOverflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1000, int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(int.MaxValue, 1000));
        }

        [Fact]
        public void Range_NotEnumerateAfterEnd()
        {
            using (var rangeEnum = Enumerable.Range(1, 1).GetEnumerator())
            {
                Assert.True(rangeEnum.MoveNext());
                Assert.False(rangeEnum.MoveNext());
                Assert.False(rangeEnum.MoveNext());
            }
        }

        [Fact]
        public void Range_EnumerableAndEnumeratorAreSame()
        {
            var rangeEnumberable = Enumerable.Range(1, 1);
            using (var rangeEnumberator = rangeEnumberable.GetEnumerator())
            {
                Assert.Same(rangeEnumberable, rangeEnumberator);
            }
        }

        [Fact]
        public void Range_GetEnumeratorReturnUniqueInstances()
        {
            var rangeEnumberable = Enumerable.Range(1, 1);
            using (var enum1 = rangeEnumberable.GetEnumerator())
            using (var enum2 = rangeEnumberable.GetEnumerator())
            {
                Assert.NotSame(enum1, enum2);
            }
        }

        [Fact]
        public void Range_ToInt32MaxValue()
        {
            int from = Int32.MaxValue - 3;
            int count = 4;
            var rangeEnumerable = Enumerable.Range(from, count);

            Assert.Equal(count, rangeEnumerable.Count());

            int[] expected = { Int32.MaxValue - 3, Int32.MaxValue - 2, Int32.MaxValue - 1, Int32.MaxValue };
            Assert.Equal(expected, rangeEnumerable);
        }
    }
}
