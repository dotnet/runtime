// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class QuicExceptionHelpers
    {
        internal static void ThrowIfFailed(uint status, string? message = null, Exception? innerException = null)
        {
            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                throw CreateExceptionForHResult(status, message, innerException);
            }
        }

        internal static Exception CreateExceptionForHResult(uint status, string? message = null, Exception? innerException = null)
        {
            return new QuicException($"{message} Error Code: {MsQuicStatusCodes.GetError(status)}", innerException, MapMsQuicStatusToHResult(status));
        }

        internal static int MapMsQuicStatusToHResult(uint status)
        {
            switch (status)
            {
                case MsQuicStatusCodes.ConnectionRefused:
                    return (int)SocketError.ConnectionRefused;  // 0x8007274D - WSAECONNREFUSED
                case MsQuicStatusCodes.ConnectionTimeout:
                    return (int)SocketError.TimedOut;           // 0x8007274C - WSAETIMEDOUT
                case MsQuicStatusCodes.HostUnreachable:
                    return (int)SocketError.HostUnreachable;
                default:
                    return 0;
            }
        }
    }
}
