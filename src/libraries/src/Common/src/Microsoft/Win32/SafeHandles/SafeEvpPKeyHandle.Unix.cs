// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal
#else
    public
#endif
    sealed class SafeEvpPKeyHandle : SafeHandle
    {
        internal static readonly SafeEvpPKeyHandle InvalidHandle = new SafeEvpPKeyHandle();

        private SafeEvpPKeyHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

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
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            // Since we didn't actually create a new handle, copy the handle
            // to the new SafeHandle.
            safeHandle.SetHandle(handle);
            return safeHandle;
        }

#if !INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
        /// <summary>
        /// The runtime version number for the loaded version of OpenSSL.
        /// </summary>
        /// <remarks>
        /// For OpenSSL 1.1+ this is the result of <code>OpenSSL_version_num()</code>,
        /// for OpenSSL 1.0.x this is the result of <code>SSLeay()</code>.
        /// </remarks>
        public static long OpenSslVersion { get; } = Interop.OpenSsl.OpenSslVersionNumber();
#endif
    }
}
