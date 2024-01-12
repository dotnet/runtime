// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Internal.Cryptography;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        /// <summary>
        ///     Generate a key from a secret agreement
        /// </summary>
        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        private static partial ErrorCode NCryptDeriveKey(
            SafeNCryptSecretHandle hSharedSecret,
            string pwszKDF,
            ref NCryptBufferDesc pParameterList,
            [MarshalAs(UnmanagedType.LPArray)] byte[]? pbDerivedKey,
            int cbDerivedKey,
            out int pcbResult,
            SecretAgreementFlags dwFlags);

        /// <summary>
        ///     Derive key material from a hash or HMAC KDF
        /// </summary>
        /// <returns></returns>
        private static byte[] DeriveKeyMaterial(
            SafeNCryptSecretHandle secretAgreement,
            string kdf,
            string hashAlgorithm,
            byte[]? hmacKey,
            byte[]? secretPrepend,
            byte[]? secretAppend,
            SecretAgreementFlags flags)
        {
            // First marshal the hash algoritm
            IntPtr hashAlgorithmString = IntPtr.Zero;

            try
            {
                hashAlgorithmString = Marshal.StringToCoTaskMemUni(hashAlgorithm);

                Span<NCryptBuffer> parameters = stackalloc NCryptBuffer[4];
                int parameterCount = 0;
                // We always need to marshal the hashing function
                NCryptBuffer hashAlgorithmBuffer = default;
                hashAlgorithmBuffer.cbBuffer = (hashAlgorithm.Length + 1) * sizeof(char);
                hashAlgorithmBuffer.BufferType = BufferType.KdfHashAlgorithm;
                hashAlgorithmBuffer.pvBuffer = hashAlgorithmString;

                parameters[parameterCount] = hashAlgorithmBuffer;
                parameterCount++;

                unsafe
                {
                    fixed (byte* pHmacKey = hmacKey, pSecretPrepend = secretPrepend, pSecretAppend = secretAppend)
                    {
                        //
                        // Now marshal the other parameters
                        //

                        if (pHmacKey != null)
                        {
                            NCryptBuffer hmacKeyBuffer = default;
                            hmacKeyBuffer.cbBuffer = hmacKey!.Length;
                            hmacKeyBuffer.BufferType = BufferType.KdfHmacKey;
                            hmacKeyBuffer.pvBuffer = new IntPtr(pHmacKey);

                            parameters[parameterCount] = hmacKeyBuffer;
                            parameterCount++;
                        }

                        if (pSecretPrepend != null)
                        {
                            NCryptBuffer secretPrependBuffer = default;
                            secretPrependBuffer.cbBuffer = secretPrepend!.Length;
                            secretPrependBuffer.BufferType = BufferType.KdfSecretPrepend;
                            secretPrependBuffer.pvBuffer = new IntPtr(pSecretPrepend);

                            parameters[parameterCount] = secretPrependBuffer;
                            parameterCount++;
                        }

                        if (pSecretAppend != null)
                        {
                            NCryptBuffer secretAppendBuffer = default;
                            secretAppendBuffer.cbBuffer = secretAppend!.Length;
                            secretAppendBuffer.BufferType = BufferType.KdfSecretAppend;
                            secretAppendBuffer.pvBuffer = new IntPtr(pSecretAppend);

                            parameters[parameterCount] = secretAppendBuffer;
                            parameterCount++;
                        }

                        return DeriveKeyMaterial(
                            secretAgreement,
                            kdf,
                            parameters.Slice(0, parameterCount),
                            flags);
                    }
                }
            }
            finally
            {
                if (hashAlgorithmString != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(hashAlgorithmString);
                }
            }
        }

        /// <summary>
        ///     Derive key material using a given KDF and secret agreement
        /// </summary>
        private static unsafe byte[] DeriveKeyMaterial(
            SafeNCryptSecretHandle secretAgreement,
            string kdf,
            ReadOnlySpan<NCryptBuffer> parameters,
            SecretAgreementFlags flags)
        {
            fixed (NCryptBuffer* pParameters = &MemoryMarshal.GetReference(parameters))
            {
                NCryptBufferDesc parameterDesc = default;
                parameterDesc.ulVersion = 0;
                parameterDesc.cBuffers = parameters.Length;
                parameterDesc.pBuffers = new IntPtr(pParameters);

                // Figure out how big the key material is
                ErrorCode error = NCryptDeriveKey(
                    secretAgreement,
                    kdf,
                    ref parameterDesc,
                    null,
                    0,
                    out int keySize,
                    flags);

                if (error != ErrorCode.ERROR_SUCCESS && !error.IsBufferTooSmall())
                {
                    throw error.ToCryptographicException();
                }

                // Allocate memory for the key material and generate it
                byte[] keyMaterial = new byte[keySize];

                error = NCryptDeriveKey(
                    secretAgreement,
                    kdf,
                    ref parameterDesc,
                    keyMaterial,
                    keyMaterial.Length,
                    out keySize,
                    flags);

                if (error != ErrorCode.ERROR_SUCCESS)
                {
                    throw error.ToCryptographicException();
                }

                // Just in case it shrank the answer once it had a buffer.
                Array.Resize(ref keyMaterial, Math.Min(keySize, keyMaterial.Length));
                return keyMaterial;
            }
        }

        /// <summary>
        ///     Derive key material from a secret agreement using a hash KDF
        /// </summary>
        internal static byte[] DeriveKeyMaterialHash(
            SafeNCryptSecretHandle secretAgreement,
            string hashAlgorithm,
            byte[]? secretPrepend,
            byte[]? secretAppend,
            SecretAgreementFlags flags)
        {
            return DeriveKeyMaterial(
                secretAgreement,
                BCryptNative.KeyDerivationFunction.Hash,
                hashAlgorithm,
                null,
                secretPrepend,
                secretAppend,
                flags);
        }

        /// <summary>
        ///     Derive key material from a secret agreement using a HMAC KDF
        /// </summary>
        internal static byte[] DeriveKeyMaterialHmac(
            SafeNCryptSecretHandle secretAgreement,
            string hashAlgorithm,
            byte[]? hmacKey,
            byte[]? secretPrepend,
            byte[]? secretAppend,
            SecretAgreementFlags flags)
        {
            return DeriveKeyMaterial(
                secretAgreement,
                BCryptNative.KeyDerivationFunction.Hmac,
                hashAlgorithm,
                hmacKey,
                secretPrepend,
                secretAppend,
                flags);
        }

        /// <summary>
        ///     Derive key material from a secret agreement using the TLS KDF
        /// </summary>
        internal static unsafe byte[] DeriveKeyMaterialTls(
            SafeNCryptSecretHandle secretAgreement,
            byte[] label,
            byte[] seed,
            SecretAgreementFlags flags)
        {
            Span<NCryptBuffer> buffers = stackalloc NCryptBuffer[2];

            fixed (byte* pLabel = label, pSeed = seed)
            {
                NCryptBuffer labelBuffer = default;
                labelBuffer.cbBuffer = label.Length;
                labelBuffer.BufferType = BufferType.KdfTlsLabel;
                labelBuffer.pvBuffer = new IntPtr(pLabel);
                buffers[0] = labelBuffer;

                NCryptBuffer seedBuffer = default;
                seedBuffer.cbBuffer = seed.Length;
                seedBuffer.BufferType = BufferType.KdfTlsSeed;
                seedBuffer.pvBuffer = new IntPtr(pSeed);
                buffers[1] = seedBuffer;

                return DeriveKeyMaterial(
                    secretAgreement,
                    BCryptNative.KeyDerivationFunction.Tls,
                    buffers,
                    flags);
            }
        }

        internal static unsafe byte[] DeriveKeyMaterialTruncate(
            SafeNCryptSecretHandle secretAgreement,
            SecretAgreementFlags flags)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            {
                throw new PlatformNotSupportedException();
            }

            byte[] result = DeriveKeyMaterial(
                secretAgreement,
                BCryptNative.KeyDerivationFunction.Raw,
                ReadOnlySpan<NCryptBuffer>.Empty,
                flags);

            // Win32 returns the result as little endian. So we need to flip it to big endian.
            Array.Reverse(result);
            return result;
        }
    }
}
