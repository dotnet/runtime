// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeDirectoryHandle : SafeHandle
    {
        private SafeDirectoryHandle() : base(IntPtr.Zero, true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Interop.Sys.CloseDir(handle) == 0;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
}
