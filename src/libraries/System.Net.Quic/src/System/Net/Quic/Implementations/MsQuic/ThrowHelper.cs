// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.MsQuic
{
    internal static class ThrowHelper
    {
        internal static Exception GetConnectionAbortedException(long errorCode)
        {
            return errorCode switch
            {
                -1 => new QuicOperationAbortedException(), // Shutdown initiated by us.
                long err => new QuicConnectionAbortedException(err) // Shutdown initiated by peer.
            };
        }

        internal static Exception GetStreamAbortedException(long errorCode)
        {
            return errorCode switch
            {
                -1 => new QuicOperationAbortedException(), // Shutdown initiated by us.
                long err => new QuicStreamAbortedException(err) // Shutdown initiated by peer.
            };
        }
    }
}