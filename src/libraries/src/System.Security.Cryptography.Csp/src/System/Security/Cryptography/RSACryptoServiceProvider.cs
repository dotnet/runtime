// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Internal.NativeCrypto;


namespace System.Security.Cryptography
{
    public sealed partial class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm
    {
        private int _keySize;
        private CspParameters _parameters;
        private bool _randomKeyContainer;
        private SafeKeyHandle _safeKeyHandle;
        private SafeProvHandle _safeProvHandle;
        private static volatile CspProviderFlags s_UseMachineKeyStore = 0;


        public RSACryptoServiceProvider()
            : this(0, new CspParameters(CapiHelper.DefaultRsaProviderType,
                                       null,
                                       null,
                                       s_UseMachineKeyStore),
                                       true)
        {
        }

        public RSACryptoServiceProvider(int dwKeySize)
            : this(dwKeySize,
                  new CspParameters(CapiHelper.DefaultRsaProviderType,
                  null,
                  null,
                  s_UseMachineKeyStore), false)
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
            _parameters = CapiHelper.SaveCspParameters(CapiHelper.CspAlgorithmType.Rsa, parameters, s_UseMachineKeyStore, ref _randomKeyContainer);

            _legalKeySizesValue = new KeySizes[] { new KeySizes(384, 16384, 8) };
            _keySize = useDefaultKeySize ? 1024 : keySize;

            // If this is not a random container we generate, create it eagerly 
            // in the constructor so we can report any errors now.
            if (!_randomKeyContainer)
            {
                GetKeyPair();
            }
        }

