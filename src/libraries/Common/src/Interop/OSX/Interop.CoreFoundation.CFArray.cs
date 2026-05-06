// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

// Declared as signed long, which has sizeof(void*) on OSX.
using CFIndex = System.IntPtr;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        [LibraryImport(Libraries.CoreFoundationLibrary, EntryPoint = "CFArrayGetCount")]
        private static partial CFIndex _CFArrayGetCount(SafeCFArrayHandle cfArray);

        // Follows the "Get" version of the "Create" rule, so needs to return an IntPtr to
        // prevent CFRelease from being called on the SafeHandle close.
        [LibraryImport(Libraries.CoreFoundationLibrary, EntryPoint = "CFArrayGetValueAtIndex")]
        private static partial IntPtr CFArrayGetValueAtIndex(SafeCFArrayHandle cfArray, CFIndex index);

        internal static long CFArrayGetCount(SafeCFArrayHandle cfArray)
        {
            return _CFArrayGetCount(cfArray).ToInt64();
        }

        internal static IntPtr CFArrayGetValueAtIndex(SafeCFArrayHandle cfArray, int index)
        {
            return CFArrayGetValueAtIndex(cfArray, new CFIndex(index));
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeCFArrayHandle : SafeHandle
    {
        public SafeCFArrayHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal SafeCFArrayHandle(IntPtr handle, bool ownsHandle)
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
