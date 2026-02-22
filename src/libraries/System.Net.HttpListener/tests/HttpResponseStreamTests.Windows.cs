// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using static System.Net.Tests.HttpListenerTimeoutManagerWindowsTests;

namespace System.Net.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
    public class HttpResponseStreamWindowsTests : IDisposable
    {
        private HttpListenerFactory _factory;
        private HttpListener _listener;
        private GetContextHelper _helper;

        public HttpResponseStreamWindowsTests()
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
            _helper = new GetContextHelper(_listener, _factory.ListeningUrl);
        }

        public void Dispose()
        {
            _factory.Dispose();
            _helper.Dispose();
        }

        [Fact] // [ActiveIssue("https://github.com/dotnet/runtime/issues/21918", TestPlatforms.AnyUnix)]
        public async Task Write_TooMuch_ThrowsProtocolViolationException()
        {
            using (HttpClient client = new HttpClient())
            {
                _ = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;
                    byte[] responseBuffer = "A long string"u8.ToArray();
                    response.ContentLength64 = responseBuffer.Length - 1;
                    try
                    {
                        Assert.Throws<ProtocolViolationException>(() => output.Write(responseBuffer, 0, responseBuffer.Length));
                        await Assert.ThrowsAsync<ProtocolViolationException>(() => output.WriteAsync(responseBuffer, 0, responseBuffer.Length));
                    }
                    finally
                    {
                        // Write the remaining bytes to guarantee a successful shutdown.
                        output.Write(responseBuffer, 0, (int)response.ContentLength64);
                        output.Close();
                    }
                }
            }
        }

        [Fact] // [ActiveIssue("https://github.com/dotnet/runtime/issues/21918", TestPlatforms.AnyUnix)]
        public async Task Write_TooLittleAsynchronouslyAndClose_ThrowsInvalidOperationException()
        {
            using (HttpClient client = new HttpClient())
            {
                _ = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;

                    byte[] responseBuffer = "A long string"u8.ToArray();
                    response.ContentLength64 = responseBuffer.Length + 1;

                    // Throws when there are bytes left to write
                    await output.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    Assert.Throws<InvalidOperationException>(() => output.Close());

                    // Write the final byte and make sure we can close.
                    await output.WriteAsync(new byte[1], 0, 1);
                    output.Close();
                }
            }
        }

        [Fact] // [ActiveIssue("https://github.com/dotnet/runtime/issues/21918", TestPlatforms.AnyUnix)]
        public async Task Write_TooLittleSynchronouslyAndClose_ThrowsInvalidOperationException()
        {
            using (HttpClient client = new HttpClient())
            {
                _ = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;

                    byte[] responseBuffer = "A long string"u8.ToArray();
                    response.ContentLength64 = responseBuffer.Length + 1;

                    // Throws when there are bytes left to write
                    output.Write(responseBuffer, 0, responseBuffer.Length);
                    Assert.Throws<InvalidOperationException>(() => output.Close());

                    // Write the final byte and make sure we can close.
                    output.Write(new byte[1], 0, 1);
                    output.Close();
                }
            }
        }

        // Windows only test as Unix implementation uses Socket.Begin/EndSend, which doesn't fail in this case
        [Fact]
        public async Task EndWrite_InvalidAsyncResult_ThrowsArgumentException()
        {
            using (HttpListenerResponse response1 = await _helper.GetResponse())
            using (Stream outputStream1 = response1.OutputStream)
            using (HttpListenerResponse response2 = await _helper.GetResponse())
            using (Stream outputStream2 = response2.OutputStream)
            {
                IAsyncResult beginWriteResult = outputStream1.BeginWrite(new byte[0], 0, 0, null, null);

                AssertExtensions.Throws<ArgumentException>("asyncResult", () => outputStream2.EndWrite(new CustomAsyncResult()));
                AssertExtensions.Throws<ArgumentException>("asyncResult", () => outputStream2.EndWrite(beginWriteResult));
            }
        }

        // Windows only test as Unix implementation uses Socket.Begin/EndSend, which doesn't fail in this case
        [Fact]
        public async Task EndWrite_CalledTwice_ThrowsInvalidOperationException()
        {
            using (HttpListenerResponse response1 = await _helper.GetResponse())
            using (Stream outputStream = response1.OutputStream)
            {
                IAsyncResult beginWriteResult = outputStream.BeginWrite(new byte[0], 0, 0, null, null);
                outputStream.EndWrite(beginWriteResult);

                Assert.Throws<InvalidOperationException>(() => outputStream.EndWrite(beginWriteResult));
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void KernelResponseBufferingEnabled_WriteAsynchronouslyInParts_SetsFlagAndSucceeds()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AppContext.SetSwitch("System.Net.HttpListener.EnableKernelResponseBuffering", true);

                using (var listenerFactory = new HttpListenerFactory())
                {
                    HttpListener listener = listenerFactory.GetListener();

                    const string expectedResponse = "hello from HttpListener";
                    Task<HttpListenerContext> serverContextTask = listener.GetContextAsync();

                    using (HttpClient client = new HttpClient())
                    {
                        Task<string> clientTask = client.GetStringAsync(listenerFactory.ListeningUrl);

                        HttpListenerContext serverContext = await serverContextTask;
                        using (HttpListenerResponse response = serverContext.Response)
                        {
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                            response.ContentLength64 = responseBuffer.Length;

                            using (Stream outputStream = response.OutputStream)
                            {
                                AssertBufferDataFlagIsSet(outputStream);

                                await outputStream.WriteAsync(responseBuffer, 0, 5);
                                await outputStream.WriteAsync(responseBuffer, 5, responseBuffer.Length - 5);
                            }
                        }

                        var clientString = await clientTask;

                        Assert.Equal(expectedResponse, clientString);
                    }
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void KernelResponseBufferingEnabled_WriteSynchronouslyInParts_SetsFlagAndSucceeds()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AppContext.SetSwitch("System.Net.HttpListener.EnableKernelResponseBuffering", true);
                using (var listenerFactory = new HttpListenerFactory())
                {
                    HttpListener listener = listenerFactory.GetListener();

                    const string expectedResponse = "hello from HttpListener";
                    Task<HttpListenerContext> serverContextTask = listener.GetContextAsync();

                    using (HttpClient client = new HttpClient())
                    {
                        Task<string> clientTask = client.GetStringAsync(listenerFactory.ListeningUrl);

                        HttpListenerContext serverContext = await serverContextTask;
                        using (HttpListenerResponse response = serverContext.Response)
                        {
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                            response.ContentLength64 = responseBuffer.Length;

                            using (Stream outputStream = response.OutputStream)
                            {
                                AssertBufferDataFlagIsSet(outputStream);

                                outputStream.Write(responseBuffer, 0, 5);
                                outputStream.Write(responseBuffer, 5, responseBuffer.Length - 5);
                            }
                        }

                        var clientString = await clientTask;

                        Assert.Equal(expectedResponse, clientString);
                    }
                }
            }).Dispose();
        }

        private static void AssertBufferDataFlagIsSet(Stream outputStream)
        {
            MethodInfo compute =
                outputStream.GetType().GetMethod("ComputeLeftToWrite", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            Assert.NotNull(compute);

            object flagsObj = compute.Invoke(outputStream, parameters: null);
            Assert.NotNull(flagsObj);

            Assert.True(flagsObj is Enum, "Expected ComputeLeftToWrite to return an enum");

            Enum flagsEnum = (Enum)flagsObj;

            Type enumType = flagsEnum.GetType();
            Enum bufferDataEnum =
                (Enum)Enum.Parse(enumType, nameof(HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA), ignoreCase: false);

            Assert.True(flagsEnum.HasFlag(bufferDataEnum));
        }
    }
}
