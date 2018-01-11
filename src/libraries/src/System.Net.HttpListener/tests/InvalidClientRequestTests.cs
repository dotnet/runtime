// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class InvalidClientRequestTests : IDisposable
    {
        public HttpListenerFactory Factory { get; }

        public InvalidClientRequestTests()
        {
            Factory = new HttpListenerFactory();
        }

        public void Dispose()
        {
            Factory?.Dispose();
        }

        public static IEnumerable<object[]> InvalidRequest_TestData()
        {
            // Request line is invalid
            yield return new object[] { "NoSpaces", null, null, null, "Bad Request" };
            yield return new object[] { "Two Spaces", null, null, null, "Bad Request" };
            yield return new object[] { "Lots Of Extra Spaces", null, null, null, "Bad Request" };

            // Version is invalid
            yield return new object[] { "GET {path} http/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "GET {path} a\0bc/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "GET {path} 12345678", null, null, null, "Bad Request" };
            yield return new object[] { "GET {path} HTTP/0.1", null, null, null, "Bad Request" };
            yield return new object[] { "GET {path} HTTP/1.1.1", null, null, null, "Bad Request" };
            yield return new object[] { "GET {path} HTTP/aaa", null, null, null, "Bad Request" };

            yield return new object[] { "GET {path} HTTP/2.2", null, null, null, "Version Not Supported" };
            yield return new object[] { "GET {path} HTTP/2.0", null, null, null, "Version Not Supported" };
            yield return new object[] { "GET {path} HTTP/3.0", null, null, null, "Version Not Supported" };

            // Invalid verb
            yield return new object[] { "( {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { ") {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "< {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "> {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "@ {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { ", {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "; {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { ": {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "\\ {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "\" {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "/ {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "[ {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "] {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "? {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "= {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "{ {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "} {path} HTTP/1.1", null, null, null, "Bad Request" };

            yield return new object[] { "\0 {path} HTTP/1.1", null, null, null, "Bad Request" };
            yield return new object[] { "\u1234 {path} HTTP/1.1", null, null, null, "Bad Request" };

            // Invalid header
            yield return new object[] { "GET {path} HTTP/1.1", null, new string[] { "Content-Length: -10" }, "\r\n", "Bad Request" };
            yield return new object[] { "POST {path} HTTP/1.1", null, new string[] { "Content-Length: -10" }, "\r\n", "Bad Request" };
            yield return new object[] { "POST {path} HTTP/1.1", null, new string[] { "Content-Length: -10" }, "\r\n", "Bad Request" };
            yield return new object[] { "GET {path} HTTP/1.1", null, new string[] { "Content-Length: -9223372036854775809" }, "\r\n", "Bad Request" };

            yield return new object[] { "GET {path} HTTP/1.1", null, new string[] { "Content-Length: 1", "Content-Length: 2" }, "\r\n", "Bad Request" };

            yield return new object[] { "GET {path} HTTP/1.1", null, new string[] { "Transfer-Encoding: garbage" }, "\r\n", "Not Implemented" };
            yield return new object[] { "POST {path} HTTP/1.1", null, new string[] { "Transfer-Encoding: garbage" }, "\r\n", "Not Implemented" };

            yield return new object[] { "POST {path} HTTP/1.1", null, new string[] { "Transfer-Encoding: chunked", "Transfer-Encoding: chunked" }, "\r\n", "Not Implemented" };

            yield return new object[] { "GET {path} HTTP/1.1", null, new string[] { "NoValue" }, null, "Bad Request" };
            yield return new object[] { "GET {path} HTTP/1.1", null, new string[] { ":" }, null, "Bad Request" };
            yield return new object[] { "GET {path} HTTP/1.1", null, new string[] { "\0:value" }, null, "Bad Request" };
            yield return new object[] { "GET {path} HTTP/1.1", null, new string[] { "value:\0" }, null, "Bad Request" };

            yield return new object[] { "GET {path} HTTP/1.1", "", null, null, "Bad Request" };
            yield return new object[] { "GET {path} HTTP/1.1", "Host: \r\n", null, null, "Bad Request" };

            yield return new object[] { "GET /something{path} HTTP/1.1", null, null, null, "Not Found" };
            yield return new object[] { "GET {path}../ HTTP/1.1", null, null, null, "Not Found" };

            // No body
            yield return new object[] { "POST {path} HTTP/1.1", null, null, null, "Length Required" };
            yield return new object[] { "PUT {path} HTTP/1.1", null, null, null, "Length Required" };
        }

        [Fact]
        public async Task GetContext_InvalidRequest_DoesNotGetContext()
        {
            // These tests take upwards of 20s if this uses [Theory].
            foreach (object[] testData in InvalidRequest_TestData())
            {
                using (Socket client = Factory.GetConnectedSocket())
                {
                    string requestLine = (string)testData[0];
                    string host = (string)testData[1];
                    IEnumerable<string> headers = (IEnumerable<string>)testData[2];
                    string content = (string)testData[3];
                    string expectedMessage = (string)testData[4];

                    Uri listeningUri = new Uri(Factory.ListeningUrl);
                    string requestLineWithPathAndQuery = requestLine.Replace("{path}", listeningUri.PathAndQuery);

                    host = host ?? $"Host: {listeningUri.Host}\r\n";
                    string fullRequest = $"{requestLineWithPathAndQuery}\r\n{host}{string.Join("\r\n", headers ?? new string[0])}{content}\r\n";

                    Task<HttpListenerContext> serverTask = Factory.GetListener().GetContextAsync();
                    client.Send(Encoding.Default.GetBytes(fullRequest));

                    byte[] errorMessageBytes = new byte[512];
                    Task<int> clientTask = Task.Run(() => client.Receive(errorMessageBytes));

                    Task completedTask = await Task.WhenAny(clientTask, serverTask);

                    // Ignore the specific error message - just make sure that this failed.
                    Assert.Same(clientTask, completedTask);
                    string errorMessage = Encoding.Default.GetString(errorMessageBytes, 0, clientTask.Result);
                    Assert.Contains(expectedMessage, errorMessage);

                    Assert.False(serverTask.IsCompleted, $"Server task was completed: {serverTask.Status}");
                }
            }
        }
    }
}
