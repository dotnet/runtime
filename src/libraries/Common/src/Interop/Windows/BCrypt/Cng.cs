// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using static Interop;
using static Interop.BCrypt;

namespace Internal.NativeCrypto
{
    internal static partial class BCryptNative
    {
        /// <summary>
        ///     Well known algorithm names
        /// </summary>
        internal static class AlgorithmName
        {
            public const string ECDH = "ECDH";                  // BCRYPT_ECDH_ALGORITHM
            public const string ECDHP256 = "ECDH_P256";         // BCRYPT_ECDH_P256_ALGORITHM
            public const string ECDHP384 = "ECDH_P384";         // BCRYPT_ECDH_P384_ALGORITHM
            public const string ECDHP521 = "ECDH_P521";         // BCRYPT_ECDH_P521_ALGORITHM
            public const string ECDsa = "ECDSA";                // BCRYPT_ECDSA_ALGORITHM
            public const string ECDsaP256 = "ECDSA_P256";       // BCRYPT_ECDSA_P256_ALGORITHM
            public const string ECDsaP384 = "ECDSA_P384";       // BCRYPT_ECDSA_P384_ALGORITHM
            public const string ECDsaP521 = "ECDSA_P521";       // BCRYPT_ECDSA_P521_ALGORITHM
            public const string MD5 = "MD5";                    // BCRYPT_MD5_ALGORITHM
            public const string Sha1 = "SHA1";                  // BCRYPT_SHA1_ALGORITHM
            public const string Sha256 = "SHA256";              // BCRYPT_SHA256_ALGORITHM
            public const string Sha384 = "SHA384";              // BCRYPT_SHA384_ALGORITHM
            public const string Sha512 = "SHA512";              // BCRYPT_SHA512_ALGORITHM
        }

        internal static class KeyDerivationFunction
        {
            public const string Hash = "HASH";                  // BCRYPT_KDF_HASH
            public const string Hmac = "HMAC";                  // BCRYPT_KDF_HMAC
            public const string Tls = "TLS_PRF";                // BCRYPT_KDF_TLS_PRF
        }
    }

    //
    // Interop layer around Windows CNG api.
    //
    internal static partial class Cng
    {
        [Flags]
        public enum OpenAlgorithmProviderFlags : int
        {
            NONE = 0x00000000,
            BCRYPT_ALG_HANDLE_HMAC_FLAG = 0x00000008,
        }

        public const string BCRYPT_3DES_ALGORITHM = "3DES";
        public const string BCRYPT_AES_ALGORITHM = "AES";
        public const string BCRYPT_DES_ALGORITHM = "DES";
        public const string BCRYPT_RC2_ALGORITHM = "RC2";

        public const string BCRYPT_CHAIN_MODE_CBC = "ChainingModeCBC";
        public const string BCRYPT_CHAIN_MODE_ECB = "ChainingModeECB";

        public static SafeAlgorithmHandle BCryptOpenAlgorithmProvider(String pszAlgId, String pszImplementation, OpenAlgorithmProviderFlags dwFlags)
        {
            SafeAlgorithmHandle hAlgorithm = null;
            NTSTATUS ntStatus = Interop.BCryptOpenAlgorithmProvider(out hAlgorithm, pszAlgId, pszImplementation, (int)dwFlags);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                throw CreateCryptographicException(ntStatus);
            return hAlgorithm;
        }

