// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Tests
{
    public class RequestStreamTest
    {
        readonly byte[] buffer = new byte[1];
        
        [Fact]
        public void WriteAsync_BufferIsNull_ThrowsArgumentNullException()
        {
            using (Stream stream = GetRequestStream())
            {
                Assert.Throws<ArgumentNullException>(() => { Task t = stream.WriteAsync(null, 0, 1); });
            }
        }

        [Fact]
        public void WriteAsync_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            using (Stream stream = GetRequestStream())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { Task t = stream.WriteAsync(buffer, -1, buffer.Length); });
            }
        }

        [Fact]
        public void WriteAsync_CountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            using (Stream stream = GetRequestStream())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { Task t = stream.WriteAsync(buffer, 0, -1); });
            }
        }

        [Fact]
        public void WriteAsync_OffsetPlusCountExceedsBufferLength_ThrowsArgumentException()
        {
            using (Stream stream = GetRequestStream())
            {
                Assert.Throws<ArgumentException>(() => { Task t = stream.WriteAsync(buffer, 0, buffer.Length+1); });
            }
        }

        [Fact]
        public void WriteAsync_OffsetPlusCountMaxValueExceedsBufferLength_ThrowsArgumentException()
        {
            using (Stream stream = GetRequestStream())
            {
                Assert.Throws<ArgumentException>(() => { Task t = stream.WriteAsync(buffer, int.MaxValue, int.MaxValue); });
            }
        }

        [Fact]
        public void WriteAsync_ValidParameters_TaskRanToCompletion()
        {
            using (Stream stream = GetRequestStream())
            {
                Task t = stream.WriteAsync(buffer, 0, buffer.Length);
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
            }
        }

        [Fact]
        public void WriteAsync_TokenIsCanceled_TaskIsCanceled()
        {
            using (Stream stream = GetRequestStream())
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Task t = stream.WriteAsync(buffer, 0, buffer.Length, cts.Token);
                Assert.True(t.IsCanceled);
            }
        }

        [Fact]
        public void FlushAsync_TaskRanToCompletion()
        {
            using (Stream stream = GetRequestStream())
            {
                Task t = stream.FlushAsync();
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
            }
        }

        [Fact]
        public void FlushAsync_TokenIsCanceled_TaskIsCanceled()
        {
            using (Stream stream = GetRequestStream())
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Task t = stream.FlushAsync(cts.Token);
                Assert.True(t.IsCanceled);
            }
        }

        private Stream GetRequestStream()
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(HttpTestServers.RemoteEchoServer);
            request.Method = "POST";
            return request.GetRequestStreamAsync().GetAwaiter().GetResult();
        }
    }
}
