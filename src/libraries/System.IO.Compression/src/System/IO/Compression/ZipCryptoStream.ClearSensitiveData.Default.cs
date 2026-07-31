// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        // Uses CryptographicOperations.ZeroMemory so the clear is not elided by the JIT,
        // preserving the guarantee that password material is actually wiped from memory.
        private static void ClearSensitiveData(Span<byte> buffer) => CryptographicOperations.ZeroMemory(buffer);
    }
}
