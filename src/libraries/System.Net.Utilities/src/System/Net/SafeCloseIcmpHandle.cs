// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    // SafeHandle to wrap handles created by IcmpCreateFile or Icmp6CreateFile
    // from iphlpapi.dll. These handles must be closed by IcmpCloseHandle.
    internal sealed class SafeCloseIcmpHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCloseIcmpHandle() : base(true)
        {
        }

        override protected bool ReleaseHandle()
        {
            return Interop.IpHlpApi.IcmpCloseHandle(handle);
        }
    }
}

