// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class RangeSetTest
    {
        private readonly RangeSet _rangeSet = new RangeSet();

        [Fact]
        public void AddSingle()
        {
            _rangeSet.Add(0);

            Assert.Equal(1, _rangeSet.Count);
            Assert.Equal(0, _rangeSet.GetMax());
            Assert.Equal(0, _rangeSet.GetMin());
        }

        [Fact]
        public void AddMany()
        {
            _rangeSet.Add(3);
            _rangeSet.Add(0);
            _rangeSet.Add(6);

            Assert.Equal(3, _rangeSet.Count);
            Assert.Equal(0, _rangeSet.GetMin());
            Assert.Equal(6, _rangeSet.GetMax());
        }

        [Theory]
        [InlineData(6, 7)]
        [InlineData(6, 6)]
        [InlineData(6, 5)]
        public void InsertContiguous(long firstEnd, long secondStart)
        {
            Assert.True(firstEnd + 1 >= secondStart, "invalid values passed to test");

            _rangeSet.Add(1, firstEnd);
            _rangeSet.Add(secondStart, 10);

            Assert.Equal(1, _rangeSet.Count);
            Assert.Equal(10, _rangeSet.GetMax());
            Assert.Equal(1, _rangeSet.GetMin());
        }

        [Theory]
        [InlineData(2, 4)]
        [InlineData(4, 2)]
        public void InsertNonContiguous(long first, long second)
        {
            _rangeSet.Add(first);
            _rangeSet.Add(second);

            Assert.Equal(2, _rangeSet.Count);
            Assert.Equal(Math.Max(first, second), _rangeSet.GetMax());
            Assert.Equal(Math.Min(first, second), _rangeSet.GetMin());
        }

        [Fact]
        public void ExpandSingleRange()
        {
            _rangeSet.Add(2, 3);
            _rangeSet.Add(0, 5);

            Assert.Equal(1, _rangeSet.Count);
            Assert.Equal(0, _rangeSet.GetMin());
            Assert.Equal(5, _rangeSet.GetMax());
        }

        [Fact]
        public void ExpandRangeInMiddle()
        {
            _rangeSet.Add(1, 2);
            _rangeSet.Add(5, 9);
            _rangeSet.Add(14, 15);

            // enlarge middle range
            _rangeSet.Add(4, 10);

            Assert.Equal(3, _rangeSet.Count);
            Assert.True(_rangeSet.Includes(4, 10));
            Assert.False(_rangeSet.Contains(3));
            Assert.False(_rangeSet.Includes(11, 11));
        }

        [Theory]
        [InlineData(3, 5)]
        [InlineData(2, 4)]
        [InlineData(4, 4)]
        [InlineData(4, 6)]
        public void MergeRanges(long start, long end)
        {
            _rangeSet.Add(1, 3);
            _rangeSet.Add(5, 6);

            _rangeSet.Add(start, end);

            Assert.Equal(1, _rangeSet.Count);
            Assert.Equal(1, _rangeSet.GetMin());
            Assert.Equal(6, _rangeSet.GetMax());
        }

        [Fact]
        public void Includes()
        {
            _rangeSet.Add(1, 3);
            _rangeSet.Add(5, 6);

            Assert.True(_rangeSet.Includes(2, 2));
            Assert.True(_rangeSet.Includes(1, 3));
            Assert.True(_rangeSet.Includes(5, 6));

            Assert.False(_rangeSet.Includes(1, 6));
        }

        [Fact]
        public void RemoveEntireRange()
        {
            _rangeSet.Add(1, 5);
            _rangeSet.Add(7, 9);
            _rangeSet.Add(11, 15);
            _rangeSet.Add(20, 21);

            _rangeSet.Remove(7, 9);
            _rangeSet.Remove(19, 22);

            Assert.Equal(2, _rangeSet.Count);
            Assert.Equal(1, _rangeSet.GetMin());
            Assert.Equal(15, _rangeSet.GetMax());
        }

        [Fact]
        public void RemoveSplitsRange()
        {
            _rangeSet.Add(1, 5);

            _rangeSet.Remove(3);

            Assert.Equal(2, _rangeSet.Count);
        }

        [Theory]
        [InlineData(1, 4)]
        [InlineData(11, 15)]
        public void RemoveNonExistingAfter(long start, long end)
        {
            _rangeSet.Add(5, 10);

            _rangeSet.Remove(start, end);

            var range = Assert.Single(_rangeSet);
            Assert.Equal(5, range.Start);
            Assert.Equal(10, range.End);
        }
    }
}
