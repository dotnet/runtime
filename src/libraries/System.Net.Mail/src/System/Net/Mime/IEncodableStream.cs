// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Net.Mime
{
    internal interface IEncodableStream
    {
        int DecodeBytes(byte[] buffer, int offset, int count);
        // This method does not account for codepoint boundaries. If encoding a string, consider using EncodeString
        int EncodeBytes(byte[] buffer, int offset, int count);
        int EncodeString(string value, Encoding encoding);
        string GetEncodedString();
    }
}
