// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicParameterHelpers
    {
        internal static unsafe Internals.SocketAddress GetSocketAddressParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param)
        {
            // MsQuic always uses storage size as if IPv6
            uint valueLen = (uint)Internals.SocketAddress.IPv6AddressSize;
            Span<byte> address = stackalloc byte[Internals.SocketAddress.IPv6AddressSize];

            fixed (byte* paddress = &MemoryMarshal.GetReference(address))
            {
                uint status = api.GetParamDelegate(nativeObject, level, param, ref valueLen, paddress);
                QuicExceptionHelpers.ThrowIfFailed(status, "GetINETParam failed.");
            }

            address = address.Slice(0, (int)valueLen);

            return new Internals.SocketAddress(SocketAddressPal.GetAddressFamily(address), address);
        }

        internal static unsafe void SetSocketAddressParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param, Internals.SocketAddress value)
        {
            // MsQuic always uses storage size as if IPv6
            Span<byte> address = stackalloc byte[Internals.SocketAddress.IPv6AddressSize];
            value.Buffer.AsSpan(0, value.Size).CopyTo(address);
            address.Slice(value.Size).Clear();

            fixed (byte* paddress = &MemoryMarshal.GetReference(address))
            {
                QuicExceptionHelpers.ThrowIfFailed(
                    api.SetParamDelegate(nativeObject, level, param, (uint)address.Length, paddress),
                    "Could not set SocketAddress");
            }
        }

        internal static unsafe ushort GetUShortParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param)
        {
            ushort value;
            uint valueLen = (uint)sizeof(ushort);

            uint status = api.GetParamDelegate(nativeObject, level, param, ref valueLen, (byte*)&value);
            QuicExceptionHelpers.ThrowIfFailed(status, "GetUShortParam failed.");
            Debug.Assert(valueLen == sizeof(ushort));

            return value;
        }

        internal static unsafe void SetUShortParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param, ushort value)
        {
            QuicExceptionHelpers.ThrowIfFailed(
                api.SetParamDelegate(nativeObject, level, param, sizeof(ushort), (byte*)&value),
                "Could not set ushort.");
        }

        internal static unsafe ulong GetULongParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param)
        {
            ulong value;
            uint valueLen = (uint)sizeof(ulong);

            uint status = api.GetParamDelegate(nativeObject, level, param, ref valueLen, (byte*)&value);
            QuicExceptionHelpers.ThrowIfFailed(status, "GetULongParam failed.");
            Debug.Assert(valueLen == sizeof(ulong));

            return value;
        }

        internal static unsafe void SetULongParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param, ulong value)
        {
            QuicExceptionHelpers.ThrowIfFailed(
                api.SetParamDelegate(nativeObject, level, param, sizeof(ulong), (byte*)&value),
                "Could not set ulong.");
        }
    }
}
