// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.InteropServices;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicAddressHelpers
    {
        internal static unsafe IPEndPoint INetToIPEndPoint(IntPtr pInetAddress)
        {
            // MsQuic always uses storage size as if IPv6 was used
            Span<byte> addressBytes = new Span<byte>((byte*)pInetAddress, Internals.SocketAddress.IPv6AddressSize);
            return new Internals.SocketAddress(SocketAddressPal.GetAddressFamily(addressBytes), addressBytes).GetIPEndPoint();
        }
    }
}
