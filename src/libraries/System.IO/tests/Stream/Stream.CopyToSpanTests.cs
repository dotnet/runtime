// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamCopyToSpanTests
    {
        [Fact]
        public void CopyTo_InvalidArgsThrows()
        {
            using Stream s = new MemoryStream();

            AssertExtensions.Throws<ArgumentNullException>("callback", () => s.CopyTo(null, null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => s.CopyTo((_, __) => { }, null, 0));

            AssertExtensions.Throws<ArgumentNullException>("callback", () => s.CopyToAsync(null, null, 0, default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => s.CopyToAsync((_, __, ___) => new ValueTask(Task.CompletedTask), null, 0, default));
        }

        [Fact]
        public void CopyToAsync_PrecanceledToken_Cancels()
        {
            using var src = new MemoryStream();
            Assert.Equal(TaskStatus.Canceled, src.CopyToAsync((_, __, ___) => new ValueTask(Task.CompletedTask), null, 4096, new CancellationToken(true)).Status);
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public async Task CopyToAsync_CancellationToken_Propagated(MemoryStream input)
        {
            using var src = input;
            src.WriteByte(0);
            src.Position = 0;

            var cancellationToken = new CancellationToken();
            await src.CopyToAsync(
                (_, __, token) => new ValueTask(Task.Run(() => Assert.Equal(cancellationToken, token))),
                null,
                4096,
                cancellationToken
            );
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public async Task CopyToAsync_State_Propagated(MemoryStream input)
        {
            using var src = input;
            src.WriteByte(0);
            src.Position = 0;

            var expected = 42;
            await src.CopyToAsync(
                (_, state, __) => new ValueTask(Task.Run(() => Assert.Equal(expected, state))),
                expected,
                4096,
                default
            );
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public void CopyTo_AllDataCopied(MemoryStream input)
        {
            using var src = input;
            src.Write(Enumerable.Range(0, 10000).Select(i => (byte)i).ToArray(), 0, 256);
            src.Position = 0;

            using var dst = new MemoryStream();
            src.CopyTo((span, _) => dst.Write(span), null, 4096);

            Assert.Equal<byte>(src.ToArray(), dst.ToArray());
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public async Task CopyToAsync_AllDataCopied(MemoryStream input)
        {
            using var src = input;
            src.Write(Enumerable.Range(0, 10000).Select(i => (byte)i).ToArray(), 0, 256);
            src.Position = 0;

            using var dst = new MemoryStream();
            await src.CopyToAsync((memory, _, ___) => dst.WriteAsync(memory), null, 4096, default);

            Assert.Equal<byte>(src.ToArray(), dst.ToArray());
        }

        private sealed class CustomMemoryStream : MemoryStream
        {
            private readonly bool _spanCopy;
            private readonly bool _sync;

            public CustomMemoryStream(bool spanCopy, bool sync)
            : base()
            {
                _spanCopy = spanCopy;
                _sync = sync;
            }

            public override void CopyTo(Stream destination, int bufferSize)
            {
                if (_sync)
                {
                    CopyToInternal(destination, bufferSize);
                }
                else
                {
                    CopyToAsyncInternal(destination, bufferSize, CancellationToken.None).GetAwaiter().GetResult();
                }
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (!_sync)
                {
                    return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
                }
                else
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        CopyToInternal(destination, bufferSize);
                        return Task.CompletedTask;
                    }
                    catch (Exception e)
                    {
                        return Task.FromException(e);
                    }
                }
            }

            private void CopyToInternal(Stream destination, int bufferSize)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    int read;
                    while ((read = Read(buffer, 0, buffer.Length)) != 0)
                    {
                        if (_spanCopy)
                            destination.Write(new ReadOnlySpan<byte>(buffer, 0, read));
                        else
                            destination.Write(buffer, 0, read);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            private async Task CopyToAsyncInternal(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    while (true)
                    {
                        int bytesRead = await ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                        if (bytesRead == 0) break;
                        if (_spanCopy)
                            await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                        else
                            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        public static IEnumerable<object[]> CopyTo_TestData()
        {
            foreach (var sync in new[] { false, true })
                foreach (var spanCopy in new[] { false, true })
                    yield return new object[] { new CustomMemoryStream(spanCopy, sync) };

            yield return new object[] { new MemoryStream() };
        }
    }
}
