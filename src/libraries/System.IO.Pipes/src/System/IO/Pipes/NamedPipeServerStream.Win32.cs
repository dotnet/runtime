// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe server
    /// </summary>
    public sealed partial class NamedPipeServerStream : PipeStream
    {
        // Depending on the Windows platform, we will try to reload a potentially missing DLL
        // and reattempt the retrieval of the impersonation username.
        private unsafe string HandleGetImpersonationUserNameError(int error, uint userNameMaxLength, char* userName)
        {
            // There is a known problem in Win32 64-bit where if sspicli is not loaded, this function fails unexpectedly with ERROR_SUCCESS, so we need to load it and reattempt
            // If sspicli is already loaded, this is a no-op. 
            // We set the handle as invalid so the library won't get unloaded.
            // ERROR_CANNOT_IMPERSONATE is thrown in Windows 7.
            if ((error == Interop.Errors.ERROR_SUCCESS || error == Interop.Errors.ERROR_CANNOT_IMPERSONATE) && Environment.Is64BitProcess)
            {
                Interop.Kernel32.LoadLibraryEx("sspicli.dll", IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32).SetHandleAsInvalid();

                if (Interop.Kernel32.GetNamedPipeHandleStateW(InternalHandle, null, null, null, null, userName, userNameMaxLength))
                {
                    return new string(userName);
                }
                error = Marshal.GetLastWin32Error();
            }

            throw WinIOError(error);
        }
    }
}
