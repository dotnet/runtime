// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/2391", TestRuntimes.Mono)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
    public class HttpListenerTests
    {
        [Fact]
        public void IgnoreWriteExceptions_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.IgnoreWriteExceptions = false);
        }

        [Fact]
        public void Stop_Disposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.Stop());
        }

        [Fact]
        public void IsListening_NotStarted_ReturnsFalse()
        {
            using (var listener = new HttpListener())
            {
                Assert.False(listener.IsListening);
            }
        }

        [Fact]
        public void IsListening_Disposed_ReturnsFalse()
        {
            var listener = new HttpListener();
            listener.Close();
            Assert.False(listener.IsListening);

            listener.Close();
            Assert.False(listener.IsListening);
        }

        [Fact]
        public void IsListening_Aborted_ReturnsFalse()
        {
            var listener = new HttpListener();
            listener.Abort();
            Assert.False(listener.IsListening);

            listener.Abort();
            Assert.False(listener.IsListening);
        }

        [Fact]
        public void IsListening_Stopped_ReturnsFalse()
        {
            var listener = new HttpListener();
            listener.Stop();
            Assert.False(listener.IsListening);

            listener.Stop();
            Assert.False(listener.IsListening);
        }

        [Fact]
        public void Start_Disposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.Start());
        }

        [Fact]
        public void GetContext_Disposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.GetContext());
            Assert.Throws<ObjectDisposedException>(() => listener.BeginGetContext(null, null));
        }

        [Fact]
        public void GetContext_NotStarted_ThrowsInvalidOperationException()
        {
            using (var listener = new HttpListener())
            {
                Assert.Throws<InvalidOperationException>(() => listener.GetContext());
                Assert.Throws<InvalidOperationException>(() => listener.BeginGetContext(null, null));
            }
        }

        [Fact]
        public void GetContext_NoPrefixes_ThrowsInvalidOperationException()
        {
            using (var listener = new HttpListener())
            {
                listener.Start();
                Assert.Throws<InvalidOperationException>(() => listener.GetContext());
                listener.BeginGetContext(null, null);
            }
        }

        [Fact]
        public void EndGetContext_NullAsyncResult_ThrowsArgumentNullException()
        {
            using (var listener = new HttpListener())
            {
                AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => listener.EndGetContext(null));
            }
        }

        [Fact]
        public void EndGetContext_InvalidAsyncResult_ThrowsArgumentException()
        {
            using (var listener1 = new HttpListener())
            using (var listener2 = new HttpListener())
            {
                listener1.Start();
                listener2.Start();

                IAsyncResult beginGetContextResult = listener1.BeginGetContext(null, null);
                AssertExtensions.Throws<ArgumentException>("asyncResult", () => listener2.EndGetContext(new CustomAsyncResult()));
                AssertExtensions.Throws<ArgumentException>("asyncResult", () => listener2.EndGetContext(beginGetContextResult));
            }
        }

        [Fact]
        public void EndGetContext_AlreadyCalled_ThrowsInvalidOperationException()
        {
            using (var listenerFactory = new HttpListenerFactory())
            using (var client = new HttpClient())
            {
                HttpListener listener = listenerFactory.GetListener();
                listener.Start();

                _ = client.GetStringAsync(listenerFactory.ListeningUrl);

                IAsyncResult beginGetContextResult = listener.BeginGetContext(null, null);
                listener.EndGetContext(beginGetContextResult);

                Assert.Throws<InvalidOperationException>(() => listener.EndGetContext(beginGetContextResult));
            }
        }

        [Fact]
        [OuterLoop]
        public async Task GetContext_StopIsCalled_GetContextUnblocked()
        {
            using var listenerFactory = new HttpListenerFactory();
            var listener = listenerFactory.GetListener();
            listener.Start();
            var listenerTask = Task.Run(() => Assert.Throws<HttpListenerException>(() => listener.GetContext()));
            await Task.Delay(1000); // Wait for listenerTask to call GetContext.
            listener.Stop();
            listener.Close();
            await listenerTask.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }
}
