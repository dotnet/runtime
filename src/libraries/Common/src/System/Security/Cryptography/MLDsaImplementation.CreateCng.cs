// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    internal sealed partial class MLDsaImplementation
    {
        [SupportedOSPlatform("windows")]
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

            CngKey key = Interop.BCrypt.BCryptExportKey(
                _key,
                bcryptBlobType,
#if SYSTEM_SECURITY_CRYPTOGRAPHY
                (ReadOnlySpan<byte> keyMaterial) => CngKey.Import(keyMaterial, cngBlobFormat));
#else
                (byte[] keyMaterial) => CngKey.Import(keyMaterial, cngBlobFormat));
#endif

#if SYSTEM_SECURITY_CRYPTOGRAPHY
            key.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;
#else
            key.SetExportPolicy(CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);
#endif
            return key;
        }
    }
}
