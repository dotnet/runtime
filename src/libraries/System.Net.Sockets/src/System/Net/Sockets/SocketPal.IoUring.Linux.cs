// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    internal static partial class SocketPal
    {
        /// <summary>Extracts <see cref="IPPacketInformation"/> from a completed io_uring recvmsg message header.</summary>
        internal static unsafe IPPacketInformation GetIoUringIPPacketInformation(Interop.Sys.MessageHeader* messageHeader, bool isIPv4, bool isIPv6) =>
            GetIPPacketInformation(messageHeader, isIPv4, isIPv6);
    }
}
