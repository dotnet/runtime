// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO
{
    public static class RowConfigReaderTests
    {
        [MemberData(nameof(BasicTestData))]
        [Theory]
        public static void BasicTest(string data)
        {
            RowConfigReader rcr = new RowConfigReader(data);
            Assert.Equal("stringVal", rcr.GetNextValue("stringKey"));
            Assert.Equal(int.MaxValue, rcr.GetNextValueAsInt32("intKey"));
            Assert.Equal(long.MaxValue, rcr.GetNextValueAsInt64("longKey"));
        }

        [MemberData(nameof(BasicTestData))]
        [Theory]
        public static void BasicTestWrongCase(string data)
        {
            // Default is Ordinal comparison. Keys with different case should not be found.
            RowConfigReader rcr = new RowConfigReader(data);
            string unused;
            Assert.False(rcr.TryGetNextValue("stringkey", out unused));
            Assert.False(rcr.TryGetNextValue("intkey", out unused));
            Assert.False(rcr.TryGetNextValue("longkey", out unused));
        }

        [MemberData(nameof(BasicTestData))]
        [Theory]
        public static void BasicTestCaseInsensitive(string data)
        {
            // Use a case-insensitive comparer and use differently-cased keys.
            RowConfigReader rcr = new RowConfigReader(data, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("stringVal", rcr.GetNextValue("stringkey"));
            Assert.Equal(int.MaxValue, rcr.GetNextValueAsInt32("intkey"));
            Assert.Equal(long.MaxValue, rcr.GetNextValueAsInt64("longkey"));
        }

        [MemberData(nameof(BasicTestData))]
        [Theory]
        public static void StaticHelper(string data)
        {
            Assert.Equal("stringVal", RowConfigReader.ReadFirstValueFromString(data, "stringKey"));
            Assert.Equal(int.MaxValue.ToString(), RowConfigReader.ReadFirstValueFromString(data, "intKey"));
            Assert.Equal(long.MaxValue.ToString(), RowConfigReader.ReadFirstValueFromString(data, "longKey"));
        }

        [InlineData("key")]
        [InlineData(" key")]
        [InlineData("key ")]
        [InlineData(" key ")]
        [InlineData("\tkey")]
        [InlineData("key\t")]
        [InlineData("\tkey\t")]
        [Theory]
        public static void MalformedLine(string data)
        {
            RowConfigReader rcr = new RowConfigReader(data);
            string unused;
            Assert.False(rcr.TryGetNextValue("key", out unused));
        }

        [Fact]
        public static void KeyDoesNotStartLine()
        {
            string data = $"key value{Environment.NewLine} key2 value2";
            RowConfigReader rcr = new RowConfigReader(data);

            string unused;
            Assert.False(rcr.TryGetNextValue("key2", out unused));

            // Can retrieve value if key includes the preceding space.
            Assert.Equal("value2", rcr.GetNextValue(" key2"));
        }

        [Fact]
        public static void KeyContainedInValue()
        {
            string data = $"first keyFirstValue{Environment.NewLine}key value";
            RowConfigReader rcr = new RowConfigReader(data);
            Assert.Equal("value", rcr.GetNextValue("key"));
        }

        [MemberData(nameof(NewlineTestData))]
        [Theory]
        public static void NewlineTests(string data)
        {
            // Test strings which have newlines mixed in between data pairs.

            RowConfigReader rcr = new RowConfigReader(data);
            Assert.Equal("00", rcr.GetNextValue("value0"));
            Assert.Equal(0, rcr.GetNextValueAsInt32("value0"));
            Assert.Equal(1, rcr.GetNextValueAsInt32("value1"));
            Assert.Equal("2", rcr.GetNextValue("value2"));
            Assert.Equal("3", rcr.GetNextValue("value3"));

            string unused;
            Assert.False(rcr.TryGetNextValue("Any", out unused));
        }

        public static IEnumerable<object[]> BasicTestData()
        {
            yield return new[] { BasicData };
            yield return new[] { BasicDataWithTabs };
        }

        private static string BasicData =>
            $"stringKey stringVal{Environment.NewLine}" +
            $"intKey {int.MaxValue}{Environment.NewLine}" +
            $"longKey {long.MaxValue}{Environment.NewLine}";

        private static string BasicDataWithTabs =>
            $"stringKey\t\t\tstringVal{Environment.NewLine}" +
            $"intKey\t\t{int.MaxValue}{Environment.NewLine}" +
            $"longKey\t\t{long.MaxValue}{Environment.NewLine}";

        public static IEnumerable<object[]> NewlineTestData()
        {
            yield return new[] { ConfigData };
            yield return new[] { ConfigDataExtraNewlines };
            yield return new[] { ConfigDataNoTrailingNewline };
        }

        private static string ConfigData => string.Format(
            "value0 00{0}value0 0{0}value1 1{0}value2 2{0}value3 3{0}",
            Environment.NewLine);

        private static string ConfigDataExtraNewlines =>
            $"value0 00{Newlines(5)}value0 0{Newlines(3)}value1 1{Newlines(1)}value2 2{Newlines(4)}value3 3{Newlines(6)}";

        private static string ConfigDataNoTrailingNewline => string.Format(
            "value0 00{0}value0 0{0}value1 1{0}value2 2{0}value3 3",
            Environment.NewLine);

        private static string Newlines(int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
