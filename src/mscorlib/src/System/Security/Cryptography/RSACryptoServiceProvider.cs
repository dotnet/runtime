// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// CSP-based implementation of RSA
//

namespace System.Security.Cryptography {
    using System;
    using System.Globalization;
    using System.IO;
    using System.Security;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    // Object layout of the RSAParameters structure
    internal class RSACspObject {
        internal byte[] Exponent;
        internal byte[] Modulus;
        internal byte[] P;
        internal byte[] Q;
        internal byte[] DP;
        internal byte[] DQ;
        internal byte[] InverseQ;
        internal byte[] D;
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class RSACryptoServiceProvider : RSA
        , ICspAsymmetricAlgorithm
    {
        private int _dwKeySize;
        private CspParameters  _parameters;
        private bool _randomKeyContainer;
        [System.Security.SecurityCritical] // auto-generated
        private SafeProvHandle _safeProvHandle;
        [System.Security.SecurityCritical] // auto-generated
        private SafeKeyHandle _safeKeyHandle;

        private static volatile CspProviderFlags s_UseMachineKeyStore = 0;

        //
        // QCalls
        //

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void DecryptKey(SafeKeyHandle pKeyContext,
                                              [MarshalAs(UnmanagedType.LPArray)] byte[] pbEncryptedKey,
                                              int cbEncryptedKey,
                                              [MarshalAs(UnmanagedType.Bool)] bool fOAEP,
                                              ObjectHandleOnStack ohRetDecryptedKey);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void EncryptKey(SafeKeyHandle pKeyContext,
                                              [MarshalAs(UnmanagedType.LPArray)] byte[] pbKey,
                                              int cbKey,
                                              [MarshalAs(UnmanagedType.Bool)] bool fOAEP,
                                              ObjectHandleOnStack ohRetEncryptedKey);
        
        //
        // public constructors
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        public RSACryptoServiceProvider() 
            : this(0, new CspParameters(Utils.DefaultRsaProviderType, null, null, s_UseMachineKeyStore), true) {
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public RSACryptoServiceProvider(int dwKeySize) 
            : this(dwKeySize, new CspParameters(Utils.DefaultRsaProviderType, null, null, s_UseMachineKeyStore), false) {
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public RSACryptoServiceProvider(CspParameters parameters) 
            : this(0, parameters, true) {
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters)
            : this(dwKeySize, parameters, false) {
        }

        //
        // private methods
        //

        [System.Security.SecurityCritical]  // auto-generated
        private RSACryptoServiceProvider(int dwKeySize, CspParameters parameters, bool useDefaultKeySize) {
            if (dwKeySize < 0)
                throw new ArgumentOutOfRangeException("dwKeySize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            _parameters = Utils.SaveCspParameters(CspAlgorithmType.Rsa, parameters, s_UseMachineKeyStore, ref _randomKeyContainer);

                LegalKeySizesValue = new KeySizes[] { new KeySizes(384, 16384, 8) };
            _dwKeySize = useDefaultKeySize ? 1024 : dwKeySize;

            // If this is not a random container we generate, create it eagerly 
            // in the constructor so we can report any errors now.
            if (!_randomKeyContainer 
#if !FEATURE_CORECLR                
                || Environment.GetCompatibilityFlag(CompatibilityFlag.EagerlyGenerateRandomAsymmKeys)
#endif //!FEATURE_CORECLR
                )
                GetKeyPair();
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void GetKeyPair () {
            if (_safeKeyHandle == null) {
                lock (this) {
                    if (_safeKeyHandle == null) {
                        // We only attempt to generate a random key on desktop runtimes because the CoreCLR
                        // RSA surface area is limited to simply verifying signatures.  Since generating a
                        // random key to verify signatures will always lead to failure (unless we happend to
                        // win the lottery and randomly generate the signing key ...), there is no need
                        // to add this functionality to CoreCLR at this point.
                        Utils.GetKeyPairHelper(CspAlgorithmType.Rsa, _parameters, _randomKeyContainer, _dwKeySize, ref _safeProvHandle, ref _safeKeyHandle);
                    }
                }
            }
        }

        [System.Security.SecuritySafeCritical] // overrides public transparent member
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed)
                _safeKeyHandle.Dispose();
            if (_safeProvHandle != null && !_safeProvHandle.IsClosed)
                _safeProvHandle.Dispose();
        }

        //
        // public properties
        //

        [System.Runtime.InteropServices.ComVisible(false)]
        public bool PublicOnly {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                GetKeyPair();
                byte[] publicKey = (byte[]) Utils._GetKeyParameter(_safeKeyHandle, Constants.CLR_PUBLICKEYONLY);
                return (publicKey[0] == 1);
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public CspKeyContainerInfo CspKeyContainerInfo {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                GetKeyPair();
                return new CspKeyContainerInfo(_parameters, _randomKeyContainer);
            }
        }

        public override int KeySize {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                GetKeyPair();
                byte[] keySize = (byte[]) Utils._GetKeyParameter(_safeKeyHandle, Constants.CLR_KEYLEN);
                _dwKeySize = (keySize[0] | (keySize[1] << 8) | (keySize[2] << 16) | (keySize[3] << 24));
                return _dwKeySize;
            }
        }

        public override string KeyExchangeAlgorithm {
            get {
                if (_parameters.KeyNumber == Constants.AT_KEYEXCHANGE)
                    return "RSA-PKCS1-KeyEx";
                return null;
            }
        }

        public override string SignatureAlgorithm {
            get { return "http://www.w3.org/2000/09/xmldsig#rsa-sha1"; }
        }

        public static bool UseMachineKeyStore {
            get { return (s_UseMachineKeyStore == CspProviderFlags.UseMachineKeyStore); }
            set { s_UseMachineKeyStore = (value ? CspProviderFlags.UseMachineKeyStore : 0); }
        }

        public bool PersistKeyInCsp {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (_safeProvHandle == null) {
                    lock (this) {
                        if (_safeProvHandle == null)
                            _safeProvHandle = Utils.CreateProvHandle(_parameters, _randomKeyContainer);
                    }
                }
                return Utils.GetPersistKeyInCsp(_safeProvHandle); 
            }
            [System.Security.SecuritySafeCritical]  // auto-generated
            set {
                bool oldPersistKeyInCsp = this.PersistKeyInCsp;
                if (value == oldPersistKeyInCsp)
                    return;

                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    if (!value) {
                        KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Delete);
                        kp.AccessEntries.Add(entry);
                    } else {
                        KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Create);
                        kp.AccessEntries.Add(entry);
                    }
                    kp.Demand();
                }

