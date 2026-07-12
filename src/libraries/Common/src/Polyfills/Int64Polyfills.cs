// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace System;

/// <summary>Provides downlevel polyfills for formatting and parsing <see cref="long"/>.</summary>
internal static class Int64Polyfills
{
    extension(long value)
    {
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            string text = value.ToString(format.IsEmpty ? null : format.ToString(), provider);
            int byteCount = Encoding.UTF8.GetByteCount(text);
            if (byteCount > utf8Destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = Encoding.UTF8.GetBytes(text, utf8Destination);
            return true;
        }

        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out long result) =>
            long.TryParse(Encoding.UTF8.GetString(utf8Text), style, provider, out result);
    }
}
