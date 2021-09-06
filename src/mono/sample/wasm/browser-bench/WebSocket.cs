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
                new ShortPartialSendMeasurement(),
                new LongPartialSendMeasurement(),
                new ShortPartialReceiveMeasurement(),
                new LongPartialReceiveMeasurement(),
            };
        }

        public abstract class WebSocketMeasurement : BenchTask.Measurement
        {
            protected ClientWebSocket client;
            public WebSocketMeasurement()
            {
            }
            public override async Task BeforeBatch()
            {
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
            public override async Task BeforeBatch()
            {
                await base.BeforeBatch();
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[64_000]);
                await client.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);

                // make sure that message arrived to receive buffer
                await Task.Delay(1000);
            }
        }

        public class ShortPartialSendMeasurement : WebSocketMeasurement
        {
            public override string Name => "ShortPartialSend";
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2]);

            public override void RunStep()
            {
                for (int i = 0; i < 30000; i++)
                {
                    buffer[0] = (byte)(i & 0xff);
                    client.SendAsync(buffer, WebSocketMessageType.Binary, false, CancellationToken.None);
                }
            }
        }

        public class LongPartialSendMeasurement : WebSocketMeasurement
        {
            public override string Name => "LongPartialSend";
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[64_000]);

            public override void RunStep()
            {
                for (int i = 0; i < 30000; i++)
                {
                    buffer[0] = (byte)(i & 0xff);
                }
                buffer[60_000] = 255;
                client.SendAsync(buffer, WebSocketMessageType.Binary, false, CancellationToken.None);
            }
        }

        public class ShortPartialReceiveMeasurement : WebSocketReceiveMeasurement
        {
            public override string Name => "ShortPartialReceive";
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2]);

            public override void RunStep()
            {
                for (int i = 0; i < 30000; i++)
                {
                    var task = client.ReceiveAsync(buffer, CancellationToken.None);
#if DEBUG
                    if (!task.IsCompleted) throw new InvalidOperationException("Expected Completed");
                    if (task.Result.Count != 2) throw new InvalidOperationException("Expected full buffer");
                    if (buffer[0] != (byte)(i & 0xff)) throw new InvalidOperationException("Expected data");
#endif
                }
            }
        }

        public class LongPartialReceiveMeasurement : WebSocketReceiveMeasurement
        {
            public override string Name => "LongPartialReceive";
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[30_0000]);

            public override void RunStep()
            {
                var task = client.ReceiveAsync(buffer, CancellationToken.None);
#if DEBUG
                if (!task.IsCompleted) throw new InvalidOperationException("Expected Completed");
                if (task.Result.Count != 30_0000) throw new InvalidOperationException("Expected full buffer");

                for (int i = 0; i < 30000; i++)
                {
                    if (buffer[0] != (byte)(i & 0xff)) throw new InvalidOperationException("Expected data");
                }
#endif
            }
        }

    }
}