        /// <summary>
        /// Retreives the key pair
        /// </summary>
        private void GetKeyPair()
        {
            if (_safeKeyHandle == null)
            {
                lock (this)
                {
                    if (_safeKeyHandle == null)
                    {
                        // We only attempt to generate a random key on desktop runtimes because the CoreCLR
                        // RSA surface area is limited to simply verifying signatures.  Since generating a
                        // random key to verify signatures will always lead to failure (unless we happend to
                        // win the lottery and randomly generate the signing key ...), there is no need
                        // to add this functionality to CoreCLR at this point.
                        CapiHelper.GetKeyPairHelper(CapiHelper.CspAlgorithmType.Rsa, _parameters,
                                            _randomKeyContainer, _keySize, ref _safeProvHandle, ref _safeKeyHandle);
                    }
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
                GetKeyPair();
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
                GetKeyPair();
                byte[] keySize = (byte[])CapiHelper.GetKeyParameter(_safeKeyHandle, Constants.CLR_KEYLEN);
                _keySize = (keySize[0] | (keySize[1] << 8) | (keySize[2] << 16) | (keySize[3] << 24));
                return _keySize;
            }
        }

        /// <summary>
        /// get set Persisted key in CSP 
        /// </summary>
        public bool PersistKeyInCsp
        {
            get
            {
                if (_safeProvHandle == null)
                {
                    lock (this)
                    {
                        if (_safeProvHandle == null)
                        {
                            _safeProvHandle = CapiHelper.CreateProvHandle(_parameters, _randomKeyContainer);
                        }
                    }
                }
                return CapiHelper.GetPersistKeyInCsp(_safeProvHandle);
            }
            set
            {
                bool oldPersistKeyInCsp = this.PersistKeyInCsp;
                if (value == oldPersistKeyInCsp)
                {
                    return; // Do nothing
                }
                CapiHelper.SetPersistKeyInCsp(_safeProvHandle, value);
            }
        }

        /// <summary>
        /// Gets the information of key if it is a public key
        /// </summary>
        public bool PublicOnly
        {
            get
            {
                GetKeyPair();
                byte[] publicKey = (byte[])CapiHelper.GetKeyParameter(_safeKeyHandle, Constants.CLR_PUBLICKEYONLY);
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
                return (s_UseMachineKeyStore == CspProviderFlags.UseMachineKeyStore);
            }
            set
            {
                s_UseMachineKeyStore = (value ? CspProviderFlags.UseMachineKeyStore : 0);
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
                throw new ArgumentNullException("rgb");
            }

            GetKeyPair();

            // size check -- must be at most the modulus size
            if (rgb.Length > (KeySize / 8))
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_Padding_DecDataTooBig, Convert.ToString(KeySize / 8)));
            }
            byte[] decryptedKey = null;
            CapiHelper.DecryptKey(_safeKeyHandle, rgb, rgb.Length, fOAEP, out decryptedKey);
            return decryptedKey;
        }

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
        /// <param name="rgb">raw data to encryt</param>
        /// <param name="fOAEP">true to use OAEP padding (PKCS #1 v2), false to use PKCS #1 type 2 padding</param>
        /// <returns>Encrypted key</returns>
        public byte[] Encrypt(byte[] rgb, bool fOAEP)
        {
            if (rgb == null)
            {
                throw new ArgumentNullException("rgb");
            }

            GetKeyPair();

            byte[] encryptedKey = null;
            CapiHelper.EncryptKey(_safeKeyHandle, rgb, rgb.Length, fOAEP, ref encryptedKey);
            return encryptedKey;
        }

        /// <summary>
        ///Exports a blob containing the key information associated with an RSACryptoServiceProvider object.
        /// </summary>
        public byte[] ExportCspBlob(bool includePrivateParameters)
        {
            GetKeyPair();
            return CapiHelper.ExportKeyBlob(includePrivateParameters, _safeKeyHandle);
        }

        /// <summary>
        /// Exports the RSAParameters
        /// </summary>
        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            GetKeyPair();
            byte[] cspBlob = ExportCspBlob(includePrivateParameters);
            return cspBlob.ToRSAParameters(includePrivateParameters);
        }

        /// <summary>
        /// This method helps Acquire the default CSP and avoids the need for static SafeProvHandle
        /// in CapiHelper class
        /// </summary>
        /// <param name="safeProvHandle"> SafeProvHandle. Intialized if successful</param>
        /// <returns>does not return. AcquireCSP throw exception</returns>
        private void AcquireSafeProviderHandle(ref SafeProvHandle safeProvHandle)
        {
            CapiHelper.AcquireCsp(new CspParameters(CapiHelper.DefaultRsaProviderType), ref safeProvHandle);
        }

        /// <summary>
        /// Imports a blob that represents RSA key information
        /// </summary>
        /// <param name="keyBlob"></param>
        public void ImportCspBlob(byte[] keyBlob)
        {
            // Free the current key handle
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed)
            {
                _safeKeyHandle.Dispose();
                _safeKeyHandle = null;
            }
            _safeKeyHandle = SafeKeyHandle.InvalidHandle;

            if (IsPublic(keyBlob))
            {
                SafeProvHandle safeProvHandleTemp = SafeProvHandle.InvalidHandle;
                AcquireSafeProviderHandle(ref safeProvHandleTemp);
                CapiHelper.ImportKeyBlob(safeProvHandleTemp, (CspProviderFlags)0, keyBlob, ref _safeKeyHandle);
                _safeProvHandle = safeProvHandleTemp;
            }
            else
            {
                if (_safeProvHandle == null)
                {
                    _safeProvHandle = CapiHelper.CreateProvHandle(_parameters, _randomKeyContainer);
                }
                CapiHelper.ImportKeyBlob(_safeProvHandle, _parameters.Flags, keyBlob, ref _safeKeyHandle);
            }
        }

        /// <summary>
        /// Imports the specified RSAParameters
        /// </summary>
        public override void ImportParameters(RSAParameters parameters)
        {
            // Free the current key handle
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed)
            {
                _safeKeyHandle.Dispose();
                _safeKeyHandle = null;
            }
            _safeKeyHandle = SafeKeyHandle.InvalidHandle;

            byte[] keyBlob = parameters.ToKeyBlob(CapiHelper.CALG_RSA_KEYX);
            ImportCspBlob(keyBlob);
            return;
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="buffer">The input data for which to compute the hash</param>
        /// <param name="offset">The offset into the array from which to begin using data</param>
        /// <param name="count">The number of bytes in the array to use as data. </param>
        /// <param name="halg">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        public byte[] SignData(byte[] buffer, int offset, int count, Object halg)
        {
            int calgHash = CapiHelper.ObjToHashAlgId(halg);
            HashAlgorithm hash = CapiHelper.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer, offset, count);
            return SignHash(hashVal, calgHash);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="buffer">The input data for which to compute the hash</param>
        /// <param name="halg">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        public byte[] SignData(byte[] buffer, Object halg)
        {
            int calgHash = CapiHelper.ObjToHashAlgId(halg);
            HashAlgorithm hash = CapiHelper.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer);
            return SignHash(hashVal, calgHash);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="inputStream">The input data for which to compute the hash</param>
        /// <param name="halg">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        public byte[] SignData(Stream inputStream, Object halg)
        {
            int calgHash = CapiHelper.ObjToHashAlgId(halg);
            HashAlgorithm hash = CapiHelper.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(inputStream);
            return SignHash(hashVal, calgHash);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="rgbHash">The input data for which to compute the hash</param>
        /// <param name="str">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        public byte[] SignHash(byte[] rgbHash, string str)
        {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
            if (PublicOnly)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);

            int calgHash = CapiHelper.NameOrOidToHashAlgId(str);

            return SignHash(rgbHash, calgHash);
        }

        /// <summary>
        /// Computes the hash value of a subset of the specified byte array using the specified hash algorithm, and signs the resulting hash value.
        /// </summary>
        /// <param name="rgbHash">The input data for which to compute the hash</param>
        /// <param name="calgHash">The hash algorithm to use to create the hash value. </param>
        /// <returns>The RSA signature for the specified data.</returns>
        private byte[] SignHash(byte[] rgbHash, int calgHash)
        {
            Debug.Assert(rgbHash != null);
            GetKeyPair();
            return CapiHelper.SignValue(_safeProvHandle, _safeKeyHandle, _parameters.KeyNumber, CapiHelper.CALG_RSA_SIGN, calgHash, rgbHash);
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
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null)
                throw new ArgumentNullException("rgbSignature");

            int calgHash = CapiHelper.NameOrOidToHashAlgId(str);
            return VerifyHash(rgbHash, calgHash, rgbSignature);
        }

        /// <summary>
        /// Verifies the signature of a hash value.
        /// </summary>
        private bool VerifyHash(byte[] rgbHash, int calgHash, byte[] rgbSignature)
        {
            GetKeyPair();
            return CapiHelper.VerifySign(_safeProvHandle, _safeKeyHandle, CapiHelper.CALG_RSA_SIGN, calgHash, rgbHash, rgbSignature);
        }

        /// <summary>
        /// find whether an RSA key blob is public.
        /// </summary>
        private static bool IsPublic(byte[] keyBlob)
        {
            if (keyBlob == null)
            {
                throw new ArgumentNullException("keyBlob");
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

        /// <summary>
        /// Since P is required, we will assume its presence is synonymous to a private key.
        /// </summary>
        /// <param name="rsaParams"></param>
        /// <returns></returns>
        private static bool IsPublic(RSAParameters rsaParams)
        {
            return (rsaParams.P == null);
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
                throw new ArgumentNullException("data");
            if (padding == null)
                throw new ArgumentNullException("padding");

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
                throw new ArgumentNullException("data");
            if (padding == null)
                throw new ArgumentNullException("padding");

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
                throw new ArgumentNullException("hash");
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException("padding");
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
                throw new ArgumentNullException("hash");
            if (signature == null)
                throw new ArgumentNullException("signature");
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException("padding");
            if (padding != RSASignaturePadding.Pkcs1)
                throw PaddingModeNotSupported();

            return VerifyHash(hash, GetAlgorithmId(hashAlgorithm), signature);
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
