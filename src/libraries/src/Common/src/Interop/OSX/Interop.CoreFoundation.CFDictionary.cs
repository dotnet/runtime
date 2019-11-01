// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        [DllImport(Libraries.CoreFoundationLibrary)]
        internal static extern IntPtr CFDictionaryGetValue(SafeCFDictionaryHandle handle, IntPtr key);
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeCFDictionaryHandle : SafeHandle
    {
        private SafeCFDictionaryHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal SafeCFDictionaryHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.CoreFoundation.CFRelease(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
