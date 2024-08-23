// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

// Declared as signed long, which has sizeof(void*) on OSX.
using CFIndex = System.IntPtr;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        [LibraryImport(Libraries.CoreFoundationLibrary)]
        private static partial CFIndex CFErrorGetCode(SafeCFErrorHandle cfError);

        [LibraryImport(Libraries.CoreFoundationLibrary)]
        private static partial SafeCFStringHandle CFErrorCopyDescription(SafeCFErrorHandle cfError);

        internal static int GetErrorCode(SafeCFErrorHandle cfError)
        {
            unchecked
            {
                return (int)(CFErrorGetCode(cfError).ToInt64());
            }
        }

        internal static string? GetErrorDescription(SafeCFErrorHandle cfError)
        {
            Debug.Assert(cfError != null);

            if (cfError.IsInvalid)
            {
                return null;
            }

            Debug.Assert(!cfError.IsClosed);

            using (SafeCFStringHandle cfString = CFErrorCopyDescription(cfError))
            {
                return CFStringToString(cfString);
            }
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeCFErrorHandle : SafeHandle
    {
        public SafeCFErrorHandle()
            : base(IntPtr.Zero, ownsHandle: true)
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
