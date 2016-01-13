// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal class SafeThreadPoolIOHandle : SafeHandle
    {
        private SafeThreadPoolIOHandle() 
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            Interop.mincore.CloseThreadpoolIo(handle);
            return true;
        }
    }
}