        public static SafeKeyHandle BCryptImportKey(this SafeAlgorithmHandle hAlg, byte[] key)
        {
            unsafe
            {
                const String BCRYPT_KEY_DATA_BLOB = "KeyDataBlob";
                int keySize = key.Length;
                int blobSize = sizeof(BCRYPT_KEY_DATA_BLOB_HEADER) + keySize;
                byte[] blob = new byte[blobSize];
                fixed (byte* pbBlob = blob)
                {
                    BCRYPT_KEY_DATA_BLOB_HEADER* pBlob = (BCRYPT_KEY_DATA_BLOB_HEADER*)pbBlob;
                    pBlob->dwMagic = BCRYPT_KEY_DATA_BLOB_HEADER.BCRYPT_KEY_DATA_BLOB_MAGIC;
                    pBlob->dwVersion = BCRYPT_KEY_DATA_BLOB_HEADER.BCRYPT_KEY_DATA_BLOB_VERSION1;
                    pBlob->cbKeyData = (uint)keySize;
                }
                Buffer.BlockCopy(key, 0, blob, sizeof(BCRYPT_KEY_DATA_BLOB_HEADER), keySize);
                SafeKeyHandle hKey;
                NTSTATUS ntStatus = Interop.BCryptImportKey(hAlg, IntPtr.Zero, BCRYPT_KEY_DATA_BLOB, out hKey, IntPtr.Zero, 0, blob, blobSize, 0);
                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                    throw CreateCryptographicException(ntStatus);
                return hKey;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BCRYPT_KEY_DATA_BLOB_HEADER
        {
            public UInt32 dwMagic;
            public UInt32 dwVersion;
            public UInt32 cbKeyData;

            public const UInt32 BCRYPT_KEY_DATA_BLOB_MAGIC = 0x4d42444b;
            public const UInt32 BCRYPT_KEY_DATA_BLOB_VERSION1 = 0x1;
        }

        public static void SetCipherMode(this SafeAlgorithmHandle hAlg, string cipherMode)
        {
            NTSTATUS ntStatus = Interop.BCryptSetProperty(hAlg, BCryptPropertyStrings.BCRYPT_CHAINING_MODE, cipherMode, (cipherMode.Length + 1) * 2, 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(ntStatus);
            }
        }

        public static void SetEffectiveKeyLength(this SafeAlgorithmHandle hAlg, int effectiveKeyLength)
        {
            NTSTATUS ntStatus = Interop.BCryptSetIntProperty(hAlg, BCryptPropertyStrings.BCRYPT_EFFECTIVE_KEY_LENGTH, ref effectiveKeyLength, 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(ntStatus);
            }
        }

        // Note: input and output are allowed to be the same buffer. BCryptEncrypt will correctly do the encryption in place according to CNG documentation.
        public static int BCryptEncrypt(this SafeKeyHandle hKey, byte[] input, int inputOffset, int inputCount, byte[] iv, byte[] output, int outputOffset, int outputCount)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(inputCount >= 0);
            Debug.Assert(inputCount <= input.Length - inputOffset);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(outputCount >= 0);
            Debug.Assert(outputCount <= output.Length - outputOffset);

            unsafe
            {
                fixed (byte* pbInput = input)
                {
                    fixed (byte* pbOutput = output)
                    {
                        int cbResult;
                        NTSTATUS ntStatus = Interop.BCryptEncrypt(hKey, pbInput + inputOffset, inputCount, IntPtr.Zero, iv, iv == null ? 0 : iv.Length, pbOutput + outputOffset, outputCount, out cbResult, 0);
                        if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                            throw CreateCryptographicException(ntStatus);
                        return cbResult;
                    }
                }
            }
        }

        // Note: input and output are allowed to be the same buffer. BCryptDecrypt will correctly do the decryption in place according to CNG documentation.
        public static int BCryptDecrypt(this SafeKeyHandle hKey, byte[] input, int inputOffset, int inputCount, byte[] iv, byte[] output, int outputOffset, int outputCount)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(inputCount >= 0);
            Debug.Assert(inputCount <= input.Length - inputOffset);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(outputCount >= 0);
            Debug.Assert(outputCount <= output.Length - outputOffset);

            unsafe
            {
                fixed (byte* pbInput = input)
                {
                    fixed (byte* pbOutput = output)
                    {
                        int cbResult;
                        NTSTATUS ntStatus = Interop.BCryptDecrypt(hKey, pbInput + inputOffset, inputCount, IntPtr.Zero, iv, iv == null ? 0 : iv.Length, pbOutput + outputOffset, outputCount, out cbResult, 0);
                        if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                            throw CreateCryptographicException(ntStatus);
                        return cbResult;
                    }
                }
            }
        }

        private static class BCryptGetPropertyStrings
        {
            public const String BCRYPT_HASH_LENGTH = "HashDigestLength";
        }

        public static String CryptFormatObject(String oidValue, byte[] rawData, bool multiLine)
        {
            const int X509_ASN_ENCODING = 0x00000001;
            const int CRYPT_FORMAT_STR_MULTI_LINE = 0x00000001;

            int dwFormatStrType = multiLine ? CRYPT_FORMAT_STR_MULTI_LINE : 0;

            int cbFormat = 0;
            if (!Interop.CryptFormatObject(X509_ASN_ENCODING, 0, dwFormatStrType, IntPtr.Zero, oidValue, rawData, rawData.Length, null, ref cbFormat))
                return null;
            StringBuilder sb = new StringBuilder((cbFormat + 1) / 2);
            if (!Interop.CryptFormatObject(X509_ASN_ENCODING, 0, dwFormatStrType, IntPtr.Zero, oidValue, rawData, rawData.Length, sb, ref cbFormat))
                return null;
            return sb.ToString();
        }

