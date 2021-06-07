// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Microsoft.Win32.SafeHandles
{
    [UnsupportedOSPlatform("tvos")]
    internal sealed class SafeGssNameHandle : SafeHandle
    {
        public override bool IsInvalid
        {
            get {  throw new PlatformNotSupportedException(); }
        }

        protected override bool ReleaseHandle() => throw new PlatformNotSupportedException();
        private SafeGssNameHandle()
            : base(IntPtr.Zero, true)
        {
        }
    }

    [UnsupportedOSPlatform("tvos")]
    internal sealed class SafeGssCredHandle : SafeHandle
    {
        private SafeGssCredHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { throw new PlatformNotSupportedException(); }
        }

        protected override bool ReleaseHandle() => throw new PlatformNotSupportedException();
    }

    [UnsupportedOSPlatform("tvos")]
    internal sealed class SafeGssContextHandle : SafeHandle
    {
        private SafeGssContextHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get {  throw new PlatformNotSupportedException(); }
        }

        protected override bool ReleaseHandle() => throw new PlatformNotSupportedException();
    }
}
