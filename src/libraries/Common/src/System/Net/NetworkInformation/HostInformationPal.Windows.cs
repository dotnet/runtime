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
        private static Interop.IpHlpApi.FIXED_INFO s_fixedInfo;
        private static bool s_fixedInfoInitialized;
        private static object s_syncObject = new object();

        public static string GetHostName()
        {
            return FixedInfo.hostName;
        }

        public static string GetDomainName()
        {
            return FixedInfo.domainName;
        }

        private static unsafe Interop.IpHlpApi.FIXED_INFO GetFixedInfo()
        {
            uint size = 0;
            Interop.IpHlpApi.FIXED_INFO fixedInfo = default;

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
                        fixedInfo = Marshal.PtrToStructure<Interop.IpHlpApi.FIXED_INFO>(buffer);
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

            return fixedInfo;
        }

        public static ref readonly Interop.IpHlpApi.FIXED_INFO FixedInfo
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref s_fixedInfo, ref s_fixedInfoInitialized, ref s_syncObject, () => GetFixedInfo());
                return ref s_fixedInfo;
            }
        }
    }
}
