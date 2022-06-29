// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.MsQuic
{
    internal static class ThrowHelper
    {
        internal static QuicException GetConnectionAbortedException(long errorCode)
        {
            return errorCode switch
            {
                -1 => GetOperationAbortedException(), // Shutdown initiated by us.
                long err => new QuicException(QuicError.ConnectionAborted, SR.Format(SR.net_quic_connectionaborted, err), err, null) // Shutdown initiated by peer.
            };
        }

        internal static QuicException GetStreamAbortedException(long errorCode)
        {
            return errorCode switch
            {
                -1 => GetOperationAbortedException(), // Shutdown initiated by us.
                long err => new QuicException(QuicError.StreamAborted, SR.Format(SR.net_quic_streamaborted, err), err, null) // Shutdown initiated by peer.
            };
        }

        internal static QuicException GetOperationAbortedException(string? message = null)
        {
            return new QuicException(QuicError.OperationAborted, message ?? SR.net_quic_operationaborted, null, null);
        }
    }
}
