// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyFromData", StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEvpPKeyHandle CryptoNative_EvpPKeyFromData(
            string algorithmName,
            ReadOnlySpan<byte> key,
            int keyLength,
            [MarshalAs(UnmanagedType.Bool)] bool privateKey);

        internal static SafeEvpPKeyHandle EvpPKeyFromData(string algorithmName, ReadOnlySpan<byte> key, bool privateKey)
        {
            SafeEvpPKeyHandle handle = CryptoNative_EvpPKeyFromData(algorithmName, key, key.Length, privateKey);

            if (handle.IsInvalid)
            {
                Exception ex = CreateOpenSslCryptographicException();
                handle.Dispose();
                throw ex;
            }

            return handle;
        }
    }
}
