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
        private readonly byte[] buffer = new byte[16 * 1024];
        private static IPEndPoint IpEndPoint = null;

        internal readonly ITestOutputHelper _output;
        private readonly ManagedQuicConnection _client;

        public TestHarness(ITestOutputHelper output, ManagedQuicConnection client)
        {
            _output = output;
            _client = client;
        }

        internal const string CertificateFilePath = "Certs/cert.crt";
        internal const string PrivateKeyFilePath = "Certs/cert.key";

        internal PacketFlight GetFlightToSend(ManagedQuicConnection from)
        {
            int written = from.SendData(buffer, out _, DateTime.Now);
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
            QuicWriter writer = new QuicWriter(new ArraySegment<byte>(buffer));
            TestHarnessContext testHarness = new TestHarnessContext(source);

            LogFlightPackets(packets, source == _client);

            foreach (PacketBase packet in packets)
            {
                packet.Serialize(writer, testHarness);
                writer.Reset(writer.Buffer.Slice(writer.BytesWritten));
            }

            destination.ReceiveData(buffer, writer.Buffer.Offset + writer.BytesWritten, IpEndPoint, DateTime.Now);
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
            // make a copy of the buffer, because decryption happens in-place
            int written = source.SendData(buffer, out _, DateTime.Now);
            var copy = buffer.AsSpan(0, written).ToArray();

            var packets = PacketBase.ParseMany(copy, written, new TestHarnessContext(source));

            LogFlightPackets(packets, source == _client);

            destination.ReceiveData(buffer, written, IpEndPoint, DateTime.Now);

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
