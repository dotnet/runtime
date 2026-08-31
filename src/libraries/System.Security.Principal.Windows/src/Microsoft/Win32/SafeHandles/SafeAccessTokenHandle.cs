// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed class SafeAccessTokenHandle : SafeHandle
    {
        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeAccessTokenHandle" />.
        /// </summary>
        public SafeAccessTokenHandle() : base(IntPtr.Zero, true) { }

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeAccessTokenHandle" /> around a Windows thread or process access token.
        /// </summary>
        /// <param name="handle">Handle to wrap</param>
        public SafeAccessTokenHandle(IntPtr handle) : base(handle, true) { }

        public static SafeAccessTokenHandle InvalidHandle
        {
            get
            {
                return new SafeAccessTokenHandle(IntPtr.Zero);
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero || handle == new IntPtr(-1);
            }
        }

        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }
    }
}
