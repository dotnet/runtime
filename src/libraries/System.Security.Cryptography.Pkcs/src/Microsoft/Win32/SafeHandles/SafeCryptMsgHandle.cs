// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeCryptMsgHandle : SafeHandle
    {
        private SafeCryptMsgHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        public sealed override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected sealed override bool ReleaseHandle()
        {
            bool success = Interop.Crypt32.CryptMsgClose(handle);
            SetHandle(IntPtr.Zero);
            return success;
        }
    }
}
