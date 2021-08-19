// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
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
        public ECDsaOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            if (pkeyHandle == null)
                throw new ArgumentNullException(nameof(pkeyHandle));
            if (pkeyHandle.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));

            ThrowIfNotSupported();
            // If ecKey is valid it has already been up-ref'd, so we can just use this handle as-is.
            SafeEcKeyHandle key = Interop.Crypto.EvpPkeyGetEcKey(pkeyHandle);
            if (key.IsInvalid)
            {
                key.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            _key = new ECOpenSsl(key);
            KeySizeValue = _key.KeySize;
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
        public ECDsaOpenSsl(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(handle));

            ThrowIfNotSupported();
            SafeEcKeyHandle ecKeyHandle = SafeEcKeyHandle.DuplicateHandle(handle);
            _key = new ECOpenSsl(ecKeyHandle);
            KeySizeValue = _key.KeySize;
        }

        /// <summary>
        /// Obtain a SafeHandle version of an EVP_PKEY* which wraps an EC_KEY* equivalent
        /// to the current key for this instance.
        /// </summary>
        /// <returns>A SafeHandle for the EC_KEY key in OpenSSL</returns>
        public SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            SafeEcKeyHandle currentKey = _key.Value;
            SafeEvpPKeyHandle pkeyHandle = Interop.Crypto.EvpPkeyCreate();

            try
            {
                // Wrapping our key in an EVP_PKEY will up_ref our key.
                // When the EVP_PKEY is Disposed it will down_ref the key.
                // So everything should be copacetic.
                if (!Interop.Crypto.EvpPkeySetEcKey(pkeyHandle, currentKey))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return pkeyHandle;
            }
            catch
            {
                pkeyHandle.Dispose();
                throw;
            }
        }

        static partial void ThrowIfNotSupported()
        {
            if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(ECDsaOpenSsl)));
            }
        }
    }
}
