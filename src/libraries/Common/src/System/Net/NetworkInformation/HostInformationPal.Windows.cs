// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.NetworkInformation
{
    internal static class HostInformationPal
    {
        // Changing this information requires a reboot, so it's safe to cache.
        private static string? s_hostName;
        private static string? s_domainName;
        private static uint s_nodeType;
        private static string? s_scopeId;
        private static bool s_enableRouting;
        private static bool s_enableProxy;
        private static bool s_enableDns;

        private static volatile bool s_initialized;
        private static object s_syncObject = new object();

        public static string GetHostName()
        {
            EnsureInitialized();
            return s_hostName!;
        }

        public static string GetDomainName()
        {
            EnsureInitialized();
            return s_domainName!;
        }

        public static uint GetNodeType()
        {
            EnsureInitialized();
            return s_nodeType;
        }

        public static string GetScopeId()
        {
            EnsureInitialized();
            return s_scopeId!;
        }

        public static bool GetEnableRouting()
        {
            EnsureInitialized();
            return s_enableRouting;
        }

        public static bool GetEnableProxy()
        {
            EnsureInitialized();
            return s_enableProxy;
        }

        public static bool GetEnableDns()
        {
            EnsureInitialized();
            return s_enableDns;
        }

        private static void EnsureInitialized()
        {
            if (!s_initialized)
                Initialize();
        }

        private static unsafe void Initialize()
        {
            lock (s_syncObject)
            {
                if (s_initialized)
                    return;

                uint size = 0;

                // First we need to get the size of the buffer
                uint result = Interop.IpHlpApi.GetNetworkParams(IntPtr.Zero, &size);

                while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
                {
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetNetworkParams(buffer, &size);
                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            Interop.IpHlpApi.FIXED_INFO* pFixedInfo = (Interop.IpHlpApi.FIXED_INFO*)buffer;

                            s_hostName = pFixedInfo->HostName;
                            s_domainName = pFixedInfo->DomainName;

                            s_hostName = pFixedInfo->HostName;
                            s_domainName = pFixedInfo->DomainName;
                            s_nodeType = pFixedInfo->nodeType;
                            s_scopeId = pFixedInfo->ScopeId;
                            s_enableRouting = pFixedInfo->enableRouting != 0;
                            s_enableProxy = pFixedInfo->enableProxy != 0;
                            s_enableDns = pFixedInfo->enableDns != 0;

                            s_initialized = true;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                // If the result include there being no information, we'll still throw
                if (result != Interop.IpHlpApi.ERROR_SUCCESS)
                {
                    throw new Win32Exception((int)result);
                }
            }
        }
    }
}
