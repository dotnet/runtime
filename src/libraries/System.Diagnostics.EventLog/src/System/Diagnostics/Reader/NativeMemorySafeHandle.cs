// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// A SafeHandle implementation over native memory allocated via <see cref="NativeMemoryHelper.Alloc"/>.
    /// </summary>
    internal sealed class NativeMemorySafeHandle : SafeHandle
    {
        public NativeMemorySafeHandle()
            : base(IntPtr.Zero, true)
        {
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
            NativeMemoryHelper.Free(handle);
            handle = IntPtr.Zero;
            return true;
        }

        public static NativeMemorySafeHandle Zero
        {
            get
            {
                return new NativeMemorySafeHandle();
            }
        }
    }
}
