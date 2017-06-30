// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
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

        [ActiveIssue(19754)] // Recombine into UnknownHeaders_Success when fixed
        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public Task UnknownHeaders_Success_Large() => UnknownHeaders_Success(1000);

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(100)]
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
                    Assert.True(false, "Client should not have completed prior to server sending response");
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
    }
}
