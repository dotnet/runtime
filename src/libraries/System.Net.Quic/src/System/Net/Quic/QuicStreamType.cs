// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic;

/// <summary>
/// Represents type of the stream.
/// </summary>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc9000.html#name-stream-types-and-identifier" />
public enum QuicStreamType
{
    /// <summary>
    /// Write-only for the peer that opened the stream. Read-only for the peer that accepted the stream.
    /// </summary>
    Unidirectional,

    /// <summary>
    /// For both peers, read and write capable.
    /// </summary>
    Bidirectional
}
