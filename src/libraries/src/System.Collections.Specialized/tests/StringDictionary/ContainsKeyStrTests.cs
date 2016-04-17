// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryContainsKeyTests
    {
        [Fact]
        public void ContainsKey_NonExistentKey_ReturnsFalse()
        {
            StringDictionary stringDictionary = new StringDictionary();
            Assert.False(stringDictionary.ContainsKey("key"));
        }

        [Fact]
        public void ContainsKey_NullKey_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("key", () => new StringDictionary().ContainsKey(null));
        }
    }
}
