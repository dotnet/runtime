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
            internal static string? MlKem512 { get; }
            internal static string? MlKem768 { get; }
            internal static string? MlKem1024 { get; }

            static EvpKemAlgs()
            {
                CryptoInitializer.Initialize();

                // Do not use property initializers for these because we need to ensure CryptoInitializer.Initialize
                // is called first. Property initializers happen before cctors, so instead set the property after the
                // initializer is run.
                MlKem512 = EvpKemAvailable(MLKemAlgorithm.MLKem512.Name);
                MlKem768 = EvpKemAvailable(MLKemAlgorithm.MLKem768.Name);
                MlKem1024 = EvpKemAvailable(MLKemAlgorithm.MLKem1024.Name);
            }

            [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemAvailable", StringMarshalling = StringMarshalling.Utf8)]
            private static partial int CryptoNative_EvpKemAvailable(string algorithm);

            private static string? EvpKemAvailable(string algorithm)
            {
                const int Available = 1;
                const int NotAvailable = 0;

                int ret = CryptoNative_EvpKemAvailable(algorithm);
                return ret switch
                {
                    Available => algorithm,
                    NotAvailable => null,
                    int other => throw Fail(other),
                };

                static CryptographicException Fail(int result)
                {
                    Debug.Fail($"Unexpected result {result} from {nameof(CryptoNative_EvpKemAvailable)}");
                    return new CryptographicException();
                }
            }
        }
    }
}
