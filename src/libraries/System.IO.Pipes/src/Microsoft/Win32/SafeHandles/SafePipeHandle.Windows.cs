// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandle
    {
        private const int DefaultInvalidHandle = 0;

        protected override bool ReleaseHandle()
        {
            return Interop.mincore.CloseHandle(handle);
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero || handle == new IntPtr(-1); }
        }
    }
}
