// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeZStdCompressHandle : SafeHandle
    {
        public SafeZStdCompressHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeCCtx(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZStdDecompressHandle : SafeHandle
    {
        public SafeZStdDecompressHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeDCtx(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZStdCDictHandle : SafeHandle
    {
        public SafeZStdCDictHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeCDict(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeZStdDDictHandle : SafeHandle
    {
        public SafeZStdDDictHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Zstd.ZSTD_freeDDict(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
