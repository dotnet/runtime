// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        // CngKeyBlob formats for RSA key blobs
        private static readonly CngKeyBlobFormat s_rsaFullPrivateBlob =
            new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAFULLPRIVATE_BLOB);

        private static readonly CngKeyBlobFormat s_rsaPrivateBlob =
            new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAPRIVATE_BLOB);

        private static readonly CngKeyBlobFormat s_rsaPublicBlob =
            new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAPUBLIC_KEY_BLOB);

        private void ImportKeyBlob(byte[] rsaBlob, bool includePrivate)
        {
            CngKeyBlobFormat blobFormat = includePrivate ? s_rsaPrivateBlob : s_rsaPublicBlob;

            CngKey newKey = CngKey.Import(rsaBlob, blobFormat);
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            Key = newKey;
        }

        private void AcceptImport(CngPkcs8.Pkcs8Response response)
        {
            Key = response.Key;
        }

        private byte[] ExportKeyBlob(bool includePrivateParameters)
        {
            return Key.Export(includePrivateParameters ? s_rsaFullPrivateBlob : s_rsaPublicBlob);
        }

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            return Key.TryExportKeyBlob(
                Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                destination,
                out bytesWritten);
        }

        private byte[] ExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount)
        {
            return Key.ExportPkcs8KeyBlob(pkcs8Password, kdfCount);
        }

        private bool TryExportEncryptedPkcs8(
            ReadOnlySpan<char> pkcs8Password,
            int kdfCount,
            Span<byte> destination,
            out int bytesWritten)
        {
            return Key.TryExportPkcs8KeyBlob(
                pkcs8Password,
                kdfCount,
                destination,
                out bytesWritten);
        }
    }
}
