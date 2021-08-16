// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Net.WebSockets
{
    /// <summary>
    /// Options that control how a <seealso cref="WebSocket"/> is created.
    /// </summary>
    public sealed class WebSocketCreationOptions
    {
        private string? _subProtocol;
        private TimeSpan _keepAliveInterval;

        /// <summary>
        /// Defines if this websocket is the server-side of the connection. The default value is false.
        /// </summary>
        public bool IsServer { get; set; }

        /// <summary>
        /// The agreed upon sub-protocol that was used when creating the connection.
        /// </summary>
        public string? SubProtocol
        {
            get => _subProtocol;
            set
            {
                if (value is not null)
                {
                    WebSocketValidate.ValidateSubprotocol(value);
                }
                _subProtocol = value;
            }
        }

        /// <summary>
        /// The keep-alive interval to use, or <see cref="TimeSpan.Zero"/> or <see cref="Timeout.InfiniteTimeSpan"/> to disable keep-alives.
        /// The default is <see cref="TimeSpan.Zero"/>.
        /// </summary>
        public TimeSpan KeepAliveInterval
        {
            get => _keepAliveInterval;
            set
            {
                if (value != Timeout.InfiniteTimeSpan && value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(KeepAliveInterval), value,
                        SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 0));
                }
                _keepAliveInterval = value;
            }
        }

        /// <summary>
        /// The agreed upon options for per message deflate.<para />
        /// Be aware that enabling compression makes the application subject to CRIME/BREACH type of attacks.
        /// It is strongly advised to turn off compression when sending data containing secrets by
        /// specifying <see cref="WebSocketMessageFlags.DisableCompression" /> flag for such messages.
        /// </summary>
        public WebSocketDeflateOptions? DangerousDeflateOptions { get; set; }
    }
}
