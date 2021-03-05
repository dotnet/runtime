// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using static Interop;
using static Interop.BCrypt;
using Microsoft.Win32.SafeHandles;

namespace Internal.NativeCrypto
{
    internal static partial class BCryptNative
    {
        /// <summary>
        ///     Well known algorithm names
        /// </summary>
        internal static class AlgorithmName
        {
            public const string DSA = "DSA";                    // BCRYPT_DSA_ALGORITHM
            public const string ECDH = "ECDH";                  // BCRYPT_ECDH_ALGORITHM
            public const string ECDHP256 = "ECDH_P256";         // BCRYPT_ECDH_P256_ALGORITHM
            public const string ECDHP384 = "ECDH_P384";         // BCRYPT_ECDH_P384_ALGORITHM
            public const string ECDHP521 = "ECDH_P521";         // BCRYPT_ECDH_P521_ALGORITHM
            public const string ECDsa = "ECDSA";                // BCRYPT_ECDSA_ALGORITHM
            public const string ECDsaP256 = "ECDSA_P256";       // BCRYPT_ECDSA_P256_ALGORITHM
            public const string ECDsaP384 = "ECDSA_P384";       // BCRYPT_ECDSA_P384_ALGORITHM
            public const string ECDsaP521 = "ECDSA_P521";       // BCRYPT_ECDSA_P521_ALGORITHM
            public const string RSA = "RSA";                    // BCRYPT_RSA_ALGORITHM
            public const string MD5 = "MD5";                    // BCRYPT_MD5_ALGORITHM
            public const string Sha1 = "SHA1";                  // BCRYPT_SHA1_ALGORITHM
            public const string Sha256 = "SHA256";              // BCRYPT_SHA256_ALGORITHM
            public const string Sha384 = "SHA384";              // BCRYPT_SHA384_ALGORITHM
            public const string Sha512 = "SHA512";              // BCRYPT_SHA512_ALGORITHM
            public const string Pbkdf2 = "PBKDF2";              // BCRYPT_PBKDF2_ALGORITHM
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
        public const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
        public const string BCRYPT_CHAIN_MODE_CFB = "ChainingModeCFB";
        public const string BCRYPT_CHAIN_MODE_CCM = "ChainingModeCCM";

        public static SafeAlgorithmHandle BCryptOpenAlgorithmProvider(string pszAlgId, string? pszImplementation, OpenAlgorithmProviderFlags dwFlags)
        {
            SafeAlgorithmHandle hAlgorithm;
            NTSTATUS ntStatus = Interop.BCryptOpenAlgorithmProvider(out hAlgorithm, pszAlgId, pszImplementation, (int)dwFlags);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                throw CreateCryptographicException(ntStatus);
            return hAlgorithm;
        }

        public static void SetFeedbackSize(this SafeAlgorithmHandle hAlg, int dwFeedbackSize)
        {
            NTSTATUS ntStatus = Interop.BCryptSetIntProperty(hAlg, BCryptPropertyStrings.BCRYPT_MESSAGE_BLOCK_LENGTH, ref dwFeedbackSize, 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(ntStatus);
            }
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

        private static Exception CreateCryptographicException(NTSTATUS ntStatus)
        {
            int hr = ((int)ntStatus) | 0x01000000;
            return hr.ToCryptographicException();
        }
    }


    internal static partial class Cng
    {
        internal static class Interop
        {
            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
            public static extern NTSTATUS BCryptOpenAlgorithmProvider(out SafeAlgorithmHandle phAlgorithm, string pszAlgId, string? pszImplementation, int dwFlags);

            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
            public static extern NTSTATUS BCryptSetProperty(SafeAlgorithmHandle hObject, string pszProperty, string pbInput, int cbInput, int dwFlags);

            [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode, EntryPoint = "BCryptSetProperty")]
            private static extern NTSTATUS BCryptSetIntPropertyPrivate(SafeBCryptHandle hObject, string pszProperty, ref int pdwInput, int cbInput, int dwFlags);

            public static unsafe NTSTATUS BCryptSetIntProperty(SafeBCryptHandle hObject, string pszProperty, ref int pdwInput, int dwFlags)
            {
                return BCryptSetIntPropertyPrivate(hObject, pszProperty, ref pdwInput, sizeof(int), dwFlags);
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
        private SafeAlgorithmHandle? _parentHandle;

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