                Utils.SetPersistKeyInCsp(_safeProvHandle, value);
            }
        }

        //
        // public methods
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override RSAParameters ExportParameters (bool includePrivateParameters) {
            GetKeyPair();
            if (includePrivateParameters) {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Export);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }
            }
            RSACspObject rsaCspObject = new RSACspObject();
            int blobType = includePrivateParameters ? Constants.PRIVATEKEYBLOB : Constants.PUBLICKEYBLOB;
            // _ExportKey will check for failures and throw an exception
            Utils._ExportKey(_safeKeyHandle, blobType, rsaCspObject);
            return RSAObjectToStruct(rsaCspObject);
        }

#if FEATURE_LEGACYNETCFCRYPTO
        [System.Security.SecurityCritical]  
#else
        [System.Security.SecuritySafeCritical]  // auto-generated
#endif
        [System.Runtime.InteropServices.ComVisible(false)]
        public byte[] ExportCspBlob (bool includePrivateParameters) {
            GetKeyPair();
            return Utils.ExportCspBlobHelper(includePrivateParameters, _parameters, _safeKeyHandle);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void ImportParameters(RSAParameters parameters) {
            // Free the current key handle
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed) {
                _safeKeyHandle.Dispose();
                _safeKeyHandle = null;
            }

            RSACspObject rsaCspObject = RSAStructToObject(parameters);
            _safeKeyHandle = SafeKeyHandle.InvalidHandle;

            if (IsPublic(parameters)) {
                // Use our CRYPT_VERIFYCONTEXT handle, CRYPT_EXPORTABLE is not applicable to public only keys, so pass false
                Utils._ImportKey(Utils.StaticProvHandle, Constants.CALG_RSA_KEYX, (CspProviderFlags) 0, rsaCspObject, ref _safeKeyHandle);
            } else {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Import);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }
                if (_safeProvHandle == null)
                    _safeProvHandle = Utils.CreateProvHandle(_parameters, _randomKeyContainer);
                // Now, import the key into the CSP; _ImportKey will check for failures.
                Utils._ImportKey(_safeProvHandle, Constants.CALG_RSA_KEYX, _parameters.Flags, rsaCspObject, ref _safeKeyHandle);
            }
        }

