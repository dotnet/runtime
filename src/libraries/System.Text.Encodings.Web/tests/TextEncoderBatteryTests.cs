// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class TextEncoderBatteryTests
    {
        private static TextEncoder GetBatteryTextEncoder()
        {
            // only even-valued scalars are allowed; odd-valued scalars are disallowed
            return new ConfigurableScalarTextEncoder(scalarValue => scalarValue % 2 == 0);
        }

        // 2 elements: [0] = input data (string), [1] = expected output data (string)
        public static IEnumerable<object[]> TestData()
        {
            static IEnumerable<(string input, string output)> RealTestData()
            {
                yield return ("", "");
                yield return ("xyz", "x[0079]z");
                yield return ("bdf", "bdf");
                yield return ("bdfbdfbdfbdfbdf", "bdfbdfbdfbdfbdf");
                yield return ("\U0001F600" /* grinning face */, "\U0001F600"); // not escaped since scalar value is even
                yield return ("\U0001F601" /* grinning face with smiling eyes */, "[1F601]"); // escaped since scalar value is odd
                yield return ("\U0001F3C0\U0001F3C1\U0001F3C2\U0001F3C3\U0001F3C4" /* various sports emoji */,
                    "\U0001F3C0[1F3C1]\U0001F3C2[1F3C3]\U0001F3C4");
                yield return ("bd\ud800fh", "bd[FFFD]fh"); // standalone high surrogate char
                yield return ("bd\udffffh", "bd[FFFD]fh"); // standalone low surrogate char
                yield return ("bd\ue000fh", "bd\ue000fh");
                yield return ("bd\ue001fh", "bd[E001]fh");
                yield return ("bd\udfd0\ud83c\udfd0\ud83cfh", "bd[FFFD]\U0001F3D0[FFFD]fh"); // U+1F3D0 VOLLEYBALL
                yield return ("bd\udfd1\ud83c\udfd1\ud83cfh", "bd[FFFD][1F3D1][FFFD]fh"); // U+1F3D1 FIELD HOCKEY STICK AND BALL
                yield return ("\ufffd\ud800\ufffd", "[FFFD][FFFD][FFFD]"); // U+FFFD is escaped since is odd
                yield return ("xyz\ud800", "x[0079]z[FFFD]"); // ends with standalone high surrogate char
                yield return ("xyz\udfff", "x[0079]z[FFFD]"); // ends with standalone low surrogate char
                yield return ("xyz\U0001F3C0", "x[0079]z\U0001F3C0"); // ends with valid surrogate pair

                // really long input which does not need to be escaped
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0x40; i < 0x4000; i += 2)
                    {
                        sb.Append((char)i);
                    }

                    yield return (sb.ToString(), sb.ToString());
                }

                // really long input which needs to be escaped
                {
                    StringBuilder sbInput = new StringBuilder();
                    StringBuilder sbOutput = new StringBuilder();

                    for (int i = 0x40; i < 0x4000; i++)
                    {
                        sbInput.Append((char)i);
                        if (i % 2 == 0)
                        {
                            sbOutput.Append((char)i);
                        }
                        else
                        {
                            sbOutput.Append($"[{i:X4}]");
                        }
                    }

                    yield return (sbInput.ToString(), sbOutput.ToString());
                }

                // really long input which contains surrogate chars (no escape needed)
                // also offset everything by 1 to account for the TextEncoder inner loop's
                // "needs more data" handling logic.
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0x10000; i < 0x14000; i += 2)
                    {
                        sb.Append(char.ConvertFromUtf32(i));
                    }

                    yield return (sb.ToString(), sb.ToString());
                    yield return ("x" + sb.ToString(), "x" + sb.ToString());
                }
            }

            foreach ((string input, string output) in RealTestData())
            {
                yield return new[] { Escape(input), Escape(output) };
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Encode_String(string input, string expectedOutput)
        {
            input = Unescape(input);
            expectedOutput = Unescape(expectedOutput);

            // Arrange

            TextEncoder encoder = GetBatteryTextEncoder();

            // Act

            string actualOutput = encoder.Encode(input);

            // Assert

            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Encode_TextWriter_String(string input, string expectedOutput)
        {
            input = Unescape(input);
            expectedOutput = Unescape(expectedOutput);

            // Arrange

            TextEncoder encoder = GetBatteryTextEncoder();
            StringWriter writer = new StringWriter();

            // Act

            encoder.Encode(writer, input);

            // Assert

            Assert.Equal(expectedOutput, writer.ToString());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Encode_TextWriter_String_WithOffset(string input, string expectedOutput)
        {
            input = Unescape(input);
            expectedOutput = Unescape(expectedOutput);

            // Arrange

            TextEncoder encoder = GetBatteryTextEncoder();
            StringWriter writer;

            // Act & assert - 1

            writer = new StringWriter();
            encoder.Encode(writer, input, 0, input.Length);
            Assert.Equal(expectedOutput, writer.ToString());

            // Act & assert - 2

            writer = new StringWriter();
            encoder.Encode(writer, "xxx" + input + "yyy", 3, input.Length);
            Assert.Equal(expectedOutput, writer.ToString());

            // Act & assert - 3

            writer = new StringWriter();
            encoder.Encode(writer, "\ud800" + input + "\udfff", 1, input.Length);
            Assert.Equal(expectedOutput, writer.ToString());
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Encode_TextWriter_CharArray_WithOffset(string input, string expectedOutput)
        {
            input = Unescape(input);
            expectedOutput = Unescape(expectedOutput);

            // Arrange

            TextEncoder encoder = GetBatteryTextEncoder();
            StringWriter writer;

            // Act & assert - 1

            writer = new StringWriter();
            encoder.Encode(writer, input.ToCharArray(), 0, input.Length);
            Assert.Equal(expectedOutput, writer.ToString());

            // Act & assert - 2

            writer = new StringWriter();
            encoder.Encode(writer, ("xxx" + input + "yyy").ToCharArray(), 3, input.Length);
            Assert.Equal(expectedOutput, writer.ToString());

            // Act & assert - 3

            writer = new StringWriter();
            encoder.Encode(writer, ("\ud800" + input + "\udfff").ToCharArray(), 1, input.Length);
            Assert.Equal(expectedOutput, writer.ToString());
        }

        /*
         * ESCAPING & UNESCAPING
         * =====================
         *
         * The xunit runner doesn't like strings that contain malformed UTF-16 data.
         * To smuggle malformed UTF-16 data across the test runner, we'll encode all surrogate
         * chars (not supplementary chars) as @XXXX. A supplementary char is thus represented
         * as @XXXX@YYYY (10 chars total) in the stream.
         */

        private static string Escape(string value)
        {
            value = value.Replace(@"@", @"@0040");
            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                sb.Append(char.IsSurrogate(ch) ? FormattableString.Invariant($@"@{(int)ch:X4}") : ch);
            }
            return sb.ToString();
        }

        private static string Unescape(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch != '@')
                {
                    sb.Append(ch);
                }
                else
                {
                    sb.Append((char)ushort.Parse(value.Substring(i + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                    i += 4;
                }
            }
            return sb.ToString();
        }
    }
}
