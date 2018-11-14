// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public partial class CompareInfoTests
    {
        [Theory]
        [MemberData(nameof(GetHashCodeTestData))]
        public void GetHashCode_Span(string source1, CompareOptions options1, string source2, CompareOptions options2, bool expectSameHashCode)
        {
            CompareInfo invariantCompare = CultureInfo.InvariantCulture.CompareInfo;

            int hashOfSource1AsString = invariantCompare.GetHashCode(source1, options1);
            int hashOfSource1AsSpan = invariantCompare.GetHashCode(source1.AsSpan(), options1);
            Assert.Equal(hashOfSource1AsString, hashOfSource1AsSpan);

            int hashOfSource2AsString = invariantCompare.GetHashCode(source2, options2);
            int hashOfSource2AsSpan = invariantCompare.GetHashCode(source2.AsSpan(), options2);
            Assert.Equal(hashOfSource2AsString, hashOfSource2AsSpan);

            Assert.Equal(expectSameHashCode, hashOfSource1AsSpan == hashOfSource2AsSpan);
        }

        [Fact]
        public void GetHashCode_EmptySpan()
        {
            Assert.Equal(0, CultureInfo.InvariantCulture.CompareInfo.GetHashCode(ReadOnlySpan<char>.Empty, CompareOptions.None));
        }

        [Fact]
        public void GetHashCode_Span_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), CompareOptions.Ordinal | CompareOptions.IgnoreSymbols));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), (CompareOptions)(-1)));
        }
    }
}