#if FEATURE_LEGACYNETCFCRYPTO
        [System.Security.SecurityCritical]  
#else
        [System.Security.SecuritySafeCritical]  // auto-generated
#endif
        [System.Runtime.InteropServices.ComVisible(false)]
        public void ImportCspBlob (byte[] keyBlob) {
            Utils.ImportCspBlobHelper(CspAlgorithmType.Rsa, keyBlob, IsPublic(keyBlob), ref _parameters, _randomKeyContainer, ref _safeProvHandle, ref _safeKeyHandle);
        }

        public byte[] SignData(Stream inputStream, Object halg) {
            int calgHash = Utils.ObjToAlgId(halg, OidGroup.HashAlgorithm);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(inputStream);
            return SignHash(hashVal, calgHash);
        }

        public byte[] SignData(byte[] buffer, Object halg) {
            int calgHash = Utils.ObjToAlgId(halg, OidGroup.HashAlgorithm);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer);
            return SignHash(hashVal, calgHash);
        }

        public byte[] SignData(byte[] buffer, int offset, int count, Object halg) {
            int calgHash = Utils.ObjToAlgId(halg, OidGroup.HashAlgorithm);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer, offset, count);
            return SignHash(hashVal, calgHash);
        }

        public bool VerifyData(byte[] buffer, Object halg, byte[] signature) {
            int calgHash = Utils.ObjToAlgId(halg, OidGroup.HashAlgorithm);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer);
            return VerifyHash(hashVal, calgHash, signature);
        }

        public byte[] SignHash(byte[] rgbHash, string str) {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
            Contract.EndContractBlock();
            if (PublicOnly)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NoPrivateKey"));

            int calgHash = X509Utils.NameOrOidToAlgId(str, OidGroup.HashAlgorithm);
            return SignHash(rgbHash, calgHash);
        }

        [SecuritySafeCritical]
        internal byte[] SignHash(byte[] rgbHash, int calgHash) {
            Contract.Requires(rgbHash != null);

            GetKeyPair();
            if (!CspKeyContainerInfo.RandomlyGenerated) {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Sign);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }
            }
            return Utils.SignValue(_safeKeyHandle, _parameters.KeyNumber, Constants.CALG_RSA_SIGN, calgHash, rgbHash);
        }

        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature) {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null)
                throw new ArgumentNullException("rgbSignature");
            Contract.EndContractBlock();

            int calgHash = X509Utils.NameOrOidToAlgId(str, OidGroup.HashAlgorithm);
            return VerifyHash(rgbHash, calgHash, rgbSignature);
        }

        [SecuritySafeCritical]
        internal bool VerifyHash(byte[] rgbHash, int calgHash, byte[] rgbSignature) {
            Contract.Requires(rgbHash != null);
            Contract.Requires(rgbSignature != null);

            GetKeyPair();

            return Utils.VerifySign(_safeKeyHandle, Constants.CALG_RSA_SIGN, calgHash, rgbHash, rgbSignature);
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
        [System.Security.SecuritySafeCritical]  // auto-generated
        public byte[] Encrypt(byte[] rgb, bool fOAEP) {
            if (rgb == null)
                throw new ArgumentNullException("rgb");
            Contract.EndContractBlock();

            GetKeyPair();

            byte[] encryptedKey = null;
            EncryptKey(_safeKeyHandle, rgb, rgb.Length, fOAEP, JitHelpers.GetObjectHandleOnStack(ref encryptedKey));
            return encryptedKey;
        }

        /// <summary>
        ///     Decrypt raw data, generally used for decrypting symmetric key material
        /// </summary>
        /// <param name="rgb">encrypted data</param>
        /// <param name="fOAEP">true to use OAEP padding (PKCS #1 v2), false to use PKCS #1 type 2 padding</param>
        /// <returns>decrypted data</returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public byte [] Decrypt(byte[] rgb, bool fOAEP) {
            if (rgb == null)
                throw new ArgumentNullException("rgb");
            Contract.EndContractBlock();

            GetKeyPair();

            // size check -- must be at most the modulus size
            if (rgb.Length > (KeySize / 8))
                throw new CryptographicException(Environment.GetResourceString("Cryptography_Padding_DecDataTooBig", KeySize / 8));

            if (!CspKeyContainerInfo.RandomlyGenerated) {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Decrypt);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }
            }

            byte[] decryptedKey = null;
            DecryptKey(_safeKeyHandle, rgb, rgb.Length, fOAEP, JitHelpers.GetObjectHandleOnStack(ref decryptedKey));
            return decryptedKey;
        }

        public override byte[] DecryptValue(byte[] rgb) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        }

        public override byte[] EncryptValue(byte[] rgb) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        }

        // 
        // private static methods
        //
        
        private static RSAParameters RSAObjectToStruct (RSACspObject rsaCspObject) {
            RSAParameters rsaParams = new RSAParameters();
            rsaParams.Exponent = rsaCspObject.Exponent;
            rsaParams.Modulus = rsaCspObject.Modulus;
            rsaParams.P = rsaCspObject.P;
            rsaParams.Q = rsaCspObject.Q;
            rsaParams.DP = rsaCspObject.DP;
            rsaParams.DQ = rsaCspObject.DQ;
            rsaParams.InverseQ = rsaCspObject.InverseQ;
            rsaParams.D = rsaCspObject.D;
            return rsaParams;
        }

        private static RSACspObject RSAStructToObject (RSAParameters rsaParams) {
            RSACspObject rsaCspObject = new RSACspObject();
            rsaCspObject.Exponent = rsaParams.Exponent;
            rsaCspObject.Modulus = rsaParams.Modulus;
            rsaCspObject.P = rsaParams.P;
            rsaCspObject.Q = rsaParams.Q;
            rsaCspObject.DP = rsaParams.DP;
            rsaCspObject.DQ = rsaParams.DQ;
            rsaCspObject.InverseQ = rsaParams.InverseQ;
            rsaCspObject.D = rsaParams.D;
            return rsaCspObject;
        }

        // find whether an RSA key blob is public.
        private static bool IsPublic (byte[] keyBlob) {
            if (keyBlob == null)
                throw new ArgumentNullException("keyBlob");
            Contract.EndContractBlock();

            // The CAPI RSA public key representation consists of the following sequence:
            //  - BLOBHEADER
            //  - RSAPUBKEY

            // The first should be PUBLICKEYBLOB and magic should be RSA_PUB_MAGIC "RSA1"
            if (keyBlob[0] != Constants.PUBLICKEYBLOB)
                return false;

            if (keyBlob[11] != 0x31 || keyBlob[10] != 0x41 || keyBlob[9] != 0x53 || keyBlob[8] != 0x52)
                return false;

            return true;
        }

        // Since P is required, we will assume its presence is synonymous to a private key.
        private static bool IsPublic(RSAParameters rsaParams) {
            return (rsaParams.P == null);
        }

