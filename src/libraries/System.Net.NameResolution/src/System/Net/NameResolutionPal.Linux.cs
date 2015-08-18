// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static partial class NameResolutionPal
    {
        private static unsafe bool TryGetHostByName(string hostName, byte* buffer, int bufferSize, Interop.libc.hostent* hostent, Interop.libc.hostent** result)
        {
            int errno;
            int err = Interop.libc.gethostbyname_r(hostName, hostent, buffer, (IntPtr)bufferSize, result, &errno);
            switch (Interop.Sys.ConvertErrorPlatformToPal(err))
            {
                case 0:
                    return true;
                case Interop.Error.ERANGE:
                    return false;
                default:
                    throw new InternalSocketException(GetSocketErrorForErrno(errno), errno);
            }
        }

        private static unsafe bool TryGetHostByAddr(Interop.libc.in_addr address, byte* buffer, int bufferSize, Interop.libc.hostent* hostent, Interop.libc.hostent** result)
        {
            int errno;
            int err = Interop.libc.gethostbyaddr_r(&address, (uint)sizeof(Interop.libc.in_addr), Interop.libc.AF_INET, hostent, buffer, (IntPtr)bufferSize, result, &errno);
            switch (Interop.Sys.ConvertErrorPlatformToPal(err))
            {
                case 0:
                    return true;
                case Interop.Error.ERANGE:
                    return false;
                default:
                    throw new InternalSocketException(GetSocketErrorForErrno(errno), errno);
            }
        }

        public static unsafe IPHostEntry GetHostByName(string hostName)
        {
            int bufferSize = 512;
            byte* stackBuffer = stackalloc byte[bufferSize];

            var hostent = default(Interop.libc.hostent);
            var result = (Interop.libc.hostent*)null;
            if (TryGetHostByName(hostName, stackBuffer, bufferSize, &hostent, &result))
            {
                return CreateHostEntry(&hostent);
            }

            for (; ;)
            {
                bufferSize *= 2;
                fixed (byte* heapBuffer = new byte[bufferSize])
                {
                    if (TryGetHostByName(hostName, heapBuffer, bufferSize, &hostent, &result))
                    {
                        return CreateHostEntry(&hostent);
                    }
                }
            }
        }

        public static unsafe IPHostEntry GetHostByAddr(IPAddress addr)
        {
            // TODO #2891: Optimize this (or decide if this legacy code can be removed):
            byte[] addressBytes = addr.GetAddressBytes();
            var address = new Interop.libc.in_addr { s_addr = unchecked((uint)BitConverter.ToInt32(addressBytes, 0)) };

            int bufferSize = 512;
            byte* stackBuffer = stackalloc byte[bufferSize];

            var hostent = default(Interop.libc.hostent);
            var result = (Interop.libc.hostent*)null;
            if (TryGetHostByAddr(address, stackBuffer, bufferSize, &hostent, &result))
            {
                return CreateHostEntry(&hostent);
            }

            for (; ;)
            {
                bufferSize *= 2;
                fixed (byte* heapBuffer = new byte[bufferSize])
                {
                    if (TryGetHostByAddr(address, heapBuffer, bufferSize, &hostent, &result))
                    {
                        return CreateHostEntry(&hostent);
                    }
                }
            }
        }
    }
}
