// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicParameterHelpers
    {
        internal static unsafe SOCKADDR_INET GetINetParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param)
        {
            SOCKADDR_INET value;
            uint valueLen = (uint)sizeof(SOCKADDR_INET);

            uint status = api.GetParamDelegate(nativeObject, level, param, ref valueLen, (byte*)&value);
            QuicExceptionHelpers.ThrowIfFailed(status, "GetINETParam failed.");
            Debug.Assert(valueLen == sizeof(SOCKADDR_INET));

            return value;
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
