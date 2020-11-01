// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Sockets
{
    internal sealed class SingleConnectionSocketContext : QuicSocketContext
    {
        private readonly EndPoint _remoteEndPoint;
        internal ConnectionContext ConnectionContext { get; }

        internal SingleConnectionSocketContext(EndPoint? localEndpoint, EndPoint remoteEndPoint,
            ManagedQuicConnection connection)
            : base(localEndpoint, remoteEndPoint, connection.IsServer)
        {
            ConnectionContext = new ConnectionContext(this, connection);
            _remoteEndPoint = remoteEndPoint;
        }

        protected override void OnDatagramReceived(in DatagramInfo datagram)
        {
            ConnectionContext.IncomingDatagramWriter.TryWrite(datagram);
        }

        protected internal override bool OnConnectionStateChanged(ManagedQuicConnection connection, QuicConnectionState newState)
        {
            switch (newState)
            {
                case QuicConnectionState.None:
                    break;
                case QuicConnectionState.Connected:
                    break;
                case QuicConnectionState.Closing:
                    break;
                case QuicConnectionState.Draining:
                    if (!connection.IsServer)
                    {
                        // clients can stop earlier because there is no danger of packets being interpreted as belonging
                        // to a new connection.
                        DetachConnection(connection);
                    }

                    break;
                case QuicConnectionState.Closed:
                    // draining timer elapsed, discard the state
                    DetachConnection(connection);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }

            return false;
        }

        protected override void OnException(Exception e)
        {
            ConnectionContext.Connection.OnSocketContextException(e);
            SignalStop();
        }

        protected internal override void DetachConnection(ManagedQuicConnection connection)
        {
            Debug.Assert(connection.IsClosed);
            Debug.Assert(connection == ConnectionContext.Connection);
            // only one connection, so we can stop the background worker and free resources
            SignalStop();
            connection.DoCleanup();
        }
    }
}
