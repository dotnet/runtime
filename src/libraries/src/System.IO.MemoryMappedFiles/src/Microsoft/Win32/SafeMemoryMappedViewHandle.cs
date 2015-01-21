// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
#pragma warning disable 0618    // SafeBuffer is obsolete
    public sealed partial class SafeMemoryMappedViewHandle : SafeBuffer
#pragma warning restore
    {
        internal SafeMemoryMappedViewHandle() 
            : base(true) 
        {
        }

        internal SafeMemoryMappedViewHandle(IntPtr handle, bool ownsHandle) 
            : base(ownsHandle)
        {
            base.SetHandle(handle);
        }
    }
}
