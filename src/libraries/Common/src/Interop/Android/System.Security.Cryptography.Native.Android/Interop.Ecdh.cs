// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        internal static bool EcdhDeriveKey(SafeEcKeyHandle ourKey, SafeEcKeyHandle peerKey, Span<byte> buffer, out int usedBuffer) =>
            EcdhDeriveKey(ourKey, peerKey, ref MemoryMarshal.GetReference(buffer), buffer.Length, out usedBuffer);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_EcdhDeriveKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EcdhDeriveKey(SafeEcKeyHandle ourKey, SafeEcKeyHandle peerKey, ref byte buffer, int bufferLength, out int usedBuffer);
    }
}
