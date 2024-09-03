// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Tests
{
    internal static class NumberFormatTestHelper
    {
        internal static void TryFormatNumberTest<T>(T i, string format, IFormatProvider provider, string expected, bool formatCasingMatchesOutput = true) where T : ISpanFormattable, IUtf8SpanFormattable
        {
            // UTF16
            {
                char[] actual;
                int charsWritten;

                // Just right and longer than needed
                for (int additional = 0; additional < 2; additional++)
                {
                    actual = new char[expected.Length + additional];
                    Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
                    Assert.Equal(expected.Length, charsWritten);
                    Assert.Equal(expected, new string(actual.AsSpan(0, charsWritten)));
                }

                // Too short
                if (expected.Length > 0)
                {
                    actual = new char[expected.Length - 1];
                    Assert.False(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
                    Assert.Equal(0, charsWritten);
                }

                if (formatCasingMatchesOutput && format != null)
                {
                    // Upper format
                    actual = new char[expected.Length];
                    Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToUpperInvariant(), provider));
                    Assert.Equal(expected.Length, charsWritten);
                    Assert.Equal(expected.ToUpperInvariant(), new string(actual));

                    // Lower format
                    actual = new char[expected.Length];
                    Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToLowerInvariant(), provider));
                    Assert.Equal(expected.Length, charsWritten);
                    Assert.Equal(expected.ToLowerInvariant(), new string(actual));
                }
            }

            // UTF8
            {
                byte[] actual;
                int charsWritten;
                int expectedLength = Encoding.UTF8.GetByteCount(expected);

                // Just right and longer than needed
                for (int additional = 0; additional < 2; additional++)
                {
                    actual = new byte[expectedLength + additional];
                    Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
                    Assert.Equal(expectedLength, charsWritten);
                    Assert.Equal(expected, Encoding.UTF8.GetString(actual.AsSpan(0, charsWritten)));
                }

                // Too short
                if (expectedLength > 0)
                {
                    actual = new byte[expectedLength - 1];
                    Assert.False(i.TryFormat(actual.AsSpan(), out charsWritten, format, provider));
                    Assert.Equal(0, charsWritten);
                }

                if (formatCasingMatchesOutput && format != null)
                {
                    // Upper format
                    actual = new byte[expectedLength];
                    Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToUpperInvariant(), provider));
                    Assert.Equal(expectedLength, charsWritten);
                    Assert.Equal(expected.ToUpperInvariant(), Encoding.UTF8.GetString(actual.AsSpan(0, charsWritten)));

                    // Lower format
                    actual = new byte[expectedLength];
                    Assert.True(i.TryFormat(actual.AsSpan(), out charsWritten, format.ToLowerInvariant(), provider));
                    Assert.Equal(expectedLength, charsWritten);
                    Assert.Equal(expected.ToLowerInvariant(), Encoding.UTF8.GetString(actual.AsSpan(0, charsWritten)));
                }
            }
        }
    }
}
