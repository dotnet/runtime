// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
#if !PROJECTN
    ///////////////////////////////////////////////////////////////
    //
    // This is implementation of Safe AllocHGlobal which is turned out
    // to be LocalAlloc down in CLR
    //
    ///////////////////////////////////////////////////////////////
#if DEBUG
    internal sealed class SafeLocalFree : DebugSafeHandle
    {
#else
    internal sealed class SafeLocalFree : SafeHandleZeroOrMinusOneIsInvalid {
#endif
        private const int LMEM_FIXED = 0;
        private const int NULL = 0;

        // This returned handle cannot be modified by the application.
        public static SafeLocalFree Zero = new SafeLocalFree(false);

        private SafeLocalFree() : base(true) { }

        private SafeLocalFree(bool ownsHandle) : base(ownsHandle) { }

        public static SafeLocalFree LocalAlloc(int cb)
        {
            SafeLocalFree result = UnsafeCommonNativeMethods.LocalAlloc(LMEM_FIXED, (UIntPtr)cb);
            if (result.IsInvalid)
            {
                result.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }
            return result;
        }

        override protected bool ReleaseHandle()
        {
            return UnsafeCommonNativeMethods.LocalFree(handle) == IntPtr.Zero;
        }
    }
#endif //!PROJECTN
}
