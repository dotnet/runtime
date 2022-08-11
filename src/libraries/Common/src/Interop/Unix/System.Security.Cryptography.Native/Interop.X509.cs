// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal delegate int X509StoreVerifyCallback(int ok, IntPtr ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NotBefore")]
        internal static partial IntPtr GetX509NotBefore(SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NotAfter")]
        internal static partial IntPtr GetX509NotAfter(SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509SignatureAlgorithm")]
        internal static partial IntPtr GetX509SignatureAlgorithm(SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509PublicKeyAlgorithm")]
        internal static partial IntPtr GetX509PublicKeyAlgorithm(SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509PublicKeyBytes")]
        internal static partial IntPtr GetX509PublicKeyBytes(SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509EvpPublicKey")]
        internal static partial SafeEvpPKeyHandle GetX509EvpPublicKey(SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeX509Crl")]
        internal static partial SafeX509CrlHandle DecodeX509Crl(byte[] buf, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeX509")]
        internal static partial SafeX509Handle DecodeX509(ref byte buf, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeX509")]
        internal static partial SafeX509Handle DecodeX509(IntPtr buf, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509DerSize")]
        internal static partial int GetX509DerSize(SafeX509Handle x);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeX509")]
        internal static partial int EncodeX509(SafeX509Handle x, byte[] buf);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509Destroy")]
        internal static partial void X509Destroy(IntPtr a);

        /// <summary>
        /// Clone the input certificate into a new object.
        /// </summary>
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509Duplicate")]
        internal static partial SafeX509Handle X509Duplicate(IntPtr handle);

        /// <summary>
        /// Clone the input certificate into a new object.
        /// </summary>
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509Duplicate")]
        internal static partial SafeX509Handle X509Duplicate(SafeX509Handle handle);

        /// <summary>
        /// Increment the native reference count of the certificate to protect against
        /// a free from another pointer-holder.
        /// </summary>
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509UpRef")]
        internal static partial SafeX509Handle X509UpRef(IntPtr handle);

        /// <summary>
        /// Increment the native reference count of the certificate to protect against
        /// a free from another pointer-holder.
        /// </summary>
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509UpRef")]
        internal static partial SafeX509Handle X509UpRef(SafeX509Handle handle);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemReadX509FromBio")]
        internal static partial SafeX509Handle PemReadX509FromBio(SafeBioHandle bio);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemReadX509FromBioAux")]
        internal static partial SafeX509Handle PemReadX509FromBioAux(SafeBioHandle bio);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetSerialNumber")]
        private static partial SafeSharedAsn1IntegerHandle X509GetSerialNumber_private(SafeX509Handle x);

        internal static SafeSharedAsn1IntegerHandle X509GetSerialNumber(SafeX509Handle x)
        {
            CheckValidOpenSslHandle(x);

            return SafeInteriorHandle.OpenInteriorHandle(
                X509GetSerialNumber_private,
                x);
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetIssuerName")]
        internal static partial IntPtr X509GetIssuerName(SafeX509Handle x);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetSubjectName")]
        internal static partial IntPtr X509GetSubjectName(SafeX509Handle x);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509CheckPurpose")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool X509CheckPurpose(SafeX509Handle x, int id, int ca);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509IssuerNameHash")]
        internal static partial ulong X509IssuerNameHash(SafeX509Handle x);

        [LibraryImport(Libraries.CryptoNative)]
        private static partial SafeSharedAsn1OctetStringHandle CryptoNative_X509FindExtensionData(
            SafeX509Handle x,
            int extensionNid);

        internal static SafeSharedAsn1OctetStringHandle X509FindExtensionData(SafeX509Handle x, int extensionNid)
        {
            CheckValidOpenSslHandle(x);

            return SafeInteriorHandle.OpenInteriorHandle(
                CryptoNative_X509FindExtensionData,
                x,
                extensionNid);
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetExtCount")]
        internal static partial int X509GetExtCount(SafeX509Handle x);

        // Returns a pointer already being tracked by the SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        // Bounds checking is in place for "loc", IntPtr.Zero is returned on violations.
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetExt")]
        internal static partial IntPtr X509GetExt(SafeX509Handle x, int loc);

        // Returns a pointer already being tracked by a SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionGetOid")]
        internal static partial IntPtr X509ExtensionGetOid(IntPtr ex);

        // Returns a pointer already being tracked by a SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionGetData")]
        internal static partial IntPtr X509ExtensionGetData(IntPtr ex);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionGetCritical")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool X509ExtensionGetCritical(IntPtr ex);

        [LibraryImport(Libraries.CryptoNative)]
        private static partial SafeX509StoreHandle CryptoNative_X509ChainNew(SafeX509StackHandle systemTrust, SafeX509StackHandle userTrust);

        internal static SafeX509StoreHandle X509ChainNew(SafeX509StackHandle systemTrust, SafeX509StackHandle userTrust)
        {
            SafeX509StoreHandle store = CryptoNative_X509ChainNew(systemTrust, userTrust);

            if (store.IsInvalid)
            {
                Exception e = CreateOpenSslCryptographicException();
                store.Dispose();
                throw e;
            }

            return store;
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreDestroy")]
        internal static partial void X509StoreDestroy(IntPtr v);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreAddCrl")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool X509StoreAddCrl(SafeX509StoreHandle ctx, SafeX509CrlHandle x);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreSetRevocationFlag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CryptoNative_X509StoreSetRevocationFlag(SafeX509StoreHandle ctx, X509RevocationFlag revocationFlag);

        internal static void X509StoreSetRevocationFlag(SafeX509StoreHandle ctx, X509RevocationFlag revocationFlag)
        {
            if (!CryptoNative_X509StoreSetRevocationFlag(ctx, revocationFlag))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxInit")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool X509StoreCtxInit(
            SafeX509StoreCtxHandle ctx,
            SafeX509StoreHandle store,
            SafeX509Handle x509,
            SafeX509StackHandle extraCerts);

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_X509VerifyCert(SafeX509StoreCtxHandle ctx);

        internal static bool X509VerifyCert(SafeX509StoreCtxHandle ctx)
        {
            int result = CryptoNative_X509VerifyCert(ctx);

            if (result < 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            return result != 0;
        }

        [LibraryImport(Libraries.CryptoNative)]
        internal static partial int CryptoNative_X509StoreCtxGetError(SafeX509StoreCtxHandle ctx);

        internal static X509VerifyStatusCode X509StoreCtxGetError(SafeX509StoreCtxHandle ctx)
        {
            return (X509VerifyStatusCode)CryptoNative_X509StoreCtxGetError(ctx);
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_X509StoreCtxReset(SafeX509StoreCtxHandle ctx);

        internal static void X509StoreCtxReset(SafeX509StoreCtxHandle ctx)
        {
            if (CryptoNative_X509StoreCtxReset(ctx) != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_X509StoreCtxRebuildChain(SafeX509StoreCtxHandle ctx);

        internal static bool X509StoreCtxRebuildChain(SafeX509StoreCtxHandle ctx)
        {
            int result = CryptoNative_X509StoreCtxRebuildChain(ctx);

            if (result < 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            return result != 0;
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetErrorDepth")]
        internal static partial int X509StoreCtxGetErrorDepth(SafeX509StoreCtxHandle ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxSetVerifyCallback")]
        internal static partial void X509StoreCtxSetVerifyCallback(SafeX509StoreCtxHandle ctx, X509StoreVerifyCallback callback);

        internal static string GetX509VerifyCertErrorString(int n)
        {
            return Marshal.PtrToStringUTF8(X509VerifyCertErrorString(n))!;
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509VerifyCertErrorString")]
        private static partial IntPtr X509VerifyCertErrorString(int n);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509CrlDestroy")]
        internal static partial void X509CrlDestroy(IntPtr a);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemWriteBioX509Crl")]
        internal static partial int PemWriteBioX509Crl(SafeBioHandle bio, SafeX509CrlHandle crl);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemReadBioX509Crl")]
        internal static partial SafeX509CrlHandle PemReadBioX509Crl(SafeBioHandle bio);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509SubjectPublicKeyInfoDerSize")]
        internal static partial int GetX509SubjectPublicKeyInfoDerSize(SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeX509SubjectPublicKeyInfo")]
        internal static partial int EncodeX509SubjectPublicKeyInfo(SafeX509Handle x509, byte[] buf);

        internal enum X509VerifyStatusCodeUniversal
        {
            X509_V_OK = 0,
            X509_V_ERR_UNSPECIFIED = 1,
            X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT = 2,
            X509_V_ERR_UNABLE_TO_GET_CRL = 3,
            X509_V_ERR_UNABLE_TO_DECRYPT_CERT_SIGNATURE = 4,
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

            // Code 24 varies.

            X509_V_ERR_PATH_LENGTH_EXCEEDED = 25,
            X509_V_ERR_INVALID_PURPOSE = 26,
            X509_V_ERR_CERT_UNTRUSTED = 27,
            X509_V_ERR_CERT_REJECTED = 28,
            X509_V_ERR_SUBJECT_ISSUER_MISMATCH = 29,
            X509_V_ERR_AKID_SKID_MISMATCH = 30,
            X509_V_ERR_AKID_ISSUER_SERIAL_MISMATCH = 31,
            X509_V_ERR_KEYUSAGE_NO_CERTSIGN = 32,
            X509_V_ERR_UNABLE_TO_GET_CRL_ISSUER = 33,
            X509_V_ERR_UNHANDLED_CRITICAL_EXTENSION = 34,
            X509_V_ERR_KEYUSAGE_NO_CRL_SIGN = 35,
            X509_V_ERR_UNHANDLED_CRITICAL_CRL_EXTENSION = 36,
            X509_V_ERR_INVALID_NON_CA = 37,
            X509_V_ERR_PROXY_PATH_LENGTH_EXCEEDED = 38,
            X509_V_ERR_KEYUSAGE_NO_DIGITAL_SIGNATURE = 39,
            X509_V_ERR_PROXY_CERTIFICATES_NOT_ALLOWED = 40,
            X509_V_ERR_INVALID_EXTENSION = 41,
            X509_V_ERR_INVALID_POLICY_EXTENSION = 42,
            X509_V_ERR_NO_EXPLICIT_POLICY = 43,
            X509_V_ERR_DIFFERENT_CRL_SCOPE = 44,
            X509_V_ERR_UNSUPPORTED_EXTENSION_FEATURE = 45,
            X509_V_ERR_UNNESTED_RESOURCE = 46,
            X509_V_ERR_PERMITTED_VIOLATION = 47,
            X509_V_ERR_EXCLUDED_VIOLATION = 48,
            X509_V_ERR_SUBTREE_MINMAX = 49,
            X509_V_ERR_APPLICATION_VERIFICATION = 50,
            X509_V_ERR_UNSUPPORTED_CONSTRAINT_TYPE = 51,
            X509_V_ERR_UNSUPPORTED_CONSTRAINT_SYNTAX = 52,
            X509_V_ERR_UNSUPPORTED_NAME_SYNTAX = 53,
            X509_V_ERR_CRL_PATH_VALIDATION_ERROR = 54,
            X509_V_ERR_SUITE_B_INVALID_VERSION = 56,
            X509_V_ERR_SUITE_B_INVALID_ALGORITHM = 57,
            X509_V_ERR_SUITE_B_INVALID_CURVE = 58,
            X509_V_ERR_SUITE_B_INVALID_SIGNATURE_ALGORITHM = 59,
            X509_V_ERR_SUITE_B_LOS_NOT_ALLOWED = 60,
            X509_V_ERR_SUITE_B_CANNOT_SIGN_P_384_WITH_P_256 = 61,
            X509_V_ERR_HOSTNAME_MISMATCH = 62,
            X509_V_ERR_EMAIL_MISMATCH = 63,
            X509_V_ERR_IP_ADDRESS_MISMATCH = 64,
        }
        internal enum X509VerifyStatusCode102
        {
            X509_V_ERR_INVALID_CA = 24,

            X509_V_ERR_INVALID_CALL = 65,
            X509_V_ERR_STORE_LOOKUP = 66,
            X509_V_ERR_PROXY_SUBJECT_NAME_VIOLATION = 67,
        }

        internal enum X509VerifyStatusCode111
        {
            X509_V_ERR_INVALID_CA = 24,

            X509_V_ERR_DANE_NO_MATCH = 65,
            X509_V_ERR_EE_KEY_TOO_SMALL = 66,
            X509_V_ERR_CA_KEY_TOO_SMALL = 67,
            X509_V_ERR_CA_MD_TOO_WEAK = 68,
            X509_V_ERR_INVALID_CALL = 69,
            X509_V_ERR_STORE_LOOKUP = 70,
            X509_V_ERR_NO_VALID_SCTS = 71,
            X509_V_ERR_PROXY_SUBJECT_NAME_VIOLATION = 72,
            X509_V_ERR_OCSP_VERIFY_NEEDED = 73,
            X509_V_ERR_OCSP_VERIFY_FAILED = 74,
            X509_V_ERR_OCSP_CERT_UNKNOWN = 75,
            X509_V_ERR_SIGNATURE_ALGORITHM_MISMATCH = 76,
            X509_V_ERR_NO_ISSUER_PUBLIC_KEY = 77,
            X509_V_ERR_UNSUPPORTED_SIGNATURE_ALGORITHM = 78,
            X509_V_ERR_EC_KEY_EXPLICIT_PARAMS = 79,
        }

        internal enum X509VerifyStatusCode30
        {
            X509_V_ERR_NO_ISSUER_PUBLIC_KEY = 24,

            X509_V_ERR_DANE_NO_MATCH = 65,
            X509_V_ERR_EE_KEY_TOO_SMALL = 66,
            X509_V_ERR_CA_KEY_TOO_SMALL = 67,
            X509_V_ERR_CA_MD_TOO_WEAK = 68,
            X509_V_ERR_INVALID_CALL = 69,
            X509_V_ERR_STORE_LOOKUP = 70,
            X509_V_ERR_NO_VALID_SCTS = 71,
            X509_V_ERR_PROXY_SUBJECT_NAME_VIOLATION = 72,
            X509_V_ERR_OCSP_VERIFY_NEEDED = 73,
            X509_V_ERR_OCSP_VERIFY_FAILED = 74,
            X509_V_ERR_OCSP_CERT_UNKNOWN = 75,
            X509_V_ERR_UNSUPPORTED_SIGNATURE_ALGORITHM = 76,
            X509_V_ERR_SIGNATURE_ALGORITHM_MISMATCH = 77,
            X509_V_ERR_SIGNATURE_ALGORITHM_INCONSISTENCY = 78,
            X509_V_ERR_INVALID_CA = 79,
            X509_V_ERR_PATHLEN_INVALID_FOR_NON_CA = 80,
            X509_V_ERR_PATHLEN_WITHOUT_KU_KEY_CERT_SIGN = 81,
            X509_V_ERR_KU_KEY_CERT_SIGN_INVALID_FOR_NON_CA = 82,
            X509_V_ERR_ISSUER_NAME_EMPTY = 83,
            X509_V_ERR_SUBJECT_NAME_EMPTY = 84,
            X509_V_ERR_MISSING_AUTHORITY_KEY_IDENTIFIER = 85,
            X509_V_ERR_MISSING_SUBJECT_KEY_IDENTIFIER = 86,
            X509_V_ERR_EMPTY_SUBJECT_ALT_NAME = 87,
            X509_V_ERR_EMPTY_SUBJECT_SAN_NOT_CRITICAL = 88,
            X509_V_ERR_CA_BCONS_NOT_CRITICAL = 89,
            X509_V_ERR_AUTHORITY_KEY_IDENTIFIER_CRITICAL = 90,
            X509_V_ERR_SUBJECT_KEY_IDENTIFIER_CRITICAL = 91,
            X509_V_ERR_CA_CERT_MISSING_KEY_USAGE = 92,
            X509_V_ERR_EXTENSIONS_REQUIRE_VERSION_3 = 93,
            X509_V_ERR_EC_KEY_EXPLICIT_PARAMS = 94,
        }

        internal readonly struct X509VerifyStatusCode : IEquatable<X509VerifyStatusCode>
        {
            internal static readonly X509VerifyStatusCode X509_V_OK = X509VerifyStatusCodeUniversal.X509_V_OK;

            public int Code { get; }

            internal X509VerifyStatusCode(int code)
            {
                Code = code;
            }

            public X509VerifyStatusCodeUniversal UniversalCode => (X509VerifyStatusCodeUniversal)Code;
            public X509VerifyStatusCode102 Code102 => (X509VerifyStatusCode102)Code;
            public X509VerifyStatusCode111 Code111 => (X509VerifyStatusCode111)Code;
            public X509VerifyStatusCode30 Code30 => (X509VerifyStatusCode30)Code;

            public bool Equals(X509VerifyStatusCode other) => Code == other.Code;

            public override bool Equals([NotNullWhen(true)] object? obj) => obj is X509VerifyStatusCode other && Equals(other);

            public override int GetHashCode() => Code.GetHashCode();

            public static bool operator ==(X509VerifyStatusCode left, X509VerifyStatusCode right) => left.Equals(right);

            public static bool operator !=(X509VerifyStatusCode left, X509VerifyStatusCode right) => !left.Equals(right);

            public static explicit operator X509VerifyStatusCode(int code)
            {
                return new X509VerifyStatusCode(code);
            }

            public static implicit operator X509VerifyStatusCode(X509VerifyStatusCodeUniversal code)
            {
                return new X509VerifyStatusCode((int)code);
            }
        }
    }
}
