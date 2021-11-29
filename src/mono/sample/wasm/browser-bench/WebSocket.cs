// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    // http://localhost:8000/?task=WebSocket
    class WebSocketTask : BenchTask
    {
        private static readonly string DefaultAzureServer = "corefx-net-http11.azurewebsites.net";
        private const string EchoHandler = "WebSocket/EchoWebSocket.ashx";
        private static readonly Uri EchoServer = new Uri("ws://" + DefaultAzureServer + "/" + EchoHandler);

        public override string Name => "WebSocket";
        public override Measurement[] Measurements => measurements;


        Measurement[] measurements;
        public WebSocketTask()
        {
            measurements = new Measurement[] {
                new PartialSend_1BMeasurement(),
                new PartialSend_64KBMeasurement(),
                new PartialSend_1MBMeasurement(),

                new PartialReceive_1BMeasurement(),
                new PartialReceive_10KBMeasurement(),
                new PartialReceive_100KBMeasurement(),
            };
        }

        public abstract class WebSocketMeasurement : BenchTask.Measurement
        {
            protected int step;
            protected ClientWebSocket client;

            public override string Name => GetType().Name.Replace("Measurement", "");

            public override async Task BeforeBatch()
            {
                step = 0;
                client = new ClientWebSocket();
                await client.ConnectAsync(EchoServer, CancellationToken.None);
            }

            public override Task AfterBatch()
            {
                client.Abort();
                client.Dispose();
                return Task.CompletedTask;
            }
        }

        public abstract class WebSocketReceiveMeasurement : WebSocketMeasurement
        {
            protected const int MaxLength = 130_000;
            protected const int MaxMessages = 100;
            public override async Task BeforeBatch()
            {
                await base.BeforeBatch();
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[MaxLength]);

                for (int i = 0; i < MaxLength; i++)
                {
                    buffer[i] = (byte)(i & 0xff);
                }

                for (int i = 0; i < MaxMessages; i++)
                {
                    await client.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
                }

                // make sure that message arrived to receive buffer
                await Task.Delay(5000);
            }
        }

        public class PartialSend_1BMeasurement : WebSocketMeasurement
        {
            public override int InitialSamples => 1000;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1]);

            protected override int CalculateSteps(int milliseconds, TimeSpan initTs)
            {
                return 250_000;
            }
            public override void RunStep()
            {
                buffer[0] = (byte)(step & 0xff);
                client.SendAsync(buffer, WebSocketMessageType.Binary, false, CancellationToken.None);
                step++;
            }
        }

        public class PartialSend_64KBMeasurement : WebSocketMeasurement
        {
            const int bufferSize = 64 * 1024;
            public override int InitialSamples => 1000;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[bufferSize]);
            public PartialSend_64KBMeasurement()
            {
                for (int i = 0; i < bufferSize; i++)
                {
                    buffer[i] = (byte)(i & 0xff);
                }
            }
            protected override int CalculateSteps(int milliseconds, TimeSpan initTs)
            {
                return 3000;
            }

            public override void RunStep()
            {
                buffer[0] = (byte)(step & 0xff);
                client.SendAsync(buffer, WebSocketMessageType.Binary, false, CancellationToken.None);
                step++;
            }
        }

        public class PartialSend_1MBMeasurement : WebSocketMeasurement
        {
            const int bufferSize = 1 * 1024 * 1024;
            public override int InitialSamples => 10;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[bufferSize]);
            public PartialSend_1MBMeasurement()
            {
                for (int i = 0; i < bufferSize; i++)
                {
                    buffer[i] = (byte)(i & 0xff);
                }
            }
            protected override int CalculateSteps(int milliseconds, TimeSpan initTs)
            {
                return 100;
            }

            public override void RunStep()
            {
                buffer[0] = (byte)(step & 0xff);
                client.SendAsync(buffer, WebSocketMessageType.Binary, false, CancellationToken.None);
                step++;
            }
        }

        public class PartialReceive_1BMeasurement : WebSocketReceiveMeasurement
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1]);

            protected override int CalculateSteps(int milliseconds, TimeSpan initTs)
            {
                return MaxLength - InitialSamples - 100;
            }

            public override void RunStep()
            {
                var task = client.ReceiveAsync(buffer, CancellationToken.None);
#if DEBUG
                if (!task.IsCompleted) throw new InvalidOperationException(Name + ": Expected Completed" + step);
                if (task.Result.Count != 1) throw new InvalidOperationException(Name + ": Expected full buffer" + step);
                if (buffer[0] != (byte)(step & 0xff)) throw new InvalidOperationException(Name + ": Expected data" + step);
#endif
                step++;
            }
        }

        public class PartialReceive_10KBMeasurement : WebSocketReceiveMeasurement
        {
            const int bufferSize = 10 * 1024;
            public override int InitialSamples => 1;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[bufferSize]);

            protected override int CalculateSteps(int milliseconds, TimeSpan initTs)
            {
                return 500;
            }

            public override void RunStep()
            {
                var task = client.ReceiveAsync(buffer, CancellationToken.None);
#if DEBUG
                if (!task.IsCompleted) throw new InvalidOperationException(Name + ": Expected Completed " + step);
                if (step == 0)
                {
                    if (task.Result.Count != buffer.Count) throw new InvalidOperationException(Name + ": Expected full buffer" + step);
                    for (int i = 0; i < bufferSize; i++)
                    {
                        if (buffer[i] != (byte)(i & 0xff)) throw new InvalidOperationException(Name + ": Expected data at " + i + " " + step);
                    }
                }
#endif
                step++;
            }
        }

        public class PartialReceive_100KBMeasurement : WebSocketReceiveMeasurement
        {
            const int bufferSize = 100_000;
            public override int InitialSamples => 1;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[bufferSize]);

            protected override int CalculateSteps(int milliseconds, TimeSpan initTs)
            {
                return MaxMessages - 1;
            }

            public override void RunStep()
            {
                if (step == 0)
                {
                    // this is GC step
                    client.ReceiveAsync(new ArraySegment<byte>(new byte[1]), CancellationToken.None);
                    return;
                }
                var task = client.ReceiveAsync(buffer, CancellationToken.None);
#if DEBUG
                if (!task.IsCompleted) throw new InvalidOperationException(Name + ": Expected Completed " + step);
                if (step == 0)
                {
                    if (task.Result.Count != buffer.Count) throw new InvalidOperationException(Name + ": Expected full buffer" + step + " " + task.Result.Count + "!");
                    for (int i = 0; i < bufferSize; i++)
                    {
                        if (buffer[i] != (byte)(i & 0xff)) throw new InvalidOperationException(Name + ": Expected data at " + i + " " + step);
                    }

                }
#endif
                step++;
            }
        }
    }
}
