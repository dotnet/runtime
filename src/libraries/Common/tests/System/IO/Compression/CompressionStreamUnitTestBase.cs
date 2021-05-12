// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public abstract class CompressionStreamUnitTestBase : CompressionStreamTestBase
    {
        private const int TaskTimeout = 30 * 1000; // Generous timeout for official test runs

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public virtual void FlushAsync_DuringWriteAsync()
        {
            byte[] buffer = new byte[100000];
            Random rand = new Random();
            rand.NextBytes(buffer);

            using (var writeStream = new ManualSyncMemoryStream(false))
            using (var compressor = CreateStream(writeStream, CompressionMode.Compress))
            {
                Task task = null;
                try
                {
                    // Write needs to be big enough to trigger a write to the underlying base stream so the WriteAsync call doesn't immediately complete.
                    task = compressor.WriteAsync(buffer, 0, buffer.Length);
                    while (task.IsCompleted)
                    {
                        rand.NextBytes(buffer);
                        task = compressor.WriteAsync(buffer, 0, buffer.Length);
                    }
                    Assert.Throws<InvalidOperationException>(() => { compressor.FlushAsync(); }); // "overlapping flushes"
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // The original WriteAsync should be able to complete
                    Assert.True(task.Wait(TaskTimeout), "Original WriteAsync Task did not complete in time");
                    Assert.True(writeStream.WriteHit, "BaseStream Write function was not called");
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task FlushAsync_DuringReadAsync()
        {
            byte[] buffer = new byte[32];
            string testFilePath = CompressedTestFile(UncompressedTestFile());
            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(testFilePath, false))
            using (var decompressor = CreateStream(readStream, CompressionMode.Decompress, true))
            {
                Task task = null;
                try
                {
                    readStream.manualResetEvent.Reset();
                    readStream.ReadHit = false;
                    task = decompressor.ReadAsync(buffer, 0, 32);
                    Assert.True(readStream.ReadHit);
                    Assert.Throws<InvalidOperationException>(() => { decompressor.FlushAsync(); }); // "overlapping read"
                }
                finally
                {
                    // Unblock Async operations
                    readStream.manualResetEvent.Set();
                    // The original ReadAsync should be able to complete
                    Assert.True(task.Wait(TaskTimeout), "Original ReadAsync Task did not complete in time");
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task FlushAsync_DuringFlushAsync()
        {
            byte[] buffer = null;
            string testFilePath = CompressedTestFile(UncompressedTestFile());
            using (var origStream = await LocalMemoryStream.readAppFileAsync(testFilePath))
            {
                buffer = origStream.ToArray();
            }

            using (var writeStream = new ManualSyncMemoryStream(false))
            using (var zip = CreateStream(writeStream, CompressionMode.Compress))
            {
                Task task = null;
                try
                {
                    writeStream.manualResetEvent.Set();
                    await zip.WriteAsync(buffer, 0, buffer.Length);
                    writeStream.manualResetEvent.Reset();
                    writeStream.WriteHit = false;
                    task = zip.FlushAsync();
                    while (!writeStream.WriteHit && task.IsCompleted)
                    {
                        zip.Write(buffer, 0, 1);
                        task = zip.FlushAsync();
                    }

                    Assert.Throws<InvalidOperationException>(() => { zip.FlushAsync(); }); // "overlapping flushes"
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // The original WriteAsync should be able to complete
                    Assert.True(task.Wait(TaskTimeout), "Original write Task did not complete in time");
                    Assert.True(writeStream.WriteHit, "Underlying Writesync function was not called.");

                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public virtual async Task Dispose_WithUnfinishedReadAsync()
        {
            string compressedPath = CompressedTestFile(UncompressedTestFile());
            int uncompressedSize = (int)new FileInfo(UncompressedTestFile()).Length;
            byte[] uncompressedBytes = new byte[uncompressedSize];

            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(compressedPath, false))
            {
                var decompressor = CreateStream(readStream, CompressionMode.Decompress, true);
                Task task = decompressor.ReadAsync(uncompressedBytes, 0, uncompressedBytes.Length);
                decompressor.Dispose();
                readStream.manualResetEvent.Set();
                Assert.Throws<AggregateException>(() => task.Wait(TaskTimeout));
            }
        }

        [Theory]
        [MemberData(nameof(UncompressedTestFiles))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task Read(string testFile)
        {
            var uncompressedStream = await LocalMemoryStream.readAppFileAsync(testFile);
            var compressedStream = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(testFile));
            var decompressor = CreateStream(compressedStream, CompressionMode.Decompress);
            var decompressorOutput = new MemoryStream();

            int _bufferSize = 1024;
            var bytes = new byte[_bufferSize];
            bool finished = false;
            int retCount;
            while (!finished)
            {
                retCount = await decompressor.ReadAsync(bytes, 0, _bufferSize);

                if (retCount != 0)
                    await decompressorOutput.WriteAsync(bytes, 0, retCount);
                else
                    finished = true;
            }
            decompressor.Dispose();
            decompressorOutput.Position = 0;
            uncompressedStream.Position = 0;

            byte[] uncompressedStreamBytes = uncompressedStream.ToArray();
            byte[] decompressorOutputBytes = decompressorOutput.ToArray();

            Assert.Equal(uncompressedStreamBytes.Length, decompressorOutputBytes.Length);
            for (int i = 0; i < uncompressedStreamBytes.Length; i++)
            {
                Assert.Equal(uncompressedStreamBytes[i], decompressorOutputBytes[i]);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task Read_EndOfStreamPosition()
        {
            var compressedStream = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(UncompressedTestFile()));
            int compressedEndPosition = (int)compressedStream.Length;
            var rand = new Random(1024);
            int _bufferSize = BufferSize * 2 - 568;
            var bytes = new byte[_bufferSize];
            rand.NextBytes(bytes);
            compressedStream.Position = compressedEndPosition;
            compressedStream.Write(bytes, 0, _bufferSize);
            compressedStream.Write(bytes, 0, _bufferSize);
            compressedStream.Position = 0;
            var decompressor = CreateStream(compressedStream, CompressionMode.Decompress);

            while (decompressor.Read(bytes, 0, _bufferSize) > 0);
            Assert.Equal(((compressedEndPosition / BufferSize) + 1) * BufferSize, compressedStream.Position);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task Read_BaseStreamSlowly()
        {
            string testFile = UncompressedTestFile();
            var uncompressedStream = await LocalMemoryStream.readAppFileAsync(testFile);
            var compressedStream = new BadWrappedStream(BadWrappedStream.Mode.ReadSlowly, File.ReadAllBytes(CompressedTestFile(testFile)));
            var decompressor = CreateStream(compressedStream, CompressionMode.Decompress);
            var decompressorOutput = new MemoryStream();

            int _bufferSize = 1024;
            var bytes = new byte[_bufferSize];
            bool finished = false;
            int retCount;
            while (!finished)
            {
                retCount = await decompressor.ReadAsync(bytes, 0, _bufferSize);

                if (retCount != 0)
                    await decompressorOutput.WriteAsync(bytes, 0, retCount);
                else
                    finished = true;
            }
            decompressor.Dispose();
            decompressorOutput.Position = 0;
            uncompressedStream.Position = 0;

            byte[] uncompressedStreamBytes = uncompressedStream.ToArray();
            byte[] decompressorOutputBytes = decompressorOutput.ToArray();

            Assert.Equal(uncompressedStreamBytes.Length, decompressorOutputBytes.Length);
            for (int i = 0; i < uncompressedStreamBytes.Length; i++)
            {
                Assert.Equal(uncompressedStreamBytes[i], decompressorOutputBytes[i]);
            }
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void CanDisposeBaseStream(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            ms.Dispose(); // This would throw if this was invalid
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Ctor_NullStream(CompressionMode mode)
        {
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, mode));
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Ctor_DisposedBaseStream(CompressionMode mode)
        {
            MemoryStream ms = new MemoryStream();
            ms.Dispose();
            AssertExtensions.Throws<ArgumentException>("stream", () => CreateStream(ms, mode));
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Ctor_InvalidStream_Throws(CompressionMode mode)
        {
            LocalMemoryStream ms = new LocalMemoryStream();
            ms.SetCanRead(mode == CompressionMode.Compress);
            ms.SetCanWrite(mode == CompressionMode.Decompress);

            AssertExtensions.Throws<ArgumentException>("stream", () => CreateStream(ms, mode));
        }



        [Fact]
        public void TestCompressCtor()
        {
            IEnumerable<Func<Stream, Stream>> CtorFunctions()
            {
                yield return new Func<Stream, Stream>((stream) => CreateStream(stream, CompressionMode.Compress));

                foreach (CompressionLevel level in new[] { CompressionLevel.Optimal, CompressionLevel.Fastest, CompressionLevel.NoCompression, CompressionLevel.SmallestSize })
                {
                    yield return new Func<Stream, Stream>((stream) => CreateStream(stream, level));

                    foreach (bool remainsOpen in new[] { true, false })
                    {
                        yield return new Func<Stream, Stream>((stream) => CreateStream(stream, level, remainsOpen));
                    }
                }
            }

            Assert.All(CtorFunctions(), (create) =>
            {
                //Create the Stream
                int _bufferSize = 1024;
                var bytes = new byte[_bufferSize];
                var baseStream = new MemoryStream(bytes, writable: true);
                Stream compressor = create(baseStream);

                //Write some data and Close the stream
                string strData = "Test Data";
                var encoding = Encoding.UTF8;
                byte[] data = encoding.GetBytes(strData);
                compressor.Write(data, 0, data.Length);
                compressor.Flush();
                compressor.Dispose();
                baseStream.Dispose();

                //Read the data
                byte[] data2 = new byte[_bufferSize];
                baseStream = new MemoryStream(bytes, writable: false);
                var decompressor = CreateStream(baseStream, CompressionMode.Decompress);
                int size = decompressor.Read(data2, 0, _bufferSize - 5);

                //Verify the data roundtripped
                for (int i = 0; i < size + 5; i++)
                {
                    if (i < data.Length)
                    {
                        Assert.Equal(data[i], data2[i]);
                    }
                    else
                    {
                        Assert.Equal(data2[i], (byte)0);
                    }
                }
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task TestLeaveOpenAfterValidDecompress()
        {
            //Create the Stream
            int _bufferSize = 1024;
            var bytes = new byte[_bufferSize];
            Stream compressedStream = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(UncompressedTestFile()));
            Stream decompressor = CreateStream(compressedStream, CompressionMode.Decompress, leaveOpen: false);

            //Read some data and Close the stream
            decompressor.Read(bytes, 0, _bufferSize);
            decompressor.Flush();
            decompressor.Dispose();

            //Check that Close has really closed the underlying stream
            Assert.Throws<ObjectDisposedException>(() => compressedStream.Read(bytes, 0, bytes.Length));
        }

        [Fact]
        public void Ctor_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionLevel.Fastest));
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionMode.Decompress));
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionMode.Compress));

            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionLevel.Fastest, true));
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionMode.Decompress, false));
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionMode.Compress, true));

            AssertExtensions.Throws<ArgumentException>("mode", () => CreateStream(new MemoryStream(), (CompressionMode)42));
            AssertExtensions.Throws<ArgumentException>("mode", () => CreateStream(new MemoryStream(), (CompressionMode)43, true));

            AssertExtensions.Throws<ArgumentException>("stream", () => CreateStream(new MemoryStream(new byte[1], writable: false), CompressionLevel.Optimal));
        }

        [Fact]
        public async Task WrapNullReturningTasksStream()
        {
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnNullTasks), CompressionMode.Decompress))
                Assert.Equal(0, await decompressor.ReadAsync(new byte[1024], 0, 1024));
        }

        [Fact]
        public async Task WrapStreamReturningBadReadValues()
        {
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooLargeCounts), CompressionMode.Decompress))
                Assert.Throws<InvalidDataException>(() => decompressor.Read(new byte[1024], 0, 1024));
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooLargeCounts), CompressionMode.Decompress))
                await Assert.ThrowsAsync<InvalidDataException>(() => decompressor.ReadAsync(new byte[1024], 0, 1024));
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooLargeCounts), CompressionMode.Decompress))
                await Assert.ThrowsAsync<InvalidDataException>(async () => { await decompressor.ReadAsync(new Memory<byte>(new byte[1024])); });

            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooSmallCounts), CompressionMode.Decompress))
                Assert.Equal(0, decompressor.Read(new byte[1024], 0, 1024));
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooSmallCounts), CompressionMode.Decompress))
                Assert.Equal(0, await decompressor.ReadAsync(new byte[1024], 0, 1024));
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooSmallCounts), CompressionMode.Decompress))
                Assert.Equal(0, await decompressor.ReadAsync(new Memory<byte>(new byte[1024])));
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void BaseStreamTest(CompressionMode mode)
        {
            using (var baseStream = new MemoryStream())
            using (var compressor = CreateStream(baseStream, mode))
            {
                Assert.Same(BaseStream(compressor), baseStream);
            }
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task BaseStream_Modify(CompressionMode mode)
        {
            using (var baseStream = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(UncompressedTestFile())))
            using (var compressor = CreateStream(baseStream, mode))
            {
                int size = 1024;
                byte[] bytes = new byte[size];
                if (mode == CompressionMode.Compress)
                    BaseStream(compressor).Write(bytes, 0, size); // This will throw if the underlying stream is not writable as expected
                else
                    BaseStream(compressor).Read(bytes, 0, size); // This will throw if the underlying stream is not readable as expected
            }
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void BaseStream_NullAfterDisposeWithFalseLeaveOpen(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Dispose();

            Assert.Null(BaseStream(compressor));

            compressor.Dispose(); // Should be a no-op
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36845", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task BaseStream_ValidAfterDisposeWithTrueLeaveOpen(CompressionMode mode)
        {
            var ms = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(UncompressedTestFile()));
            var decompressor = CreateStream(ms, mode, leaveOpen: true);
            var baseStream = BaseStream(decompressor);
            Assert.Same(ms, baseStream);
            decompressor.Dispose();

            int size = 1024;
            byte[] bytes = new byte[size];
            if (mode == CompressionMode.Compress)
                baseStream.Write(bytes, 0, size);
            else
                baseStream.Read(bytes, 0, size);
        }

        [Theory]
        [MemberData(nameof(UncompressedTestFiles))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36884", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task CompressionLevel_SizeInOrder(string testFile)
        {
            using var uncompressedStream = await LocalMemoryStream.readAppFileAsync(testFile);

            async Task<long> GetLengthAsync(CompressionLevel compressionLevel)
            {
                using var mms = new MemoryStream();
                using var compressor = CreateStream(mms, compressionLevel);
                await uncompressedStream.CopyToAsync(compressor);
                compressor.Flush();
                return mms.Length;
            }

            long noCompressionLength = await GetLengthAsync(CompressionLevel.NoCompression);
            long fastestLength = await GetLengthAsync(CompressionLevel.Fastest);
            long optimalLength = await GetLengthAsync(CompressionLevel.Optimal);
            long smallestLength = await GetLengthAsync(CompressionLevel.SmallestSize);

            Assert.True(noCompressionLength >= fastestLength);
            Assert.True(fastestLength >= optimalLength);
            Assert.True(optimalLength >= smallestLength);
        }
    }

    internal sealed class BadWrappedStream : MemoryStream
    {
        public enum Mode
        {
            Default,
            ReturnNullTasks,
            ReturnTooSmallCounts,
            ReturnTooLargeCounts,
            ReadSlowly
        }

        private readonly Mode _mode;

        public BadWrappedStream(Mode mode) { _mode = mode; }
        public BadWrappedStream(Mode mode, byte[] buffer) : base(buffer) { _mode = mode; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            switch (_mode)
            {
                case Mode.ReturnTooSmallCounts:
                    return -1;
                case Mode.ReturnTooLargeCounts:
                    return buffer.Length + 1;
                case Mode.ReadSlowly:
                    return base.Read(buffer, offset, 1);
                default:
                    return 0;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _mode == Mode.ReturnNullTasks ?
               null :
               base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Flush() { }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
    }

    internal partial class ManualSyncMemoryStream : MemoryStream
    {
        private bool isSync;
        public ManualResetEventSlim manualResetEvent = new ManualResetEventSlim(initialState: false);

        public bool ReadHit = false;  // For validation of the async methods we want to ensure they correctly delegate the async
        public bool WriteHit = false; // methods of the underlying stream. This bool acts as a toggle to check that they're being used.

        public static async Task<ManualSyncMemoryStream> GetStreamFromFileAsync(string testFile, bool sync = false)
        {
            var baseStream = await StreamHelpers.CreateTempCopyStream(testFile);
            var ms = new ManualSyncMemoryStream(sync);
            await baseStream.CopyToAsync(ms);

            ms.Position = 0;
            return ms;
        }

        public ManualSyncMemoryStream(bool sync = false) : base()
        {
            isSync = sync;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        public override int EndRead(IAsyncResult asyncResult) => TaskToApm.End<int>(asyncResult);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
        public override void EndWrite(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadHit = true;
            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken)).ConfigureAwait(false);
            }

            return await base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            WriteHit = true;
            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken)).ConfigureAwait(false);
            }

            await base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            ReadHit = true;

            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken)).ConfigureAwait(false);
            }
            return await base.ReadAsync(buffer, cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            WriteHit = true;

            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken)).ConfigureAwait(false);
            }

            await base.WriteAsync(buffer, cancellationToken);
        }
    }
}
