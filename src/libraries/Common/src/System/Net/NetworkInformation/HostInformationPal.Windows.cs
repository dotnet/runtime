// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Win32.SafeHandles;

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
            EnsureFixedInfo();
            return s_fixedInfo.hostName;
        }

        public static string GetDomainName()
        {
            EnsureFixedInfo();
            return s_fixedInfo.domainName;
        }

        public static Interop.IpHlpApi.FIXED_INFO GetFixedInfo()
        {
            uint size = 0;
            SafeLocalAllocHandle buffer = null;
            Interop.IpHlpApi.FIXED_INFO fixedInfo = new Interop.IpHlpApi.FIXED_INFO();

            // First we need to get the size of the buffer
            uint result = Interop.IpHlpApi.GetNetworkParams(SafeLocalAllocHandle.InvalidHandle, ref size);

            while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
            {
                // Now we allocate the buffer and read the network parameters.
                using (buffer = Interop.Kernel32.LocalAlloc(0, (UIntPtr)size))
                {
                    if (buffer.IsInvalid)
                    {
                        throw new OutOfMemoryException();
                    }

                    result = Interop.IpHlpApi.GetNetworkParams(buffer, ref size);
                    if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                    {
                        fixedInfo = Marshal.PtrToStructure<Interop.IpHlpApi.FIXED_INFO>(buffer.DangerousGetHandle());
                    }
                }
            }

            // If the result include there being no information, we'll still throw
            if (result != Interop.IpHlpApi.ERROR_SUCCESS)
            {
                throw new Win32Exception((int)result);
            }
            
            return fixedInfo;
        }

        private static void EnsureFixedInfo()
        {
            if (!Volatile.Read(ref s_fixedInfoInitialized))
            {
                lock (s_syncObject)
                {
                    if (!s_fixedInfoInitialized)
                    {
                        s_fixedInfo = GetFixedInfo();
                        Volatile.Write(ref s_fixedInfoInitialized, true);
                    }
                }
            }
        }
    }
}
