// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal readonly struct WinZipAesKeyMaterial;

    internal static class WinZipAesStream
    {
        internal static int GetSaltSize(int keySizeBits) => throw CreateException();

        internal static WinZipAesKeyMaterial CreateKey(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits) => throw CreateException();

        internal static Stream Create(Stream baseStream, WinZipAesKeyMaterial keyMaterial, long totalStreamSize, bool encrypting, bool leaveOpen = false) => throw CreateException();

        internal static Task<Stream> CreateAsync(Stream baseStream, WinZipAesKeyMaterial keyMaterial, long totalStreamSize, bool encrypting, bool leaveOpen = false, CancellationToken cancellationToken = default) => throw CreateException();

        private static PlatformNotSupportedException CreateException() => new(SR.WinZipEncryptionNotSupportedOnBrowser);
    }
}
