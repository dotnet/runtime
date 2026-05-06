// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OcspRequestDestroy")]
        internal static partial void OcspRequestDestroy(IntPtr ocspReq);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetOcspRequestDerSize")]
        internal static partial int GetOcspRequestDerSize(SafeOcspRequestHandle req);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeOcspRequest")]
        internal static partial int EncodeOcspRequest(SafeOcspRequestHandle req, byte[] buf);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509BuildOcspRequest")]
        internal static partial SafeOcspRequestHandle X509BuildOcspRequest(IntPtr subject, IntPtr issuer);

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial int CryptoNative_X509DecodeOcspToExpiration(
            byte* buf,
            int len,
            SafeOcspRequestHandle req,
            IntPtr subject,
            IntPtr* issuers,
            int issuersLen,
            ref long expiration);

        internal static unsafe bool X509DecodeOcspToExpiration(
            ReadOnlySpan<byte> buf,
            SafeOcspRequestHandle request,
            IntPtr x509Subject,
            ReadOnlySpan<IntPtr> x509Issuers,
            out DateTimeOffset expiration)
        {
            long timeT = 0;
            int ret;

            fixed (byte* pBuf = buf)
            fixed (IntPtr* pIssuers = x509Issuers)
            {
                ret = CryptoNative_X509DecodeOcspToExpiration(
                    pBuf,
                    buf.Length,
                    request,
                    x509Subject,
                    pIssuers,
                    x509Issuers.Length,
                    ref timeT);
            }

            if (ret == 1)
            {
                if (timeT != 0)
                {
                    expiration = DateTimeOffset.FromUnixTimeSeconds(timeT);
                }
                else
                {
                    // Something went wrong during the determination of when the response
                    // should not be used any longer.
                    // Half an hour sounds fair?
                    expiration = DateTimeOffset.UtcNow.AddMinutes(30);
                }

                return true;
            }

            Debug.Assert(ret == 0, $"Unexpected response from X509DecodeOcspToExpiration: {ret}");
            expiration = DateTimeOffset.MinValue;
            return false;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial SafeOcspResponseHandle CryptoNative_DecodeOcspResponse(ref byte buf, int len);

        internal static SafeOcspResponseHandle DecodeOcspResponse(ReadOnlySpan<byte> buf)
        {
            return CryptoNative_DecodeOcspResponse(
                ref MemoryMarshal.GetReference(buf),
                buf.Length);
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OcspResponseDestroy")]
        internal static partial void OcspResponseDestroy(IntPtr ocspReq);
    }
}

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class SafeOcspRequestHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeOcspRequestHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.OcspRequestDestroy(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }

    internal sealed class SafeOcspResponseHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeOcspResponseHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.OcspResponseDestroy(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }
}
