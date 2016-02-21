// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public class LoopbackServer
    {
        public enum TransferType
        {
            None = 0,
            ContentLength,
            Chunked
        }

        public enum TransferError
        {
            None = 0,
            ContentLengthTooLarge,
            ChunkSizeTooLarge,
            MissingChunkTerminator
        }

        public static Task StartServer(
            TransferType transferType,
            TransferError transferError,
            out IPEndPoint serverEndPoint)
        {
            var server = new TcpListener(IPAddress.Loopback, 0);
            Task serverTask = ((Func<Task>)async delegate {
                server.Start();
                using (var client = await server.AcceptSocketAsync())
                using (var stream = new NetworkStream(client))
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                {
                    // Read past request headers.
                    string line;
                    while (!string.IsNullOrEmpty(line = reader.ReadLine())) ;

                    // Determine response transfer headers.
                    string transferHeader = null;
                    string content = "This is some response content.";
                    if (transferType == TransferType.ContentLength)
                    {
                        if (transferError == TransferError.ContentLengthTooLarge)
                        {
                            transferHeader = $"Content-Length: {content.Length + 42}\r\n";
                        }
                        else
                        {
                            transferHeader = $"Content-Length: {content.Length}\r\n";
                        }
                    }
                    else if (transferType == TransferType.Chunked)
                    {
                        transferHeader = "Transfer-Encoding: chunked\r\n";
                    }

                    // Write response.
                    using (var writer = new StreamWriter(stream, Encoding.ASCII))
                    {
                        writer.Write("HTTP/1.1 200 OK\r\n");
                        writer.Write($"Date: {DateTimeOffset.UtcNow:R}\r\n");
                        writer.Write("Content-Type: text/plain\r\n");

                        if (!string.IsNullOrEmpty(transferHeader))
                        {
                            writer.Write(transferHeader);
                        }

                        writer.Write("\r\n");
                        if (transferType == TransferType.Chunked)
                        {
                            string chunkSizeInHex = string.Format(
                                "{0:x}\r\n",
                                content.Length + (transferError == TransferError.ChunkSizeTooLarge ? 42 : 0));
                            writer.Write(chunkSizeInHex);
                            writer.Write($"{content}\r\n");
                            if (transferError != TransferError.MissingChunkTerminator)
                            {
                                writer.Write("0\r\n\r\n");
                            }
                        }
                        else
                        {
                            writer.Write($"{content}\r\n");
                        }
                        writer.Flush();
                    }

                    client.Shutdown(SocketShutdown.Both);
                }
            })();

            serverEndPoint = (IPEndPoint)server.LocalEndpoint;
            return serverTask;
        }
    }
}
