// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static partial class NameResolutionPal
    {
        public static unsafe IPHostEntry GetHostByName(string hostName)
        {
            Interop.libc.hostent* hostent = Interop.libc.gethostbyname(hostName);
            if (hostent == null)
            {
                int errno = Marshal.GetLastWin32Error();
                throw new InternalSocketException(GetSocketErrorForErrno(errno), errno);
            }

            return CreateHostEntry(hostent);
        }

        public static unsafe IPHostEntry GetHostByAddr(IPAddress addr)
        {
            // TODO #2891: Optimize this (or decide if this legacy code can be removed):
            byte[] addressBytes = addr.GetAddressBytes();
            var address = new Interop.libc.in_addr { s_addr = unchecked((uint)BitConverter.ToInt32(addressBytes, 0)) };

            Interop.libc.hostent* hostent = Interop.libc.gethostbyaddr(&address, (uint)sizeof(Interop.libc.in_addr), Interop.libc.AF_INET);
            if (hostent == null)
            {
                int errno = Marshal.GetLastWin32Error();
                throw new InternalSocketException(GetSocketErrorForErrno(errno), errno);
            }

            return CreateHostEntry(hostent);
        }
    }
}
