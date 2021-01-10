// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.WebSockets
{
    /// <summary>
    /// Options to enable per-message deflate compression for <seealso cref="WebSocket" />.
    /// </summary>
    public sealed class WebSocketDeflateOptions
    {
        private int _clientMaxWindowBits = 15;
        private int _serverMaxWindowBits = 15;

        /// <summary>
        /// This parameter indicates the base-2 logarithm of the LZ77 sliding window size of the client context.
        /// Must be a value between 8 and 15. The default is 15.
        /// </summary>
        public int ClientMaxWindowBits
        {
            get => _clientMaxWindowBits;
            set
            {
                if (value < 8 || value > 15)
                    throw new ArgumentOutOfRangeException(nameof(ClientMaxWindowBits), value,
                        SR.Format(SR.net_WebSockets_ArgumentOutOfRange, 8, 15));

                _clientMaxWindowBits = value;
            }
        }

        /// <summary>
        /// When true the client-side of the connection indicates that it will persist the deflate context accross messages.
        /// The default is true.
        /// </summary>
        public bool ClientContextTakeover { get; set; } = true;

        /// <summary>
        /// This parameter indicates the base-2 logarithm of the LZ77 sliding window size of the server context.
        /// Must be a value between 8 and 15. The default is 15.
        /// </summary>
        public int ServerMaxWindowBits
        {
            get => _serverMaxWindowBits;
            set
            {
                if (value < 8 || value > 15)
                    throw new ArgumentOutOfRangeException(nameof(ServerMaxWindowBits), value,
                        SR.Format(SR.net_WebSockets_ArgumentOutOfRange, 8, 15));

                _serverMaxWindowBits = value;
            }
        }

        /// <summary>
        /// When true the server-side of the connection indicates that it will persist the deflate context accross messages.
        /// The default is true.
        /// </summary>
        public bool ServerContextTakeover { get; set; } = true;
    }
}
