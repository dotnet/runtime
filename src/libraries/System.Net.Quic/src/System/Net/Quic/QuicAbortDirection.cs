// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic
{
    [Flags]
    public enum QuicAbortDirection
    {
        /// <summary>
        /// Aborts the read direction of the stream.
        /// </summary>
        Read = 1,

        /// <summary>
        /// Aborts the write direction of the stream.
        /// </summary>
        Write = 2,

        /// <summary>
        /// Aborts both the read and write direction of the stream.
        /// </summary>
        Both = Read | Write,

        /// <summary>
        /// Aborts both the read and write direction of the stream, without waiting for the peer to acknowledge the shutdown.
        /// </summary>
        Immediate = Both | 4
    }
}
