// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Net.Mime
{
    internal interface IEncodableStream
    {
        int DecodeBytes(Span<byte> buffer);
        // This method does not account for codepoint boundaries. If encoding a string, consider using EncodeString
        int EncodeBytes(ReadOnlySpan<byte> buffer);
        int EncodeString(string value, Encoding encoding);
        string GetEncodedString();
    }
}
