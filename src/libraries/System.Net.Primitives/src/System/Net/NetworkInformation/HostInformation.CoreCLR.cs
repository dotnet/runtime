// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace System.Net.NetworkInformation
{
    internal class HostInformation
    {
        private static Interop.IpHlpApi.FIXED_INFO s_fixedInfo;
        private static bool s_fixedInfoInitialized = false;

        // Changing these requires a reboot, so they're safe to cache.
        private static volatile string s_hostName = null;
        private static volatile string s_domainName = null;

        private static object s_syncObject = new object();

        internal static Interop.IpHlpApi.FIXED_INFO GetFixedInfo()
        {
            uint size = 0;
            SafeLocalAllocHandle buffer = null;
            Interop.IpHlpApi.FIXED_INFO fixedInfo = new Interop.IpHlpApi.FIXED_INFO();

            // First we need to get the size of the buffer
            uint result = Interop.IpHlpApi.GetNetworkParams(SafeLocalAllocHandle.InvalidHandle, ref size);

            while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
            {
                try
                {
                    // Now we allocate the buffer and read the network parameters.
                    buffer = Interop.mincore_obsolete.LocalAlloc(0, (UIntPtr)size);
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
                finally
                {
                    if (buffer != null)
                    {
                        buffer.Dispose();
                    }
                }
            }

            // If the result include there being no information, we'll still throw
            if (result != Interop.IpHlpApi.ERROR_SUCCESS)
            {
                throw new NetworkInformationException((int)result);
            }
            return fixedInfo;
        }


        internal static Interop.IpHlpApi.FIXED_INFO FixedInfo
        {
            get
            {
                if (!s_fixedInfoInitialized)
                {
                    lock (s_syncObject)
                    {
                        if (!s_fixedInfoInitialized)
                        {
                            s_fixedInfo = GetFixedInfo();
                            s_fixedInfoInitialized = true;
                        }
                    }
                }
                return s_fixedInfo;
            }
        }

        // Specifies the host name for the local computer.
        internal static string HostName
        {
            get
            {
                if (s_hostName == null)
                {
                    lock (s_syncObject)
                    {
                        if (s_hostName == null)
                        {
                            s_hostName = FixedInfo.hostName;
                            s_domainName = FixedInfo.domainName;
                        }
                    }
                }
                return s_hostName;
            }
        }

        // Specifies the domain in which the local computer is registered.
        internal static string DomainName
        {
            get
            {
                if (s_domainName == null)
                {
                    lock (s_syncObject)
                    {
                        if (s_domainName == null)
                        {
                            s_hostName = FixedInfo.hostName;
                            s_domainName = FixedInfo.domainName;
                        }
                    }
                }
                return s_domainName;
            }
        }
    }
}
