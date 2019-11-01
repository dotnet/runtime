// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32, SetLastError = true)]
        private static extern bool OpenThreadToken(IntPtr ThreadHandle, TokenAccessLevels dwDesiredAccess, bool bOpenAsSelf, out SafeAccessTokenHandle phThreadToken);

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
                if (OpenThreadToken(Kernel32.GetCurrentThread(), desiredAccess, openAsSelf, out tokenHandle))
                    return true;
            }

            return false;
        }
    }
}
