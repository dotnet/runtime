// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
#if !netcore50
using System.Runtime.Serialization;
#endif

namespace System.Net.Sockets
{
#if !netcore50
    [Serializable]
#endif
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SocketInformation
    {
        private byte[] _protocolInformation;
        private SocketInformationOptions _options;

#if !netcore50
        [OptionalField]
#endif
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

        internal bool UseOnlyOverlappedIO
        {
            get
            {
                return ((_options & SocketInformationOptions.UseOnlyOverlappedIO) != 0);
            }
            set
            {
                if (value)
                {
                    _options |= SocketInformationOptions.UseOnlyOverlappedIO;
                }
                else
                {
                    _options &= ~SocketInformationOptions.UseOnlyOverlappedIO;
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
