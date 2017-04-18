// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class HttpListenerTests
    {
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void IgnoreWriteExceptions_SetDisposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.IgnoreWriteExceptions = false);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void Stop_Disposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.Stop());
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void IsListening_NotStarted_ReturnsFalse()
        {
            using (var listener = new HttpListener())
            {
                Assert.False(listener.IsListening);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void IsListening_Disposed_ReturnsFalse()
        {
            var listener = new HttpListener();
            listener.Close();
            Assert.False(listener.IsListening);

            listener.Close();
            Assert.False(listener.IsListening);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void IsListening_Aborted_ReturnsFalse()
        {
            var listener = new HttpListener();
            listener.Abort();
            Assert.False(listener.IsListening);

            listener.Abort();
            Assert.False(listener.IsListening);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void IsListening_Stopped_ReturnsFalse()
        {
            var listener = new HttpListener();
            listener.Stop();
            Assert.False(listener.IsListening);

            listener.Stop();
            Assert.False(listener.IsListening);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void Start_Disposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.Start());
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void GetContext_Disposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.GetContext());
            Assert.Throws<ObjectDisposedException>(() => listener.BeginGetContext(null, null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void GetContext_NotStarted_ThrowsInvalidOperationException()
        {
            using (var listener = new HttpListener())
            {
                Assert.Throws<InvalidOperationException>(() => listener.GetContext());
                Assert.Throws<InvalidOperationException>(() => listener.BeginGetContext(null, null));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void GetContext_NoPrefixes_ThrowsInvalidOperationException()
        {
            using (var listener = new HttpListener())
            {
                listener.Start();
                Assert.Throws<InvalidOperationException>(() => listener.GetContext());
                listener.BeginGetContext(null, null);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void EndGetContext_NullAsyncResult_ThrowsArgumentNullException()
        {
            using (var listener = new HttpListener())
            {
                Assert.Throws<ArgumentNullException>("asyncResult", () => listener.EndGetContext(null));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [ActiveIssue(18128, platforms: TestPlatforms.AnyUnix)] // No validation performed - hangs forever.
        public void EndGetContext_InvalidAsyncResult_ThrowsArgumentException()
        {
            using (var listener1 = new HttpListener())
            using (var listener2 = new HttpListener())
            {
                listener1.Start();
                listener2.Start();

                IAsyncResult beginGetContextResult = listener1.BeginGetContext(null, null);
                Assert.Throws<ArgumentException>("asyncResult", () => listener2.EndGetContext(new CustomAsyncResult()));
                Assert.Throws<ArgumentException>("asyncResult", () => listener2.EndGetContext(beginGetContextResult));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public void EndGetContext_AlreadyCalled_ThrowsInvalidOperationException()
        {
            using (var listenerFactory = new HttpListenerFactory())
            using (var client = new HttpClient())
            {
                HttpListener listener = listenerFactory.GetListener();
                listener.Start();

                Task<string> clientTask = client.GetStringAsync(listenerFactory.ListeningUrl);

                IAsyncResult beginGetContextResult = listener.BeginGetContext(null, null);
                listener.EndGetContext(beginGetContextResult);

                Assert.Throws<InvalidOperationException>(() => listener.EndGetContext(beginGetContextResult));
            }
        }
    }
}
