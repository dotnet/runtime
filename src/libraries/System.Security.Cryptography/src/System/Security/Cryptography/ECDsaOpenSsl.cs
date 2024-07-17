// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaOpenSsl : ECDsa
    {
        /// <summary>
        /// Create an ECDsaOpenSsl from an <see cref="SafeEvpPKeyHandle"/> whose value is an existing
        /// OpenSSL <c>EVP_PKEY*</c> wrapping an <c>EC_KEY*</c>
        /// </summary>
        /// <param name="pkeyHandle">A SafeHandle for an OpenSSL <c>EVP_PKEY*</c></param>
        /// <exception cref="ArgumentNullException"><paramref name="pkeyHandle"/> is <c>null</c></exception>
        /// <exception cref="ArgumentException"><paramref name="pkeyHandle"/> <see cref="SafeHandle.IsInvalid" /></exception>
        /// <exception cref="CryptographicException"><paramref name="pkeyHandle"/> is not a valid enveloped <c>EC_KEY*</c></exception>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);

            if (pkeyHandle.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));

            ThrowIfNotSupported();

            if (pkeyHandle.GetKeyType() != Interop.Crypto.EvpAlgorithmId.ECC)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            _key = new Lazy<SafeEvpPKeyHandle>(pkeyHandle.DuplicateHandle());
            ForceSetKeySize(pkeyHandle.GetKeySizeBits());
        }

        /// <summary>
        /// Create an ECDsaOpenSsl from an existing <see cref="IntPtr"/> whose value is an
        /// existing OpenSSL <c>EC_KEY*</c>.
        /// </summary>
        /// <remarks>
        /// This method will increase the reference count of the <c>EC_KEY*</c>, the caller should
        /// continue to manage the lifetime of their reference.
        /// </remarks>
        /// <param name="handle">A pointer to an OpenSSL <c>EC_KEY*</c></param>
        /// <exception cref="ArgumentException"><paramref name="handle" /> is invalid</exception>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public ECDsaOpenSsl(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(handle));

            ThrowIfNotSupported();

            using (SafeEcKeyHandle ecKeyHandle = SafeEcKeyHandle.DuplicateHandle(handle))
            {
                // CreateEvpPkeyFromEcKey already uprefs so nothing else to do
                _key = new Lazy<SafeEvpPKeyHandle>(Interop.Crypto.CreateEvpPkeyFromEcKey(ecKeyHandle));
            }

            ForceSetKeySize(_key.Value.GetKeySizeBits());
        }

        /// <summary>
        /// Obtain a SafeHandle version of an EVP_PKEY* which wraps an EC_KEY* equivalent
        /// to the current key for this instance.
        /// </summary>
        /// <returns>A SafeHandle for the EC_KEY key in OpenSSL</returns>
        public SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            ThrowIfDisposed();
            return _key.Value.DuplicateHandle();
        }

        static partial void ThrowIfNotSupported()
        {
            if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.PlatformNotSupported_CryptographyOpenSSLNotFound, nameof(ECDsaOpenSsl)));
            }
        }
    }
}
