// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaCreate")]
        internal static partial SafeRsaHandle RsaCreate();

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaUpRef")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool RsaUpRef(IntPtr rsa);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaDestroy")]
        internal static partial void RsaDestroy(IntPtr rsa);

        internal static SafeRsaHandle DecodeRsaSubjectPublicKeyInfo(ReadOnlySpan<byte> buf) =>
            DecodeRsaSubjectPublicKeyInfo(ref MemoryMarshal.GetReference(buf), buf.Length);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DecodeRsaSubjectPublicKeyInfo")]
        private static partial SafeRsaHandle DecodeRsaSubjectPublicKeyInfo(ref byte buf, int len);

        internal static int RsaPublicEncrypt(
            int flen,
            ReadOnlySpan<byte> from,
            Span<byte> to,
            SafeRsaHandle rsa,
            RsaPadding padding) =>
            RsaPublicEncrypt(flen, ref MemoryMarshal.GetReference(from), ref MemoryMarshal.GetReference(to), rsa, padding);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaPublicEncrypt")]
        private static partial int RsaPublicEncrypt(
            int flen,
            ref byte from,
            ref byte to,
            SafeRsaHandle rsa,
            RsaPadding padding);

        internal static int RsaPrivateDecrypt(
            int flen,
            ReadOnlySpan<byte> from,
            Span<byte> to,
            SafeRsaHandle rsa,
            RsaPadding padding) =>
            RsaPrivateDecrypt(flen, ref MemoryMarshal.GetReference(from), ref MemoryMarshal.GetReference(to), rsa, padding);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaPrivateDecrypt")]
        private static partial int RsaPrivateDecrypt(
            int flen,
            ref byte from,
            ref byte to,
            SafeRsaHandle rsa,
            RsaPadding padding);

        internal static int RsaSignPrimitive(
            ReadOnlySpan<byte> from,
            Span<byte> to,
            SafeRsaHandle rsa) =>
            RsaSignPrimitive(from.Length, ref MemoryMarshal.GetReference(from), ref MemoryMarshal.GetReference(to), rsa);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaSignPrimitive")]
        private static partial int RsaSignPrimitive(
            int flen,
            ref byte from,
            ref byte to,
            SafeRsaHandle rsa);

        internal static int RsaVerificationPrimitive(
            ReadOnlySpan<byte> from,
            Span<byte> to,
            SafeRsaHandle rsa) =>
            RsaVerificationPrimitive(from.Length, ref MemoryMarshal.GetReference(from), ref MemoryMarshal.GetReference(to), rsa);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaVerificationPrimitive")]
        private static partial int RsaVerificationPrimitive(
            int flen,
            ref byte from,
            ref byte to,
            SafeRsaHandle rsa);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaSize")]
        internal static partial int RsaSize(SafeRsaHandle rsa);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RsaGenerateKeyEx")]
        internal static partial int RsaGenerateKeyEx(SafeRsaHandle rsa, int bits);

        internal static RSAParameters ExportRsaParameters(SafeRsaHandle key, bool includePrivateParameters)
        {
            Debug.Assert(
                key != null && !key.IsInvalid,
                "Callers should check the key is invalid and throw an exception with a message");

            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException();
            }

            SafeBignumHandle n, e, d, p, dmp1, q, dmq1, iqmp;
            if (!GetRsaParameters(key,
                out n,
                out e,
                out d,
                out p,
                out dmp1,
                out q,
                out dmq1,
                out iqmp))
            {
                n.Dispose();
                e.Dispose();
                d.Dispose();
                p.Dispose();
                dmp1.Dispose();
                q.Dispose();
                dmq1.Dispose();
                iqmp.Dispose();
                throw new CryptographicException();
            }

            using (n)
            using (e)
            using (d)
            using (p)
            using (dmp1)
            using (q)
            using (dmq1)
            using (iqmp)
            {
                int modulusSize = RsaSize(key);

                // RSACryptoServiceProvider expects P, DP, Q, DQ, and InverseQ to all
                // be padded up to half the modulus size.
                int halfModulus = modulusSize / 2;

                RSAParameters rsaParameters = new RSAParameters
                {
                    Modulus = Crypto.ExtractBignum(n, modulusSize)!,
                    Exponent = Crypto.ExtractBignum(e, 0)!,
                };

                if (includePrivateParameters)
                {
                    rsaParameters.D = Crypto.ExtractBignum(d, modulusSize);
                    rsaParameters.P = Crypto.ExtractBignum(p, halfModulus);
                    rsaParameters.DP = Crypto.ExtractBignum(dmp1, halfModulus);
                    rsaParameters.Q = Crypto.ExtractBignum(q, halfModulus);
                    rsaParameters.DQ = Crypto.ExtractBignum(dmq1, halfModulus);
                    rsaParameters.InverseQ = Crypto.ExtractBignum(iqmp, halfModulus);
                }

                return rsaParameters;
            }
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_GetRsaParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetRsaParameters(
            SafeRsaHandle key,
            out SafeBignumHandle n,
            out SafeBignumHandle e,
            out SafeBignumHandle d,
            out SafeBignumHandle p,
            out SafeBignumHandle dmp1,
            out SafeBignumHandle q,
            out SafeBignumHandle dmq1,
            out SafeBignumHandle iqmp);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_SetRsaParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetRsaParameters(
            SafeRsaHandle key,
            byte[]? n,
            int nLength,
            byte[]? e,
            int eLength,
            byte[]? d,
            int dLength,
            byte[]? p,
            int pLength,
            byte[]? dmp1,
            int dmp1Length,
            byte[]? q,
            int qLength,
            byte[]? dmq1,
            int dmq1Length,
            byte[]? iqmp,
            int iqmpLength);

        internal enum RsaPadding : int
        {
            Pkcs1 = 0,
            OaepSHA1 = 1,
            OaepSHA256 = 2,
            OaepSHA384 = 3,
            OaepSHA512 = 4,
        }
    }
}

namespace System.Security.Cryptography
{
    internal sealed class SafeRsaHandle : SafeKeyHandle
    {
        public SafeRsaHandle()
        {
        }

        public SafeRsaHandle(IntPtr ptr)
        {
            SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.RsaDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        internal override SafeRsaHandle DuplicateHandle() => DuplicateHandle(DangerousGetHandle());

        internal static SafeRsaHandle DuplicateHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            // Reliability: Allocate the SafeHandle before calling RSA_up_ref so
            // that we don't lose a tracked reference in low-memory situations.
            SafeRsaHandle safeHandle = new SafeRsaHandle();

            if (!Interop.AndroidCrypto.RsaUpRef(handle))
            {
                safeHandle.Dispose();
                throw new CryptographicException();
            }

            safeHandle.SetHandle(handle);
            return safeHandle;
        }
    }
}
