// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace Internal.Cryptography
{
    /// <summary>
    ///     Holds a managed string marshaled as a LPCWSTR. 
    /// </summary>
    internal sealed class SafeUnicodeStringHandle : SafeHandle
    {
        /// <summary>
        ///     Marshal a String to a native LPCWSTR. It is permitted to pass "null" for the string.
        /// </summary>
        public SafeUnicodeStringHandle(string s)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            handle = Marshal.StringToHGlobalUni(s);
        }

        public sealed override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        protected sealed override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}


