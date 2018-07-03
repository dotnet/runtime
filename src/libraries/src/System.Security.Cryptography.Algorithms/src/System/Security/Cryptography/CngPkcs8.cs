// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static partial class CngPkcs8
    {
        internal struct Pkcs8Response
        {
            internal SafeNCryptKeyHandle KeyHandle;

            internal string GetAlgorithmGroup()
            {
                return CngKeyLite.GetPropertyAsString(
                    KeyHandle,
                    CngKeyLite.KeyPropertyName.AlgorithmGroup,
                    CngPropertyOptions.None);
            }

            internal void FreeKey()
            {
                KeyHandle.Dispose();
            }
        }

        private static Pkcs8Response ImportPkcs8(ReadOnlySpan<byte> keyBlob)
        {
            SafeNCryptKeyHandle handle = CngKeyLite.ImportKeyBlob(
                Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                keyBlob);

            return new Pkcs8Response
            {
                KeyHandle = handle,
            };
        }

        private static Pkcs8Response ImportPkcs8(
            ReadOnlySpan<byte> keyBlob,
            ReadOnlySpan<char> password)
        {
            SafeNCryptKeyHandle handle = CngKeyLite.ImportKeyBlob(
                Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                keyBlob,
                encrypted: true,
                password);

            return new Pkcs8Response
            {
                KeyHandle = handle,
            };
        }
    }
}
