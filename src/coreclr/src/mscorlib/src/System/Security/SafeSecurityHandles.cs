// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace Microsoft.Win32.SafeHandles {
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Security;

    // Introduce this handle to replace internal SafeTokenHandle,
    // which is mainly used to hold Windows thread or process access token
    [SecurityCritical]
    public sealed class SafeAccessTokenHandle : SafeHandle
    {
        private SafeAccessTokenHandle()
            : base(IntPtr.Zero, true)
        { }

        // 0 is an Invalid Handle
        public SafeAccessTokenHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public static SafeAccessTokenHandle InvalidHandle
        {
            [SecurityCritical]
            get { return new SafeAccessTokenHandle(IntPtr.Zero); }
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero || handle == new IntPtr(-1); }
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeLsaLogonProcessHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeLsaLogonProcessHandle() : base (true) {}

        // 0 is an Invalid Handle
        internal SafeLsaLogonProcessHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeLsaLogonProcessHandle InvalidHandle {
            get { return new SafeLsaLogonProcessHandle(IntPtr.Zero); }
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            // LsaDeregisterLogonProcess returns an NTSTATUS
            return Win32Native.LsaDeregisterLogonProcess(handle) >= 0;
        }
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeLsaMemoryHandle : SafeBuffer {
        private SafeLsaMemoryHandle() : base(true) {}

        // 0 is an Invalid Handle
        internal SafeLsaMemoryHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeLsaMemoryHandle InvalidHandle {
            get { return new SafeLsaMemoryHandle( IntPtr.Zero ); }
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return Win32Native.LsaFreeMemory(handle) == 0;
        }
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeLsaPolicyHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeLsaPolicyHandle() : base(true) {}

        // 0 is an Invalid Handle
        internal SafeLsaPolicyHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeLsaPolicyHandle InvalidHandle {
            get { return new SafeLsaPolicyHandle( IntPtr.Zero ); }
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return Win32Native.LsaClose(handle) == 0;
        }
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeLsaReturnBufferHandle : SafeBuffer {
        private SafeLsaReturnBufferHandle() : base (true) {}

        // 0 is an Invalid Handle
        internal SafeLsaReturnBufferHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeLsaReturnBufferHandle InvalidHandle {
            get { return new SafeLsaReturnBufferHandle(IntPtr.Zero); }
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            // LsaFreeReturnBuffer returns an NTSTATUS
            return Win32Native.LsaFreeReturnBuffer(handle) >= 0;
        }
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeProcessHandle() : base (true) {}

        // 0 is an Invalid Handle
        internal SafeProcessHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeProcessHandle InvalidHandle {
            get { return new SafeProcessHandle(IntPtr.Zero); }
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private SafeThreadHandle() : base (true) {}

        // 0 is an Invalid Handle
        internal SafeThreadHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }
}
