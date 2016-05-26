// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO
{
    public static class RowConfigReaderTests
    {
        [Fact]
        public static void BasicTest()
        {
            RowConfigReader rcr = new RowConfigReader(BasicData);
            Assert.Equal("stringVal", rcr.GetNextValue("stringKey"));
            Assert.Equal(12, rcr.GetNextValueAsInt32("intKey"));
        }

        [Fact]
        public static void BasicTestWrongCase()
        {
            // Default is Ordinal comparison. Keys with different case should not be found.
            RowConfigReader rcr = new RowConfigReader(BasicData);
            string unused;
            Assert.False(rcr.TryGetNextValue("stringkey", out unused));
            Assert.False(rcr.TryGetNextValue("intkey", out unused));
        }

        [Fact]
        public static void BasicTestCaseInsensitive()
        {
            // Use a case-insensitive comparer and use differently-cased keys.
            RowConfigReader rcr = new RowConfigReader(BasicData, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("stringVal", rcr.GetNextValue("stringkey"));
            Assert.Equal(12, rcr.GetNextValueAsInt32("intkey"));
        }

        [Fact]
        public static void StaticHelper()
        {
            Assert.Equal("stringVal", RowConfigReader.ReadFirstValueFromString(BasicData, "stringKey"));
            Assert.Equal("12", RowConfigReader.ReadFirstValueFromString(BasicData, "intKey"));
        }

        [InlineData(ConfigData)]
        [InlineData(ConfigDataExtraNewlines)]
        [InlineData(ConfigDataNoTrailingNewline)]
        [Theory]
        public static void NewlineTests(string data)
        {
            // Test strings which have newlines mixed in between data pairs.

            RowConfigReader rcr = new RowConfigReader(ConfigData);
            Assert.Equal("00", rcr.GetNextValue("value0"));
            Assert.Equal(0, rcr.GetNextValueAsInt32("value0"));
            Assert.Equal(1, rcr.GetNextValueAsInt32("value1"));
            Assert.Equal("2", rcr.GetNextValue("value2"));
            Assert.Equal("3", rcr.GetNextValue("value3"));

            string unused;
            Assert.False(rcr.TryGetNextValue("Any", out unused));
        }

        private const string BasicData =
@"stringKey stringVal
intKey 12
";

        private const string ConfigData =
@"value0 00
value0 0
value1 1
value2 2
value3 3
";

        private const string ConfigDataExtraNewlines =
@"value0 00




value0 0


value1 1
value2 2





value3 3




";

        private const string ConfigDataNoTrailingNewline =
@"value0 00
value0 0
value1 1
value2 2
value3 3";
    }
}
