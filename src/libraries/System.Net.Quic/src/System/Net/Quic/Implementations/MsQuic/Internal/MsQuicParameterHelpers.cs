// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicParameterHelpers
    {
        internal static unsafe SOCKADDR_INET GetINetParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param)
        {
            byte* ptr = stackalloc byte[sizeof(SOCKADDR_INET)];
            QuicBuffer buffer = new QuicBuffer
            {
                Length = (uint)sizeof(SOCKADDR_INET),
                Buffer = ptr
            };

            MsQuicStatusException.ThrowIfFailed(api.UnsafeGetParam(nativeObject, level, param, ref buffer));

            return *(SOCKADDR_INET*)ptr;
        }

        internal static unsafe void SetUshortParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param, ushort value)
        {
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = sizeof(ushort),
                Buffer = (byte*)&value
            };
            MsQuicStatusException.ThrowIfFailed(api.UnsafeSetParam(nativeObject, level, param, buffer));
        }

        internal static unsafe void SetULongParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param, ulong value)
        {
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = sizeof(ulong),
                Buffer = (byte*)&value
            };
            MsQuicStatusException.ThrowIfFailed(api.UnsafeGetParam(nativeObject, level, param, ref buffer));
        }

        internal static unsafe void SetSecurityConfig(MsQuicApi api, IntPtr nativeObject, uint level, uint param, IntPtr value)
        {
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = (uint)sizeof(void*),
                Buffer = (byte*)&value
            };
            MsQuicStatusException.ThrowIfFailed(api.UnsafeSetParam(nativeObject, level, param, buffer));
        }

        internal static unsafe ulong GetULongParam(MsQuicApi api, IntPtr nativeObject, uint level, uint param)
        {
            byte* ptr = stackalloc byte[sizeof(ulong)];
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = sizeof(ulong),
                Buffer = ptr
            };
            MsQuicStatusException.ThrowIfFailed(api.UnsafeGetParam(nativeObject, level, param, ref buffer));
            return *(ulong*)ptr;
        }
    }
}
