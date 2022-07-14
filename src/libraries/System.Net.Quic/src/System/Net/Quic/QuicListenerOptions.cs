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
    /// This property is mandatory and not setting it will result in validation error when starting the listener.
    /// </summary>
    public IPEndPoint ListenEndPoint { get; set; } = null!;

    /// <summary>
    /// List of application protocols which the listener will accept. At least one must be specified.
    /// This property is mandatory and not setting it will result in validation error when starting the listener.
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
            throw new ArgumentNullException(SR.Format(SR.net_quic_not_null_listener, nameof(QuicListenerOptions.ListenEndPoint)), argumentName);
        }
        if (ApplicationProtocols is null || ApplicationProtocols.Count <= 0)
        {
            throw new ArgumentNullException(SR.Format(SR.net_quic_not_null_not_empty_listener, nameof(QuicListenerOptions.ApplicationProtocols)), argumentName);
        }
        if (ListenBacklog == 0)
        {
            ListenBacklog = QuicDefaults.DefaultListenBacklog;
        }
        if (ConnectionOptionsCallback is null)
        {
            throw new ArgumentNullException(SR.Format(SR.net_quic_not_null_listener, nameof(QuicListenerOptions.ConnectionOptionsCallback)), argumentName);
        }
    }
}
