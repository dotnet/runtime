// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// This source file is marked up so that it can be built both as part of the BCL and as part of the fx tree
// as well. Since the security annotation process is different between the two trees, SecurityCritical
// attributes appear directly in this file, instead of being marked up by the BCL annotator tool.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics.Contracts;

namespace System.Security.Cryptography {

    /// <summary>
    ///     Native interop with CAPI. Native code definitions can be found in wincrypt.h
    /// </summary>
    internal static class CapiNative {
        /// <summary>
        ///     Class fields for CAPI algorithm identifiers
        /// </summary>
        internal enum AlgorithmClass
        {
            Any = (0 << 13),                    // ALG_CLASS_ANY
            Signature = (1 << 13),              // ALG_CLASS_SIGNATURE
            Hash = (4 << 13),                   // ALG_CLASS_HASH
            KeyExchange = (5 << 13),            // ALG_CLASS_KEY_EXCHANGE
        }

        /// <summary>
        ///     Type identifier fields for CAPI algorithm identifiers
        /// </summary>
        internal enum AlgorithmType
        {
            Any = (0 << 9),                     // ALG_TYPE_ANY
            Rsa = (2 << 9),                     // ALG_TYPE_RSA
        }

        /// <summary>
        ///     Sub identifiers for CAPI algorithm identifiers
        /// </summary>
        internal enum AlgorithmSubId
        {
            Any = 0,                            // ALG_SID_ANY

            RsaAny = 0,                         // ALG_SID_RSA_ANY

            Sha1 = 4,                           // ALG_SID_SHA1
            Sha256 = 12,                        // ALG_SID_SHA_256
            Sha384 = 13,                        // ALG_SID_SHA_384
            Sha512 = 14,                        // ALG_SID_SHA_512
        }

        /// <summary>
        ///     CAPI algorithm identifiers
        /// </summary>
        internal enum AlgorithmID
        {
            None = 0,

            RsaSign =                   (AlgorithmClass.Signature           | AlgorithmType.Rsa             | AlgorithmSubId.RsaAny),                   // CALG_RSA_SIGN
            RsaKeyExchange =            (AlgorithmClass.KeyExchange         | AlgorithmType.Rsa             | AlgorithmSubId.RsaAny),                   // CALG_RSA_KEYX

            Sha1 =                      (AlgorithmClass.Hash                | AlgorithmType.Any             | AlgorithmSubId.Sha1),                     // CALG_SHA1
            Sha256 =                    (AlgorithmClass.Hash                | AlgorithmType.Any             | AlgorithmSubId.Sha256),                   // CALG_SHA_256
            Sha384 =                    (AlgorithmClass.Hash                | AlgorithmType.Any             | AlgorithmSubId.Sha384),                   // CALG_SHA_384
            Sha512 =                    (AlgorithmClass.Hash                | AlgorithmType.Any             | AlgorithmSubId.Sha512),                   // CALG_SHA_512
        }

        /// <summary>
        ///     Flags for the CryptAcquireContext API
        /// </summary>
        [Flags]
        internal enum CryptAcquireContextFlags {
            None = 0x00000000,
            NewKeyset = 0x00000008,                         // CRYPT_NEWKEYSET
            DeleteKeyset = 0x00000010,                      // CRYPT_DELETEKEYSET
            MachineKeyset = 0x00000020,                     // CRYPT_MACHINE_KEYSET
            Silent = 0x00000040,                            // CRYPT_SILENT
            VerifyContext = unchecked((int)0xF0000000)      // CRYPT_VERIFYCONTEXT
        }

        /// <summary>
        ///     Error codes returned by CAPI
        /// </summary>
        internal enum ErrorCode {
            Ok = 0x00000000,
            MoreData = 0x000000ea,                          // ERROR_MORE_DATA
            BadHash = unchecked((int)0x80090002),           // NTE_BAD_HASH
            BadData = unchecked((int)0x80090005),           // NTE_BAD_DATA
            BadSignature = unchecked((int)0x80090006),      // NTE_BAD_SIGNATURE
            NoKey = unchecked((int)0x8009000d)              // NTE_NO_KEY
        }

        /// <summary>
        ///     Properties of CAPI hash objects
        /// </summary>
        internal enum HashProperty {
            None = 0,
            HashValue = 0x0002,                             // HP_HASHVAL
            HashSize = 0x0004,                              // HP_HASHSIZE
        }

