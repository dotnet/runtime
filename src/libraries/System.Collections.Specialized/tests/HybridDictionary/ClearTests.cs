// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryClearTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(50)]
        public void Clear(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            hybridDictionary.Clear();
            Assert.Equal(0, hybridDictionary.Count);
            Assert.Equal(0, hybridDictionary.Keys.Count);
            Assert.Equal(0, hybridDictionary.Values.Count);

            hybridDictionary.Clear();
            Assert.Equal(0, hybridDictionary.Count);
            Assert.Equal(0, hybridDictionary.Keys.Count);
            Assert.Equal(0, hybridDictionary.Values.Count);
        }
    }
}
