// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.NativeCrypto;
using System.Diagnostics;
using System.IO;

namespace System.Security.Cryptography
{
    public sealed class DSACryptoServiceProvider : DSA, ICspAsymmetricAlgorithm
    {
        private int _keySize;
        private readonly CspParameters _parameters;
        private readonly bool _randomKeyContainer;
        private SafeKeyHandle _safeKeyHandle;
        private SafeProvHandle _safeProvHandle;
        private SHA1 _sha1;
        private static volatile CspProviderFlags s_useMachineKeyStore = 0;

        /// <summary>
        /// Initializes a new instance of the DSACryptoServiceProvider class.
        /// </summary>
        public DSACryptoServiceProvider()
            : this(
                  new CspParameters(CapiHelper.DefaultDssProviderType,
                      null,
                      null,
                      s_useMachineKeyStore))
        {
        }

        /// <summary>
        /// Initializes a new instance of the DSACryptoServiceProvider class with the specified key size.
        /// </summary>
        /// <param name="dwKeySize">The size of the key for the asymmetric algorithm in bits.</param>
        public DSACryptoServiceProvider(int dwKeySize)
            : this(dwKeySize,
                  new CspParameters(CapiHelper.DefaultDssProviderType,
                      null,
                      null,
                      s_useMachineKeyStore))
        {
        }

        /// <summary>
        /// Initializes a new instance of the DSACryptoServiceProvider class with the specified parameters
        /// for the cryptographic service provider (CSP).
        /// </summary>
        /// <param name="parameters">The parameters for the CSP.</param>
        public DSACryptoServiceProvider(CspParameters parameters)
            : this(0, parameters)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DSACryptoServiceProvider class with the specified key size and parameters
        /// for the cryptographic service provider (CSP).
        /// </summary>
        /// <param name="dwKeySize">The size of the key for the cryptographic algorithm in bits.</param>
        /// <param name="parameters">The parameters for the CSP.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is required by the FIPS 186-2 DSA spec.")]
        public DSACryptoServiceProvider(int dwKeySize, CspParameters parameters)
        {
            if (dwKeySize < 0)
                throw new ArgumentOutOfRangeException(nameof(dwKeySize), SR.ArgumentOutOfRange_NeedNonNegNum);

            _parameters = CapiHelper.SaveCspParameters(
                CapiHelper.CspAlgorithmType.Dss,
                parameters,
                s_useMachineKeyStore,
                out _randomKeyContainer);

            _keySize = dwKeySize;
            _sha1 = SHA1.Create();

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
                                CapiHelper.CspAlgorithmType.Dss,
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
        /// Gets a CspKeyContainerInfo object that describes additional information about a cryptographic key pair.
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
                return new[] { new KeySizes(512, 1024, 64) }; // per FIPS 186-2
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the key should be persisted in the cryptographic
        /// service provider (CSP).
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
        /// Gets a value that indicates whether the DSACryptoServiceProvider object contains
        /// only a public key.
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
        /// Gets or sets a value indicating whether the key should be persisted in the computer's
        /// key store instead of the user profile store.
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
        /// Exports a blob containing the key information associated with an DSACryptoServiceProvider object.
        /// </summary>
        public byte[] ExportCspBlob(bool includePrivateParameters)
        {
            return CapiHelper.ExportKeyBlob(includePrivateParameters, SafeKeyHandle);
        }

        public override DSAParameters ExportParameters(bool includePrivateParameters)
        {
            byte[] cspBlob = ExportCspBlob(includePrivateParameters);
            return cspBlob.ToDSAParameters(includePrivateParameters, SafeKeyHandle);
        }

        /// <summary>
        /// This method helps Acquire the default CSP and avoids the need for static SafeProvHandle
        /// in CapiHelper class
        /// </summary>
        private SafeProvHandle AcquireSafeProviderHandle()
        {
            SafeProvHandle safeProvHandle;
            CapiHelper.AcquireCsp(new CspParameters(CapiHelper.DefaultDssProviderType), out safeProvHandle);
            return safeProvHandle;
        }

        /// <summary>
        /// Imports a blob that represents DSA key information.
        /// </summary>
        /// <param name="keyBlob">A byte array that represents a DSA key blob.</param>
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

        public override void ImportParameters(DSAParameters parameters)
        {
            byte[] keyBlob = parameters.ToKeyBlob();
            ImportCspBlob(keyBlob);
        }

        /// <summary>
        /// Computes the hash value of the specified input stream and signs the resulting hash value.
        /// </summary>
        /// <param name="inputStream">The input data for which to compute the hash.</param>
        /// <returns>The DSA signature for the specified data.</returns>
        public byte[] SignData(Stream inputStream)
        {
            byte[] hashVal = _sha1.ComputeHash(inputStream);
            return SignHash(hashVal, null);
        }

        /// <summary>
        /// Computes the hash value of the specified input stream and signs the resulting hash value.
        /// </summary>
        /// <param name="buffer">The input data for which to compute the hash.</param>
        /// <returns>The DSA signature for the specified data.</returns>
        public byte[] SignData(byte[] buffer)
        {
            byte[] hashVal = _sha1.ComputeHash(buffer);
            return SignHash(hashVal, null);
        }

        /// <summary>
        /// Signs a byte array from the specified start point to the specified end point.
        /// </summary>
        /// <param name="buffer">The input data to sign.</param>
        /// <param name="offset">The offset into the array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <returns>The DSA signature for the specified data.</returns>
        public byte[] SignData(byte[] buffer, int offset, int count)
        {
            byte[] hashVal = _sha1.ComputeHash(buffer, offset, count);
            return SignHash(hashVal, null);
        }

        /// <summary>
        /// Verifies the specified signature data by comparing it to the signature computed for the specified data.
        /// </summary>
        /// <param name="rgbData">The data that was signed.</param>
        /// <param name="rgbSignature">The signature data to be verified.</param>
        /// <returns>true if the signature verifies as valid; otherwise, false.</returns>
        public bool VerifyData(byte[] rgbData, byte[] rgbSignature)
        {
            byte[] hashVal = _sha1.ComputeHash(rgbData);
            return VerifyHash(hashVal, null, rgbSignature);
        }

        /// <summary>
        /// Creates the DSA signature for the specified data.
        /// </summary>
        /// <param name="rgbHash">The data to be signed.</param>
        /// <returns>The digital signature for the specified data.</returns>
        override public byte[] CreateSignature(byte[] rgbHash)
        {
            return SignHash(rgbHash, null);
        }

        override public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
        {
            return VerifyHash(rgbHash, null, rgbSignature);
        }

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            // we're sealed and the base should have checked this before calling us
            Debug.Assert(data != null);
            Debug.Assert(offset >= 0 && offset <= data.Length);
            Debug.Assert(count >= 0 && count <= data.Length - offset);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            if (hashAlgorithm != HashAlgorithmName.SHA1)
            {
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
            }

            return _sha1.ComputeHash(data, offset, count);
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            // we're sealed and the base should have checked this before calling us
            Debug.Assert(data != null);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            if (hashAlgorithm != HashAlgorithmName.SHA1)
            {
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
            }

            return _sha1.ComputeHash(data);
        }

