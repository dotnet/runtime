// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// The class contains interop declarations and helpers methods for them.
    /// </summary>
    internal static partial class InteropTest
    {
        private static unsafe bool TryHandleGetImpersonationUserNameError(SafePipeHandle handle, int error, uint userNameMaxLength, char* userName, out string impersonationUserName)
        {
            if ((error == Interop.Errors.ERROR_SUCCESS || error == Interop.Errors.ERROR_CANNOT_IMPERSONATE) && Environment.Is64BitProcess)
            {
                Interop.Kernel32.LoadLibraryEx("sspicli.dll", IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32).SetHandleAsInvalid();

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
