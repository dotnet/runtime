using System.Collections.Generic;
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
        internal static readonly QuicSocketContext DummySocketContet = new QuicSocketContext(new IPEndPoint(IPAddress.Any, 0), null);
        internal static IPEndPoint IpAnyEndpoint = new IPEndPoint(IPAddress.Any, 0);

        private readonly byte[] buffer = new byte[16 * 1024];

        internal readonly ITestOutputHelper _output;
        private readonly ManagedQuicConnection _client;

        private readonly QuicReader _reader;
        private readonly QuicWriter _writer;

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
        }

        internal const string CertificateFilePath = "Certs/cert.crt";
        internal const string PrivateKeyFilePath = "Certs/cert.key";

        internal PacketFlight GetFlightToSend(ManagedQuicConnection from)
        {
            _writer.Reset(buffer);
            from.SendData(_writer, out _, DateTime.Now);
            int written = _writer.BytesWritten + _writer.Buffer.Offset;
            var copy = buffer.AsSpan(0, written).ToArray();
            var packets = PacketBase.ParseMany(copy, written, new TestHarnessContext(from));

            return new PacketFlight(packets, written);
        }

        internal OneRttPacket Get1RttToSend(ManagedQuicConnection from)
        {
            var flight = GetFlightToSend(from);
            return Assert.IsType<OneRttPacket>(flight.Packets[0]);
        }

        internal OneRttPacket Send1Rtt(ManagedQuicConnection source,
            ManagedQuicConnection destination)
        {
            var flight = SendFlight(source, destination);
            return Assert.IsType<OneRttPacket>(flight.Packets[0]);
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
            foreach (PacketBase packet in packets)
            {
                packet.Serialize(_writer, testHarness);
                _writer.Reset(_writer.Buffer.Slice(_writer.BytesWritten));
            }

            _reader.Reset(buffer, 0, _writer.BytesWritten + _writer.Buffer.Offset);
            destination.ReceiveData(_reader, IpAnyEndpoint, DateTime.Now);
        }

        internal void SendPacket(ManagedQuicConnection source, ManagedQuicConnection destination, PacketBase packet)
        {
            SendFlight(source, destination, new []{packet});
        }

        private void LogFlightPackets(IEnumerable<PacketBase> packets, bool clientSending)
        {
            _output.WriteLine(clientSending ? "\nClient:" : "\nServer:");
            foreach (PacketBase packet in packets)
            {
                _output.WriteLine(packet.ToString());
            }
        }

        internal PacketFlight SendFlight(ManagedQuicConnection source, ManagedQuicConnection destination)
        {
            _writer.Reset(buffer);
            source.SendData(_writer, out _, DateTime.Now);

            // make a copy of the buffer, because decryption happens in-place
            int written = _writer.BytesWritten + _writer.Buffer.Offset;
            var copy = buffer.AsSpan(0, written).ToArray();

            var packets = PacketBase.ParseMany(copy, written, new TestHarnessContext(source));

            LogFlightPackets(packets, source == _client);

            _reader.Reset(buffer, 0, written);
            destination.ReceiveData(_reader, IpAnyEndpoint, DateTime.Now);

            return new PacketFlight(packets, written);
        }


        internal void EstablishConnection(ManagedQuicConnection client, ManagedQuicConnection server)
        {
            SendFlight(client, server);
            SendFlight(server, client);
            SendFlight(client, server);
            SendFlight(server, client);

            Assert.True(client.Connected);
            Assert.True(server.Connected);
        }
    }
}
