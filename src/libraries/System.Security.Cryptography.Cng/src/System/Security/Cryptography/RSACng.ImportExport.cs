// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        private void ImportKeyBlob(byte[] rsaBlob, bool includePrivate)
        {
            CngKeyBlobFormat blobFormat = includePrivate ? s_rsaPrivateBlob : s_rsaPublicBlob;

            CngKey newKey = CngKey.Import(rsaBlob, blobFormat);
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            Key = newKey;
        }

        private byte[] ExportKeyBlob(bool includePrivateParameters)
        {
            return Key.Export(includePrivateParameters ? s_rsaFullPrivateBlob : s_rsaPublicBlob);
        }

        // CngKeyBlob formats for RSA key blobs
        private static readonly CngKeyBlobFormat s_rsaFullPrivateBlob = new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAFULLPRIVATE_BLOB);
        private static readonly CngKeyBlobFormat s_rsaPrivateBlob = new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAPRIVATE_BLOB);
        private static readonly CngKeyBlobFormat s_rsaPublicBlob = new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAPUBLIC_KEY_BLOB);
    }
}
