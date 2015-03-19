// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Linq.Tests
{
    public partial class EnumerableTests
    {
        [Fact]
        public void Average()
        {
            var array = Enumerable.Range(1, 10).ToArray();
            var min = array.Min();
            Assert.Equal(min, 1);
        }

        [Fact]
        public void All()
        {
            var array = Enumerable.Range(1, 10).ToArray();
            Assert.True(array.All(i => i > 0));
            Assert.False(array.All(i => i > 1));
            Assert.False(array.All(i => i > 2));
            Assert.False(array.All(i => i > 3));
            Assert.False(array.All(i => i > 4));
            Assert.False(array.All(i => i > 5));
            Assert.False(array.All(i => i > 6));
            Assert.False(array.All(i => i > 7));
            Assert.False(array.All(i => i > 8));
            Assert.False(array.All(i => i > 9));
            Assert.False(array.All(i => i > 10));
        }

        [Fact]
        public void Any()
        {
            var array = Enumerable.Range(1, 10).ToArray();
            Assert.True(array.Any(i => i > 0));
            Assert.True(array.Any(i => i > 1));
            Assert.True(array.Any(i => i > 2));
            Assert.True(array.Any(i => i > 3));
            Assert.True(array.Any(i => i > 4));
            Assert.True(array.Any(i => i > 5));
            Assert.True(array.Any(i => i > 6));
            Assert.True(array.Any(i => i > 7));
            Assert.True(array.Any(i => i > 8));
            Assert.True(array.Any(i => i > 9));
            Assert.False(array.Any(i => i > 10));
        }

        [Fact]
        public void Count()
        {
            const int count = 100;
            Assert.Equal(Enumerable.Range(1, count).Count(), count);
        }

        [Fact]
        public void Distinct()
        {
            var array = new[] {1, 1, 1, 2, 3, 5, 5, 6, 6, 10};
            var distinct = array.Distinct().ToArray();

            // Ensure the result doesn't contain duplicates; sort it first to make this easier.
            // Notice that we assume the correctness of OrderBy() here, which is tested
            // in another unit.
            distinct = distinct.OrderBy(i => i).ToArray();
            for (var i = 0; i < distinct.Length - 1; i++)
            {
                Assert.False(distinct[i] == distinct[i + 1],
                    "There are some duplicates in the result of Distinct().");
            }

            // Ensure that every element in 'array' exists in 'distinct'.
            foreach (var i in array)
            {
                var correct = false;
                foreach (var j in distinct)
                {
                    if (i == j)
                        correct = true;
                }
                Assert.True(correct, "Some elements in the original enumerable are missing in the result of Distinct().");
            }

            // Ensure that every element in 'distinct' exists in 'array'.
            foreach (var i in distinct)
            {
                var correct = false;
                foreach (var j in array)
                {
                    if (i == j)
                        correct = true;
                }
                Assert.True(correct, "Some elements in the result of Distinct() don't xist in the given enumerable.");
            }
        }

        [Fact]
        public void Min()
        {
            var array = Enumerable.Range(1, 10).ToArray();
            var min = array.Min();
            Assert.Equal(min, 1);
        }

        [Fact]
        public void Max()
        {
            var array = Enumerable.Range(1, 10).ToArray();
            var max = array.Max();
            Assert.Equal(max, 10);
        }

        [Fact]
        public void Range()
        {
            var array = Enumerable.Range(1, 100).ToArray();
            Assert.Equal(array.Length, 100);
            for (var i = 0; i < array.Length; i++)
                Assert.Equal(array[i], i + 1);
        }
    }
}


