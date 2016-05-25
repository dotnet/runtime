// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    internal static partial class ECCng
    {
        internal static CngKey ImportKeyBlob(byte[] ecBlob, string curveName, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob;
            CngKey newKey = CngKey.Import(ecBlob, curveName, blobFormat);
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            return newKey;
        }

        internal static CngKey ImportFullKeyBlob(byte[] ecBlob, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccFullPrivateBlob : CngKeyBlobFormat.EccFullPublicBlob;
            CngKey newKey = CngKey.Import(ecBlob, blobFormat);
            newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;

            return newKey;
        }

        internal static byte[] ExportKeyBlob(CngKey key, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccPrivateBlob : CngKeyBlobFormat.EccPublicBlob;
            return key.Export(blobFormat);
        }

        internal static byte[] ExportFullKeyBlob(CngKey key, bool includePrivateParameters)
        {
            CngKeyBlobFormat blobFormat = includePrivateParameters ? CngKeyBlobFormat.EccFullPrivateBlob : CngKeyBlobFormat.EccFullPublicBlob;
            return key.Export(blobFormat);
        }
    }
}
