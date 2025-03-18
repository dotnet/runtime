// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Interop.Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool OpenThreadToken(
            IntPtr ThreadHandle,
            TokenAccessLevels dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bOpenAsSelf,
            out SafeAccessTokenHandle phThreadToken);

        internal static bool OpenThreadToken(TokenAccessLevels desiredAccess, WinSecurityContext openAs, out SafeAccessTokenHandle tokenHandle)
        {
            bool openAsSelf = true;
            if (openAs == WinSecurityContext.Thread)
                openAsSelf = false;

            if (OpenThreadToken(Kernel32.GetCurrentThread(), desiredAccess, openAsSelf, out tokenHandle))
                return true;

            if (openAs == WinSecurityContext.Both)
            {
                openAsSelf = false;
                tokenHandle.Dispose();
                if (OpenThreadToken(Kernel32.GetCurrentThread(), desiredAccess, openAsSelf, out tokenHandle))
                    return true;
            }

            return false;
        }
    }
}
