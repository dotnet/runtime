// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// Wrap a string- or SecureString-based object. A null value indicates IntPtr.Zero should be used.
    /// </summary>
    internal sealed partial class SafePasswordHandle : SafeHandle
    {
        public SafePasswordHandle(string password)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            if (password != null)
            {
                SetHandle(CreateHandle(password));
            }
        }

        public SafePasswordHandle(SecureString password)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            if (password != null)
            {
                SetHandle(CreateHandle(password));
            }
        }

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                FreeHandle();
            }
            SetHandle((IntPtr)(-1));
            return true;
        }

        public override bool IsInvalid => handle == (IntPtr)(-1);
    }
}
