// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OcspRequestDestroy")]
        internal static extern void OcspRequestDestroy(IntPtr ocspReq);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetOcspRequestDerSize")]
        internal static extern int GetOcspRequestDerSize(SafeOcspRequestHandle req);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeOcspRequest")]
        internal static extern int EncodeOcspRequest(SafeOcspRequestHandle req, byte[] buf);

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeOcspResponseHandle CryptoNative_DecodeOcspResponse(ref byte buf, int len);

        internal static SafeOcspResponseHandle DecodeOcspResponse(ReadOnlySpan<byte> buf)
        {
            return CryptoNative_DecodeOcspResponse(
                ref MemoryMarshal.GetReference(buf),
                buf.Length);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OcspResponseDestroy")]
        internal static extern void OcspResponseDestroy(IntPtr ocspReq);


        [DllImport(Libraries.CryptoNative)]
        private static extern X509VerifyStatusCode CryptoNative_X509ChainGetCachedOcspStatus(SafeX509StoreCtxHandle ctx, string cachePath);

        internal static X509VerifyStatusCode X509ChainGetCachedOcspStatus(SafeX509StoreCtxHandle ctx, string cachePath)
        {
            X509VerifyStatusCode response = CryptoNative_X509ChainGetCachedOcspStatus(ctx, cachePath);

            if (response < 0)
            {
                Debug.Fail($"Unexpected response from X509ChainGetCachedOcspSuccess: {response}");
                throw new CryptographicException();
            }

            return response;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern X509VerifyStatusCode CryptoNative_X509ChainVerifyOcsp(
            SafeX509StoreCtxHandle ctx,
            SafeOcspRequestHandle req,
            SafeOcspResponseHandle resp,
            string cachePath);

        internal static X509VerifyStatusCode X509ChainVerifyOcsp(
            SafeX509StoreCtxHandle ctx,
            SafeOcspRequestHandle req,
            SafeOcspResponseHandle resp,
            string cachePath)
        {
            X509VerifyStatusCode response = CryptoNative_X509ChainVerifyOcsp(ctx, req, resp, cachePath);

            if (response < 0)
            {
                Debug.Fail($"Unexpected response from X509ChainGetCachedOcspSuccess: {response}");
                throw new CryptographicException();
            }

            return response;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeOcspRequestHandle CryptoNative_X509ChainBuildOcspRequest(SafeX509StoreCtxHandle storeCtx);

        internal static SafeOcspRequestHandle X509ChainBuildOcspRequest(SafeX509StoreCtxHandle storeCtx)
        {
            SafeOcspRequestHandle req = CryptoNative_X509ChainBuildOcspRequest(storeCtx);

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
    internal class SafeOcspRequestHandle : SafeHandleZeroOrMinusOneIsInvalid
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

    internal class SafeOcspResponseHandle : SafeHandleZeroOrMinusOneIsInvalid
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
