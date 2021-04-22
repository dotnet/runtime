// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class EccSecurityTransforms
    {
        internal static ECParameters ExportPublicParametersFromPrivateKey(SafeSecKeyRefHandle handle)
        {
            const string ExportPassword = "DotnetExportPassphrase";
            byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(handle, exportPrivate: true, password: ExportPassword);
            EccKeyFormatHelper.ReadEncryptedPkcs8(keyBlob, ExportPassword, out _, out ECParameters key);
            CryptographicOperations.ZeroMemory(key.D);
            CryptographicOperations.ZeroMemory(keyBlob);
            key.D = null;
            return key;
        }

        internal ECParameters ExportParameters(bool includePrivateParameters, int keySizeInBits)
        {
            // Apple requires all private keys to be exported encrypted, but since we're trying to export
            // as parsed structures we will need to decrypt it for the user.
            const string ExportPassword = "DotnetExportPassphrase";
            SecKeyPair keys = GetOrGenerateKeys(keySizeInBits);

            if (includePrivateParameters && keys.PrivateKey == null)
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
            ThrowIfDisposed();

            bool isPrivateKey = parameters.D != null;
            bool hasPublicParameters = parameters.Q.X != null && parameters.Q.Y != null;
            SecKeyPair newKeys;

            if (isPrivateKey)
            {
                // Start with the private key, in case some of the private key fields don't
                // match the public key fields and the system determines an integrity failure.
                //
                // Public import should go off without a hitch.
                SafeSecKeyRefHandle privateKey = ImportKey(parameters);

                ECParameters publicOnly;

                if (hasPublicParameters)
                {
                    publicOnly = parameters;
                    publicOnly.D = null;
                }
                else
                {
                    publicOnly = ExportPublicParametersFromPrivateKey(privateKey);
                }

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

        private static SafeSecKeyRefHandle ImportKey(ECParameters parameters)
        {
            AsnWriter keyWriter;
            bool hasPrivateKey;

            if (parameters.D != null)
            {
                keyWriter = EccKeyFormatHelper.WriteECPrivateKey(parameters);
                hasPrivateKey = true;
            }
            else
            {
                keyWriter = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(parameters);
                hasPrivateKey = false;
            }

            byte[] rented = CryptoPool.Rent(keyWriter.GetEncodedLength());

            if (!keyWriter.TryEncode(rented, out int written))
            {
                Debug.Fail("TryEncode failed with a pre-allocated buffer");
                throw new InvalidOperationException();
            }

            // Explicitly clear the inner buffer
            keyWriter.Reset();

            try
            {
                return Interop.AppleCrypto.ImportEphemeralKey(rented.AsSpan(0, written), hasPrivateKey);
            }
            finally
            {
                CryptoPool.Return(rented, written);
            }
        }

        internal unsafe int ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();

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
