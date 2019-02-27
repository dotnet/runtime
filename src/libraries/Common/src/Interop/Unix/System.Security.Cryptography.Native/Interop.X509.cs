// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal delegate int X509StoreVerifyCallback(int ok, IntPtr ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509EvpPublicKey")]
        internal static extern SafeEvpPKeyHandle GetX509EvpPublicKey(SafeX509Handle x509);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeX509Crl")]
        internal static extern SafeX509CrlHandle DecodeX509Crl(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeX509")]
        internal static extern SafeX509Handle DecodeX509(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509DerSize")]
        internal static extern int GetX509DerSize(SafeX509Handle x);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeX509")]
        internal static extern int EncodeX509(SafeX509Handle x, byte[] buf);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509Destroy")]
        internal static extern void X509Destroy(IntPtr a);

        /// <summary>
        /// Clone the input certificate into a new object.
        /// </summary>
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509Duplicate")]
        internal static extern SafeX509Handle X509Duplicate(IntPtr handle);

        /// <summary>
        /// Clone the input certificate into a new object.
        /// </summary>
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509Duplicate")]
        internal static extern SafeX509Handle X509Duplicate(SafeX509Handle handle);

        /// <summary>
        /// Increment the native reference count of the certificate to protect against
        /// a free from another pointer-holder.
        /// </summary>
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509UpRef")]
        internal static extern SafeX509Handle X509UpRef(IntPtr handle);

        /// <summary>
        /// Increment the native reference count of the certificate to protect against
        /// a free from another pointer-holder.
        /// </summary>
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509UpRef")]
        internal static extern SafeX509Handle X509UpRef(SafeX509Handle handle);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemReadX509FromBio")]
        internal static extern SafeX509Handle PemReadX509FromBio(SafeBioHandle bio);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemReadX509FromBioAux")]
        internal static extern SafeX509Handle PemReadX509FromBioAux(SafeBioHandle bio);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetSerialNumber")]
        private static extern SafeSharedAsn1IntegerHandle X509GetSerialNumber_private(SafeX509Handle x);

        internal static SafeSharedAsn1IntegerHandle X509GetSerialNumber(SafeX509Handle x)
        {
            CheckValidOpenSslHandle(x);

            return SafeInteriorHandle.OpenInteriorHandle(
                handle => X509GetSerialNumber_private(handle),
                x);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetIssuerName")]
        internal static extern IntPtr X509GetIssuerName(SafeX509Handle x);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetSubjectName")]
        internal static extern IntPtr X509GetSubjectName(SafeX509Handle x);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509CheckPurpose")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509CheckPurpose(SafeX509Handle x, int id, int ca);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509CheckIssued")]
        internal static extern int X509CheckIssued(SafeX509Handle issuer, SafeX509Handle subject);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509IssuerNameHash")]
        internal static extern ulong X509IssuerNameHash(SafeX509Handle x);

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeSharedAsn1OctetStringHandle CryptoNative_X509FindExtensionData(
            SafeX509Handle x,
            int extensionNid);

        internal static SafeSharedAsn1OctetStringHandle X509FindExtensionData(SafeX509Handle x, int extensionNid)
        {
            CheckValidOpenSslHandle(x);

            return SafeInteriorHandle.OpenInteriorHandle(
                (handle, arg) => CryptoNative_X509FindExtensionData(handle, arg),
                x,
                extensionNid);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetExtCount")]
        internal static extern int X509GetExtCount(SafeX509Handle x);

        // Returns a pointer already being tracked by the SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        // Bounds checking is in place for "loc", IntPtr.Zero is returned on violations.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetExt")]
        internal static extern IntPtr X509GetExt(SafeX509Handle x, int loc);

        // Returns a pointer already being tracked by a SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionGetOid")]
        internal static extern IntPtr X509ExtensionGetOid(IntPtr ex);

        // Returns a pointer already being tracked by a SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionGetData")]
        internal static extern IntPtr X509ExtensionGetData(IntPtr ex);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionGetCritical")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509ExtensionGetCritical(IntPtr ex);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ChainNew")]
        internal static extern SafeX509StoreHandle X509ChainNew(SafeX509StackHandle systemTrust, string userTrustPath);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreDestory")]
        internal static extern void X509StoreDestory(IntPtr v);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreAddCert")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509StoreAddCert(SafeX509StoreHandle ctx, SafeX509Handle x);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreAddCrl")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509StoreAddCrl(SafeX509StoreHandle ctx, SafeX509CrlHandle x);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreSetRevocationFlag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptoNative_X509StoreSetRevocationFlag(SafeX509StoreHandle ctx, X509RevocationFlag revocationFlag);

        internal static void X509StoreSetRevocationFlag(SafeX509StoreHandle ctx, X509RevocationFlag revocationFlag)
        {
            if (!CryptoNative_X509StoreSetRevocationFlag(ctx, revocationFlag))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxInit")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509StoreCtxInit(
            SafeX509StoreCtxHandle ctx,
            SafeX509StoreHandle store,
            SafeX509Handle x509,
            SafeX509StackHandle extraCerts);

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_X509VerifyCert(SafeX509StoreCtxHandle ctx);

        internal static bool X509VerifyCert(SafeX509StoreCtxHandle ctx)
        {
            int result = CryptoNative_X509VerifyCert(ctx);

            if (result < 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            return result != 0;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetError")]
        internal static extern X509VerifyStatusCode X509StoreCtxGetError(SafeX509StoreCtxHandle ctx);

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_X509StoreCtxReset(SafeX509StoreCtxHandle ctx);

        internal static void X509StoreCtxReset(SafeX509StoreCtxHandle ctx)
        {
            if (CryptoNative_X509StoreCtxReset(ctx) != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_X509StoreCtxRebuildChain(SafeX509StoreCtxHandle ctx);

        internal static bool X509StoreCtxRebuildChain(SafeX509StoreCtxHandle ctx)
        {
            int result = CryptoNative_X509StoreCtxRebuildChain(ctx);

            if (result < 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            return result != 0;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetErrorDepth")]
        internal static extern int X509StoreCtxGetErrorDepth(SafeX509StoreCtxHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxSetVerifyCallback")]
        internal static extern void X509StoreCtxSetVerifyCallback(SafeX509StoreCtxHandle ctx, X509StoreVerifyCallback callback);

        internal static string GetX509VerifyCertErrorString(X509VerifyStatusCode n)
        {
            return Marshal.PtrToStringAnsi(X509VerifyCertErrorString(n));
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509VerifyCertErrorString")]
        private static extern IntPtr X509VerifyCertErrorString(X509VerifyStatusCode n);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509CrlDestroy")]
        internal static extern void X509CrlDestroy(IntPtr a);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemWriteBioX509Crl")]
        internal static extern int PemWriteBioX509Crl(SafeBioHandle bio, SafeX509CrlHandle crl);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemReadBioX509Crl")]
        internal static extern SafeX509CrlHandle PemReadBioX509Crl(SafeBioHandle bio);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509SubjectPublicKeyInfoDerSize")]
        internal static extern int GetX509SubjectPublicKeyInfoDerSize(SafeX509Handle x509);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeX509SubjectPublicKeyInfo")]
        internal static extern int EncodeX509SubjectPublicKeyInfo(SafeX509Handle x509, byte[] buf);

        internal enum X509VerifyStatusCode : int
        {
            X509_V_OK = 0,
            X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT = 2,
            X509_V_ERR_UNABLE_TO_GET_CRL = 3,
            X509_V_ERR_UNABLE_TO_DECRYPT_CRL_SIGNATURE = 5,
            X509_V_ERR_UNABLE_TO_DECODE_ISSUER_PUBLIC_KEY = 6,
            X509_V_ERR_CERT_SIGNATURE_FAILURE = 7,
            X509_V_ERR_CRL_SIGNATURE_FAILURE = 8,
            X509_V_ERR_CERT_NOT_YET_VALID = 9,
            X509_V_ERR_CERT_HAS_EXPIRED = 10,
            X509_V_ERR_CRL_NOT_YET_VALID = 11,
            X509_V_ERR_CRL_HAS_EXPIRED = 12,
            X509_V_ERR_ERROR_IN_CERT_NOT_BEFORE_FIELD = 13,
            X509_V_ERR_ERROR_IN_CERT_NOT_AFTER_FIELD = 14,
            X509_V_ERR_ERROR_IN_CRL_LAST_UPDATE_FIELD = 15,
            X509_V_ERR_ERROR_IN_CRL_NEXT_UPDATE_FIELD = 16,
            X509_V_ERR_OUT_OF_MEM = 17,
            X509_V_ERR_DEPTH_ZERO_SELF_SIGNED_CERT = 18,
            X509_V_ERR_SELF_SIGNED_CERT_IN_CHAIN = 19,
            X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT_LOCALLY = 20,
            X509_V_ERR_UNABLE_TO_VERIFY_LEAF_SIGNATURE = 21,
            X509_V_ERR_CERT_CHAIN_TOO_LONG = 22,
            X509_V_ERR_CERT_REVOKED = 23,
            X509_V_ERR_INVALID_CA = 24,
            X509_V_ERR_PATH_LENGTH_EXCEEDED = 25,
            X509_V_ERR_INVALID_PURPOSE = 26,
            X509_V_ERR_CERT_UNTRUSTED = 27,
            X509_V_ERR_CERT_REJECTED = 28,
            X509_V_ERR_KEYUSAGE_NO_CERTSIGN = 32,
            X509_V_ERR_UNABLE_TO_GET_CRL_ISSUER = 33,
            X509_V_ERR_UNHANDLED_CRITICAL_EXTENSION = 34,
            X509_V_ERR_KEYUSAGE_NO_CRL_SIGN = 35,
            X509_V_ERR_UNHANDLED_CRITICAL_CRL_EXTENSION = 36,
            X509_V_ERR_INVALID_NON_CA = 37,
            X509_V_ERR_KEYUSAGE_NO_DIGITAL_SIGNATURE = 39,
            X509_V_ERR_INVALID_EXTENSION = 41,
            X509_V_ERR_INVALID_POLICY_EXTENSION = 42,
            X509_V_ERR_NO_EXPLICIT_POLICY = 43,
        }
    }
}
