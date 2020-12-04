// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Win32.SafeHandles
{
    public partial class SafeWaitHandle
    {
        protected override bool ReleaseHandle()
        {
            CloseEventInternal(handle);
            return true;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void CloseEventInternal(IntPtr handle);
    }
}
