// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Buffers.Tests
{
    public class SearchValuesPolyfillTests
    {
        [Theory]
        [InlineData(char.MinValue)]
        [InlineData('a')]
        [InlineData((char)127)]
        [InlineData((char)128)]
        [InlineData(char.MaxValue)]
        public void SearchValues_Contains(char c)
        {
            SearchValues<char> values = SearchValues.Create([c]);
            Assert.True(values.Contains(c));
            Assert.False(values.Contains((char)(c - 1)));
            Assert.False(values.Contains((char)(c + 1)));
        }
    }
}
