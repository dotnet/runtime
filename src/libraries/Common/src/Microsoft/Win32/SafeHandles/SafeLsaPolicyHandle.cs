// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLsaPolicyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeLsaPolicyHandle() : base(true) { }

        // 0 is an Invalid Handle
        internal SafeLsaPolicyHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        override protected bool ReleaseHandle()
        {
            return Interop.Advapi32.LsaClose(handle) == 0;
        }
    }
}
