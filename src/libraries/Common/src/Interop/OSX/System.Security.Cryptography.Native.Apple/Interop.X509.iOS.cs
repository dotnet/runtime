// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        private static readonly SafeCreateHandle s_emptyExportString =
            CoreFoundation.CFStringCreateWithCString("");

        private static int AppleCryptoNative_X509ImportCertificate(
            ReadOnlySpan<byte> keyBlob,
            X509ContentType contentType,
            SafeCreateHandle cfPfxPassphrase,
            out SafeSecCertificateHandle pCertOut,
            out SafeSecIdentityHandle pPrivateKeyOut)
        {
            return AppleCryptoNative_X509ImportCertificate(
                ref MemoryMarshal.GetReference(keyBlob),
                keyBlob.Length,
                contentType,
                cfPfxPassphrase,
                out pCertOut,
                out pPrivateKeyOut);
        }

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_X509ImportCertificate(
            ref byte pbKeyBlob,
            int cbKeyBlob,
            X509ContentType contentType,
            SafeCreateHandle cfPfxPassphrase,
            out SafeSecCertificateHandle pCertOut,
            out SafeSecIdentityHandle pPrivateKeyOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_X509ImportCollection(
            ref byte pbKeyBlob,
            int cbKeyBlob,
            X509ContentType contentType,
            SafeCreateHandle cfPfxPassphrase,
            out SafeCFArrayHandle pCollectionOut);

        internal static SafeSecCertificateHandle X509ImportCertificate(
            ReadOnlySpan<byte> bytes,
            X509ContentType contentType,
            SafePasswordHandle importPassword,
            out SafeSecIdentityHandle identityHandle)
        {
            SafeCreateHandle? cfPassphrase = null;
            bool releasePassword = false;

            try
            {
                if (!importPassword.IsInvalid)
                {
                    importPassword.DangerousAddRef(ref releasePassword);
                    cfPassphrase = CoreFoundation.CFStringCreateFromSpan(importPassword.DangerousGetSpan());
                }

                return X509ImportCertificate(
                    bytes,
                    contentType,
                    cfPassphrase,
                    out identityHandle);
            }
            finally
            {
                if (releasePassword)
                {
                    importPassword.DangerousRelease();
                }

                cfPassphrase?.Dispose();
            }
        }

        private static SafeSecCertificateHandle X509ImportCertificate(
            ReadOnlySpan<byte> bytes,
            X509ContentType contentType,
            SafeCreateHandle? importPassword,
            out SafeSecIdentityHandle identityHandle)
        {
            SafeSecCertificateHandle certHandle;
            SafeCreateHandle cfPassphrase = importPassword ?? s_emptyExportString;

            int osStatus = AppleCryptoNative_X509ImportCertificate(
                bytes,
                contentType,
                cfPassphrase,
                out certHandle,
                out identityHandle);

            if (osStatus == 0)
            {
                return certHandle;
            }

            certHandle.Dispose();
            identityHandle.Dispose();

            throw CreateExceptionForOSStatus(osStatus);
        }

        internal static SafeCFArrayHandle X509ImportCollection(
            ReadOnlySpan<byte> bytes,
            X509ContentType contentType,
            SafePasswordHandle importPassword)
        {
            SafeCreateHandle cfPassphrase = s_emptyExportString;
            bool releasePassword = false;
            SafeCFArrayHandle collectionHandle;
            int osStatus;

            try
            {
                if (!importPassword.IsInvalid)
                {
                    importPassword.DangerousAddRef(ref releasePassword);
                    IntPtr passwordHandle = importPassword.DangerousGetHandle();

                    if (passwordHandle != IntPtr.Zero)
                    {
                        cfPassphrase = CoreFoundation.CFStringCreateWithCString(passwordHandle);
                    }
                }

                osStatus = AppleCryptoNative_X509ImportCollection(
                    ref MemoryMarshal.GetReference(bytes),
                    bytes.Length,
                    contentType,
                    cfPassphrase,
                    out collectionHandle);

                if (osStatus == 0)
                {
                    return collectionHandle;
                }
            }
            finally
            {
                if (releasePassword)
                {
                    importPassword.DangerousRelease();
                }

                if (cfPassphrase != s_emptyExportString)
                {
                    cfPassphrase.Dispose();
                }
            }

            collectionHandle.Dispose();
            throw CreateExceptionForOSStatus(osStatus);
        }

        internal static SafeSecCertificateHandle X509GetCertFromIdentity(SafeSecIdentityHandle identity)
        {
            SafeSecCertificateHandle cert;
            int osStatus = AppleCryptoNative_X509CopyCertFromIdentity(identity, out cert);

            if (osStatus != 0)
            {
                cert.Dispose();
                throw CreateExceptionForOSStatus(osStatus);
            }

            if (cert.IsInvalid)
            {
                cert.Dispose();
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            return cert;
        }

        internal static bool X509DemuxAndRetainHandle(
            IntPtr handle,
            out SafeSecCertificateHandle certHandle,
            out SafeSecIdentityHandle identityHandle)
        {
            int result = AppleCryptoNative_X509DemuxAndRetainHandle(handle, out certHandle, out identityHandle);

            switch (result)
            {
                case 1:
                    return true;
                case 0:
                    return false;
                default:
                    Debug.Fail($"AppleCryptoNative_X509DemuxAndRetainHandle returned {result}");
                    throw new CryptographicException();
            }
        }
    }
}

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class SafeSecIdentityHandle : SafeHandle
    {
        public SafeSecIdentityHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.CoreFoundation.CFRelease(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeSecCertificateHandle : SafeHandle
    {
        public SafeSecCertificateHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.CoreFoundation.CFRelease(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
