// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// A class that can escape a scalar value and write either UTF-16 or UTF-8 format.
    /// </summary>
    internal abstract class ScalarEscaperBase
    {
        internal abstract int EncodeUtf16(Rune value, Span<char> destination);
        internal abstract int EncodeUtf8(Rune value, Span<byte> destination);
    }
}
