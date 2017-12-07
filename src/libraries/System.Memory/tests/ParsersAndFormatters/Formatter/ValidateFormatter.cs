// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Buffers.Text.Tests
{
    public static partial class FormatterTests
    {
        private static void ValidateFormatter<T>(FormatterTestData<T> testData)
        {
            // It's useful to do the over-sized buffer test first. If the formatter api has a bug that generates output text that's longer than
            // the expected string, this test will fail with an "Expected/Actual" diff while the exact-sized buffer test would say "Oops. The api returned 'false' and no output"."
            FormatterTestData<T> oversizedTestData = new FormatterTestData<T>(testData.Value, testData.Format, testData.Precision, testData.ExpectedOutput)
            {
                PassedInBufferLength = testData.ExpectedOutput.Length + 200,
            };
            ValidateFormatterHelper(oversizedTestData);

            // Now run the test with a buffer that's exactly the right size.
            ValidateFormatterHelper(testData);

            // Now run the test with buffers that are too short.
            for (int truncatedBufferLength = 0; truncatedBufferLength < testData.ExpectedOutput.Length; truncatedBufferLength++)
            {
                FormatterTestData<T> newTestData = new FormatterTestData<T>(testData.Value, testData.Format, testData.Precision, testData.ExpectedOutput)
                {
                    PassedInBufferLength = truncatedBufferLength,
                };

                ValidateFormatterHelper(newTestData);
            }
        }

        private static void ValidateFormatterHelper<T>(FormatterTestData<T> testData)
        {
            int spanLength = testData.PassedInBufferLength;
            string expectedOutput = testData.ExpectedOutput;
            int expectedOutputLength = expectedOutput.Length;

            bool expectedSuccess = spanLength >= expectedOutputLength;
            StandardFormat format = new StandardFormat(testData.FormatSymbol, testData.Precision);
            T value = testData.Value;

            const int CanarySize = 100;

            byte[] backingArray = new byte[spanLength + CanarySize];
            Span<byte> span = new Span<byte>(backingArray, 0, spanLength);
            CanaryFill(backingArray);

            bool success = TryFormatUtf8<T>(value, span, out int bytesWritten, format);
            if (success)
            {
                if (!expectedSuccess)
                {
                    if (bytesWritten >= 0 && bytesWritten <= span.Length)
                    {
                        string unexpectedOutput = span.Slice(0, bytesWritten).ToUtf16String();
                        throw new TestException($"This format attempt ({testData}) was expected to fail due to an undersized buffer. Instead, it generated \"{unexpectedOutput}\"");
                    }
                    else
                    {
                        throw new TestException($"This format attempt ({testData}) was expected to fail due to an undersized buffer. Instead, it succeeded but set 'bytesWritten' to an out of range value: {bytesWritten}");
                    }
                }

                if (bytesWritten < 0 || bytesWritten > span.Length)
                {
                    throw new TestException($"This format attempt ({testData}) succeeded as expected but set 'bytesWritten' to an out of range value: {bytesWritten}");
                }

                string actual = span.Slice(0, bytesWritten).ToUtf16String();
                string expected = testData.ExpectedOutput;
                if (actual != expected)
                {
                    // We'll allocate (and not throw) the TestException (so that someone with a breakpoint inside TestException's constructor will break as desired) but 
                    // use Assert.Equals() to trigger the actual failure so we get XUnit's more useful comparison output into the log.
                    new TestException($"This format attempt ({testData}) succeeded as expected but generated the wrong text:\n  Expected: \"{expected}\"\n  Actual:   \"{actual}\"\n");
                    Assert.Equal(expected, actual);
                }

                // If the api scribbled into the Span past the reported 'bytesWritten' (but still within the bounds of the Span itself), this should raise eyebrows at least.
                CanaryCheck(testData, new ReadOnlySpan<byte>(backingArray, span.Length, CanarySize), "the end of the Span itself");

                // If the api scribbled beyond the range of the Span itself, it's panic time.
                CanaryCheck(testData, new ReadOnlySpan<byte>(backingArray, expectedOutputLength, span.Length - expectedOutputLength), "'bytesWritten'");
            }
            else
            {
                if (expectedSuccess)
                {
                    throw new TestException($"This format attempt ({testData}) was expected to succeed. Instead, it failed.");
                }

                if (bytesWritten != 0)
                {
                    throw new TestException($"This format attempt ({testData}) failed as expected but did not set `bytesWritten` to 0");
                }

                // Note: It's not guaranteed that partial (and useless) results will be written to the buffer despite
                // byteWritten being 0. (In particular, ulong values that are larger than long.MaxValue using the "N" format.)
                // We can only check the canary portion for overwrites.
                CanaryCheck(testData, new ReadOnlySpan<byte>(backingArray, span.Length, CanarySize), "the end of the Span itself");
            }
        }

        private static void CanaryFill(Span<byte> canaries)
        {
            for (int i = 0; i < canaries.Length; i++)
            {
                canaries[i] = 0xcc;
            }
        }

        private static void CanaryCheck<T>(FormatterTestData<T> testData, ReadOnlySpan<byte> canaries, string pastWhat)
        {
            for (int i = 0; i < canaries.Length; i++)
            {
                if (canaries[i] != 0xcc)
                    throw new TestException($"BUFFER OVERRUN! This format attempt ({testData}) wrote past {pastWhat}!");
            }
        }

        private static bool TryFormatUtf8<T>(T value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
        {
            if (typeof(T) == typeof(bool))
                return Utf8Formatter.TryFormat((bool)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(byte))
                return Utf8Formatter.TryFormat((byte)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(sbyte))
                return Utf8Formatter.TryFormat((sbyte)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(short))
                return Utf8Formatter.TryFormat((short)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(ushort))
                return Utf8Formatter.TryFormat((ushort)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(int))
                return Utf8Formatter.TryFormat((int)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(uint))
                return Utf8Formatter.TryFormat((uint)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(long))
                return Utf8Formatter.TryFormat((long)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(ulong))
                return Utf8Formatter.TryFormat((ulong)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(decimal))
                return Utf8Formatter.TryFormat((decimal)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(double))
                return Utf8Formatter.TryFormat((double)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(float))
                return Utf8Formatter.TryFormat((float)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(Guid))
                return Utf8Formatter.TryFormat((Guid)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(DateTime))
                return Utf8Formatter.TryFormat((DateTime)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(DateTimeOffset))
                return Utf8Formatter.TryFormat((DateTimeOffset)(object)value, buffer, out bytesWritten, format);

            if (typeof(T) == typeof(TimeSpan))
                return Utf8Formatter.TryFormat((TimeSpan)(object)value, buffer, out bytesWritten, format);

            throw new Exception("No formatter for type " + typeof(T));
        }

        private static bool TryFormatUtf8(object value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
        {
            Type t = value.GetType();
            if (t == typeof(bool))
                return Utf8Formatter.TryFormat((bool)value, buffer, out bytesWritten, format);

            if (t == typeof(byte))
                return Utf8Formatter.TryFormat((byte)value, buffer, out bytesWritten, format);

            if (t == typeof(sbyte))
                return Utf8Formatter.TryFormat((sbyte)value, buffer, out bytesWritten, format);

            if (t == typeof(short))
                return Utf8Formatter.TryFormat((short)value, buffer, out bytesWritten, format);

            if (t == typeof(ushort))
                return Utf8Formatter.TryFormat((ushort)value, buffer, out bytesWritten, format);

            if (t == typeof(int))
                return Utf8Formatter.TryFormat((int)value, buffer, out bytesWritten, format);

            if (t == typeof(uint))
                return Utf8Formatter.TryFormat((uint)value, buffer, out bytesWritten, format);

            if (t == typeof(long))
                return Utf8Formatter.TryFormat((long)value, buffer, out bytesWritten, format);

            if (t == typeof(ulong))
                return Utf8Formatter.TryFormat((ulong)value, buffer, out bytesWritten, format);

            if (t == typeof(decimal))
                return Utf8Formatter.TryFormat((decimal)value, buffer, out bytesWritten, format);

            if (t == typeof(double))
                return Utf8Formatter.TryFormat((double)value, buffer, out bytesWritten, format);

            if (t == typeof(float))
                return Utf8Formatter.TryFormat((float)value, buffer, out bytesWritten, format);

            if (t == typeof(Guid))
                return Utf8Formatter.TryFormat((Guid)value, buffer, out bytesWritten, format);

            if (t == typeof(DateTime))
                return Utf8Formatter.TryFormat((DateTime)value, buffer, out bytesWritten, format);

            if (t == typeof(DateTimeOffset))
                return Utf8Formatter.TryFormat((DateTimeOffset)value, buffer, out bytesWritten, format);

            if (t == typeof(TimeSpan))
                return Utf8Formatter.TryFormat((TimeSpan)value, buffer, out bytesWritten, format);

            throw new Exception("No formatter for type " + t);
        }
    }
}

