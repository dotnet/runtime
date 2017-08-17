// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpProtocolTests
    {
        public virtual Stream GetStream(Stream s)
        {
            return s;
        }

        private bool GetHeaders(byte[] buffer, int length)
        {
            int offset = 0;
            while (true)
            {
                int found = Array.IndexOf(buffer, (byte)'\r', offset, length - offset);
                if (found < 0) return false;

                if (found + 3 < length)
                {
                    if (buffer[found + 1] == (byte)'\n' &&
                        buffer[found + 2] == (byte)'\r' &&
                        buffer[found + 3] == (byte)'\n')
                    {
                        return true;
                    }
                }

                offset = found + 1;
            }
        }
        private Uri CreateServer(string response)
        {
            Socket listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listen.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listen.Listen(1);

            listen.AcceptAsync().ContinueWith(async t =>
            {
                Socket accept = t.Result;
                accept.NoDelay = true;
                Stream s = GetStream(new NetworkStream(accept));

                // Read whatever request the client sent us
                // Note, we assume there's no content
                byte[] buffer = new byte[4096];
                int length = 0;
                while (true)
                {
                    int bytesRead = await s.ReadAsync(buffer, length, buffer.Length - length);
                    if (bytesRead == 0)
                        throw new InvalidOperationException($"unexpected end of stream, buffer={Encoding.UTF8.GetString(buffer, 0, length)}");

                    length += bytesRead;

                    if (GetHeaders(buffer, length))
                        break;
                }

                // Send the specified response
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await s.WriteAsync(responseBytes, 0, responseBytes.Length);
                accept.Close();
                listen.Close();
            }, TaskContinuationOptions.ExecuteSynchronously);

            IPEndPoint ep = (IPEndPoint)listen.LocalEndPoint;
            return new Uri($"http://{ep.Address}:{ep.Port}/");
        }

        [Theory]
        [InlineData(200, "OK")]
        [InlineData(200, "Sure why not?")]
        [InlineData(200, "")]
        [InlineData(200, " ")]
        [InlineData(200, "      ")]
        [InlineData(200, "      Something")]
        [InlineData(200, "\t")]
        [InlineData(201, "Created")]
        [InlineData(202, "Accepted")]
        [InlineData(299, "This is not a real status code")]
        [InlineData(345, "redirect to nowhere")]
        [InlineData(400, "Bad Request")]
        [InlineData(500, "Internal Server Error")]
        [InlineData(600, "still valid")]
        [InlineData(999, "this\ttoo\t")]
        public async Task ValidStatusLine(int statusCode, string reason)
        {
            using (HttpClient client = new HttpClient())
            {
                string responseString = $"HTTP/1.1 {statusCode} {reason}\r\nContent-Length: 0\r\n\r\n";
                Uri serverUri = CreateServer(responseString);
                HttpResponseMessage response = await client.GetAsync(serverUri);
                Assert.Equal(statusCode, (int)response.StatusCode);
                Assert.Equal(reason, response.ReasonPhrase);
            }
        }

        [Theory]
        [InlineData("NOTHTTP/1.1")]
        [InlineData("HTTP/1.1\r\n")]
        [InlineData("HTTP/1.1 \r\n")]
        [InlineData("HTTP/1.1  ")]
        [InlineData("HTTP/1.1 200\r\n")]    // need trailing space
        [InlineData("HTTP/1.1\t200 OK")]
        [InlineData("HTTP/1.1 200\tOK")]
        [InlineData("HTTP/1.1 200OK")]
        [InlineData("HTTP/1.1 2345")]
        [InlineData("HTTP/1.1 23\r\n")]
        [InlineData("HTTP/1.1 abc")]
        [InlineData("HTTP/1.1 2bc")]
        [InlineData("HTTP/1.1 20c")]
        [InlineData("HTTP/1.1 a11")]
        [InlineData("HTTP/1.1 !11")]
        [InlineData("HTTP/3.5 200 OK")]
        [InlineData("HTTP/0.1 200 OK")]
        [InlineData("HTTP/1.12 200 OK")]
        [InlineData("HTTP/12.1 200 OK")]
        public void InvalidStatusLine(string responseString)
        {
            using (HttpClient client = new HttpClient())
            {
                Uri serverUri = CreateServer(responseString);
                Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(serverUri));
            }
        }
    }

    public sealed class ManagedHandler_HttpProtocolTests : HttpProtocolTests, IDisposable
    {
        public ManagedHandler_HttpProtocolTests() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_HttpProtocolTests_Dribble : HttpProtocolTests, IDisposable
    {
        public override Stream GetStream(Stream s)
        {
            return new DribbleStream(s);
        }

        public ManagedHandler_HttpProtocolTests_Dribble() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public class DribbleStream : Stream
    {
        private readonly Stream _wrapped;

        public DribbleStream(Stream wrapped)
        {
            _wrapped = wrapped;
        }

        public override bool CanRead => throw new NotImplementedException();
        public override bool CanSeek => throw new NotImplementedException();
        public override bool CanWrite => throw new NotImplementedException();
        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override void Flush() => throw new NotImplementedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _wrapped.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var b = new byte[1];
            for (int i = offset; i < offset + count; i++)
            {
                b[0] = buffer[i];
                await _wrapped.WriteAsync(b, 0, 1);
                await Task.Delay(10);
            }
        }
    }
}
