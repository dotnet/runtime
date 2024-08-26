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
        internal static partial class EvpKdfAlgs
        {
            private const string KbkdfAlgorithmName = "KBKDF";
            private const string HkdfAlgorithmName = "HKDF";

            internal static SafeEvpKdfHandle? Kbkdf { get; }
            internal static SafeEvpKdfHandle? Hkdf { get; }

            static EvpKdfAlgs()
            {
                CryptoInitializer.Initialize();

                // Do not use property initializers for these because we need to ensure CryptoInitializer.Initialize
                // is called first. Property initializers happen before cctors, so instead set the property after the
                // initializer is run.
                Kbkdf = EvpKdfFetch(KbkdfAlgorithmName);
                Hkdf = EvpKdfFetch(HkdfAlgorithmName);
            }

            [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKdfFetch", StringMarshalling = StringMarshalling.Utf8)]
            private static partial SafeEvpKdfHandle CryptoNative_EvpKdfFetch(string algorithm, out int haveFeature);

            private static SafeEvpKdfHandle? EvpKdfFetch(string algorithm)
            {
                SafeEvpKdfHandle kdf = CryptoNative_EvpKdfFetch(algorithm, out int haveFeature);

                if (haveFeature == 0)
                {
                    Debug.Assert(kdf.IsInvalid);
                    kdf.Dispose();
                    return null;
                }

                if (kdf.IsInvalid)
                {
                    kdf.Dispose();
                    throw CreateOpenSslCryptographicException();
                }

                return kdf;
            }
        }
    }
}
