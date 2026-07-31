// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        // System.Security.Cryptography is not referenced on the browser platform, so
        // CryptographicOperations.ZeroMemory is unavailable and Array.Clear is used instead.
        private static void ClearSensitiveData(Span<byte> buffer) => buffer.Clear();
    }
}
