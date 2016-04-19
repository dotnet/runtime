// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryGetItemTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(10)]
        public void Item_Get_NoSuchKey_ReturnsNull(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            Assert.Null(hybridDictionary["no-such-key"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(10)]
        public void Item_Get_NullKey_ThrowsArgumentNullException(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            Assert.Throws<ArgumentNullException>("key", () => hybridDictionary[null]);
        }
    }
}
