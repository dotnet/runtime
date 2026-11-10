// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class EccAppleCrypto
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
    }
}
