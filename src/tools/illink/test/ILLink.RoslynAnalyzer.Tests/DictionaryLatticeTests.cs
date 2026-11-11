// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ILLink.Shared.DataFlow;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class DictionaryLatticeTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        public void EnumerationPreservesResetAndInterfaceBehavior(int count)
        {
            DefaultValueDictionary<int, int> values = new(0);
            for (int i = 1; i <= count; i++)
                values.Set(i, i);
            var enumerator = values.GetEnumerator();
            for (int pass = 0; pass < 2; pass++)
            {
                List<int> actual = new();
                while (enumerator.MoveNext())
                    actual.Add(enumerator.Current.Value);
                Assert.Equal(Enumerable.Range(1, count), actual);
                enumerator.Reset();
            }
            Assert.Equal(Enumerable.Range(1, count), ((IEnumerable<KeyValuePair<int, int>>)values).Select(pair => pair.Value));
            Assert.Equal(Enumerable.Range(1, count), ((IEnumerable)values).Cast<KeyValuePair<int, int>>().Select(pair => pair.Value));
        }

        [Fact]
        public void DictionaryEnumerationDoesNotAllocate()
        {
            DefaultValueDictionary<int, int> values = new(0);
            values.Set(1, 1);
            int sum = 0;
            foreach (var value in values)
                sum += value.Value;

            long before = GC.GetAllocatedBytesForCurrentThread();
            foreach (var value in values)
                sum += value.Value;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(2, sum);
            Assert.Equal(0, allocated);
        }

        [Fact]
        public void EnumerationStillDetectsDictionaryMutation()
        {
            DefaultValueDictionary<int, int> values = new(0);
            values.Set(1, 1);
            var enumerator = values.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            values.Set(2, 2);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(128, false)]
        [InlineData(128, true)]
        public void MeetPreservesValuesDefaultsAndInputIsolation(int count, bool clearedLeft)
        {
            DefaultValueDictionary<int, int> left = new(0);
            DefaultValueDictionary<int, int> right = new(-1);
            if (clearedLeft)
            {
                left.Set(-1, 1);
                left.Set(-1, 0);
            }
            for (int i = 1; i <= count; i++)
                right.Set(i, i);

            DictionaryLattice<int, int, MaxLattice> lattice = new(default);
            DefaultValueDictionary<int, int> merged = lattice.Meet(left, right);

            Assert.Equal(count, merged.Count);
            Assert.Equal(0, merged.Get(count + 1));
            for (int i = 1; i <= count; i++)
                Assert.Equal(i, merged.Get(i));
            merged.Set(count + 1, 10);
            Assert.Equal(0, left.Count);
            Assert.Equal(-1, right.Get(count + 1));
        }

        [Fact]
        public void MeetCombinesOverlappingAndDisjointKeys()
        {
            DefaultValueDictionary<int, int> left = new(0);
            DefaultValueDictionary<int, int> right = new(0);
            left.Set(1, 1);
            left.Set(2, 4);
            right.Set(1, 3);
            right.Set(3, 5);

            DefaultValueDictionary<int, int> merged = new DictionaryLattice<int, int, MaxLattice>(default).Meet(left, right);

            Assert.Equal(3, merged.Count);
            Assert.Equal(3, merged.Get(1));
            Assert.Equal(4, merged.Get(2));
            Assert.Equal(5, merged.Get(3));
            Assert.Equal(1, left.Get(1));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EmptyRightPreservesLeftDefaultAndCopyIsolation(bool clearedRight)
        {
            DefaultValueDictionary<int, int> left = new(0);
            DefaultValueDictionary<int, int> right = new(-1);
            left.Set(1, 2);
            if (clearedRight)
            {
                right.Set(1, 3);
                right.Set(1, -1);
            }

            DefaultValueDictionary<int, int> merged = new DictionaryLattice<int, int, MaxLattice>(default).Meet(left, right);
            Assert.Equal(2, merged.Get(1));
            Assert.Equal(0, merged.Get(2));
            merged.Set(1, 4);
            Assert.Equal(2, left.Get(1));
        }

        private readonly struct MaxLattice : ILattice<int>
        {
            public int Top => 0;
            public int Meet(int left, int right) => Math.Max(left, right);
        }
    }
}
