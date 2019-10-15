// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeBrotliEncoderHandle : SafeHandle
    {
        public SafeBrotliEncoderHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Brotli.BrotliEncoderDestroyInstance(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    internal sealed class SafeBrotliDecoderHandle : SafeHandle
    {
        public SafeBrotliDecoderHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            Interop.Brotli.BrotliDecoderDestroyInstance(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
