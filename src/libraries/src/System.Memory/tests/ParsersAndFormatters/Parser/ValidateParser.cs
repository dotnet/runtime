// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace System.Buffers.Text.Tests
{
    public static partial class ParserTests
    {
        private static void ValidateParser<T>(ParserTestData<T> testData)
        {
            ValidateParserHelper(testData);

            if (testData.ExpectedSuccess)
            {
                // Add several bytes of junk after the real text to ensure the parser successfully ignores it. By adding lots of junk, this also
                // exercises the "string is too long to rule out overflow based on length" paths of the integer parsers.
                ParserTestData<T> testDataWithExtraCharacter = new ParserTestData<T>(testData.Text + "$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$", testData.ExpectedValue, testData.FormatSymbol, expectedSuccess: true)
                {
                    ExpectedBytesConsumed = testData.ExpectedBytesConsumed
                };
                ValidateParserHelper(testDataWithExtraCharacter);
            }
        }

        private static void ValidateParserHelper<T>(ParserTestData<T> testData)
        {
            ReadOnlySpan<byte> utf8Text = testData.Text.ToUtf8Span();
            bool success = TryParseUtf8<T>(utf8Text, out T value, out int bytesConsumed, testData.FormatSymbol);
            if (testData.ExpectedSuccess)
            {
                if (!success)
                    throw new TestException($"This parse attempt {testData} was expected to succeed: instead, it failed.");

                T expected = testData.ExpectedValue;
                T actual = value;
                if (!IsParsedValueEqual<T>(expected: expected, actual: actual))
                {
                    string expectedString = expected.DisplayString();
                    string actualString = actual.DisplayString();
                    throw new TestException($"This parse attempt {testData} succeeded as expected but parsed to the wrong value:\n  Expected: {expectedString}\n  Actual:   {actualString}\n");
                }

                int expectedBytesConsumed = testData.ExpectedBytesConsumed;
                if (expectedBytesConsumed != bytesConsumed)
                    throw new TestException($"This parse attempt {testData} returned the correct value but the wrong `bytesConsumed` value:\n  Expected: {expectedBytesConsumed}\n  Actual:   {bytesConsumed}\n");
            }
            else
            {
                if (success)
                    throw new TestException($"This parse attempt {testData} was expected to fail: instead, it succeeded and returned {value}.");

                if (bytesConsumed != 0)
                    throw new TestException($"This parse attempt {testData} failed as expected but did not set `bytesConsumed` to 0");

                if (!(value.Equals(default(T))))
                    throw new TestException($"This parse attempt {testData} failed as expected but did not set `value` to default(T)");
            }
        }

        private static bool IsParsedValueEqual<T>(T expected, T actual)
        {
            if (typeof(T) == typeof(decimal))
            {
                int[] expectedBits = decimal.GetBits((decimal)(object)expected);
                int[] actualBits = decimal.GetBits((decimal)(object)actual);
                if (!expectedBits.SequenceEqual(actualBits))
                {
                    // Do not simplify into "return expectedBits.SequenceEqual()". I want to be able to put a breakpoint here.
                    return false;
                }
                return true;
            }

            if (typeof(T) == typeof(DateTime))
            {
                DateTime expectedDateTime = (DateTime)(object)expected;
                DateTime actualDateTime = (DateTime)(object)actual;

                if (expectedDateTime.Kind != actualDateTime.Kind)
                    return false;

                if (expectedDateTime.Ticks != actualDateTime.Ticks)
                    return false;

                return true;
            }

            if (typeof(T) == typeof(DateTimeOffset))
            {
                DateTimeOffset expectedDateTimeOffset = (DateTimeOffset)(object)expected;
                DateTimeOffset actualDateTimeOffset = (DateTimeOffset)(object)actual;

                if (expectedDateTimeOffset.Offset != actualDateTimeOffset.Offset)
                    return false;

                if (!IsParsedValueEqual<DateTime>(expectedDateTimeOffset.DateTime, actualDateTimeOffset.DateTime))
                    return false;

                return true;
            }

            // Parsed floating points are constructed, not computed. Thus, we can do the exact compare.
            if (typeof(T) == typeof(double))
            {
                double expectedDouble = (double)(object)expected;
                double actualDouble = (double)(object)actual;

                unsafe
                {
                    if (*((ulong*)&expectedDouble) != *((ulong*)&actualDouble))
                        return false;

                    return true;
                }
            }

            // Parsed floating points are constructed, not computed. Thus, we can do the exact compare.
            if (typeof(T) == typeof(float))
            {
                float expectedSingle = (float)(object)expected;
                float actualSingle = (float)(object)actual;

                unsafe
                {
                    if (*((uint*)&expectedSingle) != *((uint*)&actualSingle))
                        return false;

                    return true;
                }
            }

            return expected.Equals(actual);
        }

        private static bool TryParseUtf8<T>(ReadOnlySpan<byte> text, out T value, out int bytesConsumed, char format)
        {
            if (typeof(T) == typeof(bool))
            {
                bool success = Utf8Parser.TryParse(text, out bool v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(sbyte))
            {
                bool success = Utf8Parser.TryParse(text, out sbyte v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(byte))
            {
                bool success = Utf8Parser.TryParse(text, out byte v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(short))
            {
                bool success = Utf8Parser.TryParse(text, out short v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(ushort))
            {
                bool success = Utf8Parser.TryParse(text, out ushort v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(int))
            {
                bool success = Utf8Parser.TryParse(text, out int v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(uint))
            {
                bool success = Utf8Parser.TryParse(text, out uint v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(long))
            {
                bool success = Utf8Parser.TryParse(text, out long v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(ulong))
            {
                bool success = Utf8Parser.TryParse(text, out ulong v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(decimal))
            {
                bool success = Utf8Parser.TryParse(text, out decimal v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(Guid))
            {
                bool success = Utf8Parser.TryParse(text, out Guid v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(float))
            {
                bool success = Utf8Parser.TryParse(text, out float v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(double))
            {
                bool success = Utf8Parser.TryParse(text, out double v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(DateTime))
            {
                bool success = Utf8Parser.TryParse(text, out DateTime v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(DateTimeOffset))
            {
                bool success = Utf8Parser.TryParse(text, out DateTimeOffset v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            if (typeof(T) == typeof(TimeSpan))
            {
                bool success = Utf8Parser.TryParse(text, out TimeSpan v, out bytesConsumed, format);
                value = (T)(object)v;
                return success;
            }

            throw new Exception("Unsupported type " + typeof(T));
        }

        private static bool TryParseUtf8(Type type, ReadOnlySpan<byte> text, out object value, out int bytesConsumed, char format)
        {
            if (type == typeof(bool))
            {
                bool success = Utf8Parser.TryParse(text, out bool v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(sbyte))
            {
                bool success = Utf8Parser.TryParse(text, out sbyte v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(byte))
            {
                bool success = Utf8Parser.TryParse(text, out byte v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(short))
            {
                bool success = Utf8Parser.TryParse(text, out short v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(ushort))
            {
                bool success = Utf8Parser.TryParse(text, out ushort v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(int))
            {
                bool success = Utf8Parser.TryParse(text, out int v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(uint))
            {
                bool success = Utf8Parser.TryParse(text, out uint v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(long))
            {
                bool success = Utf8Parser.TryParse(text, out long v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(ulong))
            {
                bool success = Utf8Parser.TryParse(text, out ulong v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(decimal))
            {
                bool success = Utf8Parser.TryParse(text, out decimal v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(Guid))
            {
                bool success = Utf8Parser.TryParse(text, out Guid v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(float))
            {
                bool success = Utf8Parser.TryParse(text, out float v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(double))
            {
                bool success = Utf8Parser.TryParse(text, out double v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(DateTime))
            {
                bool success = Utf8Parser.TryParse(text, out DateTime v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(DateTimeOffset))
            {
                bool success = Utf8Parser.TryParse(text, out DateTimeOffset v, out bytesConsumed, format);
                value = v;
                return success;
            }

            if (type == typeof(TimeSpan))
            {
                bool success = Utf8Parser.TryParse(text, out TimeSpan v, out bytesConsumed, format);
                value = v;
                return success;
            }

            throw new Exception("Unsupported type " + type);
        }
    }
}

