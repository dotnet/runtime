// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            _ = _client.GetStringAsync(_listeningUrl);
            HttpListenerContext context = await _listener.GetContextAsync();
            return context.Response;
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

    public static class Helpers
    {
        public static bool IsWindowsImplementation { get; } =
            (TypeExists("Interop+HttpApi") || TypeExists("System.Net.UnsafeNclNativeMethods")); // types only in Windows netcoreapp/netfx builds, respectively

        public static bool IsManagedImplementation => TypeExists("System.Net.WebSockets.ManagedWebSocket"); // type only in managed build

        private static bool TypeExists(string name) => typeof(HttpListener).Assembly.GetType(name, throwOnError: false, ignoreCase: false) != null;

        public static void WaitForSocketShutdown(Socket socket)
        {
            socket.Close();
        }

        public static bool SocketConnected(Socket socket)
        {
            return !(socket.Poll(100, SelectMode.SelectRead) && socket.Available == 0);
        }
    }
}
