// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainCopyDefault(out SafeKeychainHandle keychain);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainOpen(
            string keychainPath,
            out SafeKeychainHandle keychain);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainUnlock(
            SafeKeychainHandle keychain,
            int utf8PassphraseLength,
            byte[] utf8Passphrase);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainEnumerateCerts(
            SafeKeychainHandle keychain,
            out SafeCFArrayHandle matches,
            out int pOSStatus);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainEnumerateIdentities(
            SafeKeychainHandle keychain,
            out SafeCFArrayHandle matches,
            out int pOSStatus);

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static SafeKeychainHandle SecKeychainItemCopyKeychain(SafeKeychainItemHandle item)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static SafeKeychainHandle SecKeychainItemCopyKeychain(IntPtr item)
        {
            throw new PlatformNotSupportedException();
        }

        internal static SafeKeychainHandle SecKeychainCopyDefault()
        {
            SafeKeychainHandle keychain;
            int osStatus = AppleCryptoNative_SecKeychainCopyDefault(out keychain);

            if (osStatus == 0)
            {
                return keychain;
            }

            keychain.Dispose();
            throw CreateExceptionForOSStatus(osStatus);
        }

        internal static SafeKeychainHandle SecKeychainOpen(string keychainPath)
        {
            SafeKeychainHandle keychain;
            int osStatus = AppleCryptoNative_SecKeychainOpen(keychainPath, out keychain);

            if (osStatus == 0)
            {
                return keychain;
            }

            keychain.Dispose();
            throw CreateExceptionForOSStatus(osStatus);
        }

        internal static SafeCFArrayHandle KeychainEnumerateCerts(SafeKeychainHandle keychainHandle)
        {
            SafeCFArrayHandle matches;
            int osStatus;
            int result = AppleCryptoNative_SecKeychainEnumerateCerts(keychainHandle, out matches, out osStatus);

            if (result == 1)
            {
                return matches;
            }

            matches.Dispose();

            if (result == 0)
                throw CreateExceptionForOSStatus(osStatus);

            Debug.Fail($"Unexpected result from AppleCryptoNative_SecKeychainEnumerateCerts: {result}");
            throw new CryptographicException();
        }

        internal static SafeCFArrayHandle KeychainEnumerateIdentities(SafeKeychainHandle keychainHandle)
        {
            SafeCFArrayHandle matches;
            int osStatus;
            int result = AppleCryptoNative_SecKeychainEnumerateIdentities(keychainHandle, out matches, out osStatus);

            if (result == 1)
            {
                return matches;
            }

            matches.Dispose();

            if (result == 0)
                throw CreateExceptionForOSStatus(osStatus);

            Debug.Fail($"Unexpected result from AppleCryptoNative_SecKeychainEnumerateCerts: {result}");
            throw new CryptographicException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static SafeKeychainHandle CreateOrOpenKeychain(string keychainPath, bool createAllowed)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static unsafe SafeTemporaryKeychainHandle CreateTemporaryKeychain()
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static void SecKeychainDelete(IntPtr handle, bool throwOnError=true)
        {
            throw new PlatformNotSupportedException();
        }
    }
}

namespace System.Security.Cryptography.Apple
{
    internal class SafeKeychainItemHandle : SafeHandle
    {
        public SafeKeychainItemHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        protected override bool ReleaseHandle()
        {
            SafeTemporaryKeychainHandle.UntrackItem(handle);
            Interop.CoreFoundation.CFRelease(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal class SafeKeychainHandle : SafeHandle
    {
        public SafeKeychainHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal SafeKeychainHandle(IntPtr handle)
            : base(handle, ownsHandle: true)
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

    internal sealed class SafeTemporaryKeychainHandle : SafeKeychainHandle
    {
        private static readonly Dictionary<IntPtr, SafeTemporaryKeychainHandle> s_lookup =
            new Dictionary<IntPtr, SafeTemporaryKeychainHandle>();

        internal SafeTemporaryKeychainHandle()
        {
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        protected override bool ReleaseHandle()
        {
            throw new PlatformNotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && SafeHandleCache<SafeTemporaryKeychainHandle>.IsCachedInvalidHandle(this))
            {
                return;
            }

            base.Dispose(disposing);
        }

        public static SafeTemporaryKeychainHandle InvalidHandle =>
            SafeHandleCache<SafeTemporaryKeychainHandle>.GetInvalidHandle(() => new SafeTemporaryKeychainHandle());

        internal static void TrackKeychain(SafeTemporaryKeychainHandle toTrack)
        {
            if (toTrack.IsInvalid)
            {
                return;
            }

            lock (s_lookup)
            {
                Debug.Assert(!s_lookup.ContainsKey(toTrack.handle));

                s_lookup[toTrack.handle] = toTrack;
            }
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static void TrackItem(SafeKeychainItemHandle keychainItem)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static void UntrackItem(IntPtr keychainItem)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
