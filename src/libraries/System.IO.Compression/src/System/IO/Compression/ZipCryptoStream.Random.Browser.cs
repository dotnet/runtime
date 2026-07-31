// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        private static void FillHeaderRandomBytes(Span<byte> header)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            Guid.NewGuid().TryWriteBytes(guidBytes);
            guidBytes.Slice(0, 10).CopyTo(header);
        }
    }
}
