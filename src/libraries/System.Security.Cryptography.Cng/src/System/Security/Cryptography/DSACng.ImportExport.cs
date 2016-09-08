// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            Key = newKey;
        }
    }
}