#if !FEATURE_CORECLR        
        //
        // Adapt new RSA abstraction to legacy RSACryptoServiceProvider surface area.
        //

        // NOTE: For the new API, we go straight to CAPI for fixed set of hash algorithms and don't use crypto config here.
        //
        // Reasons:
        //       1. We're moving away from crypto config and we won't have it when porting to .NET Core
        //
        //       2. It's slow to lookup and slow to use as the base HashAlgorithm adds considerable overhead 
        //          (redundant defensive copy + double-initialization for the single-use case).
        //      

        [SecuritySafeCritical]
        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) {
            // we're sealed and the base should have checked this already
            Contract.Assert(data != null);
            Contract.Assert(offset >= 0 && offset <= data.Length);
            Contract.Assert(count >= 0 && count <= data.Length);
            Contract.Assert(!String.IsNullOrEmpty(hashAlgorithm.Name));

            using (SafeHashHandle hashHandle = Utils.CreateHash(Utils.StaticProvHandle, GetAlgorithmId(hashAlgorithm))) {
                Utils.HashData(hashHandle, data, offset, count);
                return Utils.EndHash(hashHandle);
            }
        }

        [SecuritySafeCritical]
        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) {
            // we're sealed and the base should have checked this already
            Contract.Assert(data != null);
            Contract.Assert(!String.IsNullOrEmpty(hashAlgorithm.Name));

            using (SafeHashHandle hashHandle = Utils.CreateHash(Utils.StaticProvHandle, GetAlgorithmId(hashAlgorithm))) {
                // Read the data 4KB at a time, providing similar read characteristics to a standard HashAlgorithm
                byte[] buffer = new byte[4096];
                int bytesRead = 0;
                do {
                    bytesRead = data.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0) {   
                        Utils.HashData(hashHandle, buffer, 0, bytesRead);
                    }
                } while (bytesRead > 0);

                return Utils.EndHash(hashHandle);
            }
        }
        
        private static int GetAlgorithmId(HashAlgorithmName hashAlgorithm) {
            switch (hashAlgorithm.Name) {
                case "MD5":
                    return Constants.CALG_MD5;
                case "SHA1":
                    return Constants.CALG_SHA1;
                case "SHA256":
                    return Constants.CALG_SHA_256;
                case "SHA384":
                    return Constants.CALG_SHA_384;
                case "SHA512":
                    return Constants.CALG_SHA_512;
                default:
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_UnknownHashAlgorithm", hashAlgorithm.Name));
            }
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (padding == null) {
                throw new ArgumentNullException("padding");
            }

            if (padding == RSAEncryptionPadding.Pkcs1) {
                return Encrypt(data, fOAEP: false);
            } else if (padding == RSAEncryptionPadding.OaepSHA1) {
                return Encrypt(data, fOAEP: true);
            } else {
                throw PaddingModeNotSupported();
            }
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (padding == null) {
                throw new ArgumentNullException("padding");
            }

            if (padding == RSAEncryptionPadding.Pkcs1) {
                return Decrypt(data, fOAEP: false);
            } else if (padding == RSAEncryptionPadding.OaepSHA1) {
                return Decrypt(data, fOAEP: true);
            } else {
                throw PaddingModeNotSupported();
            }
        }

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            if (hash == null) {
                throw new ArgumentNullException("hash");
            }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null) {
                throw new ArgumentNullException("padding");
            }
            if (padding != RSASignaturePadding.Pkcs1) {
                throw PaddingModeNotSupported();
            }

            return SignHash(hash, GetAlgorithmId(hashAlgorithm));
        }

        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            if (hash == null) {
                throw new ArgumentNullException("hash");
            }
            if (signature == null) {
                throw new ArgumentNullException("signature");
            }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null) {
                throw new ArgumentNullException("padding");
            }
            if (padding != RSASignaturePadding.Pkcs1) {
                throw PaddingModeNotSupported();
            }

            return VerifyHash(hash, GetAlgorithmId(hashAlgorithm), signature);
        }

        private static Exception PaddingModeNotSupported() {
            return new CryptographicException(Environment.GetResourceString("Cryptography_InvalidPaddingMode"));
        }
#endif
    }
}
