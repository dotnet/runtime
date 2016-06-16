// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    public sealed partial class SafeRegistryHandle : SafeHandle
    {
        [SecurityCritical]
        internal SafeRegistryHandle() : base(IntPtr.Zero, true) { }

        [SecurityCritical]
        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == new IntPtr(0) || handle == new IntPtr(-1); }
        }
    }
}
