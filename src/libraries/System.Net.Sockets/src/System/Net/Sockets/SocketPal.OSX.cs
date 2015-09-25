// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Net.Sockets
{
    internal static partial class SocketPal
    {
        public const bool SupportsMultipleConnectAttempts = false;

        public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
        {
            // NOTE: OS X does not support receiving IPv4 packet information for a dual-stack IPv6 socket. Instead,
            //       this information is extracted from IPv6 packet information when possible. As a result, this call
            //       is a no-op.
        }
    }
}
