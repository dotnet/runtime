// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets
{
    public struct SocketInformation
    {
        private byte[] _protocolInformation;
        private SocketInformationOptions _options;
        private EndPoint _remoteEndPoint;

        public byte[] ProtocolInformation
        {
            get
            {
                return _protocolInformation;
            }
            set
            {
                _protocolInformation = value;
            }
        }

        public SocketInformationOptions Options
        {
            get
            {
                return _options;
            }
            set
            {
                _options = value;
            }
        }

        internal bool IsNonBlocking
        {
            get
            {
                return ((_options & SocketInformationOptions.NonBlocking) != 0);
            }
            set
            {
                if (value)
                {
                    _options |= SocketInformationOptions.NonBlocking;
                }
                else
                {
                    _options &= ~SocketInformationOptions.NonBlocking;
                }
            }
        }

        internal bool IsConnected
        {
            get
            {
                return ((_options & SocketInformationOptions.Connected) != 0);
            }
            set
            {
                if (value)
                {
                    _options |= SocketInformationOptions.Connected;
                }
                else
                {
                    _options &= ~SocketInformationOptions.Connected;
                }
            }
        }

        internal bool IsListening
        {
            get
            {
                return ((_options & SocketInformationOptions.Listening) != 0);
            }
            set
            {
                if (value)
                {
                    _options |= SocketInformationOptions.Listening;
                }
                else
                {
                    _options &= ~SocketInformationOptions.Listening;
                }
            }
        }

        internal EndPoint RemoteEndPoint
        {
            get
            {
                return _remoteEndPoint;
            }
            set
            {
                _remoteEndPoint = value;
            }
        }
    }
}
