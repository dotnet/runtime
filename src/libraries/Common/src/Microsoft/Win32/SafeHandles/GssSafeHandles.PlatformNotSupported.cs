// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeGssNameHandle : SafeHandle
    {
        public static SafeGssNameHandle CreateUser(string name) => throw new PlatformNotSupportedException();
        public static SafeGssNameHandle CreateTarget(string name) => throw new PlatformNotSupportedException();
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

    internal sealed class SafeGssCredHandle : SafeHandle
    {
        public static SafeGssCredHandle CreateAcceptor() => throw new PlatformNotSupportedException();
        public static SafeGssCredHandle Create(string username, string password, bool isNtlmOnly) => throw new PlatformNotSupportedException();
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
