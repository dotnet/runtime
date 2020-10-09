// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class RobustnessTests : ManualTransmissionQuicTestBase
    {
        public RobustnessTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(2, 100)]
        [InlineData(4, 50)]
        public void EstablishConnectionInGreatLossyEnvironment(int seed, int rtt)
        {
            // no reordering yet
            Queue<PacketFlight> flights = new Queue<PacketFlight>();
            Random rand = new Random(seed);
            long rttTime = Timestamp.FromMilliseconds(rtt);

            // start the process
            flights.Enqueue(GetFlightToSend(Client));

            void CollectPackets(ManagedQuicConnection sender)
            {
                // TODO-RZ: this is safe only when no data are sent, otherwise number of packets is not limited
                do
                {
                    var response = GetFlightToSend(sender);

                    if (response.UdpDatagramSize == 0)
                    {
                        break;
                    }

                    flights.Enqueue(response);
                } while (true);
            }

            for (int i = 0; i < 1000; i++)
            {
                if (Client.Connected && Server.Connected)
                    break;

                var timeoutConnection = Client.GetNextTimerTimestamp() < Server.GetNextTimerTimestamp()
                    ? Client
                    : Server;

                Assert.False(flights.Count == 0 && timeoutConnection.GetNextTimerTimestamp() == long.MaxValue &&
                             !Client.Connected && !Server.Connected,
                    "Deadlock reached");

                Output.WriteLine($"Event {i}:");
                if (flights.Count > 0 && flights.Peek().TimeSent + rttTime < timeoutConnection.GetNextTimerTimestamp())
                {
                    var flight = flights.Dequeue();

                    // decide whether packet arrives
                    if (rand.NextDouble() < 0.5)
                    {
                        CurrentTimestamp = flight.TimeSent + rttTime;
                        var receiver = flight.Sender == Client ? Server : Client;
                        SendFlight(flight.Sender, receiver, flight.Packets);

                        // record any responses
                        CollectPackets(receiver);
                    }
                    else
                    {
                        LogFlightPackets(flight, true);
                    }
                }
                else if (timeoutConnection.GetNextTimerTimestamp() < long.MaxValue)
                {
                    CurrentTimestamp = timeoutConnection.GetNextTimerTimestamp();
                    timeoutConnection.OnTimeout(CurrentTimestamp);

                    // maxValue here would lead to deadlock during connection establishment
                    CollectPackets(timeoutConnection);
                }
                else
                {
                    break;
                }
            }

            Assert.True(Client.Connected && Server.Connected);
        }
    }
}
