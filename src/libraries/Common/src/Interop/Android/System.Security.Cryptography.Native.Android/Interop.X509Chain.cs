// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainBuild")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool X509ChainBuild(
            SafeX509ChainContextHandle ctx,
            long timeInMsFromUnixEpoch);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainGetCertificateCount")]
        private static extern int X509ChainGetCertificateCount(SafeX509ChainContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainGetCertificates")]
        private static extern int X509ChainGetCertificates(
            SafeX509ChainContextHandle ctx,
            IntPtr[] certs,
            int certsLen);

        internal static X509Certificate2[] X509ChainGetCertificates(SafeX509ChainContextHandle ctx)
        {
            int count = Interop.AndroidCrypto.X509ChainGetCertificateCount(ctx);
            var certPtrs = new IntPtr[count];

            int res = Interop.AndroidCrypto.X509ChainGetCertificates(ctx, certPtrs, certPtrs.Length);
            if (res == 0)
                throw new CryptographicException();

            Debug.Assert(res <= certPtrs.Length);

            var certs = new X509Certificate2[certPtrs.Length];
            for (int i = 0; i < res; i++)
            {
                certs[i] = new X509Certificate2(certPtrs[i]);
            }

            if (res == certPtrs.Length)
            {
                return certs;
            }

            return certs[0..res];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ValidationError
        {
            public IntPtr Message; // UTF-16 string
            public int Index;
            public int Status;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainGetErrorCount")]
        private static extern int X509ChainGetErrorCount(SafeX509ChainContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainGetErrors")]
        private static unsafe extern int X509ChainGetErrors(
            SafeX509ChainContextHandle ctx,
            [Out] ValidationError[] errors,
            int errorsLen);

        internal static ValidationError[] X509ChainGetErrors(SafeX509ChainContextHandle ctx)
        {
            int count = Interop.AndroidCrypto.X509ChainGetErrorCount(ctx);
            if (count == 0)
                return Array.Empty<ValidationError>();

            var errors = new ValidationError[count];
            int res = Interop.AndroidCrypto.X509ChainGetErrors(ctx, errors, errors.Length);
            if (res != SUCCESS)
                throw new CryptographicException();

            return errors;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainSetCustomTrustStore")]
        internal static extern int X509ChainSetCustomTrustStore(
            SafeX509ChainContextHandle ctx,
            IntPtr[] customTrustStore,
            int customTrustStoreLen);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainSupportsRevocationOptions")]
        [return:MarshalAs(UnmanagedType.U1)]
        internal static extern bool X509ChainSupportsRevocationOptions();

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509ChainValidate")]
        internal static extern int X509ChainValidate(
            SafeX509ChainContextHandle ctx,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            out byte checkedRevocation);
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
