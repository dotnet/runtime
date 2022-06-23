// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Quic;

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

        internal static unsafe QuicAddr ToQuicAddr(this IPEndPoint iPEndPoint)
        {
            // TODO: is the layout same for SocketAddress.Buffer and QuicAddr on all platforms?
            QuicAddr result = default;
            Span<byte> rawAddress = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref result, 1));

            Internals.SocketAddress address = IPEndPointExtensions.Serialize(iPEndPoint);
            Debug.Assert(address.Size <= rawAddress.Length);

            address.Buffer.AsSpan(0, address.Size).CopyTo(rawAddress);
            return result;
        }
    }
}
