// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public partial class RSA : AsymmetricAlgorithm
    {
        public static new RSA Create()
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

            private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
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

                return new DuplicateSafeNCryptKeyHandle(_keyHandle);
            }

            private byte[] ExportKeyBlob(bool includePrivateParameters)
            {
                string blobType = includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_RSAFULLPRIVATE_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_RSAPUBLIC_KEY_BLOB;

                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngKeyLite.ExportKeyBlob(keyHandle, blobType);
                }
            }

            private byte[] ExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount)
            {
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngKeyLite.ExportPkcs8KeyBlob(keyHandle, pkcs8Password, kdfCount);
                }
            }

            private bool TryExportEncryptedPkcs8(
                ReadOnlySpan<char> pkcs8Password,
                int kdfCount,
                Span<byte> destination,
                out int bytesWritten)
            {
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngKeyLite.TryExportPkcs8KeyBlob(
                        keyHandle,
                        pkcs8Password,
                        kdfCount,
                        destination,
                        out bytesWritten);
                }
            }

            private void ImportKeyBlob(byte[] rsaBlob, bool includePrivate)
            {
                string blobType = includePrivate
                    ? Interop.BCrypt.KeyBlobType.BCRYPT_RSAPRIVATE_BLOB
                    : Interop.BCrypt.KeyBlobType.BCRYPT_RSAPUBLIC_KEY_BLOB;

                SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, rsaBlob);
                SetKeyHandle(keyHandle);
            }

            private void AcceptImport(CngPkcs8.Pkcs8Response response)
            {
                SafeNCryptKeyHandle keyHandle = response.KeyHandle;
                SetKeyHandle(keyHandle);
            }

            private void SetKeyHandle(SafeNCryptKeyHandle keyHandle)
            {
                Debug.Assert(!keyHandle.IsInvalid);

                _keyHandle = keyHandle;

                int newKeySize = CngKeyLite.GetKeyLength(keyHandle);

                // Our LegalKeySizes value stores the values that we encoded as being the correct
                // legal key size limitations for this algorithm, as documented on MSDN.
                //
                // But on a new OS version we might not question if our limit is accurate, or MSDN
                // could have been inaccurate to start with.
                //
                // Since the key is already loaded, we know that Windows thought it to be valid;
                // therefore we should set KeySizeValue directly to bypass the LegalKeySizes conformance
                // check.
                //
                // For RSA there are known cases where this change matters. RSACryptoServiceProvider can
                // create a 384-bit RSA key, which we consider too small to be legal. It can also create
                // a 1032-bit RSA key, which we consider illegal because it doesn't match our 64-bit
                // alignment requirement. (In both cases Windows loads it just fine)
                ForceSetKeySize(newKeySize);
                _lastKeySize = newKeySize;
            }
        }
    }
}
