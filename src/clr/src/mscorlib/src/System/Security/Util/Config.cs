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
#if FEATURE_SERIALIZATION
    using System.Runtime.Serialization.Formatters.Binary;
#endif // FEATURE_SERIALIZATION
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

        [System.Security.SecurityCritical]  // auto-generated
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
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                GetFileLocales();
                return m_machineConfig;
            }
        }

        internal static string UserDirectory
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                GetFileLocales();
                return m_userConfig;
            }
        }

#if FEATURE_CAS_POLICY
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        internal static extern int SaveDataByte(string path, [In] byte[] data, int length);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        internal static extern bool RecoverData(ConfigId id);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        internal static extern void SetQuickCache(ConfigId id, QuickCacheEntryType quickCacheFlags);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern bool GetCacheEntry(ConfigId id, int numKey, [In] byte[] key, int keyLength, ObjectHandleOnStack retData);

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool GetCacheEntry(ConfigId id, int numKey, byte[] key, out byte[] data)
        {
            byte[] retData = null;
            bool ret = GetCacheEntry(id, numKey, key, key.Length, JitHelpers.GetObjectHandleOnStack(ref retData));

            data = retData;
            return ret;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void AddCacheEntry(ConfigId id, int numKey, [In] byte[] key, int keyLength, byte[] data, int dataLength);

        [System.Security.SecurityCritical]  // auto-generated
        internal static void AddCacheEntry(ConfigId id, int numKey, byte[] key, byte[] data)
        {
            AddCacheEntry(id, numKey, key, key.Length, data, data.Length);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        internal static extern void ResetCacheData(ConfigId id);
#endif

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void GetMachineDirectory(StringHandleOnStack retDirectory);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void GetUserDirectory(StringHandleOnStack retDirectory);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        internal static extern bool WriteToEventLog(string message);
    }
}
