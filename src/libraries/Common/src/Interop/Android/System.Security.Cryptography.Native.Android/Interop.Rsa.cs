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
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaCreate")]
        internal static extern SafeRsaHandle RsaCreate();

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaUpRef")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RsaUpRef(IntPtr rsa);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaDestroy")]
        internal static extern void RsaDestroy(IntPtr rsa);

        internal static SafeRsaHandle DecodeRsaSubjectPublicKeyInfo(ReadOnlySpan<byte> buf) =>
            DecodeRsaSubjectPublicKeyInfo(ref MemoryMarshal.GetReference(buf), buf.Length);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DecodeRsaSubjectPublicKeyInfo")]
        private static extern SafeRsaHandle DecodeRsaSubjectPublicKeyInfo(ref byte buf, int len);

        internal static int RsaPublicEncrypt(
            int flen,
            ReadOnlySpan<byte> from,
            Span<byte> to,
            SafeRsaHandle rsa,
            RsaPadding padding) =>
            RsaPublicEncrypt(flen, ref MemoryMarshal.GetReference(from), ref MemoryMarshal.GetReference(to), rsa, padding);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaPublicEncrypt")]
        private static extern int RsaPublicEncrypt(
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

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaPrivateDecrypt")]
        private static extern int RsaPrivateDecrypt(
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

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaSignPrimitive")]
        private static extern int RsaSignPrimitive(
            int flen,
            ref byte from,
            ref byte to,
            SafeRsaHandle rsa);

        internal static int RsaVerificationPrimitive(
            ReadOnlySpan<byte> from,
            Span<byte> to,
            SafeRsaHandle rsa) =>
            RsaVerificationPrimitive(from.Length, ref MemoryMarshal.GetReference(from), ref MemoryMarshal.GetReference(to), rsa);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaVerificationPrimitive")]
        private static extern int RsaVerificationPrimitive(
            int flen,
            ref byte from,
            ref byte to,
            SafeRsaHandle rsa);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaSize")]
        internal static extern int RsaSize(SafeRsaHandle rsa);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_RsaGenerateKeyEx")]
        internal static extern int RsaGenerateKeyEx(SafeRsaHandle rsa, int bits);

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

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_GetRsaParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetRsaParameters(
            SafeRsaHandle key,
            out SafeBignumHandle n,
            out SafeBignumHandle e,
            out SafeBignumHandle d,
            out SafeBignumHandle p,
            out SafeBignumHandle dmp1,
            out SafeBignumHandle q,
            out SafeBignumHandle dmq1,
            out SafeBignumHandle iqmp);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SetRsaParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetRsaParameters(
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
            NoPadding = 2,
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
                throw new CryptographicException();
            }

            safeHandle.SetHandle(handle);
            return safeHandle;
        }
    }
}
