// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Generic.Tests
{
    public partial class KeyValuePairTests
    {
        [Fact]
        public void Ctor_KeyValue_ReturnsExpected()
        {
            var keyValuePair = new KeyValuePair<int, string>(1, "2");
            Assert.Equal(1, keyValuePair.Key);
            Assert.Equal("2", keyValuePair.Value);
        }

        [Fact]
        public void ToString_NonNullKeyNonNullValue_ReturnsExpected()
        {
            var keyValuePair = new KeyValuePair<string, string>("1", "2");
            Assert.Equal("[1, 2]", keyValuePair.ToString());
        }

        [Fact]
        public void ToString_NonNullKeyNullValue_ReturnsExpected()
        {
            var keyValuePair = new KeyValuePair<string, string>("1", null);
            Assert.Equal("[1, ]", keyValuePair.ToString());
        }

        [Fact]
        public void ToString_NullKeyNonNullValue_ReturnsExpected()
        {
            var keyValuePair = new KeyValuePair<string, string>(null, "1");
            Assert.Equal("[, 1]", keyValuePair.ToString());
        }

        [Fact]
        public void ToString_NullKeyNullValue_ReturnsExpected()
        {
            var keyValuePair = new KeyValuePair<string, string>(null, null);
            Assert.Equal("[, ]", keyValuePair.ToString());
        }
    }
}
