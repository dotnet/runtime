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
        internal static partial class EvpKemAlgs
        {
            internal static SafeEvpKemHandle? MlKem512 { get; }
            internal static SafeEvpKemHandle? MlKem768 { get; }
            internal static SafeEvpKemHandle? MlKem1024 { get; }

            static EvpKemAlgs()
            {
                CryptoInitializer.Initialize();

                // Do not use property initializers for these because we need to ensure CryptoInitializer.Initialize
                // is called first. Property initializers happen before cctors, so instead set the property after the
                // initializer is run.
                MlKem512 = EvpKemFetch(MLKemAlgorithm.MLKem512.Name);
                MlKem768 = EvpKemFetch(MLKemAlgorithm.MLKem768.Name);
                MlKem1024 = EvpKemFetch(MLKemAlgorithm.MLKem1024.Name);
            }

            [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemFetch", StringMarshalling = StringMarshalling.Utf8)]
            private static partial SafeEvpKemHandle CryptoNative_EvpKemFetch(string algorithm, out int haveFeature);

            private static SafeEvpKemHandle? EvpKemFetch(string algorithm)
            {
                SafeEvpKemHandle kem = CryptoNative_EvpKemFetch(algorithm, out int haveFeature);

                if (haveFeature == 0)
                {
                    Debug.Assert(kem.IsInvalid);
                    kem.Dispose();
                    return null;
                }

                if (kem.IsInvalid)
                {
                    kem.Dispose();
                    throw CreateOpenSslCryptographicException();
                }

                return kem;
            }
        }
    }
}
