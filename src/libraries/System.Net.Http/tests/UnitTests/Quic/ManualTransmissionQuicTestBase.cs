using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    /// <summary>
    ///     Base class for tests which manually transmit packets between connections and inspect the contents of the
    ///     sent packets
    /// </summary>
    /// <remarks>
    ///     There are many overloads in an attempt to keep testing code short and expressive. It is intened that you
    ///     use:
    /// <para>
    ///       - Lose* functions if you intend the packet not be delivered at all. This function logs the contents to the
    ///         test output.
    /// </para>
    /// <para>
    ///       - Get*ToSend functions if you aren't sure that the packet should be delivered (e.g. for dropping packets
    ///         containing specific frames. The data can then be sent using one of the Send* overloads. This is the only
    ///         function which do not log the sent packets.
    /// </para>
    /// <para>
    ///       - Send* functions if you want to transmit the packets and want to inspect the contents.
    /// </para>
    /// <para>
    ///       - Intercept* functions if you want to modify the packets mid-transmit in order to provoke particular error
    ///         cases. If you don't intend to modify the packet, use Send* function, as they avoid extra serialization
    ///         calls.
    /// </para>
    /// </remarks>
    public class ManualTransmissionQuicTestBase
    {
        private const string CertificateFilePath = "Certs/cert.crt";
        private const string PrivateKeyFilePath = "Certs/cert.key";

        private static readonly QuicServerSocketContext _dummySocketContext = new QuicServerSocketContext(new IPEndPoint(IPAddress.Any, 0), null, null);
        private static readonly IPEndPoint _ipAnyEndpoint = new IPEndPoint(IPAddress.Any, 0);

        internal readonly QuicClientConnectionOptions ClientOptions;
        internal readonly QuicListenerOptions ListenerOptions;

        internal readonly ManagedQuicConnection Client;
        internal readonly ManagedQuicConnection Server;

        internal readonly ITestOutputHelper Output;

        private readonly QuicReader _reader;
        private readonly QuicWriter _writer;

        private readonly QuicSocketContext.SendContext _sendContext;
        private readonly QuicSocketContext.RecvContext _recvContext;

        private readonly byte[] buffer = new byte[16 * 1024]; // used as a communication medium

        // TODO-RZ: find a better way of hooking into the serialization/deserialization
        private readonly Dictionary<(ManagedQuicConnection, PacketType), CryptoSeal> _sealMap = new Dictionary<(ManagedQuicConnection, PacketType), CryptoSeal>();

        private readonly long _startTimestamp;
        internal long CurrentTimestamp;

        protected ManualTransmissionQuicTestBase(ITestOutputHelper output)
        {
            ClientOptions = new QuicClientConnectionOptions();
            ListenerOptions = new QuicListenerOptions
            {
                CertificateFilePath = CertificateFilePath,
                PrivateKeyFilePath = PrivateKeyFilePath
            };
            Client = CreateClient(ClientOptions);
            Server = CreateServer(ListenerOptions);

            Output = output;

            _startTimestamp = Timestamp.Now;

            _reader = new QuicReader(buffer);
            _writer = new QuicWriter(buffer);

            var sentPacketPool = new ObjectPool<SentPacket>(64);
            _sendContext = new QuicSocketContext.SendContext(sentPacketPool);
            _recvContext = new QuicSocketContext.RecvContext(sentPacketPool);

            CurrentTimestamp = _startTimestamp;
        }

        private static ManagedQuicConnection CreateClient(QuicClientConnectionOptions options)
        {
            options.RemoteEndPoint = _ipAnyEndpoint;
            return new ManagedQuicConnection(options);
        }

        private static ManagedQuicConnection CreateServer(QuicListenerOptions options)
        {
            return new ManagedQuicConnection(options, _dummySocketContext, _ipAnyEndpoint);
        }

        /// <summary>
        ///     Performs enough roundtrips to establish connection.
        /// </summary>
        internal void EstablishConnection()
        {
            int flights = 0;
            do
            {
                SendFlight(Client, Server);
                SendFlight(Server, Client);
                flights++;
            } while (!Client.Connected && !Server.Connected && flights < 10);

            Assert.True(Client.Connected);
            Assert.True(Server.Connected);
        }

        /// <summary>
        ///     Gets datagram containing packets which would the specified connection want to send.
        /// </summary>
        /// <param name="from">The sender.</param>
        /// <returns></returns>
        internal PacketFlight GetFlightToSend(ManagedQuicConnection from)
        {
            _writer.Reset(buffer);
            _sendContext.Timestamp = CurrentTimestamp;
            from.SendData(_writer, out _, _sendContext);
            int written = _writer.BytesWritten;
            var copy = buffer.AsSpan(0, written).ToArray();
            var packets = PacketBase.ParseMany(copy, new TestHarnessContext(from, _sealMap));

            // debug: check that serializing packets back gives identical datagram
            _writer.Reset(copy);
            foreach (var packet in packets)
            {
                packet.Serialize(_writer, new TestHarnessContext(from, _sealMap));
                _writer.Reset(_writer.Buffer.Slice(_writer.BytesWritten));
            }
            Debug.Assert(copy.AsSpan().SequenceEqual(buffer.AsSpan(0, written)));

            return new PacketFlight(packets, written, from, CurrentTimestamp);
        }

        /// <summary>
        ///     Same as <see cref="GetFlightToSend"/>, but expects that only single packet of given type will be sent in
        ///     the flight.
        /// </summary>
        /// <param name="from">The sender connection.</param>
        /// <typeparam name="TPacket">Type of the packet expected to be sent.</typeparam>
        /// <returns></returns>
        private TPacket GetPacketToSend<TPacket>(ManagedQuicConnection from) where TPacket : PacketBase
        {
            var flight = GetFlightToSend(from);
            return Assert.IsType<TPacket>(Assert.Single(flight.Packets));
        }

        /// <summary>
        ///     Expects sender to want to send <see cref="OneRttPacket"/>, returns it for further inspection/modification.
        /// </summary>
        /// <param name="from">The sender connection.</param>
        /// <returns></returns>
        internal OneRttPacket Get1RttToSend(ManagedQuicConnection from)
        {
            return GetPacketToSend<OneRttPacket>(from);
        }

        /// <summary>
        ///     Expects sender to want to send <see cref="InitialPacket"/>, returns it for further inspection/modification.
        /// </summary>
        /// <param name="from">The sender connection.</param>
        /// <returns></returns>
        internal InitialPacket GetInitialToSend(ManagedQuicConnection from)
        {
            return GetPacketToSend<InitialPacket>(from);
        }

        /// <summary>
        ///     Expects that source connection wants to send a flight containing a single <see cref="OneRttPacket"/>
        ///     to destination connection and returns the packet for further inspection.
        /// </summary>
        /// <param name="source">The source connection.</param>
        /// <param name="destination">The destination connection.</param>
        /// <returns></returns>
        internal OneRttPacket Send1Rtt(ManagedQuicConnection source,
            ManagedQuicConnection destination)
        {
            var flight = SendFlight(source, destination);
            return Assert.IsType<OneRttPacket>(Assert.Single(flight.Packets));
        }

        /// <summary>
        ///     Expects that source connection wants to send a flight containing a single <see cref="OneRttPacket"/>
        ///     containing (possibly among others) a frame of type <see cref="TFrame"/>, to destination connection and
        ///     returns the contained <see cref="TFrame"/> packet for further inspection.
        /// </summary>
        /// <param name="source">The source connection.</param>
        /// <param name="destination">The destination connection.</param>
        internal TFrame Send1RttWithFrame<TFrame>(ManagedQuicConnection source,
            ManagedQuicConnection destination) where TFrame : FrameBase
        {
            var packet = Send1Rtt(source, destination);
            return packet.ShouldHaveFrame<TFrame>();
        }

        /// <summary>
        ///     Intercepts single flight of packets between two connections and allows the contents of the packets to be
        ///     inspected and modified inside a callback.
        /// </summary>
        /// <param name="source">The source connection.</param>
        /// <param name="destination">The destination connection.</param>
        /// <param name="interceptCallback">Callback for inspecting and possibly modifying the packets.</param>
        internal void InterceptFlight(ManagedQuicConnection source, ManagedQuicConnection destination,
            Action<PacketFlight> interceptCallback)
        {
            var flight = GetFlightToSend(source);
            interceptCallback(flight);
            SendFlight(source, destination, flight.Packets);
        }

        /// <summary>
        ///     Intercepts a flight expected to contain a single packet of type <see cref="TPacket"/> between two
        ///     connections and allows the contents of the packet to be inspected and modified inside a callback.
        /// </summary>
        /// <param name="source">The source connection.</param>
        /// <param name="destination">The destination connection.</param>
        /// <param name="interceptCallback">Callback for inspecting and possibly modifying the packet.</param>
        private void InterceptPacket<TPacket>(ManagedQuicConnection source, ManagedQuicConnection destination,
            Action<TPacket> interceptCallback) where TPacket : PacketBase
        {
            var packet = GetPacketToSend<TPacket>(source);
            interceptCallback(packet);
            SendPacket(source, destination, packet);
        }

        /// <summary>
        ///     Intercepts a flight expected to contain a single packet of type <see cref="InitialPacket"/> between two
        ///     connections and allows the contents of the packet to be inspected and modified inside a callback.
        /// </summary>
        /// <param name="source">The source connection.</param>
        /// <param name="destination">The destination connection.</param>
        /// <param name="interceptCallback">Callback for inspecting and possibly modifying the packet.</param>
        internal void InterceptInitial(ManagedQuicConnection source, ManagedQuicConnection destination,
            Action<InitialPacket> interceptCallback)
        {
            InterceptPacket(source, destination, interceptCallback);
        }

        /// <summary>
        ///     Intercepts a flight expected to contain a single packet of type <see cref="OneRttPacket"/> between two
        ///     connections and allows the contents of the packet to be inspected and modified inside a callback.
        /// </summary>
        /// <param name="source">The source connection.</param>
        /// <param name="destination">The destination connection.</param>
        /// <param name="interceptCallback">Callback for inspecting and possibly modifying the packet.</param>
        internal void Intercept1Rtt(ManagedQuicConnection source, ManagedQuicConnection destination,
            Action<OneRttPacket> interceptCallback)
        {
            InterceptPacket(source, destination, interceptCallback);
        }

        /// <summary>
        ///     Intercepts a flight expected to contain a single packet of type <see cref="OneRttPacket"/>, with (among
        ///     others) a single frame of type <see cref="TFrame"/>, between two connections and allows the contents of
        ///     the frame to be inspected and modified inside a callback.
        /// </summary>
        /// <param name="source">The source connection.</param>
        /// <param name="destination">The destination connection.</param>
        /// <param name="interceptCallback">Callback for inspecting and possibly modifying the frame.</param>
        internal void Intercept1RttFrame<TFrame>(ManagedQuicConnection source, ManagedQuicConnection destination,
            Action<TFrame> interceptCallback) where TFrame : FrameBase
        {
            Intercept1Rtt(source, destination, packet =>
            {
                var frame = packet.ShouldHaveFrame<TFrame>();
                interceptCallback(frame);
            });
        }

        /// <summary>
        ///     Sends provided packets in a single flight
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="packets"></param>
        internal void SendFlight(ManagedQuicConnection source,
            ManagedQuicConnection destination, IEnumerable<PacketBase> packets)
        {
            TestHarnessContext testHarness = new TestHarnessContext(source, _sealMap);

            LogFlightPackets(packets, source);

            _writer.Reset(buffer);
            int written = 0;
            foreach (PacketBase packet in packets)
            {
                packet.Serialize(_writer, testHarness);
                written += _writer.BytesWritten;
                _writer.Reset(_writer.Buffer.Slice(_writer.BytesWritten));
            }

            _reader.Reset(buffer.AsMemory(0, written));
            _recvContext.Timestamp = CurrentTimestamp;
            destination.ReceiveData(_reader, _ipAnyEndpoint, _recvContext);
        }

        /// <summary>
        ///     Transmits the specified packet, as if it was sent from the <paramref name="source"/> connection, to the
        ///     <paramref name="destination"/> connection.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="packet"></param>
        /// <remarks>
        ///     It is expected that the packet was retrieved from <paramref name="source"/> to begin with.
        ///     Inserting arbitrary packets to the communication may cause the connection to enter invalid state, which
        ///     is impossible to achieve in normal circumstances.
        ///  </remarks>
        internal void SendPacket(ManagedQuicConnection source, ManagedQuicConnection destination, PacketBase packet)
        {
            SendFlight(source, destination, new []{packet});
        }

        /// <summary>
        ///     Logs the packets to the test output.
        /// </summary>
        /// <param name="flight">The flight to be logged.</param>
        /// <param name="lost">If true, the flight will be logged as lost.</param>
        internal void LogFlightPackets(PacketFlight flight, bool lost = false)
        {
            var sender = flight.Sender == Client ? "Client" : "Server";
            var lostLabel = lost ? " (Lost)" : "";
            long milliseconds = Timestamp.GetMilliseconds(flight.TimeSent - _startTimestamp);

            Output.WriteLine($"\n{(milliseconds > 0 ? $"[{milliseconds}]" : "")}{sender}{lostLabel}:");
            foreach (PacketBase packet in flight.Packets)
            {
                Output.WriteLine(packet.ToString());
            }
        }

        /// <summary>
        ///     Transmit a flight between <paramref name="source"/> and <paramref name="destination"/> connections,
        ///     logs the packets into output and returns the sent flight for further inspection.
        /// </summary>
        /// <param name="source">The sender connection.</param>
        /// <param name="destination">The destination connection.</param>
        /// <returns></returns>
        internal PacketFlight SendFlight(ManagedQuicConnection source, ManagedQuicConnection destination)
        {
            _writer.Reset(buffer);
            _sendContext.Timestamp = CurrentTimestamp;
            source.SendData(_writer, out _, _sendContext);

            // make a copy of the buffer, because decryption happens in-place
            int written = _writer.BytesWritten;
            var copy = buffer.AsSpan(0, written).ToArray();

            var packets = PacketBase.ParseMany(copy, new TestHarnessContext(source, _sealMap));

            LogFlightPackets(packets, source);

            _reader.Reset(buffer.AsMemory(0, written));
            _recvContext.Timestamp = CurrentTimestamp;
            destination.ReceiveData(_reader, _ipAnyEndpoint, _recvContext);

            return new PacketFlight(packets, written, source, CurrentTimestamp);
        }

        /// <summary>
        ///     Expects the connection to send <see cref="OneRttPacket"/>, logs it as lost and returns it for further
        ///     inspection.
        /// </summary>
        /// <param name="source">The source connection.</param>
        internal OneRttPacket Lose1RttPacket(ManagedQuicConnection source)
        {
            var flight = GetFlightToSend(source);
            var packet = Assert.IsType<OneRttPacket>(Assert.Single(flight.Packets));
            LogFlightPackets(flight, true);
            return packet;
        }

        /// <summary>
        ///     Expects the connection to send <see cref="OneRttPacket"/> containing given frame, logs it as lost and
        ///     returns the frame for further inspection.
        /// </summary>
        /// <param name="source">The source connection.</param>
        internal TFrame Lose1RttPacketWithFrame<TFrame>(ManagedQuicConnection source) where TFrame : FrameBase
        {
            var flight = GetFlightToSend(source);
            var packet = Assert.IsType<OneRttPacket>(Assert.Single(flight.Packets));
            LogFlightPackets(flight, true);
            return packet.ShouldHaveFrame<TFrame>();
        }

        /// <summary>
        ///     Helper function for checking that a particular frame is resent after loss. The function expects
        ///     that the <paramref name="source"/> connection will send <see cref="OneRttPacket"/> with
        ///     <typeparamref name="TFrame"/> frame. This packet is discarded (simulated loss), and the
        ///     <paramref name="destination"/> connection is pinged with a long enough delay in order for the original
        ///     packet be deemed lost and data resent.
        /// </summary>
        /// <param name="source">The source connection.</param>
        /// <param name="destination">The destination connection.</param>
        /// <typeparam name="TFrame">The type of frame expected to be (re)sent.</typeparam>
        internal void Lose1RttWithFrameAndCheckIfItIsResentLater<TFrame>(ManagedQuicConnection source,
            ManagedQuicConnection destination) where TFrame : FrameBase
        {
            Lose1RttPacketWithFrame<TFrame>(source);

            CurrentTimestamp += RecoveryController.InitialRtt;
            // ping the destination to elicit ack
            // TODO-RZ: calculate exact delta time needed for the packet to be deemed lost
            Client.Ping();
            // no retransmission yet
            Send1Rtt(source, destination).ShouldNotHaveFrame<TFrame>();

            // receive ack late enough for the original packet be deemed lost.
            CurrentTimestamp += 3 * RecoveryController.InitialRtt;
            Send1RttWithFrame<AckFrame>(destination, source);

            // check that the frame got resent.
            Send1RttWithFrame<TFrame>(source, destination);
        }

        internal void LogFlightPackets(IEnumerable<PacketBase> packets, ManagedQuicConnection sender, bool lost = false)
        {
            LogFlightPackets(new PacketFlight(packets.ToList(), 0, sender, CurrentTimestamp), lost);
        }

        internal void LogFlightPackets(PacketBase packet, ManagedQuicConnection sender, bool lost = false)
        {
            LogFlightPackets(new []{packet}, sender, lost);
        }
    }
}
