// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Microsoft.Win32.SafeHandles;
using static Internal.NativeCrypto.BCryptNative;

namespace System.Security.Cryptography
{
    public partial class ECDsa : AsymmetricAlgorithm
    {
        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        public static new ECDsa Create()
        {
            return new ECDsaImplementation.ECDsaCng();
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="curve">
        /// The <see cref="ECCurve"/> representing the elliptic curve.
        /// </param>
        public static ECDsa Create(ECCurve curve)
        {
            return new ECDsaImplementation.ECDsaCng(curve);
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="parameters">
        /// The <see cref="ECParameters"/> representing the elliptic curve parameters.
        /// </param>
        public static ECDsa Create(ECParameters parameters)
        {
            ECDsa ec = new ECDsaImplementation.ECDsaCng();
            ec.ImportParameters(parameters);
            return ec;
        }
    }

    internal static partial class ECDsaImplementation
    {
        public sealed partial class ECDsaCng : ECDsa
        {
            private void ImportFullKeyBlob(byte[] ecfullKeyBlob, bool includePrivateParameters)
            {
                string blobType = includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_ECCFULLPRIVATE_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_ECCFULLPUBLIC_BLOB;

                SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, ecfullKeyBlob);

                Debug.Assert(!keyHandle.IsInvalid);

                _key.SetHandle(keyHandle, AlgorithmName.ECDsa);
                ForceSetKeySize(_key.KeySize);
            }

            private void ImportKeyBlob(byte[] ecKeyBlob, string curveName, bool includePrivateParameters)
            {
                string blobType = includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_ECCPRIVATE_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_ECCPUBLIC_BLOB;

                SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, ecKeyBlob, curveName);

                Debug.Assert(!keyHandle.IsInvalid);

                _key.SetHandle(keyHandle, ECCng.EcdsaCurveNameToAlgorithm(curveName));
                ForceSetKeySize(_key.KeySize);
            }

            private byte[] ExportKeyBlob(bool includePrivateParameters)
            {
                string blobType = includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_ECCPRIVATE_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_ECCPUBLIC_BLOB;

                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
                {
                    return CngKeyLite.ExportKeyBlob(keyHandle, blobType);
                }
            }

            private byte[] ExportFullKeyBlob(bool includePrivateParameters)
            {
                string blobType = includePrivateParameters ?
                    Interop.BCrypt.KeyBlobType.BCRYPT_ECCFULLPRIVATE_BLOB :
                    Interop.BCrypt.KeyBlobType.BCRYPT_ECCFULLPUBLIC_BLOB;

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

            private void AcceptImport(CngPkcs8.Pkcs8Response response)
            {
                SafeNCryptKeyHandle keyHandle = response.KeyHandle;

                _key.SetHandle(
                    keyHandle,
                    CngKeyLite.GetPropertyAsString(
                        keyHandle,
                        CngKeyLite.KeyPropertyName.Algorithm,
                        CngPropertyOptions.None));

                ForceSetKeySize(_key.KeySize);
            }
        }
    }
}
