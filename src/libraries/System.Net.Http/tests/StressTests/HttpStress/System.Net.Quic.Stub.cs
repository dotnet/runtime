// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic;

internal static class ThrowHelper
{
    internal static Exception GetExceptionForMsQuicStatus(int status, string? message = null)
        => new QuicException(QuicError.InternalError, null, $"{message} (0x{status:x})");

    internal static void ThrowIfMsQuicError(int status, string? message = null)
    {
        if (StatusFailed(status))
        {
            throw GetExceptionForMsQuicStatus(status, message);
        }
    }
}

internal static partial class Interop
{
    internal static partial class Libraries
    {
#if WINDOWS
        internal const string MsQuic = "msquic.dll";
#elif OSX
        internal const string MsQuic = "libmsquic.dylib";
#else
        internal const string MsQuic = "libmsquic.so";
#endif
    }
}
