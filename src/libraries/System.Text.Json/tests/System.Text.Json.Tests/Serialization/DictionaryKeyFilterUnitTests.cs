// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class DictionaryKeyFilterUnitTests
    {
        [Fact]
        public static void ToCamelCaseTest()
        {
            Assert.True(IgnoreKey("$key"));
            Assert.True(IgnoreKey("$ key"));
            Assert.True(IgnoreKey("$_key"));
            Assert.True(IgnoreKey("$\0key"));
            Assert.True(IgnoreKey("$ "));
            Assert.True(IgnoreKey("$_"));
            Assert.True(IgnoreKey("$\0"));
            Assert.True(IgnoreKey("$"));

            Assert.False(IgnoreKey("key"));
            Assert.False(IgnoreKey("key$"));
            Assert.False(IgnoreKey(" $key"));
            Assert.False(IgnoreKey("_$key"));
            Assert.False(IgnoreKey("\0$key"));
            Assert.False(IgnoreKey(" $"));
            Assert.False(IgnoreKey("_$"));
            Assert.False(IgnoreKey("\0$"));
            Assert.False(IgnoreKey(""));
            Assert.False(IgnoreKey(null));

            static bool IgnoreKey(string name)
                => JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey(Encoding.UTF8.GetBytes(name));
        }
    }
}
