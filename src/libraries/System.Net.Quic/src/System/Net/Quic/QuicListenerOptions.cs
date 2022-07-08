// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic;

/// <summary>
/// Options to provide to the <see cref="QuicListener"/>.
/// </summary>
public sealed class QuicListenerOptions
{
    /// <summary>
    /// The endpoint to listen on.
    /// </summary>
    public required IPEndPoint ListenEndPoint { get; set; }

    /// <summary>
    /// List of application protocols which the listener will accept. At least one must be specified.
    /// </summary>
    public required List<SslApplicationProtocol> ApplicationProtocols { get; set; }

    /// <summary>
    /// Number of connections to be held without accepting any them, i.e. maximum size of the pending connection queue.
    /// </summary>
    public int ListenBacklog { get; set; }

    /// <summary>
    /// Selection callback to choose inbound connection options dynamically.
    /// </summary>
    public required Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> ConnectionOptionsCallback { get; set; }
}
