// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Security.Authentication;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class SchSendAuxRecordHttpTest : HttpClientHandlerTestBase
    {
        public SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) { }

        private class CircularBuffer
        {
            public CircularBuffer(int size) => _buffer = new char[size];

            private char[] _buffer;
            private int _lastBytesWriteIndex = 0;
            private int _size = 0;

            public void Add(string value)
            {
                foreach (char ch in value)
                {
                    _buffer[_lastBytesWriteIndex] = ch;

                    _lastBytesWriteIndex = ++_lastBytesWriteIndex % _buffer.Length;
                    _size = Math.Min(_buffer.Length, ++_size);
                }
            }

            public bool Equals(string value)
            {
                if (value.Length != _size)
                    return false;

                for (int i = 0; i < _size; i++)
                {
                    if (_buffer[(_lastBytesWriteIndex + i) % _buffer.Length] != value[i])
                        return false;
                }

                return true;
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task HttpClient_ClientUsesAuxRecord_Ok()
        {
            var options = new HttpsTestServer.Options();
            options.AllowedProtocols = SslProtocols.Tls;

            using (var server = new HttpsTestServer(options))
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                server.Start();

                var tasks = new Task[2];

                bool serverAuxRecordDetected = false;
                bool serverAuxRecordDetectedInconclusive = false;
                int serverTotalBytesReceived = 0;
                int serverChunks = 0;

                CircularBuffer buffer = new CircularBuffer(4);

                tasks[0] = server.AcceptHttpsClientAsync((requestString) =>
                {

                    buffer.Add(requestString);

                    serverTotalBytesReceived += requestString.Length;

                    if (serverTotalBytesReceived == 1 && serverChunks == 0)
                    {
                        serverAuxRecordDetected = true;
                    }

                    serverChunks++;

                    // Test is inconclusive if any non-CBC cipher is used:
                    if (server.Stream.CipherAlgorithm == CipherAlgorithmType.None ||
                        server.Stream.CipherAlgorithm == CipherAlgorithmType.Null ||
                        server.Stream.CipherAlgorithm == CipherAlgorithmType.Rc4)
                    {
                        serverAuxRecordDetectedInconclusive = true;
                    }

                    // Detect end of HTML request
                    if (buffer.Equals("\r\n\r\n"))
                    {
                        return Task.FromResult(HttpsTestServer.Options.DefaultResponseString);
                    }
                    else
                    {
                        return Task.FromResult<string>(null);
                    }
                });

                string requestUriString = "https://localhost:" + server.Port.ToString();
                tasks[1] = client.GetStringAsync(requestUriString);

                await tasks.WhenAllOrAnyFailed(15 * 1000);

                if (serverAuxRecordDetectedInconclusive)
                {
                    _output.WriteLine("Test inconclusive: The Operating system preferred a non-CBC or Null cipher.");
                }
                else
                {
                    Assert.True(serverAuxRecordDetected, "Server reports: Client auxiliary record not detected.");
                }
            }
        }
    }
}
