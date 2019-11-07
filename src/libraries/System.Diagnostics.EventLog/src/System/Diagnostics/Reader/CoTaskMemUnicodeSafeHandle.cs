// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// A SafeHandle implementation over a native CoTaskMem allocated via SecureStringToCoTaskMemUnicode.
    /// </summary>
    internal sealed class CoTaskMemUnicodeSafeHandle : SafeHandle
    {
        internal CoTaskMemUnicodeSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        internal CoTaskMemUnicodeSafeHandle(IntPtr handle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(handle);
        }

        internal void SetMemory(IntPtr handle)
        {
            SetHandle(handle);
        }

        internal IntPtr GetMemory()
        {
            return handle;
        }

        public override bool IsInvalid
        {
            get
            {
                return IsClosed || handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            Marshal.ZeroFreeCoTaskMemUnicode(handle);
            handle = IntPtr.Zero;
            return true;
        }

        // DONT compare CoTaskMemUnicodeSafeHandle with CoTaskMemUnicodeSafeHandle.Zero
        // use IsInvalid instead. Zero is provided where a NULL handle needed
        public static CoTaskMemUnicodeSafeHandle Zero
        {
            get
            {
                return new CoTaskMemUnicodeSafeHandle();
            }
        }
    }
}
