// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Net.Http.Tests
{
    public sealed class SocketsHttpHandlerPerfTest
    {
        const int InnerIterationCount = 1000;

        public static IEnumerable<object[]> Get_MemberData() =>
            from ssl in new[] { false, true }
            from chunkedResponse in new[] { false, true }
            from responseLength in new[] { 1, 100_000 }
            select new object[] { ssl, chunkedResponse, responseLength };

        [Benchmark(InnerIterationCount = InnerIterationCount)]
        [MemberData(nameof(Get_MemberData))]
        public async Task Get(bool ssl, bool chunkedResponse, int responseLength)
        {
            using (var serverCert = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate())
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                #region Server
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(int.MaxValue);
                string responseText =
                    "HTTP/1.1 200 OK\r\n" + (chunkedResponse ?
                    $"Transfer-Encoding: chunked\r\n\r\n{responseLength.ToString("X")}\r\n{new string('a', responseLength)}\r\n0\r\n\r\n" :
                    $"Content-Length: {responseLength}\r\n\r\n{new string('a', responseLength)}");
                ReadOnlyMemory<byte> responseBytes = Encoding.UTF8.GetBytes(responseText);

                Task serverTask = Task.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            using (Socket s = await listener.AcceptAsync())
                            {
                                try
                                {
                                    Stream stream = new NetworkStream(s);
                                    if (ssl)
                                    {
                                        var sslStream = new SslStream(stream, false, delegate { return true; });
                                        await sslStream.AuthenticateAsServerAsync(serverCert, false, SslProtocols.None, false);
                                        stream = sslStream;
                                    }

                                    using (var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 100))
                                    {
                                        while (true)
                                        {
                                            while (!string.IsNullOrEmpty(await reader.ReadLineAsync()));
                                            await stream.WriteAsync(responseBytes);
                                        }
                                    }
                                }
                                catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionAborted) { }
                            }
                        }
                    }
                    catch { }
                });
                #endregion

                var ep = (IPEndPoint)listener.LocalEndPoint;
                var uri = new Uri($"http{(ssl?"s":"")}://{ep.Address}:{ep.Port}/");
                using (var handler = new SocketsHttpHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    if (ssl)
                    {
                        handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
                    }

                    var req = new HttpRequestMessage(HttpMethod.Get, uri);

                    foreach (BenchmarkIteration iteration in Benchmark.Iterations)
                    {
                        using (iteration.StartMeasurement())
                        {
                            for (int i = 0; i < InnerIterationCount; i++)
                            {
                                using (HttpResponseMessage resp = await invoker.SendAsync(req, CancellationToken.None))
                                using (Stream respStream = await resp.Content.ReadAsStreamAsync())
                                {
                                    await respStream.CopyToAsync(Stream.Null);
                                }
                            }
                        }
                    }
                }

                listener.Dispose();
                await serverTask;
            }
        }
    }
}
