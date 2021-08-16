// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    /// <summary>
    /// Options for QUIC
    /// </summary>
    public class QuicOptions
    {
        /// <summary>
        /// Limit on the number of bidirectional streams the remote peer connection can create on an open connection.
        /// Default is 100.
        /// </summary>
        // TODO consider constraining these limits to 0 to whatever the max of the QUIC library we are using.
        public int MaxBidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// Limit on the number of unidirectional streams the remote peer connection can create on an open connection.
        /// Default is 100.
        /// </summary>
        // TODO consider constraining these limits to 0 to whatever the max of the QUIC library we are using.
        public int MaxUnidirectionalStreams { get; set; } = 100;

        /// <summary>
        /// Idle timeout for connections, after which the connection will be closed.
        /// </summary>
        public TimeSpan IdleTimeout { get; set; }
    }
}
