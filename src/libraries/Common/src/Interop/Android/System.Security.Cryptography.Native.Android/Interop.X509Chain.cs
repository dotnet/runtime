// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainCreateContext")]
        internal static extern SafeX509ChainContextHandle X509ChainCreateContext(
            SafeX509Handle cert,
            IntPtr[] extraStore,
            int extraStoreLen);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainDestroyContext")]
        internal static extern void X509ChainDestroyContext(IntPtr ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainEvaluate")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool X509ChainEvaluate(
            SafeX509ChainContextHandle ctx,
            long timeInMsFromUnixEpoch);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainGetCertificateCount")]
        private static extern int X509ChainGetCertificateCount(SafeX509ChainContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainGetCertificates")]
        private static extern int X509ChainGetCertificates(
            SafeX509ChainContextHandle ctx,
            IntPtr[] certs,
            int certsLen);

        internal static IntPtr[] X509ChainGetCertificates(SafeX509ChainContextHandle ctx)
        {
            int count = Interop.AndroidCrypto.X509ChainGetCertificateCount(ctx);
            var certPtrs = new IntPtr[count];

            int res = Interop.AndroidCrypto.X509ChainGetCertificates(ctx, certPtrs, certPtrs.Length);
            if (res != SUCCESS)
                throw new CryptographicException();

            return certPtrs;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainSetCustomTrustStore")]
        internal static extern int X509ChainSetCustomTrustStore(
            SafeX509ChainContextHandle ctx,
            IntPtr[] customTrustStore,
            int customTrustStoreLen);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainSupportsRevocationOptions")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool X509ChainSupportsRevocationOptions();

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainValidate")]
        internal static extern int X509ChainValidate(
            SafeX509ChainContextHandle ctx,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag);
    }
}

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class SafeX509ChainContextHandle : SafeHandle
    {
        public SafeX509ChainContextHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.X509ChainDestroyContext(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
