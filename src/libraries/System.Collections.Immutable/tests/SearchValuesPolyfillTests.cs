// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class SearchValuesPolyfillTests
    {
        [Fact]
        public void SearchValues_Contains_NonAsciiChar()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/XXXXX
            // Verifies that non-ASCII characters like char(200) are correctly found
            // This test specifically validates the polyfill implementation used on .NET Framework
            char c = (char)200;
            SearchValues<char> searchValues = SearchValues.Create([c]);
            Assert.True(searchValues.Contains(c));
        }

        [Fact]
        public void SearchValues_Contains_MixedAsciiAndNonAscii()
        {
            // Test that both ASCII and non-ASCII characters work correctly
            SearchValues<char> searchValues = SearchValues.Create(['A', 'Z', (char)200, (char)300]);
            
            Assert.True(searchValues.Contains('A'));
            Assert.True(searchValues.Contains('Z'));
            Assert.True(searchValues.Contains((char)200));
            Assert.True(searchValues.Contains((char)300));
            
            Assert.False(searchValues.Contains('B'));
            Assert.False(searchValues.Contains((char)250));
        }

        [Fact]
        public void SearchValues_Contains_BoundaryCharacters()
        {
            // Test boundary between ASCII (0-127) and non-ASCII (128+)
            SearchValues<char> asciiOnly = SearchValues.Create([(char)127]);
            SearchValues<char> nonAsciiOnly = SearchValues.Create([(char)128]);
            
            Assert.True(asciiOnly.Contains((char)127));
            Assert.False(asciiOnly.Contains((char)128));
            
            Assert.False(nonAsciiOnly.Contains((char)127));
            Assert.True(nonAsciiOnly.Contains((char)128));
        }
    }
}
