// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class DSAOpenSsl : DSA
    {
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public DSAOpenSsl(DSAParameters parameters)
        {
            ThrowIfNotSupported();
            // Make _key be non-null before calling ImportParameters
            _key = new Lazy<SafeDsaHandle>();
            ImportParameters(parameters);
        }

        /// <summary>
        /// Create an DSAOpenSsl from an <see cref="SafeEvpPKeyHandle"/> whose value is an existing
        /// OpenSSL <c>EVP_PKEY*</c> wrapping an <c>DSA*</c>
        /// </summary>
        /// <param name="pkeyHandle">A SafeHandle for an OpenSSL <c>EVP_PKEY*</c></param>
        /// <exception cref="ArgumentNullException"><paramref name="pkeyHandle"/> is <c>null</c></exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="pkeyHandle"/> <see cref="Runtime.InteropServices.SafeHandle.IsInvalid" />
        /// </exception>
        /// <exception cref="CryptographicException"><paramref name="pkeyHandle"/> is not a valid enveloped <c>DSA*</c></exception>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public DSAOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);

            if (pkeyHandle.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));

            ThrowIfNotSupported();
            // If dsa is valid it has already been up-ref'd, so we can just use this handle as-is.
            SafeDsaHandle key = Interop.Crypto.EvpPkeyGetDsa(pkeyHandle);
            if (key.IsInvalid)
            {
                key.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            SetKey(key);
        }

        /// <summary>
        /// Create an DSAOpenSsl from an existing <see cref="IntPtr"/> whose value is an
        /// existing OpenSSL <c>DSA*</c>.
        /// </summary>
        /// <remarks>
        /// This method will increase the reference count of the <c>DSA*</c>, the caller should
        /// continue to manage the lifetime of their reference.
        /// </remarks>
        /// <param name="handle">A pointer to an OpenSSL <c>DSA*</c></param>
        /// <exception cref="ArgumentException"><paramref name="handle" /> is invalid</exception>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public DSAOpenSsl(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(handle));

            ThrowIfNotSupported();
            SafeDsaHandle ecKeyHandle = SafeDsaHandle.DuplicateHandle(handle);
            SetKey(ecKeyHandle);
        }

        /// <summary>
        /// Obtain a SafeHandle version of an EVP_PKEY* which wraps an DSA* equivalent
        /// to the current key for this instance.
        /// </summary>
        /// <returns>A SafeHandle for the DSA key in OpenSSL</returns>
        public SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            SafeDsaHandle currentKey = _key.Value;
            SafeEvpPKeyHandle pkeyHandle = Interop.Crypto.EvpPkeyCreate();

            try
            {
                // Wrapping our key in an EVP_PKEY will up_ref our key.
                // When the EVP_PKEY is Disposed it will down_ref the key.
                // So everything should be copacetic.
                if (!Interop.Crypto.EvpPkeySetDsa(pkeyHandle, currentKey))
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
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(DSAOpenSsl)));
            }
        }
    }
}
