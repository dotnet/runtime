// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicParameterHelpers
    {
        internal static unsafe IPEndPoint GetIPEndPointParam(MsQuicApi api, MsQuicSafeHandle nativeObject, uint param, AddressFamily? addressFamilyOverride = null)
        {
            // MsQuic always uses storage size as if IPv6 was used
            uint valueLen = (uint)Internals.SocketAddress.IPv6AddressSize;
            Span<byte> address = stackalloc byte[Internals.SocketAddress.IPv6AddressSize];

            fixed (byte* paddress = &MemoryMarshal.GetReference(address))
            {
                ThrowIfFailure(api.ApiTable->GetParam(
                    nativeObject.QuicHandle,
                    param,
                    &valueLen,
                    paddress), "GetIPEndPointParam failed.");
            }

            address = address.Slice(0, (int)valueLen);

            return new Internals.SocketAddress(addressFamilyOverride ?? SocketAddressPal.GetAddressFamily(address), address).GetIPEndPoint();
        }

        internal static unsafe void SetIPEndPointParam(MsQuicApi api, MsQuicSafeHandle nativeObject, uint param, IPEndPoint value)
        {
            Internals.SocketAddress socketAddress = IPEndPointExtensions.Serialize(value);

            // MsQuic always reads same amount of memory as if IPv6 was used, so we can't pass pointer to socketAddress.Buffer directly
            Span<byte> address = stackalloc byte[Internals.SocketAddress.IPv6AddressSize];
            socketAddress.Buffer.AsSpan(0, socketAddress.Size).CopyTo(address);
            address.Slice(socketAddress.Size).Clear();

            fixed (byte* paddress = &MemoryMarshal.GetReference(address))
            {
                ThrowIfFailure(api.ApiTable->SetParam(
                    nativeObject.QuicHandle,
                    param,
                    (uint)address.Length,
                    paddress), "Could not set IPEndPoint");
            }
        }

        internal static unsafe ushort GetUShortParam(MsQuicApi api, MsQuicSafeHandle nativeObject, uint param)
        {
            ushort value;
            uint valueLen = (uint)sizeof(ushort);

            ThrowIfFailure(api.ApiTable->GetParam(
                nativeObject.QuicHandle,
                param,
                &valueLen,
                (byte*)&value), "GetUShortParam failed");
            Debug.Assert(valueLen == sizeof(ushort));

            return value;
        }

        internal static unsafe void SetUShortParam(MsQuicApi api, MsQuicSafeHandle nativeObject, uint param, ushort value)
        {
            ThrowIfFailure(api.ApiTable->SetParam(
                nativeObject.QuicHandle,
                param,
                sizeof(ushort),
                (byte*)&value), "Could not set ushort");
        }

        internal static unsafe ulong GetULongParam(MsQuicApi api, MsQuicSafeHandle nativeObject, uint param)
        {
            ulong value;
            uint valueLen = (uint)sizeof(ulong);

            ThrowIfFailure(api.ApiTable->GetParam(
                nativeObject.QuicHandle,
                param,
                &valueLen,
                (byte*)&value), "GetULongParam failed");
            Debug.Assert(valueLen == sizeof(ulong));

            return value;
        }

        internal static unsafe void SetULongParam(MsQuicApi api, MsQuicSafeHandle nativeObject, uint param, ulong value)
        {
            ThrowIfFailure(api.ApiTable->SetParam(
                nativeObject.QuicHandle,
                param,
                sizeof(ulong),
                (byte*)&value), "Could not set ulong");
        }
    }
}
