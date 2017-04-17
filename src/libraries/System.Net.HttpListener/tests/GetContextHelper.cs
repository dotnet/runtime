// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Tests
{
    public class GetContextHelper : IDisposable
    {
        private HttpClient _client;
        private HttpListener _listener;
        private string _listeningUrl;

        public GetContextHelper(HttpListener listener, string listeningUrl)
        {
            _listener = listener;
            _listeningUrl = listeningUrl;
            _client = new HttpClient();
        }

        public async Task<HttpListenerResponse> GetResponse()
        {
            // We need to create a mock request to give the HttpListener a context.
            Task<string> clientTask = _client.GetStringAsync(_listeningUrl);
            HttpListenerContext context = await _listener.GetContextAsync();
            return context.Response;
        }

        public async Task<HttpListenerRequest> GetRequest(bool chunked)
        {
            // We need to create a mock request to give the HttpListener a context.
            Task<HttpListenerContext> serverContext = _listener.GetContextAsync();

            _client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
            Task<HttpResponseMessage> clientTask = _client.PostAsync(_listeningUrl, new StringContent("Hello"));

            HttpListenerContext context = await serverContext;
            return context.Request;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }

    public class CustomAsyncResult : IAsyncResult
    {
        public object AsyncState => throw new NotImplementedException();
        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();
        public bool CompletedSynchronously => throw new NotImplementedException();
        public bool IsCompleted => throw new NotImplementedException();
    }

    public class Helpers
    {
        public static void WaitForSocketShutdown(Socket socket)
        {
            while (SocketConnected(socket));
        }

        public static bool SocketConnected(Socket socket)
        {
            return !(socket.Poll(100, SelectMode.SelectRead) && socket.Available == 0);
        }
    }
}