        /// <summary>
        ///     Flags for the CryptGenKey API
        /// </summary>
        [Flags]
        internal enum KeyGenerationFlags {
            None = 0x00000000,
            Exportable = 0x00000001,                        // CRYPT_EXPORTABLE
            UserProtected = 0x00000002,                     // CRYPT_USER_PROTECTED
            Archivable = 0x00004000                         // CRYPT_ARCHIVABLE
        }

        /// <summary>
        ///     Properties that can be read or set on a key
        /// </summary>
        internal enum KeyProperty {
            None = 0,
            AlgorithmID = 7,                                // KP_ALGID
            KeyLength = 9                                   // KP_KEYLEN
        }

        /// <summary>
        ///     Key numbers for identifying specific keys within a single container
        /// </summary>
        internal enum KeySpec {
            KeyExchange = 1,                                // AT_KEYEXCHANGE
            Signature = 2                                   // AT_SIGNATURE
        }

        /// <summary>
        ///     Well-known names of crypto service providers
        /// </summary>
        internal static class ProviderNames {
            // MS_ENHANCED_PROV
            internal const string MicrosoftEnhanced = "Microsoft Enhanced Cryptographic Provider v1.0";
        }

        /// <summary>
        ///     Provider type accessed in a crypto service provider. These provide the set of algorithms
        ///     available to use for an application.
        /// </summary>
        internal enum ProviderType {
            RsaFull = 1         // PROV_RSA_FULL
        }

        [System.Security.SecurityCritical]
        internal static class UnsafeNativeMethods {
            /// <summary>
            ///     Open a crypto service provider, if a key container is specified KeyContainerPermission
            ///     should be demanded.
            /// </summary>
            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptAcquireContext([Out] out SafeCspHandle phProv,
                                                            string pszContainer,
                                                            string pszProvider,
                                                            ProviderType dwProvType,
                                                            CryptAcquireContextFlags dwFlags);

            /// <summary>
            ///     Create an object to hash data with
            /// </summary>
            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptCreateHash(SafeCspHandle hProv,
                                                        AlgorithmID Algid,
                                                        IntPtr hKey,                        // SafeCspKeyHandle
                                                        int dwFlags,
                                                        [Out] out SafeCspHashHandle phHash);

            /// <summary>
            ///     Create a new key in the given key container
            /// </summary>
            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptGenKey(SafeCspHandle hProv,
                                                    int Algid,
                                                    uint dwFlags,
                                                    [Out] out SafeCspKeyHandle phKey);

