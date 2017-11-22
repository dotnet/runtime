// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    // Class of critical handle which uses 0 or -1 as an invalid handle.
    public abstract class CriticalHandleZeroOrMinusOneIsInvalid : CriticalHandle
    {
        protected CriticalHandleZeroOrMinusOneIsInvalid()
            : base(IntPtr.Zero)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero || handle == new IntPtr(-1);
    }
}
