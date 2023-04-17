// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        // Rfc1123 - lowercase
        //
        //   01234567890123456789012345678
        //   -----------------------------
        //   tue, 03 jan 2017 08:08:05 gmt
        //
        private static bool TryFormatDateTimeL(DateTime value, Span<byte> destination, out int bytesWritten)
        {
            if (value.TryFormat(destination, out bytesWritten, "r", CultureInfo.InvariantCulture))
            {
                Debug.Assert(bytesWritten == 29);
                Ascii.ToLowerInPlace(destination.Slice(0, bytesWritten), out bytesWritten);
                return true;
            }

            return false;
        }
    }
}
