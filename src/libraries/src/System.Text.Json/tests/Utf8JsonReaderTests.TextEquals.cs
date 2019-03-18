// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class Utf8JsonReaderTests
    {
        public static bool IsX64 { get; } = IntPtr.Size >= 8;

        [Fact]
        public static void TestTextEqualsBasic()
        {
            byte[] connectionId = Encoding.UTF8.GetBytes("connectionId");
            byte[] availableTransports = Encoding.UTF8.GetBytes("availableTransports");
            byte[] value123 = Encoding.UTF8.GetBytes("123");
            byte[] embeddedQuotes = Encoding.UTF8.GetBytes("My name is \"Ahson\"");
            bool foundId = false;
            bool foundTransports = false;
            bool foundValue = false;
            bool foundArrayValue = false;

            string jsonString = "{\"conne\\u0063tionId\":\"123\",\"availableTransports\":[\"My name is \\\"Ahson\\\"\"]}";
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.PropertyName)
                {
                    if (json.TextEquals(connectionId) && json.TextEquals("connectionId".AsSpan()))
                    {
                        foundId = true;
                    }
                    else if (json.TextEquals(availableTransports) && json.TextEquals("availableTransports".AsSpan()))
                    {
                        foundTransports = true;
                    }
                }
                else if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(value123) && json.TextEquals("123".AsSpan()))
                    {
                        foundValue = true;
                    }
                    else if (json.TextEquals(embeddedQuotes) && json.TextEquals("My name is \"Ahson\"".AsSpan()))
                    {
                        foundArrayValue = true;
                    }
                }
            }

            Assert.True(foundId);
            Assert.True(foundTransports);
            Assert.True(foundValue);
            Assert.True(foundArrayValue);
        }

        [Theory]
        [InlineData("{\"name\": \"John\"}", false)]
        [InlineData("{\"name\": \"\"}", true)]
        [InlineData("{\"name\": \"Joh\\u006e\"}", false)]
        public static void TextEqualDefault(string jsonString, bool expectedFound)
        {
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    Assert.Equal(expectedFound, json.TextEquals(default(ReadOnlySpan<byte>)));
                    Assert.Equal(expectedFound, json.TextEquals(default(ReadOnlySpan<char>)));
                    break;
                }
            }

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8Data, 1);

            json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    Assert.Equal(expectedFound, json.TextEquals(default(ReadOnlySpan<byte>)));
                    Assert.Equal(expectedFound, json.TextEquals(default(ReadOnlySpan<char>)));
                    break;
                }
            }
        }

        [Theory]
        [InlineData("{\"name\": 1234}", "name", true)]
        [InlineData("{\"name\": 1234}", "namee", false)]
        [InlineData("{\"name\": 1234}", "na\\u006de", false)]
        [InlineData("{\"name\": 1234}", "", false)]
        [InlineData("{\"\": 1234}", "name", false)]
        [InlineData("{\"\": 1234}", "na\\u006de", false)]
        [InlineData("{\"\": 1234}", "", true)]
        [InlineData("{\"na\\u006de\": 1234}", "name", true)]
        [InlineData("{\"na\\u006de\": 1234}", "namee", false)]
        [InlineData("{\"na\\u006de\": 1234}", "na\\u006de", false)]
        [InlineData("{\"na\\u006de\": 1234}", "", false)]
        public static void TestTextEquals(string jsonString, string lookUpString, bool expectedFound)
        {
            byte[] lookup = Encoding.UTF8.GetBytes(lookUpString);
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);
            bool found = false;

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.PropertyName)
                {
                    if (json.TextEquals(lookup) && json.TextEquals(lookUpString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.Equal(expectedFound, found);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8Data, 1);
            found = false;

            json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.PropertyName)
                {
                    if (json.TextEquals(lookup) && json.TextEquals(lookUpString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.Equal(expectedFound, found);
        }

        [Theory]
        [InlineData("{\"name\": \"John\"}", "John", true)]
        [InlineData("{\"name\": \"John\"}", "Johna", false)]
        [InlineData("{\"name\": \"John\"}", "Joh\\u006e", false)]
        [InlineData("{\"name\": \"John\"}", "", false)]
        [InlineData("{\"name\": \"\"}", "John", false)]
        [InlineData("{\"name\": \"\"}", "Joh\\u006e", false)]
        [InlineData("{\"name\": \"\"}", "", true)]
        [InlineData("{\"name\": \"Joh\\u006e\"}", "John", true)]
        [InlineData("{\"name\": \"Joh\\u006e\"}", "Johna", false)]
        [InlineData("{\"name\": \"Joh\\u006e\"}", "Joh\\u006e", false)]
        [InlineData("{\"name\": \"Joh\\u006e\"}", "", false)]
        public static void TestTextEqualsValue(string jsonString, string lookUpString, bool expectedFound)
        {
            byte[] lookup = Encoding.UTF8.GetBytes(lookUpString);
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);
            bool found = false;

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(lookup) && json.TextEquals(lookUpString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.Equal(expectedFound, found);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8Data, 1);
            found = false;

            json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(lookup) && json.TextEquals(lookUpString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.Equal(expectedFound, found);
        }

        [Fact]
        public static void TestTextEqualsLargeMatch()
        {
            var jsonChars = new char[320];  // Some value larger than 256 (stack threshold)
            jsonChars.AsSpan().Fill('a');
            byte[] lookup = Encoding.UTF8.GetBytes(jsonChars);

            ReadOnlySpan<char> escapedA = new char[6] { '\\', 'u', '0', '0', '6', '1' };

            ReadOnlySpan<byte> lookupSpan = lookup.AsSpan(0, lookup.Length - escapedA.Length + 1);   // remove extra characters that were replaced by escaped bytes
            Span<char> lookupChars = new char[jsonChars.Length];
            jsonChars.CopyTo(lookupChars);
            lookupChars = lookupChars.Slice(0, lookupChars.Length - escapedA.Length + 1);

            // Replacing 'a' with '\u0061', so a net change of 5.
            // escapedA.Length - 1 = 6 - 1 = 5
            for (int i = 0; i < jsonChars.Length - escapedA.Length + 1; i++)
            {
                jsonChars.AsSpan().Fill('a');
                escapedA.CopyTo(jsonChars.AsSpan(i));
                string jsonString = "\"" + new string(jsonChars) + "\"";
                byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

                bool found = false;

                var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
                while (json.Read())
                {
                    if (json.TokenType == JsonTokenType.String)
                    {
                        if (json.TextEquals(lookupSpan) && json.TextEquals(lookupChars))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                Assert.True(found, $"Json String: {jsonString}");

                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8Data, 1);
                found = false;

                json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
                while (json.Read())
                {
                    if (json.TokenType == JsonTokenType.String)
                    {
                        if (json.TextEquals(lookupSpan) && json.TextEquals(lookupChars))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                Assert.True(found, $"Json String: {jsonString}  | Look up: {Encoding.UTF8.GetString(lookupSpan.ToArray())}");
            }
        }

        [Fact]
        public static void TestTextEqualsLargeMismatch()
        {
            var jsonChars = new char[320];  // Some value larger than 256 (stack threshold)
            jsonChars.AsSpan().Fill('a');
            ReadOnlySpan<char> escapedA = new char[6] { '\\', 'u', '0', '0', '6', '1' };

            byte[] originalLookup = Encoding.UTF8.GetBytes(jsonChars);

            char[] originalLookupChars = new char[jsonChars.Length];
            Array.Copy(jsonChars, originalLookupChars, jsonChars.Length);

            for (int i = 1; i < jsonChars.Length - 6; i++)
            {
                jsonChars.AsSpan().Fill('a');
                escapedA.CopyTo(jsonChars.AsSpan(i));
                string jsonString = "\"" + new string(jsonChars) + "\"";
                byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

                for (int j = 0; j < 3; j++)
                {
                    Span<byte> lookup = new byte[originalLookup.Length];
                    originalLookup.CopyTo(lookup);
                    lookup = lookup.Slice(0, lookup.Length - escapedA.Length + 1);    // remove extra characters that were replaced by escaped bytes

                    Span<char> lookupChars = new char[originalLookupChars.Length];
                    originalLookupChars.CopyTo(lookupChars);
                    lookupChars = lookupChars.Slice(0, lookupChars.Length - escapedA.Length + 1);    // remove extra characters that were replaced by escaped bytes

                    switch (j)
                    {
                        case 0:
                            lookup[i] = (byte)'b';
                            lookupChars[i] = 'b';
                            break;
                        case 1:
                            lookup[i + 1] = (byte)'b';
                            lookupChars[i + 1] = 'b';
                            break;
                        case 2:
                            lookup[i - 1] = (byte)'b';
                            lookupChars[i - 1] = 'b';
                            break;
                    }

                    bool found = false;

                    var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
                    while (json.Read())
                    {
                        if (json.TokenType == JsonTokenType.String)
                        {
                            if (json.TextEquals(lookup) || json.TextEquals(lookupChars))
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    Assert.False(found, $"Json String: {jsonString}");

                    ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8Data, 1);
                    found = false;

                    json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
                    while (json.Read())
                    {
                        if (json.TokenType == JsonTokenType.String)
                        {
                            if (json.TextEquals(lookup) || json.TextEquals(lookupChars))
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    Assert.False(found);
                }
            }
        }

        [Theory]
        [InlineData("\"\\u0061\\u0061\"")]
        [InlineData("\"aaaaaaaaaaaa\"")]
        public static void TestTextEqualsTooSmallToMatch(string jsonString)
        {
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            bool found = false;

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(new byte[] { (byte)'a' }) || json.TextEquals(new char[] { 'a' }))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.False(found);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8Data, 1);
            found = false;

            json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(new byte[] { (byte)'a' }) || json.TextEquals(new char[] { 'a' }))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.False(found);
        }

        [Theory]
        [InlineData("\"\\u0061\\u0061\"")]
        [InlineData("\"aaaaaaaaaaaa\"")]
        public static void TestTextEqualsTooLargeToMatch(string jsonString)
        {
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            var lookupString = new string('a', 13);

            bool found = false;

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(Encoding.UTF8.GetBytes(lookupString)) || json.TextEquals(lookupString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.False(found);

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8Data, 1);
            found = false;

            json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(Encoding.UTF8.GetBytes(lookupString)) || json.TextEquals(lookupString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.False(found);
        }

        [Theory]
        [InlineData("\"aaabbb\"", "aaaaaa")]
        [InlineData("\"bbbaaa\"", "aaaaaa")]
        public static void TextMismatchSameLength(string jsonString, string lookupString)
        {
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            bool found = false;

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(Encoding.UTF8.GetBytes(lookupString)) || json.TextEquals(lookupString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.False(found);

            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(utf8Data);
            found = false;

            json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(Encoding.UTF8.GetBytes(lookupString)) || json.TextEquals(lookupString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.False(found);
        }

        [Fact]
        public static void TextEqualsEscapedCharAtTheLastSegment()
        {
            string jsonString = "\"aaaaaa\\u0061\"";
            string lookupString = "aaaaaaa";
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            bool found = false;

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(Encoding.UTF8.GetBytes(lookupString)) || json.TextEquals(lookupString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.True(found);

            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(utf8Data);
            found = false;

            json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(Encoding.UTF8.GetBytes(lookupString)) || json.TextEquals(lookupString.AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.True(found);
        }

        [Fact]
        public static void TestTextEqualsMismatchMultiSegment()
        {
            string jsonString = "\"Hi, \\\"Ahson\\\"!\"";
            byte[] lookup = Encoding.UTF8.GetBytes("Hello, \"Ahson\"");
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);
            bool found = false;

            // Segment 1: "Hi, \"A
            // Segment 2: hson\"!"
            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(utf8Data);

            var json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(lookup) || json.TextEquals("Hello, \"Ahson\"".AsSpan()))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.False(found);
        }

        [Theory]
        [InlineData("\"hello\"", new char[1] { (char)0xDC01 })]    // low surrogate - invalid
        [InlineData("\"hello\"", new char[1] { (char)0xD801 })]    // high surrogate - missing pair
        public static void InvalidUTF16Search(string jsonString, char[] lookup)
        {
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);
            bool found = false;

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.TextEquals(lookup))
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.False(found);
        }

        [Fact]
        public static void LargeLookupUTF16()
        {
            string jsonString = "\"hello\"";
            string lookup = new string('a', 1_000);
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            Assert.True(json.Read());
            Assert.Equal(JsonTokenType.String, json.TokenType);
            Assert.False(json.TextEquals(lookup.AsSpan()));
        }

        [Fact]
        public static void LargeLookupUTF8()
        {
            string jsonString = "\"hello\"";
            byte[] lookup = new byte[1_000];
            lookup.AsSpan().Fill((byte)'a');
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            Assert.True(json.Read());
            Assert.Equal(JsonTokenType.String, json.TokenType);
            Assert.False(json.TextEquals(lookup));
        }

        [ConditionalFact(nameof(IsX64))]
        [OuterLoop]
        public static void LookupOverflow()
        {
            char[] jsonString = new char[400_000_002];

            jsonString.AsSpan().Fill('a');
            jsonString[0] = '"';
            jsonString[jsonString.Length - 1] = '"';

            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            Assert.True(json.Read());
            Assert.Equal(JsonTokenType.String, json.TokenType);

            try
            {
                json.TextEquals(jsonString.AsSpan(1, jsonString.Length - 2));
                Assert.True(false, $"Expected OverflowException was not thrown when calling TextEquals with large lookup string");
            }
            catch (OverflowException)
            { }
        }

        [Theory]
        [InlineData("/*comment*/[1234, true, false, /*comment*/ null, {}]/*comment*/")]
        public static void TestTextEqualsInvalid(string jsonString)
        {
            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state);

            try
            {
                json.TextEquals(default(ReadOnlySpan<byte>));
                Assert.True(false, $"Expected InvalidOperationException was not thrown when calling TextEquals with TokenType = {json.TokenType}");
            }
            catch (InvalidOperationException)
            { }

            try
            {
                json.TextEquals(default(ReadOnlySpan<char>));
                Assert.True(false, $"Expected InvalidOperationException was not thrown when calling TextEquals(char) with TokenType = {json.TokenType}");
            }
            catch (InvalidOperationException)
            { }

            while (json.Read())
            {
                try
                {
                    json.TextEquals(default(ReadOnlySpan<byte>));
                    Assert.True(false, $"Expected InvalidOperationException was not thrown when calling TextEquals with TokenType = {json.TokenType}");
                }
                catch (InvalidOperationException)
                { }

                try
                {
                    json.TextEquals(default(ReadOnlySpan<char>));
                    Assert.True(false, $"Expected InvalidOperationException was not thrown when calling TextEquals(char) with TokenType = {json.TokenType}");
                }
                catch (InvalidOperationException)
                { }
            }

            Assert.Equal(utf8Data.Length, json.BytesConsumed);
        }
    }
}
