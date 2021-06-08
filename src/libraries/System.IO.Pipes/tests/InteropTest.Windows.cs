// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            return TryHandleGetImpersonationUserNameError(handle, Marshal.GetLastPInvokeError(), UserNameMaxLength, userName, out impersonationUserName);
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

        private static unsafe bool TryHandleGetImpersonationUserNameError(SafePipeHandle handle, int error, uint userNameMaxLength, char* userName, out string impersonationUserName)
        {
            if ((error == Interop.Errors.ERROR_SUCCESS || error == Interop.Errors.ERROR_CANNOT_IMPERSONATE) && Environment.Is64BitProcess)
            {
                Interop.Kernel32.LoadLibraryEx("sspicli.dll", IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);

                if (Interop.Kernel32.GetNamedPipeHandleStateW(handle, null, null, null, null, userName, userNameMaxLength))
                {
                    impersonationUserName = new string(userName);
                    return true;
                }
            }

            impersonationUserName = string.Empty;
            return false;
        }
    }
}
