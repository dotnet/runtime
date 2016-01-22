// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeTokenHandle : SafeHandle
    {
        private SafeTokenHandle() : base(IntPtr.Zero, true) { }

        // 0 is an Invalid Handle
        internal SafeTokenHandle(IntPtr handle) : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        internal static SafeTokenHandle InvalidHandle
        {
            get { return new SafeTokenHandle(IntPtr.Zero); }
        }

        public override bool IsInvalid
        {
            get
            { return handle == new IntPtr(0) || handle == new IntPtr(-1); }
        }

        override protected bool ReleaseHandle()
        {
            return Interop.mincore.CloseHandle(handle);
        }
    }
}
