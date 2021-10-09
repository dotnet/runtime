// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace System.Net.Quic
{
    /// <summary>
    /// Options to provide to the <see cref="QuicConnection"/> when connecting to a Listener.
    /// </summary>
    public class QuicClientConnectionOptions : QuicOptions
    {
        /// <summary>
        /// Client authentication options to use when establishing a <see cref="QuicConnection"/>.
        /// </summary>
        public SslClientAuthenticationOptions? ClientAuthenticationOptions { get; set; }

        /// <summary>
        /// The local endpoint that will be bound to.
        /// </summary>
        public IPEndPoint? LocalEndPoint { get; set; }

        /// <summary>
        /// The endpoint to connect to.
        /// </summary>
        public EndPoint? RemoteEndPoint { get; set; }

        public QuicClientConnectionOptions()
        {
            IdleTimeout = TimeSpan.FromTicks(2 * TimeSpan.TicksPerMinute);
        }
    }
}
