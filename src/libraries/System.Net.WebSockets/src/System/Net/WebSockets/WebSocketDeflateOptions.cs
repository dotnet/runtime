﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.WebSockets
{
    /// <summary>
    /// Options to enable per-message deflate compression for <seealso cref="WebSocket" />.
    /// </summary>
    /// <remarks>
    /// Although the WebSocket spec allows window bits from 8 to 15, the current implementation doesn't support 8 bits.
    /// For more information refer to the zlib manual https://zlib.net/manual.html.
    /// </remarks>
    public sealed class WebSocketDeflateOptions
    {
        private int _clientMaxWindowBits = 15;
        private int _serverMaxWindowBits = 15;

        /// <summary>
        /// This parameter indicates the base-2 logarithm of the LZ77 sliding window size of the client context.
        /// Must be a value between 9 and 15. The default is 15.
        /// </summary>
        /// <remarks>https://tools.ietf.org/html/rfc7692#section-7.1.2.2</remarks>
        public int ClientMaxWindowBits
        {
            get => _clientMaxWindowBits;
            set
            {
                if (value < 9 || value > 15)
                    throw new ArgumentOutOfRangeException(nameof(ClientMaxWindowBits), value,
                        SR.Format(SR.net_WebSockets_ArgumentOutOfRange, 9, 15));

                _clientMaxWindowBits = value;
            }
        }

        /// <summary>
        /// When true the client-side of the connection indicates that it will persist the deflate context accross messages.
        /// The default is true.
        /// </summary>
        /// <remarks>https://tools.ietf.org/html/rfc7692#section-7.1.1.2</remarks>
        public bool ClientContextTakeover { get; set; } = true;

        /// <summary>
        /// This parameter indicates the base-2 logarithm of the LZ77 sliding window size of the server context.
        /// Must be a value between 9 and 15. The default is 15.
        /// </summary>
        /// <remarks>https://tools.ietf.org/html/rfc7692#section-7.1.2.1</remarks>
        public int ServerMaxWindowBits
        {
            get => _serverMaxWindowBits;
            set
            {
                if (value < 9 || value > 15)
                    throw new ArgumentOutOfRangeException(nameof(ServerMaxWindowBits), value,
                        SR.Format(SR.net_WebSockets_ArgumentOutOfRange, 9, 15));

                _serverMaxWindowBits = value;
            }
        }

        /// <summary>
        /// When true the server-side of the connection indicates that it will persist the deflate context accross messages.
        /// The default is true.
        /// </summary>
        /// <remarks>https://tools.ietf.org/html/rfc7692#section-7.1.1.1</remarks>
        public bool ServerContextTakeover { get; set; } = true;
    }
}
