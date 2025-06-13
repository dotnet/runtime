// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class MLDsaImplementation
    {
        internal CngKey CreateEphemeralCng()
        {
            string bcryptBlobType =
                _hasSeed ? Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB :
                _hasSecretKey ? Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB :
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB;

            CngKeyBlobFormat cngBlobFormat =
                _hasSecretKey ? CngKeyBlobFormat.PQDsaPrivateBlob :
                _hasSeed ? CngKeyBlobFormat.PQDsaPrivateSeedBlob :
                CngKeyBlobFormat.PQDsaPublicBlob;

            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, bcryptBlobType);
            CngKey key;

            try
            {
                key = CngKey.Import(keyBlob, cngBlobFormat);
            }
            finally
            {
                CryptoPool.Return(keyBlob);
            }

            key.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;
            return key;
        }
    }
}
