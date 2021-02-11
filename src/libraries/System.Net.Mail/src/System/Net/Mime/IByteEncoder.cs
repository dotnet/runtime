// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Net.Mime
{
    internal interface IByteEncoder
    {
        // This method does not account for codepoint boundaries. If encoding a string, consider using EncodeString
        int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF);
        void AppendPadding();
        int EncodeString(string value, Encoding encoding);
        string GetEncodedString();
    }
}
