// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    internal sealed class SafeEvpCipherCtxHandle : SafeHandle
    {
        private SafeEvpCipherCtxHandle() : 
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.EVP_CIPHER_CTX_cleanup(handle);
            Marshal.FreeHGlobal(handle);
            return true;
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero; }
        }

        internal static SafeEvpCipherCtxHandle Create()
        {
            IntPtr memPtr = IntPtr.Zero;
            bool succeeded = false;
            SafeEvpCipherCtxHandle safeHandle = null;

            try
            {
                memPtr = Marshal.AllocHGlobal(Interop.libcrypto.EVP_CIPHER_CTX_SIZE);
                safeHandle = new SafeEvpCipherCtxHandle();
                safeHandle.SetHandle(memPtr);

                Interop.libcrypto.EVP_CIPHER_CTX_init(safeHandle);

                succeeded = true;
                return safeHandle;
            }
            finally
            {
                if (!succeeded)
                {
                    // If we made it to SetHandle, and failed calling EVP_CIPHER_CTX_init
                    // then OpenSSL hasn't built this object yet, and we shouldn't call
                    // EVP_CIPHER_CTX_cleanup.
                    if (safeHandle != null && !safeHandle.IsInvalid)
                    {
                        safeHandle.SetHandleAsInvalid();
                    }

                    if (memPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(memPtr);
                    }
                }
            }
        }
    }
}
