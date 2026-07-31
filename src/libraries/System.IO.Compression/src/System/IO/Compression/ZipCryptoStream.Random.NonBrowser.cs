// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        private static void FillHeaderRandomBytes(Span<byte> header)
        {
            RandomNumberGenerator.Fill(header.Slice(0, 10));
        }
    }
}