        /// <summary>
        /// Computes the signature for the specified hash value by encrypting it with the private key.
        /// </summary>
        /// <param name="rgbHash">The hash value of the data to be signed.</param>
        /// <param name="str">The name of the hash algorithm used to create the hash value of the data.</param>
        /// <returns>The DSA signature for the specified hash value.</returns>
        public byte[] SignHash(byte[] rgbHash, string str)
        {
            if (rgbHash == null)
                throw new ArgumentNullException(nameof(rgbHash));
            if (PublicOnly)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);

            int calgHash = CapiHelper.NameOrOidToHashAlgId(str);

            if (rgbHash.Length != _sha1.HashSize / 8)
                throw new CryptographicException(string.Format(SR.Cryptography_InvalidHashSize, "SHA1", _sha1.HashSize / 8));

            return CapiHelper.SignValue(
                SafeProvHandle,
                SafeKeyHandle,
                _parameters.KeyNumber,
                CapiHelper.CALG_DSS_SIGN,
                calgHash,
                rgbHash);
        }

        /// <summary>
        /// Verifies the specified signature data by comparing it to the signature computed for the specified hash value.
        /// </summary>
        /// <param name="rgbHash">The hash value of the data to be signed.</param>
        /// <param name="str">The name of the hash algorithm used to create the hash value of the data.</param>
        /// <param name="rgbSignature">The signature data to be verified.</param>
        /// <returns>true if the signature verifies as valid; otherwise, false.</returns>
        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature)
        {
            if (rgbHash == null)
                throw new ArgumentNullException(nameof(rgbHash));
            if (rgbSignature == null)
                throw new ArgumentNullException(nameof(rgbSignature));

            int calgHash = CapiHelper.NameOrOidToHashAlgId(str);

            return CapiHelper.VerifySign(
                SafeProvHandle,
                SafeKeyHandle,
                CapiHelper.CALG_DSS_SIGN,
                calgHash,
                rgbHash,
                rgbSignature);
        }

        /// <summary>
        /// Find whether a DSS key blob is public.
        /// </summary>
        private static bool IsPublic(byte[] keyBlob)
        {
            if (keyBlob == null)
            {
                throw new ArgumentNullException(nameof(keyBlob));
            }

            // The CAPI DSS public key representation consists of the following sequence:
            //  - BLOBHEADER (the first byte is bType)
            //  - DSSPUBKEY or DSSPUBKEY_VER3 (the first field is the magic field)

            // The first byte should be PUBLICKEYBLOB
            if (keyBlob[0] != CapiHelper.PUBLICKEYBLOB)
            {
                return false;
            }

            // Magic should be DSS_MAGIC or DSS_PUB_MAGIC_VER3
            if ((keyBlob[11] != 0x31 && keyBlob[11] != 0x33) || keyBlob[10] != 0x53 || keyBlob[9] != 0x53 || keyBlob[8] != 0x44)
            {
                return false;
            }

            return true;
        }
    }
}
