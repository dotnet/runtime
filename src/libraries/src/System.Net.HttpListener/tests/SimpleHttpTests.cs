// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class SimpleHttpTests : IDisposable
    {
        private HttpListenerFactory _factory;
        private HttpListener _listener;

        public SimpleHttpTests()
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
        }

        public void Dispose() => _factory.Dispose();

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public static void Supported_True()
        {
            Assert.True(HttpListener.IsSupported);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
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

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
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

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
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

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void BasicTest_StopNoStart_NoException()
        {
            HttpListener listener = new HttpListener();
            listener.Stop();
            listener.Close();
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void BasicTest_CloseNoStart_NoException()
        {
            HttpListener listener = new HttpListener();
            listener.Close();
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void BasicTest_AbortNoStart_NoException()
        {
            HttpListener listener = new HttpListener();
            listener.Abort();
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
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

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task RequestTransferEncoding_MixedCaseChunked_Success()
        {
            Task<HttpListenerContext> serverContext = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = true;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Z"));

                HttpListenerContext context = await serverContext;
                Assert.Equal(-1, context.Request.ContentLength64);
                Assert.Equal("chunked", context.Request.Headers["Transfer-Encoding"]);
                byte[] buffer = new byte[10];
                Assert.Equal(1, context.Request.InputStream.Read(buffer, 0, buffer.Length));
                Assert.Equal((byte)'Z', buffer[0]);
                context.Response.Close();

                using (HttpResponseMessage response = await clientTask)
                {
                    Assert.Equal(200, (int)response.StatusCode);
                }
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task UnknownHeaders_Success()
        {
            Task<HttpListenerContext> server = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.ConnectionClose = true;

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, _factory.ListeningUrl);

                for (int i = 0; i < 1000; i++)
                {
                    requestMessage.Headers.Add($"custom{i}", i.ToString());
                }

                Task<HttpResponseMessage> clientTask = client.SendAsync(requestMessage);
                HttpListenerContext context = await server;
                
                for (int i = 0; i < 1000; i++)
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
        
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task SimpleRequest_Succeeds()
        {
            const string expectedResponse = "hello from HttpListener";
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await serverContextTask;
                using (var response = serverContext.Response)
                {
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                    response.ContentLength64 = responseBuffer.Length;

                    using (var output = response.OutputStream)
                    {
                        await output.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    }
                }

                var clientString = await clientTask;

                Assert.Equal(expectedResponse, clientString);
            }
        }
    }
}
