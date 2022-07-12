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
    public IPEndPoint ListenEndPoint { get; set; } = null!;

    /// <summary>
    /// List of application protocols which the listener will accept. At least one must be specified.
    /// </summary>
    public List<SslApplicationProtocol> ApplicationProtocols { get; set; } = null!;

    /// <summary>
    /// Number of connections to be held without accepting any them, i.e. maximum size of the pending connection queue.
    /// </summary>
    public int ListenBacklog { get; set; }

    /// <summary>
    /// Selection callback to choose inbound connection options dynamically.
    /// </summary>
    public Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> ConnectionOptionsCallback { get; set; } = null!;

    /// <summary>
    /// Validates the options and potentially sets platform specific defaults.
    /// </summary>
    /// <param name="argumentName">Name of the from the caller.</param>
    internal void Validate(string argumentName)
    {
        if (ListenEndPoint is null)
        {
            throw new ArgumentNullException($"'{nameof(QuicListenerOptions.ListenEndPoint)}' must be specified to start the listener.", argumentName);
        }
        if (ApplicationProtocols is null || ApplicationProtocols.Count <= 0)
        {
            throw new ArgumentException($"'{nameof(QuicListenerOptions.ApplicationProtocols)}' must be specified and contain at least one item to start the listener.", argumentName);
        }
        if (ListenBacklog == 0)
        {
            ListenBacklog = QuicDefaults.DefaultListenBacklog;
        }
        if (ConnectionOptionsCallback is null)
        {
            throw new ArgumentNullException($"'{nameof(QuicListenerOptions.ConnectionOptionsCallback)}' must be specified to start the listener.", argumentName);
        }
    }
}
