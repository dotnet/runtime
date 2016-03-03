// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    /// <summary>
    /// Provides a test-only, overly-simplified HTTP proxy for a single GET request.  It is not meant to be robust,
    /// but simply for verification that a GET request is in fact getting routed through a designated proxy
    /// and providing credentials if expected.
    /// </summary>
    internal sealed class LoopbackGetRequestHttpProxy
    {
        public struct ProxyResult
        {
            public byte[] ResponseContent;
            public string AuthenticationHeaderValue;
        }

        public static Task<ProxyResult> StartAsync(out int port, bool requireAuth)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return StartAsync(listener, requireAuth);
        }

        private static async Task<ProxyResult> StartAsync(TcpListener listener, bool requireAuth)
        {
            ProxyResult result = new ProxyResult();
            var headers = new Dictionary<string, string>();
            Socket clientSocket = null;
            Stream clientStream = null;
            StreamReader clientReader = null;
            string url = null;
            try
            {
                // Get and parse the incoming request
                Func<Task> getAndReadRequest = async () => {
                    clientSocket = await listener.AcceptSocketAsync().ConfigureAwait(false);
                    clientStream = new NetworkStream(clientSocket, ownsSocket: false);
                    clientReader = new StreamReader(clientStream, Encoding.ASCII);
                    headers.Clear();

                    url = clientReader.ReadLine().Split(' ')[1];
                    string line;
                    while (!string.IsNullOrEmpty(line = clientReader.ReadLine()))
                    {
                        string[] headerParts = line.Split(':');
                        headers.Add(headerParts[0].Trim(), headerParts[1].Trim());
                    }
                };
                await getAndReadRequest().ConfigureAwait(false);

                // If we're expecting credentials, look for them, and if we didn't get them, send back 
                // a response and process a new request.
                if (requireAuth && !headers.ContainsKey("Proxy-Authorization"))
                {
                    Task<Socket> secondListen = listener.AcceptSocketAsync();

                    // Send back a 407
                    await clientSocket.SendAsync(
                        new ArraySegment<byte>(Encoding.ASCII.GetBytes("HTTP/1.1 407 Proxy Auth Required\r\nProxy-Authenticate: Basic\r\n\r\n")),
                        SocketFlags.None).ConfigureAwait(false);
                    clientSocket.Shutdown(SocketShutdown.Send);
                    clientSocket.Dispose();

                    // Wait for a new connection that should have an auth header this time and parse it
                    await getAndReadRequest().ConfigureAwait(false);
                }

                // Store any auth header we may have for later comparison
                string authValue;
                if (headers.TryGetValue("Proxy-Authorization", out authValue))
                {
                    result.AuthenticationHeaderValue = Encoding.UTF8.GetString(Convert.FromBase64String(authValue.Substring("Basic ".Length)));
                }

                // Forward the request to the server
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers) request.Headers.Add(header.Key, header.Value);
                using (HttpClient outboundClient = new HttpClient())
                using (HttpResponseMessage response = await outboundClient.SendAsync(request).ConfigureAwait(false))
                {
                    // Transfer the response headers from the server to the client
                    var sb = new StringBuilder($"HTTP/{response.Version.ToString(2)} {(int)response.StatusCode} {response.ReasonPhrase}\r\n");
                    foreach (var header in response.Headers.Concat(response.Content.Headers))
                    {
                        sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");
                    }
                    sb.Append("\r\n");
                    byte[] headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
                    await clientStream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);

                    // Forward the content from the server, both to the client and to a memory stream which we'll use
                    // to return the data from the proxy.
                    var resultBody = new MemoryStream();
                    using (Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        byte[] buffer = new byte[0x1000];
                        int bytesRead = 0;
                        while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            await clientStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                            resultBody.Write(buffer, 0, bytesRead);
                        }
                    }

                    // Return the result
                    result.ResponseContent = resultBody.ToArray();
                    return result;
                }
            }
            finally
            {
                clientSocket.Dispose();
                listener.Stop();
            }
        }
    }
}
