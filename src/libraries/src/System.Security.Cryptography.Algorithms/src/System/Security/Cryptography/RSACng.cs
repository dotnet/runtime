// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public partial class RSA : AsymmetricAlgorithm
    {
        public static RSA Create()
        {
            return new RSAImplementation.RSACng();
        }
    }

    internal static partial class RSAImplementation
    {
        public sealed partial class RSACng : RSA
        {
            private SafeNCryptKeyHandle _keyHandle;
            private int _lastKeySize;

            private SafeNCryptKeyHandle GetKeyHandle()
            {
                int keySize = KeySize;

                if (_lastKeySize != keySize)
                {
                    if (_keyHandle != null)
                    {
                        _keyHandle.Dispose();
                    }

                    const string BCRYPT_RSA_ALGORITHM = "RSA";

                    _keyHandle = CngKeyLite.GenerateNewExportableKey(BCRYPT_RSA_ALGORITHM, keySize);
                    _lastKeySize = keySize;
                }

                return _keyHandle;
            }

            private byte[] ExportKeyBlob(bool includePrivateParameters)
            {
                string blobType = includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_RSAFULLPRIVATE_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_PUBLIC_KEY_BLOB;

                SafeNCryptKeyHandle keyHandle = GetKeyHandle();

                return CngKeyLite.ExportKeyBlob(keyHandle, blobType);
            }

            private void ImportKeyBlob(byte[] rsaBlob, bool includePrivate)
            {
                string blobType = includePrivate ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_RSAPRIVATE_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_PUBLIC_KEY_BLOB;

                SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, rsaBlob);

                Debug.Assert(!keyHandle.IsInvalid);

                _keyHandle = keyHandle;
                int newKeySize = CngKeyLite.GetKeyLength(keyHandle);
                KeySize = _lastKeySize = newKeySize;
            }
        }
    }
}
