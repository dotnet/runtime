// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

using Microsoft.DotNet.XUnitExtensions;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/2391", TestRuntimes.Mono)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
    public class SimpleHttpTests : IDisposable
    {
        private HttpListenerFactory _factory;
        private HttpListener _listener;
        private readonly ITestOutputHelper _output;

        public SimpleHttpTests(ITestOutputHelper output)
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
            _output = output;
        }

        public void Dispose() => _factory.Dispose();

        [Fact]
        public static void Supported_True()
        {
            Assert.True(HttpListener.IsSupported);
        }

        [Fact]
        public void BasicTest_StartStop_NoException()
        {
            HttpListener listener = new HttpListener();
            try
            {
                listener.Start();
                listener.Stop();
                listener.Close();
            }
            finally
            {
                listener.Close();
            }
        }

        [Fact]
        public void BasicTest_StartCloseAbort_NoException()
        {
            HttpListener listener = new HttpListener();
            try
            {
                listener.Start();
                listener.Close();
                listener.Abort();
            }
            finally
            {
                listener.Close();
            }
        }

        [Fact]
        public void BasicTest_StartAbortClose_NoException()
        {
            HttpListener listener = new HttpListener();
            try
            {
                listener.Start();
                listener.Abort();
                listener.Close();
            }
            finally
            {
                listener.Close();
            }
        }

        [Fact]
        public void BasicTest_StopNoStart_NoException()
        {
            HttpListener listener = new HttpListener();
            listener.Stop();
            listener.Close();
        }

        [Fact]
        public void BasicTest_CloseNoStart_NoException()
        {
            HttpListener listener = new HttpListener();
            listener.Close();
        }

        [Fact]
        public void BasicTest_AbortNoStart_NoException()
        {
            HttpListener listener = new HttpListener();
            listener.Abort();
        }

        [Fact]
        public void BasicTest_StartThrowsAbortCalledInFinally_AbortDoesntThrow()
        {
            HttpListener listener = new HttpListener();
            // try to add an invalid prefix (invalid port number)
            listener.Prefixes.Add("http://doesntexist:99999999/");
            try
            {
                Assert.Throws<HttpListenerException>(() => listener.Start());
            }
            finally
            {
                // even though listener wasn't started (state is 'Closed'), Abort() must not throw.
                listener.Abort();

                // calling Abort() again should still not throw.
                listener.Abort();
            }
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task UnknownHeaders_Success(int numHeaders)
        {
            Task<HttpListenerContext> server = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.ConnectionClose = true;

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, _factory.ListeningUrl);

                for (int i = 0; i < numHeaders; i++)
                {
                    requestMessage.Headers.Add($"custom{i}", i.ToString());
                }

                Task<HttpResponseMessage> clientTask = client.SendAsync(requestMessage);

                if (clientTask == await Task.WhenAny(server, clientTask))
                {
                    (await clientTask).EnsureSuccessStatusCode();
                    Assert.Fail("Client should not have completed prior to server sending response");
                }

                HttpListenerContext context = await server;

                for (int i = 0; i < numHeaders; i++)
                {
                    Assert.Equal(i.ToString(), context.Request.Headers[$"custom{i}"]);
                }

                context.Response.Close();

                using (HttpResponseMessage response = await clientTask)
                {
                    Assert.Equal(200, (int)response.StatusCode);
                }
            }
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ListenerRestart_Success(bool sync)
        {
            const string Content = "ListenerRestart_GetContext_Success";
            using (HttpListenerFactory factory = new HttpListenerFactory())
            using (HttpClient client = new HttpClient())
            {
                HttpListener listener = factory.GetListener();

                _output.WriteLine("Connecting to {0}", factory.ListeningUrl);
                Task<string> clientTask = client.GetStringAsync(factory.ListeningUrl);

                HttpListenerContext context = sync
                    ? listener.GetContext()
                    : listener.EndGetContext(listener.BeginGetContext(ar => { }, null));

                HttpListenerResponse response = context.Response;
                response.OutputStream.Write(Encoding.UTF8.GetBytes(Content));
                response.OutputStream.Close();

                string body = await clientTask;
                Assert.Equal(Content, body);

                // Stop and start listener again.
                listener.Stop();
                try
                {
                    // This may fail if something else took our port while restarting.
                    listener.Start();
                }
                catch (HttpListenerException e)
                {
                    _output.WriteLine(e.Message);
                    // Skip test if we lost race and we are unable to bind on same port again.
                    throw new SkipTestException("Unable to restart listener");
                }

                _output.WriteLine("Connecting to {0} after restart", factory.ListeningUrl);

                // Repeat request to be sure listener is working.
                clientTask = client.GetStringAsync(factory.ListeningUrl);

                context = sync
                    ? listener.GetContext()
                    : listener.EndGetContext(listener.BeginGetContext(ar => { }, null));

                response = context.Response;
                response.OutputStream.Write(Encoding.UTF8.GetBytes(Content));
                response.OutputStream.Close();

                body = await clientTask;
                Assert.Equal(Content, body);
            }
        }
    }
}
