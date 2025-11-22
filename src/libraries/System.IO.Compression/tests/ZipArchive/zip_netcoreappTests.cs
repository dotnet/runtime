// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_netcoreappTests : ZipFileTestBase
    {
        [Theory]
        [InlineData("sharpziplib.zip", 0)]
        [InlineData("Linux_RW_RW_R__.zip", 0x8000 + 0x0100 + 0x0080 + 0x0020 + 0x0010 + 0x0004)]
        [InlineData("Linux_RWXRW_R__.zip", 0x8000 + 0x01C0 + 0x0020 + 0x0010 + 0x0004)]
        [InlineData("OSX_RWXRW_R__.zip", 0x8000 + 0x01C0 + 0x0020 + 0x0010 + 0x0004)]
        public static async Task Read_UnixFilePermissions(string zipName, uint expectedAttr)
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(compat(zipName)), ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry e in archive.Entries)
                {
                    Assert.Equal(expectedAttr, ((uint)e.ExternalAttributes) >> 16);
                }
            }
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        [InlineData((0x8000 + 0x01C0 + 0x0020 + 0x0010 + 0x0004) << 16)]
        public static async Task RoundTrips_UnixFilePermissions(int expectedAttr)
        {
            using (var stream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip")))
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Update, true))
                {
                    foreach (ZipArchiveEntry e in archive.Entries)
                    {
                        e.ExternalAttributes = expectedAttr;
                        Assert.Equal(expectedAttr, e.ExternalAttributes);
                    }
                }
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry e in archive.Entries)
                    {
                        Assert.Equal(expectedAttr, e.ExternalAttributes);
                    }
                }
            }
        }

        [Fact]
        public static async Task AsyncOnlyStream_NoSynchronousCalls()
        {
            // This test verifies that async Zip methods don't make synchronous calls
            // which would fail with async-only streams (e.g., Kestrel response streams)
            var innerStream = new MemoryStream();
            var asyncOnlyStream = new AsyncOnlyStream(innerStream);
            byte[] testData = new byte[1024];
            Random.Shared.NextBytes(testData);

            await using (var zipArchive = new ZipArchive(asyncOnlyStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zipArchive.CreateEntry("TestEntry");
                await using (var entryStream = await entry.OpenAsync())
                {
                    await entryStream.WriteAsync(testData);
                    await entryStream.FlushAsync();
                }
            }

            // Verify the archive was created successfully by reading from the inner stream
            innerStream.Position = 0;
            using (var zipArchive = new ZipArchive(innerStream, ZipArchiveMode.Read))
            {
                Assert.Single(zipArchive.Entries);
                var entry = zipArchive.Entries[0];
                Assert.Equal("TestEntry", entry.Name);
                Assert.Equal(testData.Length, entry.Length);

                using (var entryStream = entry.Open())
                {
                    byte[] readData = new byte[testData.Length];
                    int bytesRead = entryStream.Read(readData);
                    Assert.Equal(testData.Length, bytesRead);
                    Assert.Equal(testData, readData);
                }
            }
        }

        private sealed class AsyncOnlyStream : Stream
        {
            private readonly MemoryStream _innerStream;

            public AsyncOnlyStream(MemoryStream innerStream)
            {
                _innerStream = innerStream;
            }

            public override void Flush()
            {
                throw new NotSupportedException("Synchronous operations not supported");
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("Synchronous operations not supported");
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _innerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _innerStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("Synchronous operations not supported");
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _innerStream.FlushAsync(cancellationToken);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return _innerStream.WriteAsync(buffer, cancellationToken);
            }

            public override ValueTask DisposeAsync()
            {
                return _innerStream.DisposeAsync();
            }

            public override bool CanRead => _innerStream.CanRead;

            public override bool CanSeek => _innerStream.CanSeek;

            public override bool CanWrite => _innerStream.CanWrite;

            public override long Length => _innerStream.Length;

            public override long Position
            {
                get => _innerStream.Position;
                set => _innerStream.Position = value;
            }
        }
    }
}
