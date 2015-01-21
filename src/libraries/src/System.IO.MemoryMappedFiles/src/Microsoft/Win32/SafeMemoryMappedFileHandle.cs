// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    public sealed partial class SafeMemoryMappedFileHandle : SafeHandle
    {
        internal SafeMemoryMappedFileHandle()
            : base(new IntPtr(DefaultInvalidHandleValue), true)
        {
        }

        internal SafeMemoryMappedFileHandle(IntPtr handle, bool ownsHandle)
            : base(new IntPtr(DefaultInvalidHandleValue), ownsHandle)
        {
            SetHandle(handle);
        }
    }
}
