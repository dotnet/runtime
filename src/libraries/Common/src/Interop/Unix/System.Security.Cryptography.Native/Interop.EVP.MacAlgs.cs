// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal static partial class EvpMacAlgs
        {
            internal static SafeEvpMacHandle? Kmac128 { get; } = EvpMacFetch(HashAlgorithmNames.KMAC128);
            internal static SafeEvpMacHandle? Kmac256 { get; } = EvpMacFetch(HashAlgorithmNames.KMAC256);

            [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacFetch", StringMarshalling = StringMarshalling.Utf8)]
            private static partial SafeEvpMacHandle CryptoNative_EvpMacFetch(string algorithm, out int haveFeature);

            private static SafeEvpMacHandle? EvpMacFetch(string algorithm)
            {
                SafeEvpMacHandle mac = CryptoNative_EvpMacFetch(algorithm, out int haveFeature);

                if (haveFeature == 0)
                {
                    Debug.Assert(mac.IsInvalid);
                    mac.Dispose();
                    return null;
                }

                if (mac.IsInvalid)
                {
                    mac.Dispose();
                    throw CreateOpenSslCryptographicException();
                }

                return mac;
            }
        }
    }
}
