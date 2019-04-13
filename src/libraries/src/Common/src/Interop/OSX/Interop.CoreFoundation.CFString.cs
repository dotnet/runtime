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
        /// <summary>
        /// Returns the interior pointer of the cfString if it has the specified encoding.
        /// If it has the wrong encoding, or if the interior pointer isn't being shared for some reason, returns NULL
        /// </summary>
        [DllImport(Libraries.CoreFoundationLibrary)]
        private static extern IntPtr CFStringGetCStringPtr(
            SafeCFStringHandle cfString,
            CFStringBuiltInEncodings encoding);

        [DllImport(Libraries.CoreFoundationLibrary)]
        private static extern SafeCFDataHandle CFStringCreateExternalRepresentation(
            IntPtr alloc,
            SafeCFStringHandle theString,
            CFStringBuiltInEncodings encoding,
            byte lossByte);

        internal static string CFStringToString(SafeCFStringHandle cfString)
        {
            Debug.Assert(cfString != null);
            Debug.Assert(!cfString.IsInvalid);
            Debug.Assert(!cfString.IsClosed);

            // If the string is already stored internally as UTF-8 we can (usually)
            // get the raw pointer to the data blob, then we can Marshal in the string
            // via pointer semantics, avoiding a copy.
            IntPtr interiorPointer = CFStringGetCStringPtr(
                cfString,
                CFStringBuiltInEncodings.kCFStringEncodingUTF8);

            if (interiorPointer != IntPtr.Zero)
            {
                return Marshal.PtrToStringUTF8(interiorPointer);
            }

            SafeCFDataHandle cfData = CFStringCreateExternalRepresentation(
                IntPtr.Zero,
                cfString,
                CFStringBuiltInEncodings.kCFStringEncodingUTF8,
                0);

            using (cfData)
            {
                bool addedRef = false;

                try
                {
                    cfData.DangerousAddRef(ref addedRef);

                    unsafe
                    {
                        // Note that CFDataGetLength(cfData).ToInt32() will throw on
                        // too large of an input. Since a >2GB string is pretty unlikely,
                        // that's considered a good thing here.
                        return Encoding.UTF8.GetString(
                            CFDataGetBytePtr(cfData),
                            CFDataGetLength(cfData).ToInt32());
                    }
                }
                finally
                {
                    if (addedRef)
                    {
                        cfData.DangerousRelease();
                    }
                }
            }
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeCFStringHandle : SafeHandle
    {
        internal SafeCFStringHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal SafeCFStringHandle(IntPtr handle, bool ownsHandle)
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
