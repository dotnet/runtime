// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.ProcessThread_L1, SetLastError = true)]
        private static extern bool OpenThreadToken(IntPtr ThreadHandle, TokenAccessLevels dwDesiredAccess, bool bOpenAsSelf, out SafeAccessTokenHandle phThreadToken);

        internal static bool OpenThreadToken(TokenAccessLevels desiredAccess, WinSecurityContext openAs, out SafeAccessTokenHandle tokenHandle)
        {
            bool openAsSelf = true;
            if (openAs == WinSecurityContext.Thread)
                openAsSelf = false;

            if (OpenThreadToken(GetCurrentThread(), desiredAccess, openAsSelf, out tokenHandle))
                return true;

            if (openAs == WinSecurityContext.Both)
            {
                openAsSelf = false;
                if (OpenThreadToken(GetCurrentThread(), desiredAccess, openAsSelf, out tokenHandle))
                    return true;
            }

            return false;
        }
    }
}
