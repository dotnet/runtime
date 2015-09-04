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
    public class RepeatTests
    {
        [Fact]
        public void Repeat_ProduceCorrectSequence()
        {
            var repeatSequence = Enumerable.Repeat(1, 100);
            int count = 0;
            foreach (var val in repeatSequence)
            {
                count++;
                Assert.Equal(1, val);
            }

            Assert.Equal(100, count);
        }

        [Fact]
        public void Repeat_ToArray_ProduceCorrectResult()
        {
            var array = Enumerable.Repeat(1, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Equal(1, array[i]);
        }

        [Fact]
        public void Repeat_ProduceSameObject()
        {
            object objectInstance = new object();
            var array = Enumerable.Repeat(objectInstance, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Same(objectInstance, array[i]);
        }

        [Fact]
        public void Repeat_WorkWithNullElement()
        {
            object objectInstance = null;
            var array = Enumerable.Repeat(objectInstance, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Null(array[i]);
        }


        [Fact]
        public void Repeat_ZeroCountLeadToEmptySequence()
        {
            var array = Enumerable.Repeat(1, 0).ToArray();
            Assert.Equal(array.Length, 0);
        }

        [Fact]
        public void Repeat_ThrowExceptionOnNegativeCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Repeat(1, -1));
        }


        [Fact]
        public void Repeat_NotEnumerateAfterEnd()
        {
            using (var repeatEnum = Enumerable.Repeat(1, 1).GetEnumerator())
            {
                Assert.True(repeatEnum.MoveNext());
                Assert.False(repeatEnum.MoveNext());
                Assert.False(repeatEnum.MoveNext());
            }
        }

        [Fact]
        public void Repeat_EnumerableAndEnumeratorAreSame()
        {
            var repeatEnumerable = Enumerable.Repeat(1, 1);
            using (var repeatEnumerator = repeatEnumerable.GetEnumerator())
            {
                Assert.Same(repeatEnumerable, repeatEnumerator);
            }
        }

        [Fact]
        public void Repeat_GetEnumeratorReturnUniqueInstances()
        {
            var repeatEnumerable = Enumerable.Repeat(1, 1);
            using (var enum1 = repeatEnumerable.GetEnumerator())
            using (var enum2 = repeatEnumerable.GetEnumerator())
            {
                Assert.NotSame(enum1, enum2);
            }
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            Assert.Equal(Enumerable.Repeat(-3, 0), Enumerable.Repeat(-3, 0));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            Assert.Equal(Enumerable.Repeat("SSS", 99), Enumerable.Repeat("SSS", 99));
        }
        
        [Fact]
        public void CountOneSingleResult()
        {
            int[] expected = { -15 };

            Assert.Equal(expected, Enumerable.Repeat(-15, 1));
        }

        [Fact]
        public void RepeatArbitraryCorrectResults()
        {
            int[] expected = { 12, 12, 12, 12, 12, 12, 12, 12 };

            Assert.Equal(expected, Enumerable.Repeat(12, 8));
        }

        [Fact]
        public void RepeatNull()
        {
            int?[] expected = { null, null, null, null };

            Assert.Equal(expected, Enumerable.Repeat((int?)null, 4));
        }
    }
}
