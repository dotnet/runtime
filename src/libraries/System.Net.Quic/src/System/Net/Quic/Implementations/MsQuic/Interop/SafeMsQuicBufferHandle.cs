// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal sealed unsafe class SafeMsQuicBufferHandle : SafeHandle
    {
        public int Count;

        public override bool IsInvalid => handle == IntPtr.Zero;

        public SafeMsQuicBufferHandle(int count)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            IntPtr buffer = Marshal.AllocHGlobal(sizeof(QuicBuffer) * count);
            SetHandle(buffer);
            Count = count;
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
