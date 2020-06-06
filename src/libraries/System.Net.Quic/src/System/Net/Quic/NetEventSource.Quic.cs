// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Net.Quic.Implementations.Managed;
using System.Net.Security;

namespace System.Net
{
#if FEATURE_QUIC_STANDALONE
    [EventSource(Name = "Microsoft-System-Net-Quic")]
#endif
    internal sealed partial class NetEventSource : EventSource
    {
        private const int ConnectClientStartId = NextAvailableEventId;
        private const int ConnectSuccessId = ConnectClientStartId + 1;

        private const int PacketSentId = ConnectSuccessId + 1;
        private const int PacketLostId = PacketSentId + 1;
        private const int PacketDroppedId = PacketLostId + 1;
        private const int SetEncryptionSecretsId = PacketDroppedId + 1;
        private const int DatagramSentId = SetEncryptionSecretsId + 1;
        private const int DatagramRecvId = DatagramSentId + 1;
        private const int NewConnectionIdReceivedId = DatagramRecvId + 1;

        [NonEvent]
        public static void NewClientConnection(ManagedQuicConnection connection, byte[] scid, byte[] dcid)
        {
            if (IsEnabled)
            {
                Log.NewClientConnection(IdOf(connection), scid, dcid);
            }
        }

        [Event(ConnectClientStartId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void NewClientConnection(string connection, byte[] scid, byte[] dcid)
        {
            WriteEvent(ConnectClientStartId, connection, scid, dcid);
        }

        [NonEvent]
        public static void Connected(ManagedQuicConnection connection)
        {
            if (IsEnabled)
            {
                Log.Connected(IdOf(connection));
            }
        }

        [Event(ConnectSuccessId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void Connected(string connection)
        {
            WriteEvent(ConnectSuccessId, connection);
        }

        [NonEvent]
        public static void PacketLost(ManagedQuicConnection connection, int size)
        {
            if (IsEnabled)
            {
                Log.PacketLost(IdOf(connection), size);
            }
        }

        [Event(PacketLostId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void PacketLost(string connection, int size)
        {
            WriteEvent(PacketLostId, connection, size);
        }

        [NonEvent]
        public static void PacketSent(ManagedQuicConnection connection, int size)
        {
            if (IsEnabled)
            {
                Log.PacketSent(IdOf(connection), size);
            }
        }

        [Event(PacketSentId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void PacketSent(string connection, int size)
        {
            WriteEvent(PacketSentId, connection, size);
        }

        [NonEvent]
        public static void PacketDropped(ManagedQuicConnection connection, int size)
        {
            if (IsEnabled)
            {
                Log.PacketDropped(IdOf(connection), size);
            }
        }

        [Event(PacketDroppedId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void PacketDropped(string connection, int size)
        {
            WriteEvent(PacketDroppedId, connection, size);
        }

        [NonEvent]
        public static void SetEncryptionSecrets(ManagedQuicConnection connection, EncryptionLevel level, TlsCipherSuite cipherSuite, ReadOnlySpan<byte> readSecret, ReadOnlySpan<byte> writeSecret)
        {
            if (IsEnabled)
            {
                Log.SetEncryptionSecrets(IdOf(connection), level, cipherSuite, readSecret.ToArray(), writeSecret.ToArray());
            }
        }

        [Event(SetEncryptionSecretsId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void SetEncryptionSecrets(string connection, EncryptionLevel level, TlsCipherSuite cipherSuite,
            byte[] readSecret, byte[] writeSecret)
        {
            WriteEvent(SetEncryptionSecretsId, connection, level, cipherSuite, readSecret, writeSecret);
        }

        [NonEvent]
        public static void DatagramReceived(ManagedQuicConnection connection, ReadOnlySpan<byte> datagram)
        {
            if (Log.IsEnabled(EventLevel.Verbose, Keywords.Debug))
            {
                Log.DatagramReceived(IdOf(connection), datagram.ToArray());
            }
        }

        [Event(DatagramRecvId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void DatagramReceived(string connection, byte[] datagram)
        {
            WriteEvent(DatagramRecvId, connection, datagram);
        }

        [NonEvent]
        public static void DatagramSent(ManagedQuicConnection connection, ReadOnlySpan<byte> datagram)
        {
            if (Log.IsEnabled(EventLevel.Verbose, Keywords.Debug))
            {
                Log.DatagramSent(IdOf(connection), datagram.ToArray());
            }
        }

        [Event(DatagramSentId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void DatagramSent(string connection, byte[] datagram)
        {
            WriteEvent(DatagramSentId, connection, datagram);
        }

        [NonEvent]
        public static void NewConnectionIdReceived(ManagedQuicConnection connection, byte[] connectionId)
        {
            if (IsEnabled)
            {
                Log.NewConnectionIdReceived(IdOf(connection), connectionId);
            }
        }

        [Event(NewConnectionIdReceivedId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void NewConnectionIdReceived(string connection, byte[] connectionId)
        {
            WriteEvent(NewConnectionIdReceivedId, connection, connectionId);
        }
    }
}
