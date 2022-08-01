// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class EccSecurityTransforms
    {
        private static ECParameters ExportParametersFromLegacyKey(SecKeyPair keys, bool includePrivateParameters)
        {
            // Apple requires all private keys to be exported encrypted, but since we're trying to export
            // as parsed structures we will need to decrypt it for the user.
            const string ExportPassword = "DotnetExportPassphrase";

            byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(
                includePrivateParameters ? keys.PrivateKey : keys.PublicKey,
                exportPrivate: includePrivateParameters,
                password: ExportPassword);

            try
            {
                if (!includePrivateParameters)
                {
                    EccKeyFormatHelper.ReadSubjectPublicKeyInfo(
                        keyBlob,
                        out _,
                        out ECParameters key);
                    return key;
                }
                else
                {
                    EccKeyFormatHelper.ReadEncryptedPkcs8(
                        keyBlob,
                        (ReadOnlySpan<char>)ExportPassword,
                        out _,
                        out ECParameters key);
                    return key;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBlob);
            }
        }

        private static void ExtractPublicKeyFromPrivateKey(ref ECParameters ecParameters)
        {
            using (SafeSecKeyRefHandle secPrivateKey = ImportLegacyPrivateKey(ref ecParameters))
            {
                const string ExportPassword = "DotnetExportPassphrase";
                byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(secPrivateKey, exportPrivate: true, password: ExportPassword);
                EccKeyFormatHelper.ReadEncryptedPkcs8(keyBlob, (ReadOnlySpan<char>)ExportPassword, out _, out ecParameters);
                CryptographicOperations.ZeroMemory(keyBlob);
            }
        }

        private static SafeSecKeyRefHandle ImportLegacyPrivateKey(ref ECParameters parameters)
        {
            AsnWriter keyWriter = EccKeyFormatHelper.WriteECPrivateKey(parameters);

            byte[] rented = CryptoPool.Rent(keyWriter.GetEncodedLength());

            if (!keyWriter.TryEncode(rented, out int written))
            {
                Debug.Fail("TryEncode failed with a pre-allocated buffer");
                throw new InvalidOperationException();
            }

            // Explicitly clear the inner buffer
            keyWriter.Reset();

            try
            {
                return Interop.AppleCrypto.ImportEphemeralKey(rented.AsSpan(0, written), true);
            }
            finally
            {
                CryptoPool.Return(rented, written);
            }
        }
    }
}
