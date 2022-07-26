// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic;

internal static class MsQuicHelpers
{
    internal static bool TryParse(this EndPoint endPoint, out string? host, out IPAddress? address, out int port)
    {
        if (endPoint is DnsEndPoint dnsEndPoint)
        {
            host = IPAddress.TryParse(dnsEndPoint.Host, out address) ? null : dnsEndPoint.Host;
            port = dnsEndPoint.Port;
            return true;
        }

        if (endPoint is IPEndPoint ipEndPoint)
        {
            host = null;
            address = ipEndPoint.Address;
            port = ipEndPoint.Port;
            return true;
        }

        host = default;
        address = default;
        port = default;
        return false;
    }

    internal static unsafe IPEndPoint ToIPEndPoint(this ref QuicAddr quicAddress, AddressFamily? addressFamilyOverride = null)
    {
        // MsQuic always uses storage size as if IPv6 was used
        Span<byte> addressBytes = new Span<byte>((byte*)Unsafe.AsPointer(ref quicAddress), Internals.SocketAddress.IPv6AddressSize);
        return new Internals.SocketAddress(addressFamilyOverride ?? SocketAddressPal.GetAddressFamily(addressBytes), addressBytes).GetIPEndPoint();
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

    internal static unsafe T GetMsQuicParameter<T>(MsQuicSafeHandle handle, uint parameter)
        where T : unmanaged
    {
        T value;
        uint length = (uint)sizeof(T);

        int status = MsQuicApi.Api.ApiTable->GetParam(
            handle.QuicHandle,
            parameter,
            &length,
            (byte*)&value);

        if (StatusFailed(status))
        {
            throw ThrowHelper.GetExceptionForMsQuicStatus(status, $"GetParam({handle}, {parameter}) failed");
        }

        return value;
    }

    internal static unsafe void SetMsQuicParameter<T>(MsQuicSafeHandle handle, uint parameter, T value)
        where T : unmanaged
    {
        int status = MsQuicApi.Api.ApiTable->SetParam(
            handle.QuicHandle,
            parameter,
            (uint)sizeof(T),
            (byte*)&value);

        if (StatusFailed(status))
        {
            throw ThrowHelper.GetExceptionForMsQuicStatus(status, $"SetParam({handle}, {parameter}) failed");
        }
    }
}
