// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Tests;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class ReadOnlyMemoryContentTest : StandaloneStreamConformanceTests
    {
        protected override Task<Stream> CreateReadOnlyStreamCore(byte[]? initialData) => new ReadOnlyMemoryContent(initialData).ReadAsStreamAsync();
        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream>(null);
        protected override Task<Stream> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream>(null);
        protected override bool CanSetLength => false;

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

        public static IEnumerable<object[]> ContentLengthsAndUseArraysAndReadStreamAsync()
        {
            foreach (int length in new[] { 0, 1, 4096 })
            {
                foreach (bool useArray in new[] { true, false })
                {
                    foreach (bool readStreamAsync in new[] { true, false })
                    {
                        yield return new object[] { length, useArray, readStreamAsync };
                    }
                }
            }
        }

        public static IEnumerable<object[]> UseArrays()
        {
            yield return new object[] { true };
            yield return new object[] { false };
        }

        public static IEnumerable<object[]> UseArraysAndReadStreamAsync()
        {
            yield return new object[] { true, true };
            yield return new object[] { true, false };
            yield return new object[] { false, true };
            yield return new object[] { false, false };
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArrays))]
        public void ContentLength_LengthMatchesArrayLength(int contentLength, bool useArray)
        {
            using (ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out _, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            {
                Assert.Equal(contentLength, content.Headers.ContentLength);
            }
        }

        [Theory]
        [MemberData(nameof(UseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_TrivialMembersHaveExpectedValuesAndBehavior(bool useArray, bool readStreamAsync)
        {
            const int ContentLength = 42;
            
            using (ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            using (Stream stream = await content.ReadAsStreamAsync(readStreamAsync))
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
                await Assert.ThrowsAsync<NotSupportedException>(async () => await stream.WriteAsync(new byte[1], 0, 1));
                await Assert.ThrowsAsync<NotSupportedException>(async () => await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])));

                // nops
                stream.Flush();
                await stream.FlushAsync();
            }
        }

        [Theory]
        [MemberData(nameof(UseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_Seek(bool useArray, bool readStreamAsync)
        {
            const int ContentLength = 42;
            
            using (ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            using (Stream s = await content.ReadAsStreamAsync(readStreamAsync))
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
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_ReadByte_MatchesInput(int contentLength, bool useArray, bool readStreamAsync)
        {
            using (ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            using (Stream stream = await content.ReadAsStreamAsync(readStreamAsync))
            {
                for (int i = 0; i < contentLength; i++)
                {
                    Assert.Equal(memory.Span[i], stream.ReadByte());
                    Assert.Equal(i + 1, stream.Position);
                }
                Assert.Equal(-1, stream.ReadByte());
                Assert.Equal(stream.Length, stream.Position);
            }
        }

        [Theory]
        [MemberData(nameof(UseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_Read_InvalidArguments(bool useArray, bool readStreamAsync)
        {
            const int ContentLength = 42;
            
            using (ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            using (Stream stream = await content.ReadAsStreamAsync(readStreamAsync))
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
        }

        [Theory]
        [InlineData(0, false, false)] // Read(byte[], ...)
        [InlineData(1, false, false)] // Read(Span<byte>, ...)
        [InlineData(2, false, false)] // ReadAsync(byte[], ...)
        [InlineData(3, false, false)] // ReadAsync(Memory<byte>,...)
        [InlineData(4, false, false)] // Begin/EndRead(byte[],...)
        [InlineData(0, false, true)] // Read(byte[], ...)
        [InlineData(1, false, true)] // Read(Span<byte>, ...)
        [InlineData(2, false, true)] // ReadAsync(byte[], ...)
        [InlineData(3, false, true)] // ReadAsync(Memory<byte>,...)
        [InlineData(4, false, true)] // Begin/EndRead(byte[],...)
        [InlineData(0, true, false)] // Read(byte[], ...)
        [InlineData(1, true, false)] // Read(Span<byte>, ...)
        [InlineData(2, true, false)] // ReadAsync(byte[], ...)
        [InlineData(3, true, false)] // ReadAsync(Memory<byte>,...)
        [InlineData(4, true, false)] // Begin/EndRead(byte[],...)
        [InlineData(0, true, true)] // Read(byte[], ...)
        [InlineData(1, true, true)] // Read(Span<byte>, ...)
        [InlineData(2, true, true)] // ReadAsync(byte[], ...)
        [InlineData(3, true, true)] // ReadAsync(Memory<byte>,...)
        [InlineData(4, true, true)] // Begin/EndRead(byte[],...)
        public async Task ReadAsStreamAsync_ReadMultipleBytes_MatchesInput(int mode, bool useArray, bool readStreamAsync)
        {
            const int ContentLength = 1024;

            using (ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            {
                var buffer = new byte[3];

                using (Stream stream = await content.ReadAsStreamAsync(readStreamAsync))
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
            }
        }

        [Theory]
        [MemberData(nameof(UseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_ReadWithCancelableToken_MatchesInput(bool useArray, bool readStreamAsync)
        {
            const int ContentLength = 100;

            using (ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            {
                var buffer = new byte[1];
                var cts = new CancellationTokenSource();
                int bytesRead;

                using (Stream stream = await content.ReadAsStreamAsync(readStreamAsync))
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
            }
        }

        [Theory]
        [MemberData(nameof(UseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_ReadWithCanceledToken_MatchesInput(bool useArray, bool readStreamAsync)
        {
            const int ContentLength = 2;

            using (ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            {
                using (Stream stream = await content.ReadAsStreamAsync(readStreamAsync))
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stream.ReadAsync(new byte[1], 0, 1, new CancellationToken(true)));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await stream.ReadAsync(new Memory<byte>(new byte[1]), new CancellationToken(true)));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await stream.CopyToAsync(new MemoryStream(), 1, new CancellationToken(true)));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArrays))]
        public async Task CopyToAsync_AllContentCopied(int contentLength, bool useArray)
        {
            using (ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            {
                var destination = new MemoryStream();
                await content.CopyToAsync(destination);

                Assert.Equal<byte>(memory.ToArray(), destination.ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_CopyTo_AllContentCopied(int contentLength, bool useArray, bool readStreamAsync)
        {
            using (ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            {
                var destination = new MemoryStream();
                using (Stream s = await content.ReadAsStreamAsync(readStreamAsync))
                {
                    s.CopyTo(destination);
                }

                Assert.Equal<byte>(memory.ToArray(), destination.ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(UseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_CopyTo_InvalidArguments(bool useArray, bool readStreamAsync)
        {
            const int ContentLength = 42;
            using (ReadOnlyMemoryContent content = CreateContent(ContentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            {
                using (Stream s = await content.ReadAsStreamAsync(readStreamAsync))
                {
                    AssertExtensions.Throws<ArgumentNullException>("destination", () => s.CopyTo(null));
                    AssertExtensions.Throws<ArgumentNullException>("destination", () => { s.CopyToAsync(null); });

                    AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => s.CopyTo(new MemoryStream(), 0));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { s.CopyToAsync(new MemoryStream(), 0); });

                    Assert.Throws<NotSupportedException>(() => s.CopyTo(new MemoryStream(new byte[1], writable: false)));
                    Assert.Throws<NotSupportedException>(() => { s.CopyToAsync(new MemoryStream(new byte[1], writable: false)); });

                    var disposedDestination = new MemoryStream();
                    disposedDestination.Dispose();
                    Assert.Throws<ObjectDisposedException>(() => s.CopyTo(disposedDestination));
                    Assert.Throws<ObjectDisposedException>(() => { s.CopyToAsync(disposedDestination); });
                }
            }
        }

        [Theory]
        [MemberData(nameof(ContentLengthsAndUseArraysAndReadStreamAsync))]
        public async Task ReadAsStreamAsync_CopyToAsync_AllContentCopied(int contentLength, bool useArray, bool readStreamAsync)
        {
            using (ReadOnlyMemoryContent content = CreateContent(contentLength, useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner))
            using (memoryOwner)
            {
                var destination = new MemoryStream();
                using (Stream s = await content.ReadAsStreamAsync(readStreamAsync))
                {
                    await s.CopyToAsync(destination);
                }

                Assert.Equal<byte>(memory.ToArray(), destination.ToArray());
            }
        }

        private static ReadOnlyMemoryContent CreateContent(int contentLength, bool useArray, out Memory<byte> memory, out IMemoryOwner<byte> memoryOwner)
        {
            if (useArray)
            {
                memory = new byte[contentLength];
                memoryOwner = null;
            }
            else
            {
                memoryOwner = new NativeMemoryManager(contentLength);
                memory = memoryOwner.Memory;
            }

            new Random(contentLength).NextBytes(memory.Span);

            return new ReadOnlyMemoryContent(memory);
        }
    }
}
