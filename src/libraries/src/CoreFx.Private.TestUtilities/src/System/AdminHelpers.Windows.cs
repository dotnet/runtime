// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace System
{
    public static partial class AdminHelpers
    {
        /// <summary>
        /// Returns true if the current process is elevated (in Windows).
        /// </summary>
        public unsafe static bool IsProcessElevated()
        {
            IntPtr processHandle = Interop.Kernel32.GetCurrentProcess();
            SafeAccessTokenHandle token;
            if (!Interop.Advapi32.OpenProcessToken(processHandle, TokenAccessLevels.Read, out token))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Open process token failed");
            }

            using (token)
            {
                Interop.Advapi32.TOKEN_ELEVATION elevation = new Interop.Advapi32.TOKEN_ELEVATION();
                uint ignore;
                if (!Interop.Advapi32.GetTokenInformation(
                    token,
                    Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenElevation,
                    &elevation,
                    (uint)sizeof(Interop.Advapi32.TOKEN_ELEVATION),
                    out ignore))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Get token information failed");
                }
                return elevation.TokenIsElevated != Interop.BOOL.FALSE;
            }
        }

    }
}
