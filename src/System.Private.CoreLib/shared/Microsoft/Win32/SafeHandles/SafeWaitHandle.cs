// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeWaitHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by P/Invoke marshaler
        private SafeWaitHandle() : base(true)
        {
        }

        public SafeWaitHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }
    }
}
