// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Net.Sockets
{
    internal static partial class SocketPal
    {
        public const bool SupportsMultipleConnectAttempts = true;

        static unsafe partial void PrimeForNextConnectAttempt(int fileDescriptor, int socketAddressLen)
        {
            // On Linux, a non-blocking socket that fails a connect() attempt needs to be kicked
            // with another connect to AF_UNSPEC before further connect() attempts will return
            // valid errors. Otherwise, further connect() attempts will return ECONNABORTED.
            
            var socketAddressBuffer = stackalloc byte[socketAddressLen];
            var sockAddr = (Interop.libc.sockaddr*)socketAddressBuffer;
            sockAddr->sa_family = Interop.libc.AF_UNSPEC;

            int err = Interop.libc.connect(fileDescriptor, sockAddr, (uint)socketAddressLen);
            Debug.Assert(err == 0, "TryCompleteConnect: failed to disassociate socket after failed connect()");
        }

        public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }
    }
}
