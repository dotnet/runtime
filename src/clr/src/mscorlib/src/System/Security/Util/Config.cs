// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Util {
    using System;
    using System.Security.Util;
    using System.Security.Policy;
    using System.Security.Permissions;
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;

    // Duplicated in vm\COMSecurityConfig.h
[Serializable]
[Flags]
    internal enum QuickCacheEntryType
    {
        FullTrustZoneMyComputer = 0x1000000,
        FullTrustZoneIntranet = 0x2000000,
        FullTrustZoneInternet = 0x4000000,
        FullTrustZoneTrusted = 0x8000000,
        FullTrustZoneUntrusted = 0x10000000,
        FullTrustAll = 0x20000000,
    }

    internal static class Config {
        private static volatile string m_machineConfig;
        private static volatile string m_userConfig;

        private static void GetFileLocales()
        {
            if (m_machineConfig == null)
            {
                string machineConfig = null;
                GetMachineDirectory(JitHelpers.GetStringHandleOnStack(ref machineConfig));
                m_machineConfig = machineConfig;
            }
            if (m_userConfig == null)
            {
                string userConfig = null;
                GetUserDirectory(JitHelpers.GetStringHandleOnStack(ref userConfig));
                m_userConfig = userConfig;
        }
        }

        internal static string MachineDirectory
        {
            get
            {
                GetFileLocales();
                return m_machineConfig;
            }
        }

        internal static string UserDirectory
        {
            get
            {
                GetFileLocales();
                return m_userConfig;
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void GetMachineDirectory(StringHandleOnStack retDirectory);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void GetUserDirectory(StringHandleOnStack retDirectory);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        internal static extern bool WriteToEventLog(string message);
    }
}
