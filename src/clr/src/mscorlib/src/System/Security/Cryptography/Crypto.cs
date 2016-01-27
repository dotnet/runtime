// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using Microsoft.Win32;
    using System.Runtime.Serialization;
    using System.Globalization;

    // This enum represents cipher chaining modes: cipher block chaining (CBC), 
    // electronic code book (ECB), output feedback (OFB), cipher feedback (CFB),
    // and ciphertext-stealing (CTS).  Not all implementations will support all modes.
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum CipherMode {            // Please keep in sync with wincrypt.h
        CBC = 1,
        ECB = 2,
        OFB = 3,
        CFB = 4,
        CTS = 5
    }

    // This enum represents the padding method to use for filling out short blocks.
    // "None" means no padding (whole blocks required). 
    // "PKCS7" is the padding mode defined in RFC 2898, Section 6.1.1, Step 4, generalized
    // to whatever block size is required.
    // "Zeros" means pad with zero bytes to fill out the last block.
    // "ISO 10126" is the same as PKCS5 except that it fills the bytes before the last one with 
    // random bytes. "ANSI X.923" fills the bytes with zeros and puts the number of padding 
    // bytes in the last byte.

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum PaddingMode {
        None = 1,
        PKCS7 = 2,
        Zeros = 3,
        ANSIX923 = 4,
        ISO10126 = 5
    }

    // This structure is used for returning the set of legal key sizes and
    // block sizes of the symmetric algorithms.
    // Note: this class should be sealed, otherwise someone could sub-class it and the read-only
    // properties we depend on can have setters. Ideally, we should have a struct here (value type)
    // but we use what we have now and try to close the hole allowing someone to specify an invalid key size
#if !FEATURE_CORECLR
[System.Runtime.InteropServices.ComVisible(true)]
#endif // !FEATURE_CORECLR
    public sealed class KeySizes {
        private int m_minSize;
        private int m_maxSize;
        private int m_skipSize;

        public int MinSize {
            get { return m_minSize; }
        }

        public int MaxSize {
            get { return m_maxSize; }
        }

        public int SkipSize {
            get { return m_skipSize; }
        }

        public KeySizes(int minSize, int maxSize, int skipSize) {
            m_minSize = minSize; m_maxSize = maxSize; m_skipSize = skipSize;
        }
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CryptographicException : SystemException {
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        public CryptographicException()
            : base(Environment.GetResourceString("Arg_CryptographyException")) {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO);
        }

        public CryptographicException(String message)
            : base(message) {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO);
        }

        public CryptographicException(String format, String insert)
            : base(String.Format(CultureInfo.CurrentCulture, format, insert)) {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO);
        }

        public CryptographicException(String message, Exception inner)
            : base(message, inner) {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public CryptographicException(int hr) 
            : this(
#if FEATURE_LEGACYNETCFCRYPTO
            CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 ? GetLegacyNetCFMessage(hr) : 
#endif
            Win32Native.GetMessage(hr)) {            
            if ((hr & 0x80000000) != 0x80000000)
                hr = (hr & 0x0000FFFF) | unchecked((int)0x80070000);
            SetErrorCode(hr);
        }

#if FEATURE_SERIALIZATION
        protected CryptographicException(SerializationInfo info, StreamingContext context) : base (info, context) {}
#endif // FEATURE_SERIALIZATION

        // This method is only called from inside the VM.
        private static void ThrowCryptographicException (int hr) {
            throw new CryptographicException(hr);
        }

#if FEATURE_LEGACYNETCFCRYPTO
        // NetCF throws CryptographicExceptions with the text "Unknown Error (hresult here)."
        private static string GetLegacyNetCFMessage(int hr) {
            if ((uint)hr == 0x8000701a) {  // PAL_ERROR_CRYPT_PROV_TYPE_NOT_DEF
                return Environment.GetResourceString("Cryptography_LegacyNetCF_CSP_CouldNotAcquire");
            }
            return Environment.GetResourceString("Cryptography_LegacyNetCF_UnknownError", hr.ToString("X", CultureInfo.InvariantCulture));
        }
#endif
    }

    [Serializable()]
[System.Runtime.InteropServices.ComVisible(true)]
    public class CryptographicUnexpectedOperationException : CryptographicException {

        public CryptographicUnexpectedOperationException()
            : base() {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO_UNEX_OPER);
        }

        public CryptographicUnexpectedOperationException(String message)
            : base(message) {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO_UNEX_OPER);
        }

        public CryptographicUnexpectedOperationException(String format, String insert)
            : base(String.Format(CultureInfo.CurrentCulture, format, insert)) {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO_UNEX_OPER);
        }

        public CryptographicUnexpectedOperationException(String message, Exception inner)
            : base(message, inner) {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO_UNEX_OPER);
        }

#if FEATURE_SERIALIZATION
        protected CryptographicUnexpectedOperationException(SerializationInfo info, StreamingContext context) : base (info, context) {}
#endif // FEATURE_SERIALIZATION
    }

}
