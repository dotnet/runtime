// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using Internal.NativeCrypto;
using static Internal.NativeCrypto.CapiHelper;

namespace System.Security.Cryptography
{
    public sealed partial class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm
    {
        private int _keySize;
        private readonly CspParameters _parameters;
        private readonly bool _randomKeyContainer;
        private SafeKeyHandle _safeKeyHandle;
        private SafeProvHandle _safeProvHandle;
        private static volatile CspProviderFlags s_useMachineKeyStore = 0;

        public RSACryptoServiceProvider()
            : this(0, new CspParameters(CapiHelper.DefaultRsaProviderType,
                                       null,
                                       null,
                                       s_useMachineKeyStore),
                                       true)
        {
        }

        public RSACryptoServiceProvider(int dwKeySize)
            : this(dwKeySize,
                  new CspParameters(CapiHelper.DefaultRsaProviderType,
                  null,
                  null,
                  s_useMachineKeyStore), false)
        {
        }

        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters)
            : this(dwKeySize, parameters, false)
        {
        }

        public RSACryptoServiceProvider(CspParameters parameters)
            : this(0, parameters, true)
        {
        }

        private RSACryptoServiceProvider(int keySize, CspParameters parameters, bool useDefaultKeySize)
        {
            if (keySize < 0)
            {
                throw new ArgumentOutOfRangeException("dwKeySize", "ArgumentOutOfRange_NeedNonNegNum");
            }

            _parameters = CapiHelper.SaveCspParameters(
                CapiHelper.CspAlgorithmType.Rsa,
                parameters,
                s_useMachineKeyStore,
                out _randomKeyContainer);

            _keySize = useDefaultKeySize ? 1024 : keySize;

            // If this is not a random container we generate, create it eagerly
            // in the constructor so we can report any errors now.
            if (!_randomKeyContainer)
            {
                // Force-read the SafeKeyHandle property, which will summon it into existence.
                SafeKeyHandle localHandle = SafeKeyHandle;
                Debug.Assert(localHandle != null);
            }
        }

        private SafeProvHandle SafeProvHandle
        {
            get
            {
                if (_safeProvHandle == null)
                {
                    lock (_parameters)
                    {
                        if (_safeProvHandle == null)
                        {
                            SafeProvHandle hProv = CapiHelper.CreateProvHandle(_parameters, _randomKeyContainer);

                            Debug.Assert(hProv != null);
                            Debug.Assert(!hProv.IsInvalid);
                            Debug.Assert(!hProv.IsClosed);

                            _safeProvHandle = hProv;
                        }
                    }

                    return _safeProvHandle;
                }

                return _safeProvHandle;
            }
            set
            {
                lock (_parameters)
                {
                    SafeProvHandle current = _safeProvHandle;

                    if (ReferenceEquals(value, current))
                    {
                        return;
                    }

                    if (current != null)
                    {
                        SafeKeyHandle keyHandle = _safeKeyHandle;
                        _safeKeyHandle = null;
                        keyHandle?.Dispose();
                        current.Dispose();
                    }

                    _safeProvHandle = value;
                }
            }
        }

        private SafeKeyHandle SafeKeyHandle
        {
            get
            {
                if (_safeKeyHandle == null)
                {
                    lock (_parameters)
                    {
                        if (_safeKeyHandle == null)
                        {
                            SafeKeyHandle hKey = CapiHelper.GetKeyPairHelper(
                                CapiHelper.CspAlgorithmType.Rsa,
                                _parameters,
                                _keySize,
                                SafeProvHandle);

                            Debug.Assert(hKey != null);
                            Debug.Assert(!hKey.IsInvalid);
                            Debug.Assert(!hKey.IsClosed);

                            _safeKeyHandle = hKey;
                        }
                    }
                }

                return _safeKeyHandle;
            }

            set
            {
                lock (_parameters)
                {
                    SafeKeyHandle current = _safeKeyHandle;

                    if (ReferenceEquals(value, current))
                    {
                        return;
                    }

                    _safeKeyHandle = value;
                    current?.Dispose();
                }
            }
        }

        /// <summary>
        /// CspKeyContainerInfo property
        /// </summary>
        public CspKeyContainerInfo CspKeyContainerInfo
        {
            get
            {
                // Desktop compat: Read the SafeKeyHandle property to force the key to load,
                // because it might throw here.
                SafeKeyHandle localHandle = SafeKeyHandle;
                Debug.Assert(localHandle != null);

                return new CspKeyContainerInfo(_parameters, _randomKeyContainer);
            }
        }

