// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainItemCopyKeychain(
            IntPtr item,
            out SafeKeychainHandle keychain);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SecKeychainCreate")]
        private static extern unsafe int AppleCryptoNative_SecKeychainCreateTemporary(
            string path,
            int utf8PassphraseLength,
            byte* utf8Passphrase,
            out SafeTemporaryKeychainHandle keychain);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainCreate(
            string path,
            int utf8PassphraseLength,
            byte[] utf8Passphrase,
            out SafeKeychainHandle keychain);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainDelete(IntPtr keychain);

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
        private static extern int AppleCryptoNative_SetKeychainNeverLock(SafeKeychainHandle keychain);

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

        private static SafeKeychainHandle SecKeychainItemCopyKeychain(SafeHandle item)
        {
            bool addedRef = false;

            try
            {
                item.DangerousAddRef(ref addedRef);
                var handle = SecKeychainItemCopyKeychain(item.DangerousGetHandle());
                return handle;
            }
            finally
            {
                if (addedRef)
                {
                    item.DangerousRelease();
                }
            }
        }

        internal static SafeKeychainHandle SecKeychainItemCopyKeychain(SafeKeychainItemHandle item)
            => SecKeychainItemCopyKeychain((SafeHandle)item);

        internal static SafeKeychainHandle SecKeychainItemCopyKeychain(SafeSecKeyRefHandle item)
            => SecKeychainItemCopyKeychain((SafeHandle)item);

        internal static SafeKeychainHandle SecKeychainItemCopyKeychain(IntPtr item)
        {
            SafeKeychainHandle keychain;
            int osStatus = AppleCryptoNative_SecKeychainItemCopyKeychain(item, out keychain);

            // A whole lot of NULL is expected from this.
            // Any key or cert which isn't keychain-backed, and this is the primary way we'd find that out.
            if (keychain.IsInvalid)
            {
                GC.SuppressFinalize(keychain);
            }

            if (osStatus == 0)
            {
                return keychain;
            }

            throw CreateExceptionForOSStatus(osStatus);
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

        internal static SafeKeychainHandle CreateOrOpenKeychain(string keychainPath, bool createAllowed)
        {
            const int errSecAuthFailed = -25293;
            const int errSecDuplicateKeychain = -25296;

            SafeKeychainHandle keychain;
            int osStatus;

            if (createAllowed)
            {
                // Attempt to create first
                osStatus = AppleCryptoNative_SecKeychainCreate(
                    keychainPath,
                    0,
                    Array.Empty<byte>(),
                    out keychain);

                if (osStatus == 0)
                {
                    return keychain;
                }

                if (osStatus != errSecDuplicateKeychain)
                {
                    keychain.Dispose();
                    throw CreateExceptionForOSStatus(osStatus);
                }
            }

            osStatus = AppleCryptoNative_SecKeychainOpen(keychainPath, out keychain);
            if (osStatus == 0)
            {
                // Try to unlock with empty password to match our behaviour in CreateKeychain.
                // If the password doesn't match then ignore it silently and fallback to the
                // default behavior of user interaction.
                osStatus = AppleCryptoNative_SecKeychainUnlock(keychain, 0, Array.Empty<byte>());
                if (osStatus == 0 || osStatus == errSecAuthFailed)
                {
                    return keychain;
                }
            }

            keychain.Dispose();
            throw CreateExceptionForOSStatus(osStatus);
        }

        internal static unsafe SafeTemporaryKeychainHandle CreateTemporaryKeychain()
        {
            const int randomSize = 256;
            string tmpKeychainPath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString("N") + ".keychain");

            // Use a random password so that if a keychain is abandoned it isn't recoverable.
            // We use stack to minimize lingering
            Span<byte> random = stackalloc byte[randomSize];
            RandomNumberGenerator.Fill(random);

            // Create hex-like UTF8 string.
            Span<byte> utf8Passphrase =  stackalloc byte[randomSize * 2 +1];
            utf8Passphrase[randomSize * 2] = 0; // null termination for C string.

            for (int i = 0; i < random.Length; i++)
            {
                // Instead of true hexadecimal, we simply take lower and upper 4 bits and we offset them from ASCII 'A'
                // to get printable form. We dont use managed string to avoid lingering copies.
                utf8Passphrase[i*2] = (byte)((random[i] & 0x0F) + 65);
                utf8Passphrase[i*2 + 1] = (byte)((random[i] >> 4) & 0x0F + 65);
            }

            // clear the binary bits.
            CryptographicOperations.ZeroMemory(random);

            SafeTemporaryKeychainHandle keychain;
            int osStatus;

            fixed (byte* ptr = utf8Passphrase)
            {
                osStatus = AppleCryptoNative_SecKeychainCreateTemporary(
                    tmpKeychainPath,
                    utf8Passphrase.Length,
                    ptr,
                    out keychain);
            }

            CryptographicOperations.ZeroMemory(utf8Passphrase);
            SafeTemporaryKeychainHandle.TrackKeychain(keychain);

            if (osStatus == 0)
            {
                osStatus = AppleCryptoNative_SetKeychainNeverLock(keychain);
            }

            if (osStatus != 0)
            {
                keychain.Dispose();
                throw CreateExceptionForOSStatus(osStatus);
            }

            return keychain;
        }

        internal static void SecKeychainDelete(IntPtr handle, bool throwOnError=true)
        {
            int osStatus = AppleCryptoNative_SecKeychainDelete(handle);

            if (throwOnError && osStatus != 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }
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

        protected override bool ReleaseHandle()
        {
            lock (s_lookup)
            {
                s_lookup.Remove(handle);
            }

            Interop.AppleCrypto.SecKeychainDelete(handle, throwOnError: false);
            return base.ReleaseHandle();
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

        internal static void TrackItem(SafeKeychainItemHandle keychainItem)
        {
            if (keychainItem.IsInvalid)
                return;

            using (SafeKeychainHandle keychain = Interop.AppleCrypto.SecKeychainItemCopyKeychain(keychainItem))
            {
                if (keychain.IsInvalid)
                {
                    return;
                }

                lock (s_lookup)
                {
                    SafeTemporaryKeychainHandle? temporaryHandle;

                    if (s_lookup.TryGetValue(keychain.DangerousGetHandle(), out temporaryHandle))
                    {
                        bool ignored = false;
                        temporaryHandle.DangerousAddRef(ref ignored);
                    }
                }
            }
        }

        internal static void UntrackItem(IntPtr keychainItem)
        {
            using (SafeKeychainHandle keychain = Interop.AppleCrypto.SecKeychainItemCopyKeychain(keychainItem))
            {
                if (keychain.IsInvalid)
                {
                    return;
                }

                lock (s_lookup)
                {
                    SafeTemporaryKeychainHandle? temporaryHandle;

                    if (s_lookup.TryGetValue(keychain.DangerousGetHandle(), out temporaryHandle))
                    {
                        temporaryHandle.DangerousRelease();
                    }
                }
            }
        }
    }
}
