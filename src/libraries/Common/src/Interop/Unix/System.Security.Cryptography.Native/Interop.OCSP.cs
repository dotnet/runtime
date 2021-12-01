// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OcspRequestDestroy")]
        internal static partial void OcspRequestDestroy(IntPtr ocspReq);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetOcspRequestDerSize")]
        internal static partial int GetOcspRequestDerSize(SafeOcspRequestHandle req);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeOcspRequest")]
        internal static partial int EncodeOcspRequest(SafeOcspRequestHandle req, byte[] buf);

        [GeneratedDllImport(Libraries.CryptoNative)]
        private static partial SafeOcspResponseHandle CryptoNative_DecodeOcspResponse(ref byte buf, int len);

        internal static SafeOcspResponseHandle DecodeOcspResponse(ReadOnlySpan<byte> buf)
        {
            return CryptoNative_DecodeOcspResponse(
                ref MemoryMarshal.GetReference(buf),
                buf.Length);
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OcspResponseDestroy")]
        internal static partial void OcspResponseDestroy(IntPtr ocspReq);

        [GeneratedDllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        private static partial int CryptoNative_X509ChainGetCachedOcspStatus(
            SafeX509StoreCtxHandle ctx,
            string cachePath,
            int chainDepth);

        internal static X509VerifyStatusCode X509ChainGetCachedOcspStatus(SafeX509StoreCtxHandle ctx, string cachePath, int chainDepth)
        {
            X509VerifyStatusCode response = (X509VerifyStatusCode)CryptoNative_X509ChainGetCachedOcspStatus(ctx, cachePath, chainDepth);

            if (response.Code < 0)
            {
                Debug.Fail($"Unexpected response from X509ChainGetCachedOcspSuccess: {response}");
                throw new CryptographicException();
            }

            return response;
        }

        [GeneratedDllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        private static partial int CryptoNative_X509ChainVerifyOcsp(
            SafeX509StoreCtxHandle ctx,
            SafeOcspRequestHandle req,
            SafeOcspResponseHandle resp,
            string cachePath,
            int chainDepth);

        internal static X509VerifyStatusCode X509ChainVerifyOcsp(
            SafeX509StoreCtxHandle ctx,
            SafeOcspRequestHandle req,
            SafeOcspResponseHandle resp,
            string cachePath,
            int chainDepth)
        {
            X509VerifyStatusCode response = (X509VerifyStatusCode)CryptoNative_X509ChainVerifyOcsp(ctx, req, resp, cachePath, chainDepth);

            if (response.Code < 0)
            {
                Debug.Fail($"Unexpected response from X509ChainGetCachedOcspSuccess: {response}");
                throw new CryptographicException();
            }

            return response;
        }

        [GeneratedDllImport(Libraries.CryptoNative)]
        private static partial SafeOcspRequestHandle CryptoNative_X509ChainBuildOcspRequest(
            SafeX509StoreCtxHandle storeCtx,
            int chainDepth);

        internal static SafeOcspRequestHandle X509ChainBuildOcspRequest(SafeX509StoreCtxHandle storeCtx, int chainDepth)
        {
            SafeOcspRequestHandle req = CryptoNative_X509ChainBuildOcspRequest(storeCtx, chainDepth);

            if (req.IsInvalid)
            {
                req.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return req;
        }
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
