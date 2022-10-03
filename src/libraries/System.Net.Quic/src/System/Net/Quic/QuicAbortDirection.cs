// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic;

/// <summary>
/// Specifies direction of the <see cref="QuicStream"/> which is to be <see cref="QuicStream.Abort(QuicAbortDirection, long)">aborted</see>.
/// </summary>
[Flags]
public enum QuicAbortDirection
{
    /// <summary>
    /// Abort read side of the stream.
    /// </summary>
    Read = 1,
    /// <summary>
    /// Abort write side of the stream.
    /// </summary>
    Write = 2,
    /// <summary>
    /// Abort both sides of the stream, i.e.: <see cref="Read"/> and <see cref="Write"/>) at the same time.
    /// </summary>
    Both = Read | Write
}
