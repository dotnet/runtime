// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicLogHelper
    {
        internal static string GetLogId(SafeMsQuicStreamHandle handle)
        {
            return $"[strm][0x{GetIntPtrHex(handle)}]";
        }

        internal static string GetLogId(SafeMsQuicConnectionHandle handle)
        {
            return $"[conn][0x{GetIntPtrHex(handle)}]";
        }

        private static string GetIntPtrHex(SafeHandle handle)
        {
            return handle.DangerousGetHandle().ToString("X11");
        }
    }
}
