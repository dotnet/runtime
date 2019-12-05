// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;

namespace System.Net.Quic
{
    /// <summary>
    /// Options to provide to the <see cref="QuicConnection"/> when connecting to a Listener.
    /// </summary>
    public class QuicClientConnectionOptions
    {
        /// <summary>
        /// Client authentication options to use when establishing a <see cref="QuicConnection"/>.
        /// </summary>
        public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; }

        /// <summary>
        /// The endpoint to connect to.
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// The endpoint to connect to.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// Limit on the number of bidirectional streams a connection can create
        /// to the listener.
        /// Default is 100.
        /// </summary>
        // TODO consider constraining these limits to 0 to whatever the max of the QUIC library we are using.
        public short MaxBidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// Limit on the number of unidirectional streams a connection can create
        /// to the listener.
        /// Default is 100.
        /// </summary>
        // TODO consider constraining these limits to 0 to whatever the max of the QUIC library we are using.
        public short MaxUnidirectionalStreams { get; set; } = 100;
    }
}
