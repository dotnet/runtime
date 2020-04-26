using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    internal class TestHarness
    {
        internal static readonly QuicServerSocketContext DummySocketContet = new QuicServerSocketContext(new IPEndPoint(IPAddress.Any, 0), null, null);
        internal static IPEndPoint IpAnyEndpoint = new IPEndPoint(IPAddress.Any, 0);

        private readonly byte[] buffer = new byte[16 * 1024];

        internal readonly ITestOutputHelper _output;
        private readonly ManagedQuicConnection _client;

        private readonly QuicReader _reader;
        private readonly QuicWriter _writer;

        private readonly long _startTimestamp = Implementations.Managed.Internal.Timestamp.Now;
        internal long Timestamp;

        internal static ManagedQuicConnection CreateClient(QuicClientConnectionOptions options)
        {
            options.RemoteEndPoint = IpAnyEndpoint;
            return new ManagedQuicConnection(options);
        }

        internal static ManagedQuicConnection CreateServer(QuicListenerOptions options)
        {
            return new ManagedQuicConnection(options, DummySocketContet, IpAnyEndpoint);
        }

        public TestHarness(ITestOutputHelper output, ManagedQuicConnection client)
        {
            _output = output;
            _client = client;

            _reader = new QuicReader(buffer);
            _writer = new QuicWriter(buffer);

            Timestamp = _startTimestamp;
        }

        internal const string CertificateFilePath = "Certs/cert.crt";
        internal const string PrivateKeyFilePath = "Certs/cert.key";

        internal PacketFlight GetFlightToSend(ManagedQuicConnection from)
        {
            _writer.Reset(buffer);
            from.SendData(_writer, out _, Timestamp);
            int written = _writer.BytesWritten;
            var copy = buffer.AsSpan(0, written).ToArray();
            var packets = PacketBase.ParseMany(copy, written, new TestHarnessContext(from));

            // debug: check that serializing packets back gives identical datagram
            _writer.Reset(copy);
            foreach (var packet in packets)
            {
                packet.Serialize(_writer, new TestHarnessContext(from));
                _writer.Reset(_writer.Buffer.Slice(_writer.BytesWritten));
            }
            Debug.Assert(copy.AsSpan().SequenceEqual(buffer.AsSpan(0, written)));

            return new PacketFlight(packets, written, from, Timestamp);
        }

        internal TPacket GetPacketToSend<TPacket>(ManagedQuicConnection from) where TPacket : PacketBase
        {
            var flight = GetFlightToSend(from);
            return Assert.IsType<TPacket>(Assert.Single(flight.Packets));
        }

        internal OneRttPacket Get1RttToSend(ManagedQuicConnection from)
        {
            return GetPacketToSend<OneRttPacket>(from);
        }

        internal InitialPacket GetInitialToSend(ManagedQuicConnection from)
        {
            return GetPacketToSend<InitialPacket>(from);
        }

        internal OneRttPacket Send1Rtt(ManagedQuicConnection source,
            ManagedQuicConnection destination)
        {
            var flight = SendFlight(source, destination);
            return Assert.IsType<OneRttPacket>(Assert.Single(flight.Packets));
        }

        internal TFrame Send1RttWithFrame<TFrame>(ManagedQuicConnection source,
            ManagedQuicConnection destination) where TFrame : FrameBase
        {
            var packet = Send1Rtt(source, destination);
            return packet.ShouldHaveFrame<TFrame>();
        }

        internal void Intercept1Rtt(ManagedQuicConnection source, ManagedQuicConnection destination,
            Action<OneRttPacket> interceptCallback)
        {
            var packet = Get1RttToSend(source);
            interceptCallback(packet);
            SendPacket(source, destination, packet);
        }

        internal void Intercept1RttFrame<TFrame>(ManagedQuicConnection source, ManagedQuicConnection destination,
            Action<TFrame> interceptCallback) where TFrame : FrameBase
        {
            Intercept1Rtt(source, destination, packet =>
            {
                var frame = packet.ShouldHaveFrame<TFrame>();
                interceptCallback(frame);
            });
        }

        internal void SendFlight(ManagedQuicConnection source,
            ManagedQuicConnection destination, IEnumerable<PacketBase> packets)
        {
            TestHarnessContext testHarness = new TestHarnessContext(source);

            LogFlightPackets(packets, source == _client);

            _writer.Reset(buffer);
            int written = 0;
            foreach (PacketBase packet in packets)
            {
                packet.Serialize(_writer, testHarness);
                written += _writer.BytesWritten;
                _writer.Reset(_writer.Buffer.Slice(_writer.BytesWritten));
            }

            _reader.Reset(buffer.AsMemory(0, written));
            destination.ReceiveData(_reader, IpAnyEndpoint, Timestamp);
        }

        internal void SendPacket(ManagedQuicConnection source, ManagedQuicConnection destination, PacketBase packet)
        {
            SendFlight(source, destination, new []{packet});
        }

        internal void LogFlightPackets(PacketFlight flight, bool lost = false)
        {
            var sender = flight.Sender == _client ? "Client" : "Server";
            var lostLabel = lost ? " (Lost)" : "";
            long milliseconds = Implementations.Managed.Internal.Timestamp.GetMilliseconds(flight.TimeSent - _startTimestamp);
            _output.WriteLine($"\n[{milliseconds}] {sender}{lostLabel}:");
            foreach (PacketBase packet in flight.Packets)
            {
                _output.WriteLine(packet.ToString());
            }
        }

        internal void LogFlightPackets(IEnumerable<PacketBase> packets, bool clientSending, bool lost = false)
        {
            LogFlightPackets(new PacketFlight(packets.ToList(), 0, clientSending ? _client : null, Timestamp), lost);
        }

        internal PacketFlight SendFlight(ManagedQuicConnection source, ManagedQuicConnection destination,
            long packetTravelTime = 0)
        {
            _writer.Reset(buffer);
            source.SendData(_writer, out _, Timestamp);

            Timestamp += packetTravelTime;

            // make a copy of the buffer, because decryption happens in-place
            int written = _writer.BytesWritten;
            var copy = buffer.AsSpan(0, written).ToArray();

            var packets = PacketBase.ParseMany(copy, written, new TestHarnessContext(source));

            LogFlightPackets(packets, source == _client);

            _reader.Reset(buffer.AsMemory(0, written));
            destination.ReceiveData(_reader, IpAnyEndpoint, Timestamp);

            return new PacketFlight(packets, written, source, Timestamp);
        }


        internal void EstablishConnection(ManagedQuicConnection client, ManagedQuicConnection server)
        {
            int flights = 0;
            do
            {
                SendFlight(client, server);
                SendFlight(server, client);
                flights++;
            } while (!client.Connected && !server.Connected && flights < 10);

            Assert.True(client.Connected);
            Assert.True(server.Connected);
        }
    }
}
