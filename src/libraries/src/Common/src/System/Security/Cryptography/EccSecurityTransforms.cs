// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed class EccSecurityTransforms : IDisposable
    {
        private SecKeyPair _keys;

        public void Dispose()
        {
            _keys?.Dispose();
            _keys = null;
        }

        internal int GenerateKey(ECCurve curve)
        {
            curve.Validate();

            if (!curve.IsNamed)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            int keySize;

            switch (curve.Oid.Value)
            {
                case Oids.secp256r1:
                    keySize = 256;
                    break;
                case Oids.secp384r1:
                    keySize = 384;
                    break;
                case Oids.secp521r1:
                    keySize = 521;
                    break;
                default:
                    throw new PlatformNotSupportedException(
                        SR.Format(SR.Cryptography_CurveNotSupported, curve.Oid.Value));
            }

            GenerateKey(keySize);
            return keySize;
        }

        private SecKeyPair GenerateKey(int keySizeInBits)
        {
            SafeSecKeyRefHandle publicKey;
            SafeSecKeyRefHandle privateKey;

            Interop.AppleCrypto.EccGenerateKey(keySizeInBits, out publicKey, out privateKey);

            SecKeyPair newPair = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
            SetKey(newPair);
            return newPair;
        }

        internal SecKeyPair GetOrGenerateKeys(int keySizeInBits)
        {
            SecKeyPair current = _keys;

            if (current != null)
            {
                return current;
            }

            return GenerateKey(keySizeInBits);
        }

        internal int SetKeyAndGetSize(SecKeyPair keyPair)
        {
            int size = GetKeySize(keyPair);
            SetKey(keyPair);
            return size;
        }

        private void SetKey(SecKeyPair keyPair)
        {
            SecKeyPair current = _keys;
            _keys = keyPair;
            current?.Dispose();
        }

        internal ECParameters ExportParameters(bool includePrivateParameters, int keySizeInBits)
        {
            // Apple requires all private keys to be exported encrypted, but since we're trying to export
            // as parsed structures we will need to decrypt it for the user.
            const string ExportPassword = "DotnetExportPassphrase";
            SecKeyPair keys = GetOrGenerateKeys(keySizeInBits);

            if (keys.PublicKey == null ||
                (includePrivateParameters && keys.PrivateKey == null))
            { 
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(
                includePrivateParameters ? keys.PrivateKey : keys.PublicKey,
                exportPrivate: includePrivateParameters,
                password: ExportPassword);

            try
            {
                if (!includePrivateParameters)
                {
                    EccKeyFormatHelper.ReadSubjectPublicKeyInfo(
                        keyBlob,
                        out int localRead,
                        out ECParameters key);
                    return key;
                }
                else
                {
                    EccKeyFormatHelper.ReadEncryptedPkcs8(
                        keyBlob,
                        ExportPassword,
                        out int localRead,
                        out ECParameters key);
                    return key;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBlob);
            }
        }

        internal int ImportParameters(ECParameters parameters)
        {
            parameters.Validate();

            bool isPrivateKey = parameters.D != null;
            SecKeyPair newKeys;

            if (isPrivateKey)
            {
                // Start with the private key, in case some of the private key fields don't
                // match the public key fields and the system determines an integrity failure.
                //
                // Public import should go off without a hitch.
                SafeSecKeyRefHandle privateKey = ImportKey(parameters);

                ECParameters publicOnly = parameters;
                publicOnly.D = null;

                SafeSecKeyRefHandle publicKey;
                try
                {
                    publicKey = ImportKey(publicOnly);
                }
                catch
                {
                    privateKey.Dispose();
                    throw;
                }

                newKeys = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
            }
            else
            {
                SafeSecKeyRefHandle publicKey = ImportKey(parameters);
                newKeys = SecKeyPair.PublicOnly(publicKey);
            }

            int size = GetKeySize(newKeys);
            SetKey(newKeys);

            return size;
        }

        private static int GetKeySize(SecKeyPair newKeys)
        {
            long size = Interop.AppleCrypto.EccGetKeySizeInBits(newKeys.PublicKey);
            Debug.Assert(size == 256 || size == 384 || size == 521, $"Unknown keysize ({size})");
            return (int)size;
        }

        private static SafeSecKeyRefHandle ImportKey(ECParameters parameters)
        {
            if (parameters.D != null)
            {
                using (AsnWriter privateKey = EccKeyFormatHelper.WriteECPrivateKey(parameters))
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(privateKey.EncodeAsSpan(), true);
                }
            }
            else
            {
                using (AsnWriter publicKey = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(parameters))
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(publicKey.EncodeAsSpan(), false);
                }
            }
        }

        internal unsafe int ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    // Validate the DER value and get the number of bytes.
                    EccKeyFormatHelper.ReadSubjectPublicKeyInfo(
                        manager.Memory,
                        out int localRead);

                    SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.ImportEphemeralKey(source.Slice(0, localRead), false);
                    SecKeyPair newKeys = SecKeyPair.PublicOnly(publicKey);
                    int size = GetKeySize(newKeys);
                    SetKey(newKeys);

                    bytesRead = localRead;
                    return size;
                }
            }
        }
    }
}
