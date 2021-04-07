// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class SafeCertContextHandle : SafePointerHandle<SafeCertContextHandle>
    {
        protected override bool ReleaseHandle()
        {
            bool success = Interop.Crypt32.CertFreeCertificateContext(handle);
            return success;
        }
    }

    internal sealed class SafeCertStoreHandle : SafePointerHandle<SafeCertStoreHandle>
    {
        protected sealed override bool ReleaseHandle()
        {
            bool success = Interop.Crypt32.CertCloseStore(handle, 0);
            return success;
        }
    }

    internal abstract class SafePointerHandle<T> : SafeHandle where T : SafeHandle, new()
    {
        protected SafePointerHandle() : base(IntPtr.Zero, true)
        {
        }

        public sealed override bool IsInvalid => handle == IntPtr.Zero;

        public static T InvalidHandle => SafeHandleCache<T>.GetInvalidHandle(() => new T());

        protected override void Dispose(bool disposing)
        {
            if (!SafeHandleCache<T>.IsCachedInvalidHandle(this))
            {
                base.Dispose(disposing);
            }
        }
    }
}
