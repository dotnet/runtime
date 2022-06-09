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
        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
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

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_X509ChainHasStapledOcsp(SafeX509StoreCtxHandle storeCtx);

        internal static bool X509ChainHasStapledOcsp(SafeX509StoreCtxHandle storeCtx)
        {
            int resp = CryptoNative_X509ChainHasStapledOcsp(storeCtx);

            if (resp == 1)
            {
                return true;
            }

            Debug.Assert(resp == 0, $"Unexpected response from X509ChainHasStapledOcsp: {resp}");
            return false;
        }

        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
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

        [LibraryImport(Libraries.CryptoNative)]
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
