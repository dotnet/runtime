// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public partial class DSA : AsymmetricAlgorithm
    {
        public static new DSA Create()
        {
            return new DSAImplementation.DSACng();
        }
    }

    internal static partial class DSAImplementation
    {
        public sealed partial class DSACng : DSA
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

                    const string BCRYPT_DSA_ALGORITHM = "DSA";

                    _keyHandle = CngKeyLite.GenerateNewExportableKey(BCRYPT_DSA_ALGORITHM, keySize);
                    _lastKeySize = keySize;
                }

                return new DuplicateSafeNCryptKeyHandle(_keyHandle);
            }

            private byte[] ExportKeyBlob(bool includePrivateParameters)
            {
                // Use generic blob type for multiple version support
                string blobType = includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_PRIVATE_KEY_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_PUBLIC_KEY_BLOB;

                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngKeyLite.ExportKeyBlob(keyHandle, blobType);
                }
            }

            private void ImportKeyBlob(byte[] rsaBlob, bool includePrivate)
            {
                // Use generic blob type for multiple version support
                string blobType = includePrivate ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_PRIVATE_KEY_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_PUBLIC_KEY_BLOB;

                SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, rsaBlob);

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
                ForceSetKeySize(newKeySize);
                _lastKeySize = newKeySize;
            }
        }
    }
}
