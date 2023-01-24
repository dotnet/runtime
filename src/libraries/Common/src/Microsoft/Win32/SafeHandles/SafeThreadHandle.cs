// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeThreadHandle : SafeHandle
    {
        public SafeThreadHandle() : base(invalidHandleValue: 0, ownsHandle: true) { }

        public override bool IsInvalid => handle is 0 or -1;

        protected override bool ReleaseHandle() => Interop.Kernel32.CloseHandle(handle);
    }
}
