// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Transactions
{
    // Keep an interface pointer that will not be used in a SafeHandle derived so
    // that it will be properly released.
    internal sealed class SafeIUnknown : SafeHandle
    {
        private SafeIUnknown() : base(IntPtr.Zero, true) { }

        internal SafeIUnknown(IntPtr unknown) : base(IntPtr.Zero, true)
        {
            handle = unknown;
        }

        public override bool IsInvalid => IsClosed || IntPtr.Zero == handle;

        override protected bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.
            IntPtr ptr = handle;
            handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                Marshal.Release(ptr);
            }
            return true;
        }
    }
}
