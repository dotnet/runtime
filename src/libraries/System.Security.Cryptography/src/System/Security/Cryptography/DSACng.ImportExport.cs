// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public sealed partial class DSACng : DSA
    {
        private byte[] ExportKeyBlob(bool includePrivateParameters)
        {
            // Use generic blob type for multiple version support
            CngKeyBlobFormat blobFormat = includePrivateParameters ?
                CngKeyBlobFormat.GenericPrivateBlob :
                CngKeyBlobFormat.GenericPublicBlob;

            return Key.Export(blobFormat);
        }

        private void ImportKeyBlob(byte[] dsaBlob, bool includePrivate)
        {
            // Use generic blob type for multiple version support
            CngKeyBlobFormat blobFormat = includePrivate ?
                CngKeyBlobFormat.GenericPrivateBlob :
                CngKeyBlobFormat.GenericPublicBlob;

            CngKey newKey = CngKey.Import(dsaBlob, blobFormat);
            try
            {
                newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;
                Key = newKey;
            }
            catch
            {
                newKey.Dispose();
                throw;
            }
        }

        private void AcceptImport(CngPkcs8.Pkcs8Response response)
        {
            try
            {
                Key = response.Key;
            }
            catch
            {
                response.FreeKey();
                throw;
            }
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
