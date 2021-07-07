// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class Utf8JsonReaderTests
    {
        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.ValidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsConversionToDateTime(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTime expected = DateTime.Parse(expectedString);

                    Assert.True(json.TryGetDateTime(out DateTime actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTime());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.ValidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsConversionToDateTimeOffset(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTimeOffset expected = DateTimeOffset.Parse(expectedString);

                    Assert.True(json.TryGetDateTimeOffset(out DateTimeOffset actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTimeOffset());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.ValidISO8601TestsWithUtcOffset), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsWithUTCOffsetToDateTime(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTime expected = DateTime.ParseExact(expectedString, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                    Assert.True(json.TryGetDateTime(out DateTime actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTime());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.ValidISO8601TestsWithUtcOffset), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsWithUTCOffsetToDateTimeOffset(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTimeOffset expected = DateTimeOffset.ParseExact(expectedString, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                    Assert.True(json.TryGetDateTimeOffset(out DateTimeOffset actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTimeOffset());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.InvalidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsInvalidConversionToDateTime(string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                Assert.False(json.TryGetDateTime(out DateTime actualDateTime));
                Assert.Equal(default, actualDateTime);

                try
                {
                    DateTime value = json.GetDateTime();
                    Assert.True(false, "Expected GetDateTime to throw FormatException due to invalid ISO 8601 input.");
                }
                catch (FormatException)
                { }
            }
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.InvalidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsInvalidConversionToDateTimeOffset(string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    Assert.False(json.TryGetDateTimeOffset(out DateTimeOffset actualDateTime));
                    Assert.Equal(default, actualDateTime);

                    try
                    {
                        DateTimeOffset value = json.GetDateTimeOffset();
                        Assert.True(false, "Expected GetDateTimeOffset to throw FormatException due to invalid ISO 8601 input.");
                    }
                    catch (FormatException)
                    { }
                }
            }
        }

        [Fact]
        // https://github.com/dotnet/runtime/issues/30095
        public static void TestingDateTimeMinValue_UtcOffsetGreaterThan0()
        {
            string jsonString = @"""0001-01-01T00:00:00""";
            string expectedString = "0001-01-01T00:00:00";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTime expected = DateTime.Parse(expectedString);

                    Assert.True(json.TryGetDateTime(out DateTime actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTime());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);

            // Test upstream serializer.
            Assert.Equal(DateTime.Parse(expectedString), JsonSerializer.Deserialize<DateTime>(jsonString));
        }

        [Fact]
        public static void TestingDateTimeMaxValue()
        {
            string jsonString = @"""9999-12-31T23:59:59""";
            string expectedString = "9999-12-31T23:59:59";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTime expected = DateTime.Parse(expectedString);

                    Assert.True(json.TryGetDateTime(out DateTime actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTime());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);

            // Test upstream serializer.
            Assert.Equal(DateTime.Parse(expectedString), JsonSerializer.Deserialize<DateTime>(jsonString));
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.InvalidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TryGetDateTime_HasValueSequence_False(string testString)
        {
            static void test(string testString, bool isFinalBlock)
            {
                byte[] dataUtf8 = Encoding.UTF8.GetBytes(testString);
                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);
                var json = new Utf8JsonReader(sequence, isFinalBlock: isFinalBlock, state: default);

                Assert.True(json.Read(), "json.Read()");
                Assert.Equal(JsonTokenType.String, json.TokenType);
                Assert.True(json.HasValueSequence, "json.HasValueSequence");
                // If the string is empty, the ValueSequence is empty, because it contains all 0 bytes between the two characters
                Assert.Equal(string.IsNullOrEmpty(testString), json.ValueSequence.IsEmpty);
                Assert.False(json.TryGetDateTime(out DateTime actual), "json.TryGetDateTime(out DateTime actual)");
                Assert.Equal(DateTime.MinValue, actual);

                JsonTestHelper.AssertThrows<FormatException>(json, (jsonReader) => jsonReader.GetDateTime());
            }

            test(testString, isFinalBlock: true);
            test(testString, isFinalBlock: false);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.InvalidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TryGetDateTimeOffset_HasValueSequence_False(string testString)
        {
            static void test(string testString, bool isFinalBlock)
            {
                byte[] dataUtf8 = Encoding.UTF8.GetBytes(testString);
                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);
                var json = new Utf8JsonReader(sequence, isFinalBlock: isFinalBlock, state: default);

                Assert.True(json.Read(), "json.Read()");
                Assert.Equal(JsonTokenType.String, json.TokenType);
                Assert.True(json.HasValueSequence, "json.HasValueSequence");
                // If the string is empty, the ValueSequence is empty, because it contains all 0 bytes between the two characters
                Assert.Equal(string.IsNullOrEmpty(testString), json.ValueSequence.IsEmpty);
                Assert.False(json.TryGetDateTimeOffset(out DateTimeOffset actual), "json.TryGetDateTimeOffset(out DateTimeOffset actual)");
                Assert.Equal(DateTimeOffset.MinValue, actual);

                JsonTestHelper.AssertThrows<FormatException>(json, (jsonReader) => jsonReader.GetDateTimeOffset());
            }

            test(testString, isFinalBlock: true);
            test(testString, isFinalBlock: false);
        }
    }
}