        /// <summary>
        /// _keySize property
        /// </summary>
        public override int KeySize
        {
            get
            {
                byte[] keySize = CapiHelper.GetKeyParameter(SafeKeyHandle, Constants.CLR_KEYLEN);
                _keySize = (keySize[0] | (keySize[1] << 8) | (keySize[2] << 16) | (keySize[3] << 24));
                return _keySize;
            }
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                return new[] { new KeySizes(384, 16384, 8) };
            }
        }

        /// <summary>
        /// get set Persisted key in CSP
        /// </summary>
        public bool PersistKeyInCsp
        {
            get
            {
                return CapiHelper.GetPersistKeyInCsp(SafeProvHandle);
            }
            set
            {
                bool oldPersistKeyInCsp = this.PersistKeyInCsp;
                if (value == oldPersistKeyInCsp)
                {
                    return; // Do nothing
                }
                CapiHelper.SetPersistKeyInCsp(SafeProvHandle, value);
            }
        }

        /// <summary>
        /// Gets the information of key if it is a public key
        /// </summary>
        public bool PublicOnly
        {
            get
            {
                byte[] publicKey = CapiHelper.GetKeyParameter(SafeKeyHandle, Constants.CLR_PUBLICKEYONLY);
                return (publicKey[0] == 1);
            }
        }

        /// <summary>
        /// MachineKey store properties
        /// </summary>
        public static bool UseMachineKeyStore
        {
            get
            {
                return (s_useMachineKeyStore == CspProviderFlags.UseMachineKeyStore);
            }
            set
            {
                s_useMachineKeyStore = (value ? CspProviderFlags.UseMachineKeyStore : 0);
            }
        }

        /// <summary>
        ///     Decrypt raw data, generally used for decrypting symmetric key material
        /// </summary>
        /// <param name="rgb">encrypted data</param>
        /// <param name="fOAEP">true to use OAEP padding (PKCS #1 v2), false to use PKCS #1 type 2 padding</param>
        /// <returns>decrypted data</returns>
        public byte[] Decrypt(byte[] rgb, bool fOAEP)
        {
            if (rgb == null)
            {
                throw new ArgumentNullException(nameof(rgb));
            }

            // Save the KeySize value to a local because it has non-trivial cost.
            int keySize = KeySize;

            // size check -- must be at most the modulus size
            if (rgb.Length > (keySize / 8))
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_Padding_DecDataTooBig, Convert.ToString(keySize / 8)));
            }

            byte[] decryptedKey;
            CapiHelper.DecryptKey(SafeKeyHandle, rgb, rgb.Length, fOAEP, out decryptedKey);
            return decryptedKey;
        }

        /// <summary>
        /// This method is not supported. Use Decrypt(byte[], RSAEncryptionPadding) instead.
        /// </summary>
        public override byte[] DecryptValue(byte[] rgb) => base.DecryptValue(rgb);

        /// <summary>
        /// Dispose the key handles
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed)
            {
                _safeKeyHandle.Dispose();
            }
            if (_safeProvHandle != null && !_safeProvHandle.IsClosed)
            {
                _safeProvHandle.Dispose();
            }
        }

        /// <summary>
        ///     Encrypt raw data, generally used for encrypting symmetric key material.
        /// </summary>
        /// <remarks>
        ///     This method can only encrypt (keySize - 88 bits) of data, so should not be used for encrypting
        ///     arbitrary byte arrays. Instead, encrypt a symmetric key with this method, and use the symmetric
        ///     key to encrypt the sensitive data.
        /// </remarks>
        /// <param name="rgb">raw data to encrypt</param>
        /// <param name="fOAEP">true to use OAEP padding (PKCS #1 v2), false to use PKCS #1 type 2 padding</param>
        /// <returns>Encrypted key</returns>
        public byte[] Encrypt(byte[] rgb, bool fOAEP)
        {
            if (rgb == null)
            {
                throw new ArgumentNullException(nameof(rgb));
            }

            byte[] encryptedKey = null;
            CapiHelper.EncryptKey(SafeKeyHandle, rgb, rgb.Length, fOAEP, ref encryptedKey);
            return encryptedKey;
        }

        /// <summary>
        /// This method is not supported. Use Encrypt(byte[], RSAEncryptionPadding) instead.
        /// </summary>
        public override byte[] EncryptValue(byte[] rgb) => base.EncryptValue(rgb);

        /// <summary>
        ///Exports a blob containing the key information associated with an RSACryptoServiceProvider object.
        /// </summary>
        public byte[] ExportCspBlob(bool includePrivateParameters)
        {
            return CapiHelper.ExportKeyBlob(includePrivateParameters, SafeKeyHandle);
        }

        /// <summary>
        /// Exports the RSAParameters
        /// </summary>
        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            byte[] cspBlob = ExportCspBlob(includePrivateParameters);
            return cspBlob.ToRSAParameters(includePrivateParameters);
        }

        /// <summary>
        /// This method helps Acquire the default CSP and avoids the need for static SafeProvHandle
        /// in CapiHelper class
        /// </summary>
        private SafeProvHandle AcquireSafeProviderHandle()
        {
            SafeProvHandle safeProvHandle;
            CapiHelper.AcquireCsp(new CspParameters(CapiHelper.DefaultRsaProviderType), out safeProvHandle);
            return safeProvHandle;
        }

        /// <summary>
        /// Imports a blob that represents RSA key information
        /// </summary>
        /// <param name="keyBlob"></param>
        public void ImportCspBlob(byte[] keyBlob)
        {
            SafeKeyHandle safeKeyHandle;

            if (IsPublic(keyBlob))
            {
                SafeProvHandle safeProvHandleTemp = AcquireSafeProviderHandle();
                CapiHelper.ImportKeyBlob(safeProvHandleTemp, (CspProviderFlags)0, false, keyBlob, out safeKeyHandle);

                // The property set will take care of releasing any already-existing resources.
                SafeProvHandle = safeProvHandleTemp;
            }
            else
            {
                CapiHelper.ImportKeyBlob(SafeProvHandle, _parameters.Flags, false, keyBlob, out safeKeyHandle);
            }

            // The property set will take care of releasing any already-existing resources.
            SafeKeyHandle = safeKeyHandle;
        }

        /// <summary>
        /// Imports the specified RSAParameters
        /// </summary>
        public override void ImportParameters(RSAParameters parameters)
        {
            byte[] keyBlob = parameters.ToKeyBlob();
            ImportCspBlob(keyBlob);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the
        /// specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="buffer">The input data for which to compute the hash</param>
        /// <param name="offset">The offset into the array from which to begin using data</param>
        /// <param name="count">The number of bytes in the array to use as data. </param>
        /// <param name="halg">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        public byte[] SignData(byte[] buffer, int offset, int count, object halg)
        {
            int calgHash = CapiHelper.ObjToHashAlgId(halg);
            HashAlgorithm hash = CapiHelper.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer, offset, count);
            return SignHash(hashVal, calgHash);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the
        /// specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="buffer">The input data for which to compute the hash</param>
        /// <param name="halg">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        public byte[] SignData(byte[] buffer, object halg)
        {
            int calgHash = CapiHelper.ObjToHashAlgId(halg);
            HashAlgorithm hash = CapiHelper.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer);
            return SignHash(hashVal, calgHash);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the
        /// specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="inputStream">The input data for which to compute the hash</param>
        /// <param name="halg">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        public byte[] SignData(Stream inputStream, object halg)
        {
            int calgHash = CapiHelper.ObjToHashAlgId(halg);
            HashAlgorithm hash = CapiHelper.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(inputStream);
            return SignHash(hashVal, calgHash);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the
        /// specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="rgbHash">The input data for which to compute the hash</param>
        /// <param name="str">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        public byte[] SignHash(byte[] rgbHash, string str)
        {
            if (rgbHash == null)
                throw new ArgumentNullException(nameof(rgbHash));
            if (PublicOnly)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);

            int calgHash = CapiHelper.NameOrOidToHashAlgId(str, OidGroup.HashAlgorithm);

            return SignHash(rgbHash, calgHash);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the
        /// specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="rgbHash">The input data for which to compute the hash</param>
        /// <param name="calgHash">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        private byte[] SignHash(byte[] rgbHash, int calgHash)
        {
            Debug.Assert(rgbHash != null);

            return CapiHelper.SignValue(
                SafeProvHandle,
                SafeKeyHandle,
                _parameters.KeyNumber,
                CapiHelper.CALG_RSA_SIGN,
                calgHash,
                rgbHash);
        }

        /// <summary>
        /// Verifies the signature of a hash value.
        /// </summary>
        public bool VerifyData(byte[] buffer, object halg, byte[] signature)
        {
            int calgHash = CapiHelper.ObjToHashAlgId(halg);
            HashAlgorithm hash = CapiHelper.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer);
            return VerifyHash(hashVal, calgHash, signature);
        }

        /// <summary>
        /// Verifies the signature of a hash value.
        /// </summary>
        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature)
        {
            if (rgbHash == null)
                throw new ArgumentNullException(nameof(rgbHash));
            if (rgbSignature == null)
                throw new ArgumentNullException(nameof(rgbSignature));

            int calgHash = CapiHelper.NameOrOidToHashAlgId(str, OidGroup.HashAlgorithm);
            return VerifyHash(rgbHash, calgHash, rgbSignature);
        }

        /// <summary>
        /// Verifies the signature of a hash value.
        /// </summary>
        private bool VerifyHash(byte[] rgbHash, int calgHash, byte[] rgbSignature)
        {
            return CapiHelper.VerifySign(
                SafeProvHandle,
                SafeKeyHandle,
                CapiHelper.CALG_RSA_SIGN,
                calgHash,
                rgbHash,
                rgbSignature);
        }

        /// <summary>
        /// find whether an RSA key blob is public.
        /// </summary>
        private static bool IsPublic(byte[] keyBlob)
        {
            if (keyBlob == null)
            {
                throw new ArgumentNullException(nameof(keyBlob));
            }
            // The CAPI RSA public key representation consists of the following sequence:
            //  - BLOBHEADER
            //  - RSAPUBKEY

            // The first should be PUBLICKEYBLOB and magic should be RSA_PUB_MAGIC "RSA1"
            if (keyBlob[0] != CapiHelper.PUBLICKEYBLOB)
            {
                return false;
            }
            if (keyBlob[11] != 0x31 || keyBlob[10] != 0x41 || keyBlob[9] != 0x53 || keyBlob[8] != 0x52)
            {
                return false;
            }
            return true;
        }

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            // we're sealed and the base should have checked this already
            Debug.Assert(data != null);
            Debug.Assert(count >= 0 && count <= data.Length);
            Debug.Assert(offset >= 0 && offset <= data.Length - count);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (HashAlgorithm hash = GetHashAlgorithm(hashAlgorithm))
            {
                return hash.ComputeHash(data, offset, count);
            }
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            // we're sealed and the base should have checked this already
            Debug.Assert(data != null);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (HashAlgorithm hash = GetHashAlgorithm(hashAlgorithm))
            {
                return hash.ComputeHash(data);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "MD5 is used when the user asks for it.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is used when the user asks for it.")]
        private static HashAlgorithm GetHashAlgorithm(HashAlgorithmName hashAlgorithm)
        {
            switch (hashAlgorithm.Name)
            {
                case "MD5":
                    return MD5.Create();
                case "SHA1":
                    return SHA1.Create();
                case "SHA256":
                    return SHA256.Create();
                case "SHA384":
                    return SHA384.Create();
                case "SHA512":
                    return SHA512.Create();
                default:
                    throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
            }
        }

        private static int GetAlgorithmId(HashAlgorithmName hashAlgorithm)
        {
            switch (hashAlgorithm.Name)
            {
                case "MD5":
                    return CapiHelper.CALG_MD5;
                case "SHA1":
                    return CapiHelper.CALG_SHA1;
                case "SHA256":
                    return CapiHelper.CALG_SHA_256;
                case "SHA384":
                    return CapiHelper.CALG_SHA_384;
                case "SHA512":
                    return CapiHelper.CALG_SHA_512;
                default:
                    throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
            }
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            if (padding == RSAEncryptionPadding.Pkcs1)
            {
                return Encrypt(data, fOAEP: false);
            }
            else if (padding == RSAEncryptionPadding.OaepSHA1)
            {
                return Encrypt(data, fOAEP: true);
            }
            else
            {
                throw PaddingModeNotSupported();
            }
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            if (padding == RSAEncryptionPadding.Pkcs1)
            {
                return Decrypt(data, fOAEP: false);
            }
            else if (padding == RSAEncryptionPadding.OaepSHA1)
            {
                return Decrypt(data, fOAEP: true);
            }
            else
            {
                throw PaddingModeNotSupported();
            }
        }

        public override byte[] SignHash(
            byte[] hash,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));
            if (padding != RSASignaturePadding.Pkcs1)
                throw PaddingModeNotSupported();

            return SignHash(hash, GetAlgorithmId(hashAlgorithm));
        }

        public override bool VerifyHash(
            byte[] hash,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));
            if (padding != RSASignaturePadding.Pkcs1)
                throw PaddingModeNotSupported();

            return VerifyHash(hash, GetAlgorithmId(hashAlgorithm), signature);
        }

        public override string KeyExchangeAlgorithm
        {
            get
            {
                if (_parameters.KeyNumber == (int) KeySpec.AT_KEYEXCHANGE)
                {
                    return "RSA-PKCS1-KeyEx";
                }
                return null;
            }
        }

        public override string SignatureAlgorithm
        {
            get
            {
                return "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
            }
        }

        private static Exception PaddingModeNotSupported()
        {
            return new CryptographicException(SR.Cryptography_InvalidPaddingMode);
        }

        private static Exception HashAlgorithmNameNullOrEmpty()
        {
            return new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
        }
    }
}
