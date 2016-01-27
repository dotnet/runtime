// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {

    using System.Security.AccessControl;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

#if FEATURE_MACL && FEATURE_CRYPTO

    [Serializable]
    internal enum CryptoAPITransformMode {
        Encrypt = 0,
        Decrypt = 1
    }

[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class CryptoAPITransform : ICryptoTransform {
        private int BlockSizeValue;
        private byte[] IVValue;
        private CipherMode ModeValue;
        private PaddingMode PaddingValue;
        private CryptoAPITransformMode encryptOrDecrypt;
        private byte[] _rgbKey;
        private byte[] _depadBuffer = null;
        [System.Security.SecurityCritical] // auto-generated
        private SafeKeyHandle _safeKeyHandle;
        [System.Security.SecurityCritical] // auto-generated
        private SafeProvHandle _safeProvHandle;

        private CryptoAPITransform () {}
        [System.Security.SecurityCritical]  // auto-generated
        internal CryptoAPITransform(int algid, int cArgs, int[] rgArgIds,
                                    Object[] rgArgValues, byte[] rgbKey, PaddingMode padding, 
                                    CipherMode cipherChainingMode, int blockSize,
                                    int feedbackSize, bool useSalt,
                                    CryptoAPITransformMode encDecMode) {
            int dwValue;
            byte[] rgbValue;

            BlockSizeValue = blockSize;
            ModeValue = cipherChainingMode;
            PaddingValue = padding;
            encryptOrDecrypt = encDecMode;

            // Copy the input args
            int _cArgs = cArgs;
            int[] _rgArgIds = new int[rgArgIds.Length];
            Array.Copy(rgArgIds, _rgArgIds, rgArgIds.Length);
            _rgbKey = new byte[rgbKey.Length];
            Array.Copy(rgbKey, _rgbKey, rgbKey.Length);
            Object[] _rgArgValues = new Object[rgArgValues.Length];
            // an element of rgArgValues can only be an int or a byte[]
            for (int j = 0; j < rgArgValues.Length; j++) {
                if (rgArgValues[j] is byte[]) {
                    byte[] rgbOrig = (byte[]) rgArgValues[j];
                    byte[] rgbNew = new byte[rgbOrig.Length];
                    Array.Copy(rgbOrig, rgbNew, rgbOrig.Length);
                    _rgArgValues[j] = rgbNew;
                    continue;
                }
                if (rgArgValues[j] is int) {
                    _rgArgValues[j] = (int) rgArgValues[j];
                    continue;
                }
                if (rgArgValues[j] is CipherMode) {
                    _rgArgValues[j] = (int) rgArgValues[j];
                    continue;
                }
            }

            _safeProvHandle = Utils.AcquireProvHandle(new CspParameters(Utils.DefaultRsaProviderType));

            SafeKeyHandle safeKeyHandle = SafeKeyHandle.InvalidHandle;
            // _ImportBulkKey will check for failures and throw an exception
            Utils._ImportBulkKey(_safeProvHandle, algid, useSalt, _rgbKey, ref safeKeyHandle);
            _safeKeyHandle = safeKeyHandle;

            for (int i=0; i<cArgs; i++) {
                switch (rgArgIds[i]) {
                case Constants.KP_IV:
                    IVValue = (byte[]) _rgArgValues[i];
                    rgbValue = IVValue;
                    Utils.SetKeyParamRgb(_safeKeyHandle, _rgArgIds[i], rgbValue, rgbValue.Length);
                    break;

                case Constants.KP_MODE:
                    ModeValue = (CipherMode) _rgArgValues[i];
                    dwValue = (Int32) _rgArgValues[i];
                SetAsDWord:
                    Utils.SetKeyParamDw(_safeKeyHandle, _rgArgIds[i], dwValue);
                    break;

                case Constants.KP_MODE_BITS:
                    dwValue = (Int32) _rgArgValues[i];
                    goto SetAsDWord;

                case Constants.KP_EFFECTIVE_KEYLEN:
                    dwValue = (Int32) _rgArgValues[i];
                    goto SetAsDWord;

                default:
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeyParameter"), "_rgArgIds[i]");
                }
            }
        }

        public void Dispose() {
            Clear();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Clear() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void Dispose(bool disposing) {
            if (disposing) {
                // We need to always zeroize the following fields because they contain sensitive data
                if (_rgbKey != null) {
                    Array.Clear(_rgbKey,0,_rgbKey.Length);
                    _rgbKey = null;
                }
                if (IVValue != null) {
                    Array.Clear(IVValue,0,IVValue.Length);
                    IVValue = null;
                }
                if (_depadBuffer != null) {
                    Array.Clear(_depadBuffer, 0, _depadBuffer.Length);
                    _depadBuffer = null;
                }

                if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed) {
                _safeKeyHandle.Dispose();
                }
                if (_safeProvHandle != null && !_safeProvHandle.IsClosed) {
                _safeProvHandle.Dispose();
        }
            }
        }

        //
        // public properties
        //

        public IntPtr KeyHandle {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            get { return _safeKeyHandle.DangerousGetHandle(); }
        }

        public int InputBlockSize {
            get { return(BlockSizeValue/8); }
        }

        public int OutputBlockSize {
            get { return(BlockSizeValue/8); }
        }

        public bool CanTransformMultipleBlocks {
            get { return(true); }
        }

        public bool CanReuseTransform {
            get { return(true); }
        }

        //
        // public methods
        //

        // This routine resets the internal state of the CryptoAPITransform 
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(false)]
        public void Reset() {
            _depadBuffer = null;
            // just ensure we've called CryptEncrypt with the true flag
            byte[] temp = null;
            Utils._EncryptData(_safeKeyHandle, EmptyArray<Byte>.Value, 0, 0, ref temp, 0, PaddingValue, true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) {
            // Note: special handling required if decrypting & using padding because the padding adds to the end of the last
            // block, we have to buffer an entire block's worth of bytes in case what I just transformed turns out to be 
            // the last block Then in TransformFinalBlock we strip off the padding.

            if (inputBuffer == null) throw new ArgumentNullException("inputBuffer");
            if (outputBuffer == null) throw new ArgumentNullException("outputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if ((inputCount <= 0) || (inputCount % InputBlockSize != 0) || (inputCount > inputBuffer.Length)) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (encryptOrDecrypt == CryptoAPITransformMode.Encrypt) {
                // if we're encrypting we can always push out the bytes because no padding mode
                // removes bytes during encryption
                return Utils._EncryptData(_safeKeyHandle, inputBuffer, inputOffset, inputCount, ref outputBuffer, outputOffset, PaddingValue, false);
            } else {
                if (PaddingValue == PaddingMode.Zeros || PaddingValue == PaddingMode.None) {
                    // like encryption, if we're using None or Zeros padding on decrypt we can write out all
                    // the bytes.  Note that we cannot depad a block partially padded with Zeros because
                    // we can't tell if those zeros are plaintext or pad.
                    return Utils._DecryptData(_safeKeyHandle, inputBuffer, inputOffset, inputCount, ref outputBuffer, outputOffset, PaddingValue, false);
                } else {
                    // OK, now we're in the special case.  Check to see if this is the *first* block we've seen
                    // If so, buffer it and return null zero bytes
                    if (_depadBuffer == null) {
                        _depadBuffer = new byte[InputBlockSize];
                        // copy the last InputBlockSize bytes to _depadBuffer everything else gets processed and returned
                        int inputToProcess = inputCount - InputBlockSize;
                        Buffer.InternalBlockCopy(inputBuffer, inputOffset+inputToProcess, _depadBuffer, 0, InputBlockSize);
                        return Utils._DecryptData(_safeKeyHandle, inputBuffer, inputOffset, inputToProcess, ref outputBuffer, outputOffset, PaddingValue, false);
                    } else {
                        // we already have a depad buffer, so we need to decrypt that info first & copy it out
                        int r = Utils._DecryptData(_safeKeyHandle, _depadBuffer, 0, _depadBuffer.Length, ref outputBuffer, outputOffset, PaddingValue, false);
                        outputOffset += OutputBlockSize;
                        int inputToProcess = inputCount - InputBlockSize;
                        Buffer.InternalBlockCopy(inputBuffer, inputOffset+inputToProcess, _depadBuffer, 0, InputBlockSize);
                        r = Utils._DecryptData(_safeKeyHandle, inputBuffer, inputOffset, inputToProcess, ref outputBuffer, outputOffset, PaddingValue, false);
                        return (OutputBlockSize + r);
                    }
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) { 
            if (inputBuffer == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if ((inputCount < 0) || (inputCount > inputBuffer.Length)) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (encryptOrDecrypt == CryptoAPITransformMode.Encrypt) {
                // If we're encrypting we can always return what we compute because there's no _depadBuffer
                byte[] transformedBytes = null;
                Utils._EncryptData(_safeKeyHandle, inputBuffer, inputOffset, inputCount, ref transformedBytes, 0, PaddingValue, true);
                Reset();
                return transformedBytes;
            } else {
                if (inputCount%InputBlockSize != 0)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_SSD_InvalidDataSize"));

                if (_depadBuffer == null) {
                    byte[] transformedBytes = null;
                    Utils._DecryptData(_safeKeyHandle, inputBuffer, inputOffset, inputCount, ref transformedBytes, 0, PaddingValue, true);
                    Reset();
                    return transformedBytes;
                } else {
                    byte[] temp = new byte[_depadBuffer.Length + inputCount];
                    Buffer.InternalBlockCopy(_depadBuffer, 0, temp, 0, _depadBuffer.Length);
                    Buffer.InternalBlockCopy(inputBuffer, inputOffset, temp, _depadBuffer.Length, inputCount);
                    byte[] transformedBytes = null;
                    Utils._DecryptData(_safeKeyHandle, temp, 0, temp.Length, ref transformedBytes, 0, PaddingValue, true);
                    Reset();
                    return transformedBytes;
                }
            }
        }
    }
#endif // FEATURE_MACL && FEATURE_CRYPTO

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    [Flags]
    public enum CspProviderFlags {
        NoFlags                 = 0x0000,
        UseMachineKeyStore      = 0x0001,
        UseDefaultKeyContainer  = 0x0002,
        UseNonExportableKey     = 0x0004,
        UseExistingKey          = 0x0008,
        UseArchivableKey        = 0x0010,
        UseUserProtectedKey     = 0x0020,
        NoPrompt                = 0x0040,
        CreateEphemeralKey      = 0x0080
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class CspParameters
    {
        public int          ProviderType;
        public string       ProviderName;
        public string       KeyContainerName;
        public int          KeyNumber;

        private int m_flags;
        public CspProviderFlags Flags {
            get { return (CspProviderFlags) m_flags; }
            set { 
                int allFlags = 0x00FF; // this should change if more values are added to CspProviderFlags
                Contract.Assert((CspProviderFlags.UseMachineKeyStore |
                                CspProviderFlags.UseDefaultKeyContainer |
                                CspProviderFlags.UseNonExportableKey |
                                CspProviderFlags.UseExistingKey |
                                CspProviderFlags.UseArchivableKey |
                                CspProviderFlags.UseUserProtectedKey |
                                CspProviderFlags.NoPrompt |
                                CspProviderFlags.CreateEphemeralKey) == (CspProviderFlags)allFlags, "allFlags does not match all CspProviderFlags");
                
                int flags = (int) value;
                if ((flags & ~allFlags) != 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)value), "value");
                m_flags = flags;
            }
        }

#if FEATURE_MACL
        private CryptoKeySecurity m_cryptoKeySecurity;
        public CryptoKeySecurity CryptoKeySecurity {
            get {
                return m_cryptoKeySecurity;
            }
            set {
                m_cryptoKeySecurity = value;
            }
        }
#endif

#if (FEATURE_CRYPTO && FEATURE_X509_SECURESTRINGS) || FEATURE_CORECLR
        private SecureString m_keyPassword;
        public SecureString KeyPassword {
            get {
                return m_keyPassword;
            }
            set {
                m_keyPassword = value;
                // Parent handle and PIN are mutually exclusive.
                m_parentWindowHandle = IntPtr.Zero;
            }
        }

        private IntPtr m_parentWindowHandle;
        public IntPtr ParentWindowHandle {
            get {
                return m_parentWindowHandle;
            }
            set {
                m_parentWindowHandle = value;
                // Parent handle and PIN are mutually exclusive.
                m_keyPassword = null;
            }
        }
#endif

        public CspParameters () : this(Utils.DefaultRsaProviderType, null, null) {}

        public CspParameters (int dwTypeIn) : this(dwTypeIn, null, null) {}

        public CspParameters (int dwTypeIn, string strProviderNameIn) : this(dwTypeIn, strProviderNameIn, null) {}

        public CspParameters (int dwTypeIn, string strProviderNameIn, string strContainerNameIn) :
            this (dwTypeIn, strProviderNameIn, strContainerNameIn, CspProviderFlags.NoFlags) {}

#if FEATURE_MACL && FEATURE_CRYPTO && FEATURE_X509_SECURESTRINGS
        public CspParameters (int providerType, string providerName, string keyContainerName,
                              CryptoKeySecurity cryptoKeySecurity, SecureString keyPassword)
            : this (providerType, providerName, keyContainerName) {
            m_cryptoKeySecurity = cryptoKeySecurity;
            m_keyPassword = keyPassword;
        }

        public CspParameters (int providerType, string providerName, string keyContainerName,
                              CryptoKeySecurity cryptoKeySecurity, IntPtr parentWindowHandle)
            : this (providerType, providerName, keyContainerName) {
            m_cryptoKeySecurity = cryptoKeySecurity;
            m_parentWindowHandle = parentWindowHandle;
        }
#endif // #if FEATURE_MACL && FEATURE_CRYPTO && FEATURE_X509_SECURESTRINGS

        internal CspParameters (int providerType, string providerName, string keyContainerName, CspProviderFlags flags) {
            ProviderType = providerType;
            ProviderName = providerName;
            KeyContainerName = keyContainerName;
            KeyNumber = -1;
            Flags = flags;
        }

        // copy constructor
        internal CspParameters (CspParameters parameters) {
            ProviderType = parameters.ProviderType;
            ProviderName = parameters.ProviderName;
            KeyContainerName = parameters.KeyContainerName;
            KeyNumber = parameters.KeyNumber;
            Flags = parameters.Flags;
#if FEATURE_MACL            
            m_cryptoKeySecurity = parameters.m_cryptoKeySecurity;
#endif // FEATURE_MACL
#if FEATURE_CRYPTO && FEATURE_X509_SECURESTRINGS
            m_keyPassword = parameters.m_keyPassword;
            m_parentWindowHandle = parameters.m_parentWindowHandle;
#endif // FEATURE_CRYPTO && FEATURE_X509_SECURESTRINGS
        }
    }
}
