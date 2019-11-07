// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// The class contains interop declarations and helpers methods for them.
    /// </summary>
    internal static partial class InteropTest
    {
        internal static unsafe bool CancelIoEx(SafeHandle handle)
        {
            return Interop.Kernel32.CancelIoEx(handle, null);
        }

        internal static unsafe bool TryGetImpersonationUserName(SafePipeHandle handle, out string impersonationUserName)
        {
            const uint UserNameMaxLength = Interop.Kernel32.CREDUI_MAX_USERNAME_LENGTH + 1;
            char* userName = stackalloc char[(int)UserNameMaxLength];

            if (Interop.Kernel32.GetNamedPipeHandleStateW(handle, null, null, null, null, userName, UserNameMaxLength))
            {
                impersonationUserName = new string(userName);
                return true;
            }

            return TryHandleGetImpersonationUserNameError(handle, Marshal.GetLastWin32Error(), UserNameMaxLength, userName, out impersonationUserName);
        }

        internal static unsafe bool TryGetNumberOfServerInstances(SafePipeHandle handle, out uint numberOfServerInstances)
        {
            uint serverInstances;

            if (Interop.Kernel32.GetNamedPipeHandleStateW(handle, null, &serverInstances, null, null, null, 0))
            {
                numberOfServerInstances = serverInstances;
                return true;
            }

            numberOfServerInstances = 0;
            return false;
        }

        // @todo: These are called by some Unix-specific tests. Those tests should really be split out into
        // partial classes and included only in Unix builds.
        internal static bool TryGetHostName(out string hostName) { throw new Exception("Should not call on Windows."); }
    }
}
