// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net.Security
{
#if DEBUG
    internal abstract class SafeDeleteContext : DebugSafeHandle
    {
#else
    internal abstract class SafeDeleteContext : SafeHandle
    {
#endif
        public SafeDeleteContext(IntPtr handle) : base(handle, true)
        {
        }

        public SafeDeleteContext(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            get { return (IntPtr.Zero == handle); }
        }

        protected override bool ReleaseHandle()
        {
            return true;
        }
    }
}
