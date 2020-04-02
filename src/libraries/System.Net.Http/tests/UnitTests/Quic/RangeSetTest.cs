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
            Assert.Equal(0ul, _rangeSet.GetMax());
            Assert.Equal(0ul, _rangeSet.GetMin());
        }

        [Fact]
        public void AddMany()
        {
            _rangeSet.Add(0);
            _rangeSet.Add(3);
            _rangeSet.Add(6);

            Assert.Equal(3, _rangeSet.Count);
            Assert.Equal(0u , _rangeSet.GetMin());
            Assert.Equal(6u , _rangeSet.GetMax());
        }

        [Theory]
        [InlineData(6, 7)]
        [InlineData(6, 6)]
        [InlineData(6, 5)]
        public void InsertContiguous(ulong firstEnd, ulong secondStart)
        {
            Assert.True(firstEnd + 1 >= secondStart, "invalid values passed to test");

            _rangeSet.Add(1, firstEnd);
            _rangeSet.Add(secondStart, 10);

            Assert.Equal(1, _rangeSet.Count);
            Assert.Equal(10ul, _rangeSet.GetMax());
            Assert.Equal(1ul, _rangeSet.GetMin());
        }

        [Theory]
        [InlineData(2, 4)]
        [InlineData(4, 2)]
        public void InsertNonContiguousAbove(ulong first, ulong second)
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
            Assert.Equal(0u , _rangeSet.GetMin());
            Assert.Equal(5u , _rangeSet.GetMax());
        }

        [Theory]
        [InlineData(3, 5)]
        [InlineData(2, 4)]
        [InlineData(4, 4)]
        [InlineData(4, 6)]
        public void MergeRanges(ulong start, ulong end)
        {
            _rangeSet.Add(1, 3);
            _rangeSet.Add(5, 6);

            _rangeSet.Add(start, end);

            Assert.Equal(1, _rangeSet.Count);
            Assert.Equal(1u , _rangeSet.GetMin());
            Assert.Equal(6u , _rangeSet.GetMax());
        }
    }
}
