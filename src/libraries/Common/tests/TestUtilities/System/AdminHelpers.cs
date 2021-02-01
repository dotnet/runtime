// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System
{
    public static class AdminHelpers
    {
        /// <summary>
        /// Runs the given command as sudo (for Unix).
        /// </summary>
        /// <param name="commandLine">The command line to run as sudo</param>
        /// <returns> Returns the process exit code (0 typically means it is successful)</returns>
        public static int RunAsSudo(string commandLine)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "sudo",
                Arguments = commandLine
            };

            using (Process process = Process.Start(startInfo))
            {
                Assert.True(process.WaitForExit(30000));
                return process.ExitCode;
            }
        }

        public static unsafe bool IsProcessElevated()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                uint userId = Interop.Sys.GetEUid();
                return(userId == 0);
            }

            SafeAccessTokenHandle token;
            if (!Interop.Advapi32.OpenProcessToken(Interop.Kernel32.GetCurrentProcess(), TokenAccessLevels.Read, out token))
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
