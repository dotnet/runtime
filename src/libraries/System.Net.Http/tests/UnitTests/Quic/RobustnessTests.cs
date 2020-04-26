using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class RobustnessTests
    {
        private readonly ITestOutputHelper output;

        private readonly ManagedQuicConnection _client;
        private readonly ManagedQuicConnection _server;

        private readonly TestHarness _harness;

        public RobustnessTests(ITestOutputHelper output)
        {
            this.output = output;

            // TODO-RZ: remove console logging for tests
            Console.SetOut(new XUnitTextWriter(output));

            _client = TestHarness.CreateClient(new QuicClientConnectionOptions());
            _server = TestHarness.CreateServer(new QuicListenerOptions
            {
                CertificateFilePath = TestHarness.CertificateFilePath,
                PrivateKeyFilePath = TestHarness.PrivateKeyFilePath
            });

            _harness = new TestHarness(output, _client);
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(2, 100)]
        [InlineData(3, 50)]
        public void EstablishConnectionInGreatLossyEnvironment(int seed, int rtt)
        {
            // no reordering yet
            Queue<PacketFlight> flights = new Queue<PacketFlight>();
            Random rand = new Random(seed);
            long rttTime = Timestamp.FromMilliseconds(rtt);

            // start the process
            flights.Enqueue(_harness.GetFlightToSend(_client));

            void CollectPackets(ManagedQuicConnection sender)
            {
                // TODO-RZ: this is safe only when no data are sent, otherwise number of packets is not limited
                do
                {
                    var response = _harness.GetFlightToSend(sender);

                    if (response.UdpDatagramSize == 0)
                    {
                        break;
                    }

                    flights.Enqueue(response);
                } while (true);
            }

            for (int i = 0; i < 1000; i++)
            {
                if (_client.Connected && _server.Connected)
                    break;

                var timeoutConnection = _client.GetNextTimerTimestamp() < _server.GetNextTimerTimestamp()
                    ? _client
                    : _server;

                Assert.False(flights.Count == 0 && timeoutConnection.GetNextTimerTimestamp() == long.MaxValue &&
                    !_client.Connected && !_server.Connected,
                    "Deadlock reached");

                output.WriteLine($"Event {i}:");
                if (flights.Count > 0 && flights.Peek().TimeSent + rttTime < timeoutConnection.GetNextTimerTimestamp())
                {
                    var flight = flights.Dequeue();

                    // decide whether packet arrives
                    if (rand.NextDouble() < 0.5)
                    {
                        _harness.Timestamp = flight.TimeSent + rttTime;
                        var receiver = flight.Sender == _client ? _server : _client;
                        _harness.SendFlight(flight.Sender, receiver, flight.Packets);

                        // record any responses
                        CollectPackets(receiver);
                    }
                    else
                    {
                        _harness.LogFlightPackets(flight, true);
                    }
                }
                else if (timeoutConnection.GetNextTimerTimestamp() < long.MaxValue)
                {
                    _harness.Timestamp = timeoutConnection.GetNextTimerTimestamp();

                    // maxValue here would lead to deadlock during connection establishment
                    CollectPackets(timeoutConnection);
                }
                else
                {
                    break;
                }
            }

            Assert.True(_client.Connected && _server.Connected);
        }
    }
}
