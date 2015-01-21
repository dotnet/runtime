// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedViewHandle
    {
        protected override bool ReleaseHandle()
        {
            if (Interop.mincore.UnmapViewOfFile(handle) != 0)
            {
                handle = IntPtr.Zero;
                return true;
            }
            return false;
        }
    }
}
