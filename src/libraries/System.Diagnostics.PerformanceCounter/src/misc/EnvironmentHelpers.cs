// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Security;
using System.Security.Principal;

namespace System
{
    internal static class EnvironmentHelpers
    {
        private static volatile bool s_isAppContainerProcess;
        private static volatile bool s_isAppContainerProcessInitialized;

        public static bool IsAppContainerProcess
        {
            get
            {
                if (!s_isAppContainerProcessInitialized)
                {
                    if (Environment.OSVersion.Version.Major < 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor <= 1))
                    {
                        // Windows 7 or older.
                        s_isAppContainerProcess = false;
                    }
                    else
                    {
                        s_isAppContainerProcess = HasAppContainerToken();
                    }

                    s_isAppContainerProcessInitialized = true;
                }

                return s_isAppContainerProcess;
            }
        }

        private static unsafe bool HasAppContainerToken()
        {
            int* dwIsAppContainerPtr = stackalloc int[1];
            uint dwLength = 0;

            using (WindowsIdentity wi = WindowsIdentity.GetCurrent(TokenAccessLevels.Query))
            {
                if (!Interop.Advapi32.GetTokenInformation(wi.Token, (uint)Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenIsAppContainer, new IntPtr(dwIsAppContainerPtr), sizeof(int), out dwLength))
                {
                    throw new Win32Exception();
                }
            }

            return (*dwIsAppContainerPtr != 0);
        }
    }
}
