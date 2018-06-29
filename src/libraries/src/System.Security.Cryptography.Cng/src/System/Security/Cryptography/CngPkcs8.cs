// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class CngPkcs8
    {
        internal struct Pkcs8Response
        {
            internal CngKey Key;

            internal string GetAlgorithmGroup()
            {
                return Key.AlgorithmGroup.AlgorithmGroup;
            }

            internal void FreeKey()
            {
                Key.Dispose();
            }
        }

        private static Pkcs8Response ImportPkcs8(ReadOnlySpan<byte> keyBlob)
        {
            CngKey key = CngKey.Import(keyBlob, CngKeyBlobFormat.Pkcs8PrivateBlob);
            key.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;

            return new Pkcs8Response
            {
                Key = key,
            };
        }

        private static Pkcs8Response ImportPkcs8(
            ReadOnlySpan<byte> keyBlob,
            ReadOnlySpan<char> password)
        {
            CngKey key = CngKey.ImportEncryptedPkcs8(keyBlob, password);
            key.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;

            return new Pkcs8Response
            {
                Key = key,
            };
        }
    }
}
