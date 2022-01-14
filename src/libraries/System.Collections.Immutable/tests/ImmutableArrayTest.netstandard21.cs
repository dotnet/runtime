// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public partial class ImmutableArrayTest : SimpleElementImmutablesTestBase
    {
        [Fact]
        public void AsSpanRoundTripEmptyArrayTests_RangeInput()
        {
            ImmutableArray<int> immutableArray = ImmutableArray.Create(Array.Empty<int>());

            ReadOnlySpan<int> rangedSpan = immutableArray.AsSpan(new Range(0, 0));
            Assert.Equal(immutableArray, rangedSpan.ToArray());
            Assert.Equal(immutableArray.Length, rangedSpan.Length);
        }

        [Fact]
        public void AsSpanEmptyRangeNotInitialized()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.AsSpan(new Range(0, 0)));
        }

        [Theory]
        [MemberData(nameof(RangeIndexLengthData))]
        public void AsSpanStartLength_RangeInput(IEnumerable<int> source, int start, int length)
        {
            var array = source.ToImmutableArray();
            var expected = source.Skip(start).Take(length);

            Assert.Equal(expected, array.AsSpan(new Range(start, start + length)).ToArray());
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void AsSpanStartLengthInvalid_RangeInput(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(new Range(-1, 0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(new Range(array.Length + 1, array.Length + 2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(new Range(0, -1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(new Range(0, array.Length + 1)));
        }
    }
}