        private enum NTSTATUS : uint
        {
            STATUS_SUCCESS = 0x0,
            STATUS_NOT_FOUND = 0xc0000225,
            STATUS_INVALID_PARAMETER = 0xc000000d,
            STATUS_NO_MEMORY = 0xc0000017,
        }

        private static Exception CreateCryptographicException(NTSTATUS ntStatus)
        {
            int hr = ((int)ntStatus) | 0x01000000;
            return hr.ToCryptographicException();
        }
    }


    internal static partial class Cng
    {
        private static class Interop
        {
            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
            public static extern NTSTATUS BCryptOpenAlgorithmProvider(out SafeAlgorithmHandle phAlgorithm, String pszAlgId, String pszImplementation, int dwFlags);

            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
            public static extern unsafe NTSTATUS BCryptSetProperty(SafeAlgorithmHandle hObject, String pszProperty, String pbInput, int cbInput, int dwFlags);

            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode, EntryPoint = "BCryptSetProperty")]
            private static extern unsafe NTSTATUS BCryptSetIntPropertyPrivate(SafeBCryptHandle hObject, string pszProperty, ref int pdwInput, int cbInput, int dwFlags);

            public static unsafe NTSTATUS BCryptSetIntProperty(SafeBCryptHandle hObject, string pszProperty, ref int pdwInput, int dwFlags)
            {
                return BCryptSetIntPropertyPrivate(hObject, pszProperty, ref pdwInput, sizeof(int), dwFlags);
            }

            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
            public static extern NTSTATUS BCryptImportKey(SafeAlgorithmHandle hAlgorithm, IntPtr hImportKey, String pszBlobType, out SafeKeyHandle hKey, IntPtr pbKeyObject, int cbKeyObject, byte[] pbInput, int cbInput, int dwFlags);

            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
            public static extern unsafe NTSTATUS BCryptEncrypt(SafeKeyHandle hKey, byte* pbInput, int cbInput, IntPtr paddingInfo, [In,Out] byte [] pbIV, int cbIV, byte* pbOutput, int cbOutput, out int cbResult, int dwFlags);

            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
            public static extern unsafe NTSTATUS BCryptDecrypt(SafeKeyHandle hKey, byte* pbInput, int cbInput, IntPtr paddingInfo, [In, Out] byte[] pbIV, int cbIV, byte* pbOutput, int cbOutput, out int cbResult, int dwFlags);

            [DllImport(Libraries.Crypt32, CharSet = CharSet.Ansi, SetLastError = true, BestFitMapping = false)]
            public static extern bool CryptFormatObject(
                [In]      int dwCertEncodingType,   // only valid value is X509_ASN_ENCODING
                [In]      int dwFormatType,         // unused - pass 0.
                [In]      int dwFormatStrType,      // select multiline
                [In]      IntPtr pFormatStruct,     // unused - pass IntPtr.Zero
                [MarshalAs(UnmanagedType.LPStr)]
                [In]      String lpszStructType,    // OID value
                [In]      byte[] pbEncoded,         // Data to be formatted
                [In]      int cbEncoded,            // Length of data to be formatted
                [MarshalAs(UnmanagedType.LPWStr)]
                [Out]     StringBuilder pbFormat,   // Receives formatted string.
                [In, Out] ref int pcbFormat);       // Sends/receives length of formatted String.
        }
    }

    internal abstract class SafeBCryptHandle : SafeHandle
    {
        public SafeBCryptHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public sealed override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }
    }

    internal sealed class SafeAlgorithmHandle : SafeBCryptHandle
    {
        protected sealed override bool ReleaseHandle()
        {
            uint ntStatus = BCryptCloseAlgorithmProvider(handle, 0);
            return ntStatus == 0;
        }

        [DllImport(Libraries.BCrypt)]
        private static extern uint BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int dwFlags);
    }

    internal sealed class SafeKeyHandle : SafeBCryptHandle
    {
        private SafeAlgorithmHandle _parentHandle = null;

        public void SetParentHandle(SafeAlgorithmHandle parentHandle)
        {
            Debug.Assert(_parentHandle == null);
            Debug.Assert(parentHandle != null);
            Debug.Assert(!parentHandle.IsInvalid);

            bool ignore = false;
            parentHandle.DangerousAddRef(ref ignore);

            _parentHandle = parentHandle;
        }

        protected sealed override bool ReleaseHandle()
        {
            if (_parentHandle != null)
            {
                _parentHandle.DangerousRelease();
                _parentHandle = null;
            }

            uint ntStatus = BCryptDestroyKey(handle);
            return ntStatus == 0;
        }

        [DllImport(Libraries.BCrypt)]
        private static extern uint BCryptDestroyKey(IntPtr hKey);
    }
}

