// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// The class contains interop declarations and helpers methods for them.
    /// </summary>
    internal static partial class InteropTest
    {
        [DllImport("libc", SetLastError = true)]
        private static extern unsafe int gethostname(byte* name, int len);

        internal static unsafe bool TryGetHostName(out string hostName)
        {
            const int HOST_NAME_MAX = 255; // man gethostname
            const int ArrLength = HOST_NAME_MAX + 1;

            byte* name = stackalloc byte[ArrLength];
            int result = gethostname(name, ArrLength);

            if (result == 0)
            {
                hostName = Marshal.PtrToStringAnsi((IntPtr)name);
                return true;
            }

            hostName = "";
            return false;
        }

        // @todo: These are called by some Windows-specific tests. Those tests should really be split out into
        // partial classes and included only in Windows builds.
        internal static unsafe bool CancelIoEx(SafeHandle handle) { throw new Exception("Should not call on Unix."); }
        internal static bool TryGetImpersonationUserName(SafePipeHandle handle, out string impersonationUserName) { throw new Exception("Should not call on Unix."); }
        internal static bool TryGetNumberOfServerInstances(SafePipeHandle handle, out uint numberOfServerInstances) { throw new Exception("Should not call on Unix."); }
    }
}
