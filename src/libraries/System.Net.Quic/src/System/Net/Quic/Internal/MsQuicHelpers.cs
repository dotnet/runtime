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

    internal static unsafe IPEndPoint QuicAddrToIPEndPoint(QuicAddr* quicAddress, AddressFamily? addressFamilyOverride = null)
    {
        // MsQuic always uses storage size as if IPv6 was used
        Span<byte> addressBytes = new Span<byte>(quicAddress, SocketAddressPal.IPv6AddressSize);
        if (addressFamilyOverride != null)
        {
            SocketAddressPal.SetAddressFamily(addressBytes, (AddressFamily)addressFamilyOverride!);
        }
        return IPEndPointExtensions.CreateIPEndPoint(addressBytes);
    }

    internal static unsafe QuicAddr ToQuicAddr(this IPEndPoint ipEndPoint)
    {
        QuicAddr result = default;
        Span<byte> rawAddress = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref result, 1));
        ipEndPoint.Serialize(rawAddress);
        return result;
    }

    internal static unsafe T GetMsQuicParameter<T>(MsQuicSafeHandle handle, uint parameter)
        where T : unmanaged
    {
        T value;
        GetMsQuicParameter(handle, parameter, (uint)sizeof(T), (byte*)&value);
        return value;
    }
    internal static unsafe void GetMsQuicParameter(MsQuicSafeHandle? handle, uint parameter, uint length, byte* value)
    {
        int status = MsQuicApi.Api.GetParam(
            handle,
            parameter,
            &length,
            value);

        if (StatusFailed(status))
        {
            ThrowHelper.ThrowMsQuicException(status, $"GetParam({handle}, {parameter}) failed");
        }
    }

    internal static unsafe void SetMsQuicParameter<T>(MsQuicSafeHandle handle, uint parameter, T value)
        where T : unmanaged
    {
        SetMsQuicParameter(handle, parameter, (uint)sizeof(T), (byte*)&value);
    }
    internal static unsafe void SetMsQuicParameter(MsQuicSafeHandle handle, uint parameter, uint length, byte* value)
    {
        int status = MsQuicApi.Api.SetParam(
            handle,
            parameter,
            length,
            value);

        if (StatusFailed(status))
        {
            ThrowHelper.ThrowMsQuicException(status, $"SetParam({handle}, {parameter}) failed");
        }
    }
}
