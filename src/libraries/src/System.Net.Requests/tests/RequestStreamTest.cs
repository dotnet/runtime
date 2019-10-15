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

        #region Write

        [Fact]
        public async Task Write_BufferIsNull_ThrowsArgumentNullException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => stream.Write(null, 0, 1));
            });
        }

        [Fact]
        public async Task Write_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => stream.Write(buffer, -1, buffer.Length));
            });
        }

        [Fact]
        public async Task Write_CountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", "size", () => stream.Write(buffer, 0, -1));
            });
        }

        [Fact]
        public async Task Write_OffsetPlusCountExceedsBufferLength_ThrowsArgumentException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", "size", () => stream.Write(buffer, 0, buffer.Length + 1));
            });
        }

        [Fact]
        public async Task Write_OffsetPlusCountMaxValueExceedsBufferLength_Throws()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => stream.Write(buffer, int.MaxValue, int.MaxValue));
            });
        }

#endregion

        #region WriteAsync

        [Fact]
        public async Task WriteAsync_BufferIsNull_ThrowsArgumentNullException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => { Task t = stream.WriteAsync(null, 0, 1); });
            });
        }

        [Fact]
        public async Task WriteAsync_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => { Task t = stream.WriteAsync(buffer, -1, buffer.Length); });
            });
        }

        [Fact]
        public async Task WriteAsync_CountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", "size", () => { Task t = stream.WriteAsync(buffer, 0, -1); });
            });
        }

        [Fact]
        public async Task WriteAsync_OffsetPlusCountExceedsBufferLength_ThrowsArgumentException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", "size", () => { Task t = stream.WriteAsync(buffer, 0, buffer.Length + 1); });
            });
        }

        [Fact]
        public async Task WriteAsync_OffsetPlusCountMaxValueExceedsBufferLength_Throws()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => { Task t = stream.WriteAsync(buffer, int.MaxValue, int.MaxValue); });
            });
        }

        [Fact]
        public async Task WriteAsync_ValidParameters_TaskRanToCompletion()
        {
            await GetRequestStream((stream) =>
            {
                Task t = stream.WriteAsync(buffer, 0, buffer.Length);
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
            });
        }

        [Fact]
        public async Task WriteAsync_TokenIsCanceled_TaskIsCanceled()
        {
            await GetRequestStream((stream) =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Task t = stream.WriteAsync(buffer, 0, buffer.Length, cts.Token);
                Assert.True(t.IsCanceled);
            });
        }

        #endregion

        #region BeginWrite

        [Fact]
        public async Task BeginWriteAsync_BufferIsNull_ThrowsArgumentNullException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => stream.BeginWrite(null, 0, 1, null, null));
            });
        }

        [Fact]
        public async Task BeginWriteAsync_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => stream.BeginWrite(buffer, -1, buffer.Length, null, null));
            });
        }

        [Fact]
        public async Task BeginWriteAsync_CountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", "size", () => stream.BeginWrite(buffer, 0, -1, null, null));
            });
        }

        [Fact]
        public async Task BeginWriteAsync_OffsetPlusCountExceedsBufferLength_ThrowsArgumentException()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", "size", () => stream.BeginWrite(buffer, 0, buffer.Length + 1, null, null));
            });
        }

        [Fact]
        public async Task BeginWriteAsync_OffsetPlusCountMaxValueExceedsBufferLength_Throws()
        {
            await GetRequestStream((stream) =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => stream.BeginWrite(buffer, int.MaxValue, int.MaxValue, null, null));
            });
        }

        [Fact]
        public async Task BeginWriteAsync_ValidParameters_TaskRanToCompletion()
        {
            await GetRequestStream((stream) =>
            {
                object state = new object();
                IAsyncResult result = stream.BeginWrite(buffer, 0, buffer.Length, null, state);
                stream.EndWrite(result);
                Assert.True(result.IsCompleted);
            });
        }

        #endregion

        [Fact]
        public async Task FlushAsync_TaskRanToCompletion()
        {
            await GetRequestStream((stream) =>
            {
                Task t = stream.FlushAsync();
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
            });
        }

        [Fact]
        public async Task FlushAsync_TokenIsCanceled_TaskIsCanceled()
        {
            await GetRequestStream((stream) =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Task t = stream.FlushAsync(cts.Token);
                Assert.True(t.IsCanceled);
            });
        }

        private async Task GetRequestStream(Action<Stream> requestAction)
        {
            await LoopbackServer.CreateServerAsync((server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "POST";
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestAction(requestStream);
                }

                return Task.FromResult<object>(null);
            });
        }
    }
}
