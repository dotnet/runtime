// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AggregateTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Aggregate((x, y) => x + y), q.Aggregate((x, y) => x + y));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Aggregate((x, y) => x + y), q.Aggregate((x, y) => x + y));
        }

        [Fact]
        public void EmptySource()
        {
            int[] source = { };
            
            Assert.Throws<InvalidOperationException>(() => source.RunOnce().Aggregate((x, y) => x + y));
        }

        [Fact]
        public void SingleElement()
        {
            int[] source = { 5 };
            int expected = 5;

            Assert.Equal(expected, source.Aggregate((x, y) => x + y));
        }

        [Fact]
        public void SingleElementRunOnce()
        {
            int[] source = { 5 };
            int expected = 5;

            Assert.Equal(expected, source.RunOnce().Aggregate((x, y) => x + y));
        }

        [Fact]
        public void TwoElements()
        {
            int[] source = { 5, 6 };
            int expected = 11;

            Assert.Equal(expected, source.Aggregate((x, y) => x + y));
        }

        [Fact]
        public void MultipleElements()
        {
            int[] source = { 5, 6, 0, -4 };
            int expected = 7;

            Assert.Equal(expected, source.Aggregate((x, y) => x + y));
        }

        [Fact]
        public void MultipleElementsRunOnce()
        {
            int[] source = { 5, 6, 0, -4 };
            int expected = 7;

            Assert.Equal(expected, source.RunOnce().Aggregate((x, y) => x + y));
        }

        [Fact]
        public void EmptySourceAndSeed()
        {
            int[] source = { };
            long seed = 2;
            long expected = 2;

            Assert.Equal(expected, source.Aggregate(seed, (x, y) => x * y));
        }

        [Fact]
        public void SingleElementAndSeed()
        {
            int[] source = { 5 };
            long seed = 2;
            long expected = 10;

            Assert.Equal(expected, source.Aggregate(seed, (x, y) => x * y));
        }

        [Fact]
        public void TwoElementsAndSeed()
        {
            int[] source = { 5, 6 };
            long seed = 2;
            long expected = 60;

            Assert.Equal(expected, source.Aggregate(seed, (x, y) => x * y));
        }

        [Fact]
        public void MultipleElementsAndSeed()
        {
            int[] source = { 5, 6, 2, -4 };
            long seed = 2;
            long expected = -480;

            Assert.Equal(expected, source.Aggregate(seed, (x, y) => x * y));
        }

        [Fact]
        public void MultipleElementsAndSeedRunOnce()
        {
            int[] source = { 5, 6, 2, -4 };
            long seed = 2;
            long expected = -480;

            Assert.Equal(expected, source.RunOnce().Aggregate(seed, (x, y) => x * y));
        }

        [Fact]
        public void NoElementsSeedResultSeletor()
        {
            int[] source = { };
            long seed = 2;
            double expected = 7;

            Assert.Equal(expected, source.Aggregate(seed, (x, y) => x * y, x => x + 5.0));
        }

        [Fact]
        public void SingleElementSeedResultSelector()
        {
            int[] source = { 5 };
            long seed = 2;
            long expected = 15;

            Assert.Equal(expected, source.Aggregate(seed, (x, y) => x * y, x => x + 5.0));
        }

        [Fact]
        public void TwoElementsSeedResultSelector()
        {
            int[] source = { 5, 6 };
            long seed = 2;
            long expected = 65;

            Assert.Equal(expected, source.Aggregate(seed, (x, y) => x * y, x => x + 5.0));
        }

        [Fact]
        public void MultipleElementsSeedResultSelector()
        {
            int[] source = { 5, 6, 2, -4 };
            long seed = 2;
            long expected = -475;

            Assert.Equal(expected, source.Aggregate(seed, (x, y) => x * y, x => x + 5.0));
        }

        [Fact]
        public void MultipleElementsSeedResultSelectorRunOnce()
        {
            int[] source = { 5, 6, 2, -4 };
            long seed = 2;
            long expected = -475;

            Assert.Equal(expected, source.RunOnce().Aggregate(seed, (x, y) => x * y, x => x + 5.0));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Aggregate((x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Aggregate(0, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Aggregate(0, (x, y) => x + y, i => i));
        }

        [Fact]
        public void NullFunc()
        {
            Func<int, int, int> func = null;
            AssertExtensions.Throws<ArgumentNullException>("func", () => Enumerable.Range(0, 3).Aggregate(func));
            AssertExtensions.Throws<ArgumentNullException>("func", () => Enumerable.Range(0, 3).Aggregate(0, func));
            AssertExtensions.Throws<ArgumentNullException>("func", () => Enumerable.Range(0, 3).Aggregate(0, func, i => i));
        }

        [Fact]
        public void NullResultSelector()
        {
            Func<int, int> resultSelector = null;
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Range(0, 3).Aggregate(0, (x, y) => x + y, resultSelector));
        }
    }
}
