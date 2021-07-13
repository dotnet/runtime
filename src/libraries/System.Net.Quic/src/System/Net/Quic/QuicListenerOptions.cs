// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace System.Net.Quic
{
    /// <summary>
    /// Options to provide to the <see cref="QuicListener"/>.
    /// </summary>
    public class QuicListenerOptions : QuicOptions
    {
        /// <summary>
        /// Server Ssl options to use for ALPN, SNI, etc.
        /// </summary>
        public SslServerAuthenticationOptions? ServerAuthenticationOptions { get; set; }

        /// <summary>
        /// The endpoint to listen on.
        /// </summary>
        public IPEndPoint? ListenEndPoint { get; set; }

        /// <summary>
        /// Number of connections to be held without accepting the connection.
        /// </summary>
        public int ListenBacklog { get; set; } = 512;

        public QuicListenerOptions()
        {
            IdleTimeout = TimeSpan.FromTicks(10 * TimeSpan.TicksPerMinute);
        }
    }
}