            /// <summary>
            ///     Fill a buffer with randomly generated data
            /// </summary>
            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptGenRandom(SafeCspHandle hProv,
                                                       int dwLen,
                                                       [In, Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbBuffer);

            /// <summary>
            ///     Fill a buffer with randomly generated data
            /// </summary>
            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern unsafe bool CryptGenRandom(SafeCspHandle hProv,
                                                       int dwLen,
                                                       byte* pbBuffer);

            /// <summary>
            ///     Read the value of a property from a hash object
            /// </summary>
            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptGetHashParam(SafeCspHashHandle hHash,
                                                          HashProperty dwParam,
                                                          [In, Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbData,
                                                          [In, Out] ref int pdwDataLen,
                                                          int dwFlags);

            /// <summary>
            ///     Read the value of a property from a key
            /// </summary>
            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptGetKeyParam(SafeCspKeyHandle hKey,
                                                         KeyProperty dwParam,
                                                         [In, Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbData,
                                                         [In, Out] ref int pdwDataLen,
                                                         int dwFlags);

            /// <summary>
            ///     Import a key blob into a CSP
            /// </summary>
            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptImportKey(SafeCspHandle hProv,
                                                       [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbData,
                                                       int pdwDataLen,
                                                       IntPtr hPubKey,                      // SafeCspKeyHandle
                                                       KeyGenerationFlags dwFlags,
                                                       [Out] out SafeCspKeyHandle phKey);

            /// <summary>
            ///     Set the value of a property on a hash object
            /// </summary>
            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptSetHashParam(SafeCspHashHandle hHash,
                                                          HashProperty dwParam,
                                                          [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbData,
                                                          int dwFlags);

            /// <summary>
            ///     Verify the a digital signature
            /// </summary>
            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptVerifySignature(SafeCspHashHandle hHash,
                                                             [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbSignature,
                                                             int dwSigLen,
                                                             SafeCspKeyHandle hPubKey,
                                                             string sDescription,
                                                             int dwFlags);
        }

        /// <summary>
        ///     Acquire a handle to a crypto service provider and optionally a key container
        /// </summary>
        [SecurityCritical]
        internal static SafeCspHandle AcquireCsp(string keyContainer,
                                                 string providerName,
                                                 ProviderType providerType,
                                                 CryptAcquireContextFlags flags) {
            Contract.Assert(keyContainer == null, "Key containers are not supported");

            // Specifying both verify context (for an ephemeral key) and machine keyset (for a persisted machine key)
            // does not make sense.  Additionally, Widows is beginning to lock down against uses of MACHINE_KEYSET
            // (for instance in the app container), even if verify context is present.   Therefore, if we're using
            // an ephemeral key, strip out MACHINE_KEYSET from the flags.
            if (((flags & CryptAcquireContextFlags.VerifyContext) == CryptAcquireContextFlags.VerifyContext) &&
                ((flags & CryptAcquireContextFlags.MachineKeyset) == CryptAcquireContextFlags.MachineKeyset)) {
                flags &= ~CryptAcquireContextFlags.MachineKeyset;
            }

            SafeCspHandle cspHandle = null;
            if (!UnsafeNativeMethods.CryptAcquireContext(out cspHandle,
                                                            keyContainer,
                                                            providerName,
                                                            providerType,
                                                            flags)) {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return cspHandle;
        }

        /// <summary>
        ///     Create a CSP hash object for the specified hash algorithm
        /// </summary>
        [SecurityCritical]
        internal static SafeCspHashHandle CreateHashAlgorithm(SafeCspHandle cspHandle, AlgorithmID algorithm) {
            Contract.Assert(cspHandle != null && !cspHandle.IsInvalid, "cspHandle != null && !cspHandle.IsInvalid");
            Contract.Assert(((AlgorithmClass)algorithm & AlgorithmClass.Hash) == AlgorithmClass.Hash, "Invalid hash algorithm");

            SafeCspHashHandle hashHandle = null;
            if (!UnsafeNativeMethods.CryptCreateHash(cspHandle, algorithm, IntPtr.Zero, 0, out hashHandle)) {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return hashHandle;
        }

        /// <summary>
        ///     Fill a buffer with random data generated by the CSP
        /// </summary>
        [SecurityCritical]
        internal static void GenerateRandomBytes(SafeCspHandle cspHandle, byte[] buffer) {
            Contract.Assert(cspHandle != null && !cspHandle.IsInvalid, "cspHandle != null && !cspHandle.IsInvalid");
            Contract.Assert(buffer != null && buffer.Length > 0, "buffer != null && buffer.Length > 0");

            if (!UnsafeNativeMethods.CryptGenRandom(cspHandle, buffer.Length, buffer)) {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        ///     Fill part of a buffer with random data generated by the CSP
        /// </summary>
        [SecurityCritical]
        internal static unsafe void GenerateRandomBytes(SafeCspHandle cspHandle, byte[] buffer, int offset, int count)
        {
            Contract.Assert(cspHandle != null && !cspHandle.IsInvalid, "cspHandle != null && !cspHandle.IsInvalid");
            Contract.Assert(buffer != null && buffer.Length > 0, "buffer != null && buffer.Length > 0");
            Contract.Assert(offset >= 0 && count > 0, "offset >= 0 && count > 0");
            Contract.Assert(buffer.Length >= offset + count, "buffer.Length >= offset + count");

            fixed (byte* pBuffer = &buffer[offset])
            {
                if (!UnsafeNativeMethods.CryptGenRandom(cspHandle, count, pBuffer))
                {
                    throw new CryptographicException(Marshal.GetLastWin32Error());
                }
            }
        }

        /// <summary>
        ///     Get a DWORD sized property of a hash object
        /// </summary>
        [SecurityCritical]
        internal static int GetHashPropertyInt32(SafeCspHashHandle hashHandle, HashProperty property) {
            byte[] rawProperty = GetHashProperty(hashHandle, property);
            Contract.Assert(rawProperty.Length == sizeof(int) || rawProperty.Length == 0, "Unexpected property size");
            return rawProperty.Length == sizeof(int) ? BitConverter.ToInt32(rawProperty, 0) : 0;
        }

        /// <summary>
        ///     Get an arbitrary property of a hash object
        /// </summary>
        [SecurityCritical]
        internal static byte[] GetHashProperty(SafeCspHashHandle hashHandle, HashProperty property) {
            Contract.Assert(hashHandle != null && !hashHandle.IsInvalid, "keyHandle != null && !keyHandle.IsInvalid");

            int bufferSize = 0;
            byte[] buffer = null;

            // Figure out how big of a buffer we need to hold the property
            if (!UnsafeNativeMethods.CryptGetHashParam(hashHandle, property, buffer, ref bufferSize, 0)) {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != (int)ErrorCode.MoreData) {
                    throw new CryptographicException(errorCode);
                }
            }

            // Now get the property bytes directly
            buffer = new byte[bufferSize];
            if (!UnsafeNativeMethods.CryptGetHashParam(hashHandle, property, buffer, ref bufferSize, 0)) {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return buffer;
        }

        /// <summary>
        ///     Get a DWORD sized property of a key stored in a CSP
        /// </summary>
        [SecurityCritical]
        internal static int GetKeyPropertyInt32(SafeCspKeyHandle keyHandle, KeyProperty property) {
            byte[] rawProperty = GetKeyProperty(keyHandle, property);
            Contract.Assert(rawProperty.Length == sizeof(int) || rawProperty.Length == 0, "Unexpected property size");
            return rawProperty.Length == sizeof(int) ? BitConverter.ToInt32(rawProperty, 0) : 0;
        }

        /// <summary>
        ///     Get an arbitrary property of a key stored in a CSP
        /// </summary>
        [SecurityCritical]
        internal static byte[] GetKeyProperty(SafeCspKeyHandle keyHandle, KeyProperty property) {
            Contract.Assert(keyHandle != null && !keyHandle.IsInvalid, "keyHandle != null && !keyHandle.IsInvalid");

            int bufferSize = 0;
            byte[] buffer = null;

            // Figure out how big of a buffer we need to hold the property
            if (!UnsafeNativeMethods.CryptGetKeyParam(keyHandle, property, buffer, ref bufferSize, 0)) {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != (int)ErrorCode.MoreData) {
                    throw new CryptographicException(errorCode);
                }
            }

            // Now get the property bytes directly
            buffer = new byte[bufferSize];
            if (!UnsafeNativeMethods.CryptGetKeyParam(keyHandle, property, buffer, ref bufferSize, 0)) {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return buffer;
        }

        /// <summary>
        ///     Set an arbitrary property on a hash object
        /// </summary>
        [SecurityCritical]
        internal static void SetHashProperty(SafeCspHashHandle hashHandle,
                                             HashProperty property,
                                             byte[] value) {
            Contract.Assert(hashHandle != null && !hashHandle.IsInvalid, "hashHandle != null && !hashHandle.IsInvalid");

            if (!UnsafeNativeMethods.CryptSetHashParam(hashHandle, property, value, 0)) {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        ///     Verify that the digital signature created with the specified hash and asymmetric algorithm
        ///     is valid for the given hash value.
        /// </summary>
        [SecurityCritical]
        internal static bool VerifySignature(SafeCspHandle cspHandle,
                                             SafeCspKeyHandle keyHandle,
                                             AlgorithmID signatureAlgorithm,
                                             AlgorithmID hashAlgorithm,
                                             byte[] hashValue,
                                             byte[] signature) {
            Contract.Assert(cspHandle != null && !cspHandle.IsInvalid, "cspHandle != null && !cspHandle.IsInvalid");
            Contract.Assert(keyHandle != null && !keyHandle.IsInvalid, "keyHandle != null && !keyHandle.IsInvalid");
            Contract.Assert(((AlgorithmClass)signatureAlgorithm & AlgorithmClass.Signature) == AlgorithmClass.Signature, "Invalid signature algorithm");
            Contract.Assert(((AlgorithmClass)hashAlgorithm & AlgorithmClass.Hash) == AlgorithmClass.Hash, "Invalid hash algorithm");
            Contract.Assert(hashValue != null, "hashValue != null");
            Contract.Assert(signature != null, "signature != null");

            // CAPI and the CLR have inverse byte orders for signatures, so we need to reverse before verifying
            byte[] signatureValue = new byte[signature.Length];
            Array.Copy(signature, signatureValue, signatureValue.Length);
            Array.Reverse(signatureValue);

            using (SafeCspHashHandle hashHandle = CreateHashAlgorithm(cspHandle, hashAlgorithm)) {
                // Make sure the hash value is the correct size and import it into the CSP
                if (hashValue.Length != GetHashPropertyInt32(hashHandle, HashProperty.HashSize)) {
                    throw new CryptographicException((int)ErrorCode.BadHash);
                }
                SetHashProperty(hashHandle, HashProperty.HashValue, hashValue);

                // Do the signature verification.  A TRUE result means that the signature was valid.  A FALSE
                // result either means an invalid signature or some other error, so we need to check the last
                // error to see which occurred.
                if (UnsafeNativeMethods.CryptVerifySignature(hashHandle,
                                                                signatureValue,
                                                                signatureValue.Length,
                                                                keyHandle,
                                                                null,
                                                                0)) {
                    return true;
                }
                else {
                    int error = Marshal.GetLastWin32Error();

                    if (error != (int)ErrorCode.BadSignature) {
                        throw new CryptographicException(error);
                    }

                    return false;
                }
            }
        }
    }

    /// <summary>
    ///    SafeHandle representing a native HCRYPTPROV on Windows, or representing all state associated with
    ///    loading a CSSM CSP on the Mac.  The HCRYPTPROV SafeHandle usage is straightforward, however CSSM
    ///    usage is slightly different.
    ///     
    ///    For CSSM we hold three pieces of state:
    ///      * m_initializedCssm - a flag indicating that CSSM_Init() was successfully called
    ///      * m_cspModuleGuid   - the module GUID of the CSP we loaded, if that CSP was successfully loaded
    ///      * handle            - handle resulting from attaching to the CSP
    ///       
    ///    We need to keep all three pieces of state, since we need to teardown in a specific order. If
    ///    these pieces of state were in seperate SafeHandles we could not guarantee their order of
    ///    finalization.
    /// </summary>
    [SecurityCritical]
    internal sealed class SafeCspHandle : SafeHandleZeroOrMinusOneIsInvalid {

        private SafeCspHandle() : base(true) {
        }

        [DllImport("advapi32")]
#if FEATURE_CORECLR || FEATURE_CER
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif // FEATURE_CORECLR || FEATURE_CER
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool CryptReleaseContext(IntPtr hProv, int dwFlags);

        /// <summary>
        ///     Clean up the safe handle's resources.
        ///     
        ///     On Windows the cleanup is a straightforward release of the HCRYPTPROV handle.  However, on
        ///     the Mac, CSSM requires that we release resources in the following order:
        ///     
        ///       1. Detach from the CSP
        ///       2. Unload the CSP
        ///       3. Terminate CSSM
        ///       
        ///     Both the unload and terminate operations are ref-counted by CSSM, so it is safe to do these
        ///     even if other handles are open on the CSP or other CSSM objects are in use.
        /// </summary>
        [SecurityCritical]
        protected override bool ReleaseHandle() {
            return CryptReleaseContext(handle, 0);
        }
        
    }

    /// <summary>
    ///     SafeHandle representing a native HCRYPTHASH
    /// </summary>
    [SecurityCritical]
    internal sealed class SafeCspHashHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeCspHashHandle() : base(true) {
        }

        [DllImport("advapi32")]
#if FEATURE_CORECLR || FEATURE_CER
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif // FEATURE_CORECLR || FEATURE_CER
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool CryptDestroyHash(IntPtr hKey);

        [SecurityCritical]
        protected override bool ReleaseHandle() {
            return CryptDestroyHash(handle);
        }
    }

    /// <summary>
    ///     SafeHandle representing a native HCRYPTKEY on Windows.
    ///     
    ///     On the Mac, we generate our keys by hand, so they are really just CSSM_KEY structures along with
    ///     the associated data blobs.  Because of this, the only resource that needs to be released when the
    ///     key is freed is the memory associated with the key blob.
    ///    
    ///     However, in order for a SafeCspKeyHandle to marshal as a CSSM_KEY_PTR, as one would expect, the
    ///     handle value on the Mac is actually a pointer to the CSSM_KEY.  We maintain a seperate m_data
    ///     pointer which is the buffer holding the actual key data.
    ///     
    ///     Both of these details add a further invarient that on the Mac a SafeCspKeyHandle may never be an
    ///     [out] parameter from an API.  This is because we always expect that we control the memory buffer
    ///     that the CSSM_KEY resides in and that we don't have to call CSSM_FreeKey on the data.
    ///     
    ///     Keeping this in a SafeHandle rather than just marshaling the key structure direclty buys us a
    ///     level of abstraction, in that if we ever do need to work with keys that require a CSSM_FreeKey
    ///     call, we can continue to use the same key handle object.  It also means that keys are represented
    ///     by the same type on both Windows and Mac, so that consumers of the CapiNative layer don't have
    ///     to know the difference between the two.
    /// </summary>
    [SecurityCritical]
    internal sealed class SafeCspKeyHandle : SafeHandleZeroOrMinusOneIsInvalid {

        internal SafeCspKeyHandle() : base(true) {
        }

        [DllImport("advapi32")]
#if FEATURE_CORECLR || FEATURE_CER
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif // FEATURE_CORECLR || FEATURE_CER
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool CryptDestroyKey(IntPtr hKey);

        [SecurityCritical]
        protected override bool ReleaseHandle() {
            return CryptDestroyKey(handle);
        }
    }
}
