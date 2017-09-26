// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Security.Claims;
using System.Runtime.InteropServices;

namespace System
{
    internal static class EnvironmentHelpers
    {
        private static volatile bool s_IsAppContainerProcess;
        private static volatile bool s_IsAppContainerProcessInitalized;
        internal const int TokenIsAppContainer = 29;

        public static bool IsAppContainerProcess {
            get {
                if(!s_IsAppContainerProcessInitalized) {
                   if(Environment.OSVersion.Platform != PlatformID.Win32NT) {
                       s_IsAppContainerProcess = false;
                   } else if(Environment.OSVersion.Version.Major < 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor <= 1)) {
                       // Windows 7 or older.
                       s_IsAppContainerProcess = false;
                   } else {
                       s_IsAppContainerProcess = HasAppContainerToken();
                   }

                    s_IsAppContainerProcessInitalized = true;
                }

                return s_IsAppContainerProcess;
            }
        }

        [SecuritySafeCritical]
        private static unsafe bool HasAppContainerToken() {
            int* dwIsAppContainerPtr = stackalloc int[1];
            uint dwLength = 0;

            using (WindowsIdentity wi = WindowsIdentity.GetCurrent(TokenAccessLevels.Query)) {
                if (!GetTokenInformation(wi.Token, TokenIsAppContainer, new IntPtr(dwIsAppContainerPtr), sizeof(int), out dwLength)) {
                    throw new Win32Exception();
                }
            }

            return (*dwIsAppContainerPtr != 0);
        }

        [DllImport(ExternDll.Advapi32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern bool GetTokenInformation (
            [In]  IntPtr                TokenHandle,
            [In]  uint                  TokenInformationClass,
            [In]  IntPtr                TokenInformation,
            [In]  uint                  TokenInformationLength,
            [Out] out uint              ReturnLength);
    }
}
