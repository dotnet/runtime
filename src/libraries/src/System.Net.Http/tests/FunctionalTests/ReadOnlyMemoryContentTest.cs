// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class ReadOnlyMemoryContentTest
    {
        public static IEnumerable<object[]> ContentLengthsAndUseArrays()
        {
            foreach (int length in new[] { 0, 1, 4096 })
            {
                foreach (bool useArray in new[] { true, false })
                {
                    yield return new object[] { length, useArray };
                }
            }
        }

        public static IEnumerable<object[]> TrueFalse()
        {
            yield return new object[] { true };
            yield return new object[] { false };
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArrays))]
        public void ContentLength_LengthMatchesArrayLength(int contentLength, bool useArray)
        {
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out memory, out ownedMemory);

            Assert.Equal(contentLength, content.Headers.ContentLength);

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(TrueFalse))]
        public async Task ReadAsStreamAsync_TrivialMembersHaveExpectedValuesAndBehavior(bool useArray)
        {
            const int ContentLength = 42;
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out memory, out ownedMemory);

            using (Stream stream = await content.ReadAsStreamAsync())
            {
                // property values
                Assert.Equal(ContentLength, stream.Length);
                Assert.Equal(0, stream.Position);
                Assert.True(stream.CanRead);
                Assert.True(stream.CanSeek);
                Assert.False(stream.CanWrite);

                // not supported
                Assert.Throws<NotSupportedException>(() => stream.SetLength(12345));
                Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
                Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
                Assert.Throws<NotSupportedException>(() => stream.Write(new ReadOnlySpan<byte>(new byte[1])));
                await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(new byte[1], 0, 1));
                await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])));

                // nops
                stream.Flush();
                await stream.FlushAsync();
            }

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(TrueFalse))]
        public async Task ReadAsStreamAsync_Seek(bool useArray)
        {
            const int ContentLength = 42;
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out memory, out ownedMemory);

            using (Stream s = await content.ReadAsStreamAsync())
            {
                foreach (int pos in new[] { 0, ContentLength / 2, ContentLength - 1 })
                {
                    s.Position = pos;
                    Assert.Equal(pos, s.Position);
                    Assert.Equal(memory.Span[pos], s.ReadByte());
                }

                foreach (int pos in new[] { 0, ContentLength / 2, ContentLength - 1 })
                {
                    Assert.Equal(0, s.Seek(0, SeekOrigin.Begin));
                    Assert.Equal(memory.Span[0], s.ReadByte());
                }

                Assert.Equal(ContentLength, s.Seek(0, SeekOrigin.End));
                Assert.Equal(s.Position, s.Length);
                Assert.Equal(-1, s.ReadByte());

                Assert.Equal(0, s.Seek(-ContentLength, SeekOrigin.End));
                Assert.Equal(0, s.Position);
                Assert.Equal(memory.Span[0], s.ReadByte());

                s.Position = 0;
                Assert.Equal(0, s.Seek(0, SeekOrigin.Current));
                Assert.Equal(0, s.Position);

                Assert.Equal(1, s.Seek(1, SeekOrigin.Current));
                Assert.Equal(1, s.Position);
                Assert.Equal(memory.Span[1], s.ReadByte());
                Assert.Equal(2, s.Position);
                Assert.Equal(3, s.Seek(1, SeekOrigin.Current));
                Assert.Equal(1, s.Seek(-2, SeekOrigin.Current));

                Assert.Equal(int.MaxValue, s.Seek(int.MaxValue, SeekOrigin.Begin));
                Assert.Equal(int.MaxValue, s.Position);
                Assert.Equal(int.MaxValue, s.Seek(0, SeekOrigin.Current));
                Assert.Equal(int.MaxValue, s.Position);
                Assert.Equal(int.MaxValue, s.Seek(int.MaxValue - ContentLength, SeekOrigin.End));
                Assert.Equal(int.MaxValue, s.Position);
                Assert.Equal(-1, s.ReadByte());
                Assert.Equal(int.MaxValue, s.Position);

                Assert.Throws<ArgumentOutOfRangeException>("value", () => s.Position = -1);
                Assert.Throws<IOException>(() => s.Seek(-1, SeekOrigin.Begin));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => s.Position = (long)int.MaxValue + 1);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => s.Seek((long)int.MaxValue + 1, SeekOrigin.Begin));

                Assert.ThrowsAny<ArgumentException>(() => s.Seek(0, (SeekOrigin)42));
            }

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArrays))]
        public async Task ReadAsStreamAsync_ReadByte_MatchesInput(int contentLength, bool useArray)
        {
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out memory, out ownedMemory);

            using (Stream stream = await content.ReadAsStreamAsync())
            {
                for (int i = 0; i < contentLength; i++)
                {
                    Assert.Equal(memory.Span[i], stream.ReadByte());
                    Assert.Equal(i + 1, stream.Position);
                }
                Assert.Equal(-1, stream.ReadByte());
                Assert.Equal(stream.Length, stream.Position);
            }

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(TrueFalse))]
        public async Task ReadAsStreamAsync_Read_InvalidArguments(bool useArray)
        {
            const int ContentLength = 42;
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out memory, out ownedMemory);

            using (Stream stream = await content.ReadAsStreamAsync())
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => stream.Read(null, 0, 0));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => { stream.ReadAsync(null, 0, 0); });

                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => stream.Read(new byte[1], -1, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => stream.Read(new byte[1], -1, 1));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => stream.Read(new byte[1], 0, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => stream.Read(new byte[1], 0, -1));

                Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(new byte[1], 2, 0); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(new byte[1], 2, 0); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(new byte[1], 0, 2); });
                Assert.ThrowsAny<ArgumentException>(() => { stream.ReadAsync(new byte[1], 0, 2); });
            }

            ownedMemory?.Dispose();
        }

        [Theory]
        [InlineData(0, false)] // Read(byte[], ...)
        [InlineData(1, false)] // Read(Span<byte>, ...)
        [InlineData(2, false)] // ReadAsync(byte[], ...)
        [InlineData(3, false)] // ReadAsync(Memory<byte>,...)
        [InlineData(4, false)] // Begin/EndRead(byte[],...)
        [InlineData(0, true)] // Read(byte[], ...)
        [InlineData(1, true)] // Read(Span<byte>, ...)
        [InlineData(2, true)] // ReadAsync(byte[], ...)
        [InlineData(3, true)] // ReadAsync(Memory<byte>,...)
        [InlineData(4, true)] // Begin/EndRead(byte[],...)
        public async Task ReadAsStreamAsync_ReadMultipleBytes_MatchesInput(int mode, bool useArray)
        {
            const int ContentLength = 1024;

            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out memory, out ownedMemory);

            var buffer = new byte[3];

            using (Stream stream = await content.ReadAsStreamAsync())
            {
                for (int i = 0; i < ContentLength; i += buffer.Length)
                {
                    int bytesRead =
                        mode == 0 ? stream.Read(buffer, 0, buffer.Length) :
                        mode == 1 ? stream.Read(new Span<byte>(buffer)) :
                        mode == 2 ? await stream.ReadAsync(buffer, 0, buffer.Length) :
                        mode == 3 ? await stream.ReadAsync(new Memory<byte>(buffer)) :
                        await Task.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, 0, buffer.Length, null);

                    Assert.Equal(Math.Min(buffer.Length, ContentLength - i), bytesRead);
                    for (int j = 0; j < bytesRead; j++)
                    {
                        Assert.Equal(memory.Span[i + j], buffer[j]);
                    }

                    Assert.Equal(i + bytesRead, stream.Position);
                }

                Assert.Equal(0,
                    mode == 0 ? stream.Read(buffer, 0, buffer.Length) :
                    mode == 1 ? stream.Read(new Span<byte>(buffer)) :
                    mode == 2 ? await stream.ReadAsync(buffer, 0, buffer.Length) :
                    mode == 3 ? await stream.ReadAsync(new Memory<byte>(buffer)) :
                    await Task.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, 0, buffer.Length, null));
            }

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(TrueFalse))]
        public async Task ReadAsStreamAsync_ReadWithCancelableToken_MatchesInput(bool useArray)
        {
            const int ContentLength = 100;

            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out memory, out ownedMemory);

            var buffer = new byte[1];
            var cts = new CancellationTokenSource();
            int bytesRead;

            using (Stream stream = await content.ReadAsStreamAsync())
            {
                for (int i = 0; i < ContentLength; i++)
                {
                    switch (i % 2)
                    {
                        case 0:
                            bytesRead = await stream.ReadAsync(buffer, 0, 1, cts.Token);
                            break;
                        default:
                            bytesRead = await stream.ReadAsync(new Memory<byte>(buffer), cts.Token);
                            break;
                    }
                    Assert.Equal(1, bytesRead);
                    Assert.Equal(memory.Span[i], buffer[0]);
                }
            }

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(TrueFalse))]
        public async Task ReadAsStreamAsync_ReadWithCanceledToken_MatchesInput(bool useArray)
        {
            const int ContentLength = 2;

            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out memory, out ownedMemory);

            using (Stream stream = await content.ReadAsStreamAsync())
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.ReadAsync(new byte[1], 0, 1, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await stream.ReadAsync(new Memory<byte>(new byte[1]), new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await stream.CopyToAsync(new MemoryStream(), 1, new CancellationToken(true)));
            }

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArrays))]
        public async Task CopyToAsync_AllContentCopied(int contentLength, bool useArray)
        {
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out memory, out ownedMemory);

            var destination = new MemoryStream();
            await content.CopyToAsync(destination);

            Assert.Equal<byte>(memory.ToArray(), destination.ToArray());

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArrays))]
        public async Task ReadAsStreamAsync_CopyTo_AllContentCopied(int contentLength, bool useArray)
        {
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out memory, out ownedMemory);

            var destination = new MemoryStream();
            using (Stream s = await content.ReadAsStreamAsync())
            {
                s.CopyTo(destination);
            }

            Assert.Equal<byte>(memory.ToArray(), destination.ToArray());

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(TrueFalse))]
        public async Task ReadAsStreamAsync_CopyTo_InvalidArguments(bool useArray)
        {
            const int ContentLength = 42;
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out memory, out ownedMemory);

            using (Stream s = await content.ReadAsStreamAsync())
            {
                AssertExtensions.Throws<ArgumentNullException>("destination", () => s.CopyTo(null));
                AssertExtensions.Throws<ArgumentNullException>("destination", () => { s.CopyToAsync(null); });

                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => s.CopyTo(new MemoryStream(), 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { s.CopyToAsync(new MemoryStream(), 0); });

                Assert.Throws<NotSupportedException>(() => s.CopyTo(new MemoryStream(new byte[1], writable:false)));
                Assert.Throws<NotSupportedException>(() => { s.CopyToAsync(new MemoryStream(new byte[1], writable: false)); });

                var disposedDestination = new MemoryStream();
                disposedDestination.Dispose();
                Assert.Throws<ObjectDisposedException>(() => s.CopyTo(disposedDestination));
                Assert.Throws<ObjectDisposedException>(() => { s.CopyToAsync(disposedDestination); });
            }

            ownedMemory?.Dispose();
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArrays))]
        public async Task ReadAsStreamAsync_CopyToAsync_AllContentCopied(int contentLength, bool useArray)
        {
            Memory<byte> memory;
            OwnedMemory<byte> ownedMemory;
            ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out memory, out ownedMemory);

            var destination = new MemoryStream();
            using (Stream s = await content.ReadAsStreamAsync())
            {
                await s.CopyToAsync(destination);
            }

            Assert.Equal<byte>(memory.ToArray(), destination.ToArray());

            ownedMemory?.Dispose();
        }

        private static ReadOnlyMemoryContent CreateContent(int contentLength, bool useArray, out Memory<byte> memory, out OwnedMemory<byte> ownedMemory)
        {
            if (useArray)
            {
                memory = new byte[contentLength];
                ownedMemory = null;
            }
            else
            {
                ownedMemory = new NativeOwnedMemory(contentLength);
                memory = ownedMemory.Memory;
            }

            new Random(contentLength).NextBytes(memory.Span);

            return new ReadOnlyMemoryContent(memory);
        }
    }
}
