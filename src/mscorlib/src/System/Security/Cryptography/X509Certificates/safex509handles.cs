// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates {
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using Microsoft.Win32.SafeHandles;

    // Since we need sometimes to delete the key container associated with a cert
    // context, the handle used in this class is actually a pointer
    // to a CERT_CTX unmanaged structure defined in COMX509Certificate.h

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeCertContextHandle() : base (true) {}

        // 0 is an Invalid Handle
        internal SafeCertContextHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeCertContextHandle InvalidHandle {
            get { return new SafeCertContextHandle(IntPtr.Zero); }
        }

        internal IntPtr pCertContext {
            get {
                if (handle == IntPtr.Zero)
                    return IntPtr.Zero;

                return Marshal.ReadIntPtr(handle);
            }
        }

        // This method handles the case where pCert == NULL
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern void _FreePCertContext(IntPtr pCert);

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            _FreePCertContext(handle);
            return true;
        }
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeCertStoreHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeCertStoreHandle() : base (true) {}

        // 0 is an Invalid Handle
        internal SafeCertStoreHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeCertStoreHandle InvalidHandle {
            get { return new SafeCertStoreHandle(IntPtr.Zero); }
        }

        // This method handles the case where hCertStore == NULL
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern void _FreeCertStoreContext(IntPtr hCertStore);

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            _FreeCertStoreContext(handle);
            return true;
        }
    }
}
