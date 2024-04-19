// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class Utf8JsonReaderTests
    {
        [Fact]
        public static void TestTextEqualsBasic()
        {
            bool foundId = false;
            bool foundTransports = false;
            bool foundValue = false;
            bool foundArrayValue = false;

            var json = new Utf8JsonReader("{\"conne\\u0063tionId\":\"123\",\"availableTransports\":[\"My name is \\\"Ahson\\\"\"]}"u8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.PropertyName)
                {
                    if (json.ValueTextEquals("connectionId"u8) && json.ValueTextEquals("connectionId".AsSpan()))
                    {
                        foundId = true;
                    }
                    else if (json.ValueTextEquals("availableTransports"u8) && json.ValueTextEquals("availableTransports".AsSpan()))
                    {
                        foundTransports = true;
                    }
                }
                else if (json.TokenType == JsonTokenType.String)
                {
                    if (json.ValueTextEquals("123"u8) && json.ValueTextEquals("123".AsSpan()))
                    {
                        foundValue = true;
                    }
                    else if (json.ValueTextEquals("My name is \"Ahson\""u8) && json.ValueTextEquals("My name is \"Ahson\"".AsSpan()))
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
                    Assert.Equal(expectedFound, json.ValueTextEquals(default(ReadOnlySpan<byte>)));
                    Assert.Equal(expectedFound, json.ValueTextEquals(default(ReadOnlySpan<char>)));
                    Assert.Equal(expectedFound, json.ValueTextEquals(default(string)));
                    break;
                }
            }

            ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(utf8Data, 1);

            json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    Assert.Equal(expectedFound, json.ValueTextEquals(default(ReadOnlySpan<byte>)));
                    Assert.Equal(expectedFound, json.ValueTextEquals(default(ReadOnlySpan<char>)));
                    Assert.Equal(expectedFound, json.ValueTextEquals(default(string)));
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
                    if (json.ValueTextEquals(lookup) &&
                        json.ValueTextEquals(lookUpString) &&
                        json.ValueTextEquals(lookUpString.AsSpan()))
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
                    if (json.ValueTextEquals(lookup) &&
                        json.ValueTextEquals(lookUpString) &&
                        json.ValueTextEquals(lookUpString.AsSpan()))
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
                    if (json.ValueTextEquals(lookup) &&
                        json.ValueTextEquals(lookUpString) &&
                        json.ValueTextEquals(lookUpString.AsSpan()))
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
                    if (json.ValueTextEquals(lookup) &&
                        json.ValueTextEquals(lookUpString) &&
                        json.ValueTextEquals(lookUpString.AsSpan()))
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
                        if (json.ValueTextEquals(lookupSpan) &&
                            json.ValueTextEquals(lookupChars) &&
                            json.ValueTextEquals(new string(lookupChars.ToArray())))
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
                        if (json.ValueTextEquals(lookupSpan) &&
                            json.ValueTextEquals(lookupChars) &&
                            json.ValueTextEquals(new string(lookupChars.ToArray())))
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
                            if (json.ValueTextEquals(lookup) ||
                                json.ValueTextEquals(lookupChars) ||
                                json.ValueTextEquals(new string(lookupChars.ToArray())))
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
                            if (json.ValueTextEquals(lookup) ||
                                json.ValueTextEquals(lookupChars) ||
                                json.ValueTextEquals(new string(lookupChars.ToArray())))
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
                    if (json.ValueTextEquals(new byte[] { (byte)'a' }) ||
                        json.ValueTextEquals(new char[] { 'a' }) ||
                        json.ValueTextEquals("a"))
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
                    if (json.ValueTextEquals(new byte[] { (byte)'a' }) ||
                        json.ValueTextEquals(new char[] { 'a' }) ||
                        json.ValueTextEquals("a"))
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
                    if (json.ValueTextEquals(Encoding.UTF8.GetBytes(lookupString)) ||
                        json.ValueTextEquals(lookupString.AsSpan()) ||
                        json.ValueTextEquals(lookupString))
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
                    if (json.ValueTextEquals(Encoding.UTF8.GetBytes(lookupString)) ||
                        json.ValueTextEquals(lookupString.AsSpan()) ||
                        json.ValueTextEquals(lookupString))
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
                    if (json.ValueTextEquals(Encoding.UTF8.GetBytes(lookupString)) ||
                        json.ValueTextEquals(lookupString.AsSpan()) ||
                        json.ValueTextEquals(lookupString))
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
                    if (json.ValueTextEquals(Encoding.UTF8.GetBytes(lookupString)) ||
                        json.ValueTextEquals(lookupString.AsSpan()) ||
                        json.ValueTextEquals(lookupString))
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
                    if (json.ValueTextEquals(Encoding.UTF8.GetBytes(lookupString)) ||
                        json.ValueTextEquals(lookupString.AsSpan()) ||
                        json.ValueTextEquals(lookupString))
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
                    if (json.ValueTextEquals(Encoding.UTF8.GetBytes(lookupString)) ||
                        json.ValueTextEquals(lookupString.AsSpan()) ||
                        json.ValueTextEquals(lookupString))
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
            byte[] utf8Data = "\"Hi, \\\"Ahson\\\"!\""u8.ToArray();
            bool found = false;

            // Segment 1: "Hi, \"A
            // Segment 2: hson\"!"
            ReadOnlySequence<byte> sequence = JsonTestHelper.CreateSegments(utf8Data);

            var json = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    if (json.ValueTextEquals("Hello, \"Ahson\""u8) ||
                        json.ValueTextEquals("Hello, \"Ahson\"".AsSpan()) ||
                        json.ValueTextEquals("Hello, \"Ahson\""))
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
                    if (json.ValueTextEquals(lookup))
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
            Assert.False(json.ValueTextEquals(lookup.AsSpan()));
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
            Assert.False(json.ValueTextEquals(lookup));
        }

        // NOTE: LookupOverflow test is constrained to run on Windows and MacOSX because it causes
        //       problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can
        //       succeed even if there is not enough memory but then the test may get killed by the OOM killer at the
        //       time the memory is accessed which triggers the full memory allocation.
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))]
        [OuterLoop]
        public static void LookupOverflow()
        {
            char[] jsonString = new char[800_000_002];

            jsonString.AsSpan().Fill('a');
            jsonString[0] = '"';
            jsonString[jsonString.Length - 1] = '"';

            byte[] utf8Data = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(utf8Data, isFinalBlock: true, state: default);
            Assert.True(json.Read());
            Assert.Equal(JsonTokenType.String, json.TokenType);

            try
            {
                json.ValueTextEquals(jsonString.AsSpan(1, jsonString.Length - 2));
                Assert.Fail($"Expected OverflowException was not thrown when calling ValueTextEquals with large lookup string");
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
                json.ValueTextEquals(default(ReadOnlySpan<byte>));
                Assert.Fail($"Expected InvalidOperationException was not thrown when calling ValueTextEquals with TokenType = {json.TokenType}");
            }
            catch (InvalidOperationException)
            { }

            try
            {
                json.ValueTextEquals(default(ReadOnlySpan<char>));
                Assert.Fail($"Expected InvalidOperationException was not thrown when calling ValueTextEquals(char) with TokenType = {json.TokenType}");
            }
            catch (InvalidOperationException)
            { }

            try
            {
                json.ValueTextEquals(default(string));
                Assert.Fail($"Expected InvalidOperationException was not thrown when calling ValueTextEquals(char) with TokenType = {json.TokenType}");
            }
            catch (InvalidOperationException)
            { }

            while (json.Read())
            {
                try
                {
                    json.ValueTextEquals(default(ReadOnlySpan<byte>));
                    Assert.Fail($"Expected InvalidOperationException was not thrown when calling ValueTextEquals with TokenType = {json.TokenType}");
                }
                catch (InvalidOperationException)
                { }

                try
                {
                    json.ValueTextEquals(default(ReadOnlySpan<char>));
                    Assert.Fail($"Expected InvalidOperationException was not thrown when calling ValueTextEquals(char) with TokenType = {json.TokenType}");
                }
                catch (InvalidOperationException)
                { }

                try
                {
                    json.ValueTextEquals(default(string));
                    Assert.Fail($"Expected InvalidOperationException was not thrown when calling ValueTextEquals(char) with TokenType = {json.TokenType}");
                }
                catch (InvalidOperationException)
                { }
            }

            Assert.Equal(utf8Data.Length, json.BytesConsumed);
        }
    }
}
