// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// Represents a safe handle to a Windows pseudo console created with CreatePseudoConsole.
    /// </summary>
    internal sealed class SafePseudoConsoleHandle : SafeHandle
    {
        public SafePseudoConsoleHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Interop.Kernel32.ClosePseudoConsole(handle);
            return true;
        }
    }
}
