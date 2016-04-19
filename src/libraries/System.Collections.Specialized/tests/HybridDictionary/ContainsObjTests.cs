// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryContainsTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(50)]
        public void Contains_NoSuchKey_ReturnsFalse(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            Assert.False(hybridDictionary.Contains("no-such-key"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(50)]
        public void Contains_NullKey_ThrowsArgumentNullException(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            Assert.Throws<ArgumentNullException>("key", () => hybridDictionary.Contains(null));
        }

        [Theory]
        [InlineData(5)]
        [InlineData(50)]
        public void Contains_Class_DoesntOverrideEquals(int count)
        {
            HybridDictionary hybridDictionary = new HybridDictionary();
            for (int i = 0; i < count; i++)
            {
                object key = new object();
                hybridDictionary.Add(key, "value");
                Assert.True(hybridDictionary.Contains(key));
                Assert.Throws<ArgumentException>(() => hybridDictionary.Add(key, "value"));
            }
            Assert.False(hybridDictionary.Contains(new object()));
        }

        [Theory]
        [InlineData(5)]
        [InlineData(50)]
        public void Contains_Struct_DoesntOverrideEquals(int count)
        {
            HybridDictionary hybridDictionary = new HybridDictionary();
            for (int i = 0; i < count; i++)
            {
                object key = new SpecialStruct { IntValue = i, StringValue = "one" };
                hybridDictionary.Add(key, "value");
                Assert.True(hybridDictionary.Contains(key));
                Assert.True(hybridDictionary.Contains(new SpecialStruct { IntValue = i, StringValue = "one" }));
                Assert.False(hybridDictionary.Contains(new SpecialStruct { IntValue = i + 1, StringValue = "one" }));
            }
        }

        public struct SpecialStruct
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }
    }
}
