// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using NativeLong=System.IntPtr;
using NativeULong=System.UIntPtr;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal static extern void X509_free(IntPtr a);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeX509Handle d2i_X509(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern unsafe int i2d_X509(SafeX509Handle x, byte** @out);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeX509Handle X509_dup(IntPtr handle);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeX509Handle X509_dup(SafeX509Handle handle);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeX509Handle PEM_read_bio_X509_AUX(SafeBioHandle bio, IntPtr zero, IntPtr zero1, IntPtr zero2);

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_get_serialNumber(SafeX509Handle x);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeX509NameHandle d2i_X509_NAME(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int X509_NAME_print_ex(SafeBioHandle @out, SafeX509NameHandle nm, int indent, NativeULong flags);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void X509_NAME_free(IntPtr a);

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_get_issuer_name(SafeX509Handle a);

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_get_subject_name(SafeX509Handle a);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509_check_purpose(SafeX509Handle x, int id, int ca);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int X509_check_issued(SafeX509Handle issuer, SafeX509Handle subject);

        [DllImport(Libraries.LibCrypto)]
        internal static extern NativeULong X509_issuer_name_hash(SafeX509Handle x);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int X509_get_ext_count(SafeX509Handle x);

        // Returns a pointer already being tracked by the SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        // Bounds checking is in place for "loc", IntPtr.Zero is returned on violations.
        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_get_ext(SafeX509Handle x, int loc);

        // Returns a pointer already being tracked by a SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_EXTENSION_get_object(IntPtr ex);

        // Returns a pointer already being tracked by a SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_EXTENSION_get_data(IntPtr ex);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509_EXTENSION_get_critical(IntPtr ex);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeX509StoreHandle X509_STORE_new();

        [DllImport(Libraries.LibCrypto)]
        internal static extern void X509_STORE_free(IntPtr v);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509_STORE_add_cert(SafeX509StoreHandle ctx, SafeX509Handle x);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509_STORE_add_crl(SafeX509StoreHandle ctx, SafeX509CrlHandle x);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509_STORE_set_flags(SafeX509StoreHandle ctx, X509VerifyFlags flags);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeX509StoreCtxHandle X509_STORE_CTX_new();

        [DllImport(Libraries.LibCrypto)]
        internal static extern void X509_STORE_CTX_free(IntPtr v);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509_STORE_CTX_init(SafeX509StoreCtxHandle ctx, SafeX509StoreHandle store, SafeX509Handle x509, IntPtr zero);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int X509_verify_cert(SafeX509StoreCtxHandle ctx);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeX509StackHandle X509_STORE_CTX_get1_chain(SafeX509StoreCtxHandle ctx);

        [DllImport(Libraries.LibCrypto)]
        internal static extern X509VerifyStatusCode X509_STORE_CTX_get_error(SafeX509StoreCtxHandle ctx);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int X509_STORE_CTX_get_error_depth(SafeX509StoreCtxHandle ctx);

        [DllImport(Libraries.LibCrypto)]
        internal static extern string X509_verify_cert_error_string(X509VerifyStatusCode n);
        
        [DllImport(Libraries.LibCrypto)]
        internal static extern void X509_CRL_free(IntPtr a);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeX509CrlHandle d2i_X509_CRL(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int PEM_write_bio_X509_CRL(SafeBioHandle bio, SafeX509CrlHandle crl);

        [DllImport(Libraries.LibCrypto)]
        private static extern SafeX509CrlHandle PEM_read_bio_X509_CRL(SafeBioHandle bio, IntPtr zero, IntPtr zero1, IntPtr zero2);

        internal static SafeX509CrlHandle PEM_read_bio_X509_CRL(SafeBioHandle bio)
        {
            return PEM_read_bio_X509_CRL(bio, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        // This is "unsigned long" in native, which means ulong on x64, uint on x86
        // But we only support x64 for now.
        [Flags]
        internal enum X509VerifyFlags : ulong
        {
            None = 0,
            X509_V_FLAG_CB_ISSUER_CHECK = 0x0001,
            X509_V_FLAG_USE_CHECK_TIME = 0x0002,
            X509_V_FLAG_CRL_CHECK = 0x0004,
            X509_V_FLAG_CRL_CHECK_ALL = 0x0008,
            X509_V_FLAG_IGNORE_CRITICAL = 0x0010,
            X509_V_FLAG_X509_STRICT = 0x0020,
            X509_V_FLAG_ALLOW_PROXY_CERTS = 0x0040,
            X509_V_FLAG_POLICY_CHECK = 0x0080,
            X509_V_FLAG_EXPLICIT_POLICY = 0x0100,
            X509_V_FLAG_INHIBIT_ANY = 0x0200,
            X509_V_FLAG_INHIBIT_MAP = 0x0400,
            X509_V_FLAG_NOTIFY_POLICY = 0x0800,

            X509_V_FLAG_CHECK_SS_SIGNATURE = 0x4000,
        }

        internal enum X509VerifyStatusCode
        {
            X509_V_OK = 0,
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
            X509_V_ERR_INVALID_CA = 24,
            X509_V_ERR_PATH_LENGTH_EXCEEDED = 25,
            X509_V_ERR_INVALID_PURPOSE = 26,
            X509_V_ERR_CERT_UNTRUSTED = 27,
            X509_V_ERR_CERT_REJECTED = 28,
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
            X509_V_ERR_UNNESTED_RESOURCE = 44,
            X509_V_ERR_APPLICATION_VERIFICATION = 50,
        }
    }
}
