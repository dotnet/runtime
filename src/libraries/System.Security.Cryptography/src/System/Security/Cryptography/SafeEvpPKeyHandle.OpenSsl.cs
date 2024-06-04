// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public sealed partial class SafeEvpPKeyHandle : SafeHandle
    {
        internal static readonly SafeEvpPKeyHandle InvalidHandle = new SafeEvpPKeyHandle();

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public SafeEvpPKeyHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public SafeEvpPKeyHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.EvpPkeyDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        /// <summary>
        /// Create another instance of SafeEvpPKeyHandle which has an independent lifetime
        /// from this instance, but tracks the same resource.
        /// </summary>
        /// <returns>An equivalent SafeEvpPKeyHandle with a different lifetime</returns>
        public SafeEvpPKeyHandle DuplicateHandle()
        {
            if (IsInvalid)
                throw new InvalidOperationException(SR.Cryptography_OpenInvalidHandle);

            // Reliability: Allocate the SafeHandle before calling UpRefEvpPkey so
            // that we don't lose a tracked reference in low-memory situations.
            SafeEvpPKeyHandle safeHandle = new SafeEvpPKeyHandle();

            int success = Interop.Crypto.UpRefEvpPkey(this);

            if (success != 1)
            {
                Debug.Fail("Called UpRefEvpPkey on a key which was already marked for destruction");
                Exception e = Interop.Crypto.CreateOpenSslCryptographicException();
                safeHandle.Dispose();
                throw e;
            }

            // Since we didn't actually create a new handle, copy the handle
            // to the new SafeHandle.
            safeHandle.SetHandle(handle);
            return safeHandle;
        }

        /// <summary>
        ///   Open a named private key using a named OpenSSL <code>ENGINE</code>.
        /// </summary>
        /// <param name="engineName">
        ///   The name of the <code>ENGINE</code> to process the private key open request.
        /// </param>
        /// <param name="keyId">
        ///   The name of the key to open.
        /// </param>
        /// <returns>
        ///   The opened key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="engineName"/> or <paramref name="keyId"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="engineName"/> or <paramref name="keyId"/> is the empty string.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the key could not be opened via the specified ENGINE.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     This operation will fail if OpenSSL cannot successfully load the named <code>ENGINE</code>,
        ///     or if the named <code>ENGINE</code> cannot load the named key.
        ///   </para>
        ///   <para>
        ///     Not all <code>ENGINE</code>s support loading private keys.
        ///   </para>
        ///   <para>
        ///     The syntax for <paramref name="keyId"/> is determined by each individual
        ///     <code>ENGINE</code>.
        ///   </para>
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenPrivateKeyFromEngine(string engineName, string keyId)
        {
            ArgumentException.ThrowIfNullOrEmpty(engineName);
            ArgumentException.ThrowIfNullOrEmpty(keyId);

            if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
            }

            return Interop.Crypto.LoadPrivateKeyFromEngine(engineName, keyId);
        }

        /// <summary>
        ///   Open a named public key using a named OpenSSL <code>ENGINE</code>.
        /// </summary>
        /// <param name="engineName">
        ///   The name of the <code>ENGINE</code> to process the public key open request.
        /// </param>
        /// <param name="keyId">
        ///   The name of the key to open.
        /// </param>
        /// <returns>
        ///   The opened key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="engineName"/> or <paramref name="keyId"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="engineName"/> or <paramref name="keyId"/> is the empty string.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   the key could not be opened via the specified ENGINE.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     This operation will fail if OpenSSL cannot successfully load the named <code>ENGINE</code>,
        ///     or if the named <code>ENGINE</code> cannot load the named key.
        ///   </para>
        ///   <para>
        ///     Not all <code>ENGINE</code>s support loading public keys, even ones that support
        ///     loading private keys.
        ///   </para>
        ///   <para>
        ///     The syntax for <paramref name="keyId"/> is determined by each individual
        ///     <code>ENGINE</code>.
        ///   </para>
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static SafeEvpPKeyHandle OpenPublicKeyFromEngine(string engineName, string keyId)
        {
            ArgumentException.ThrowIfNullOrEmpty(engineName);
            ArgumentException.ThrowIfNullOrEmpty(keyId);

            if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
            }

            return Interop.Crypto.LoadPublicKeyFromEngine(engineName, keyId);
        }
    }
}
