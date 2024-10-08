// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public static partial class HKDF
    {
        private static readonly bool s_hasOpenSslImplementation = Interop.Crypto.EvpKdfAlgs.Hkdf is not null;

        private static void Extract(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> salt,
            Span<byte> prk)
        {
            if (s_hasOpenSslImplementation)
            {
                Debug.Assert(Interop.Crypto.EvpKdfAlgs.Hkdf is not null);
                Debug.Assert(hashAlgorithmName.Name is not null);

                Interop.Crypto.HkdfExtract(Interop.Crypto.EvpKdfAlgs.Hkdf, ikm, hashAlgorithmName.Name, salt, prk);
            }
            else
            {
                HKDFManagedImplementation.Extract(hashAlgorithmName, hashLength, ikm, salt, prk);
            }
        }

        private static void Expand(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> prk,
            Span<byte> output,
            ReadOnlySpan<byte> info)
        {
            if (s_hasOpenSslImplementation)
            {
                Debug.Assert(Interop.Crypto.EvpKdfAlgs.Hkdf is not null);
                Debug.Assert(hashAlgorithmName.Name is not null);

                Interop.Crypto.HkdfExpand(Interop.Crypto.EvpKdfAlgs.Hkdf, prk, hashAlgorithmName.Name, info, output);
            }
            else
            {
                HKDFManagedImplementation.Expand(hashAlgorithmName, hashLength, prk, output, info);
            }
        }

        private static void DeriveKeyCore(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> ikm,
            Span<byte> output,
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> info)
        {
            if (s_hasOpenSslImplementation)
            {
                Debug.Assert(Interop.Crypto.EvpKdfAlgs.Hkdf is not null);
                Debug.Assert(hashAlgorithmName.Name is not null);

                Interop.Crypto.HkdfDeriveKey(
                    Interop.Crypto.EvpKdfAlgs.Hkdf,
                    ikm,
                    hashAlgorithmName.Name,
                    salt,
                    info,
                    output);
            }
            else
            {
                HKDFManagedImplementation.DeriveKey(hashAlgorithmName, hashLength, ikm, output, salt, info);
            }
        }
    }
}
