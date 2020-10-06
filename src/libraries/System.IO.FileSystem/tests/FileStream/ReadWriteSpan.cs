// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public abstract class FileStream_ReadWrite_Span : FileSystemTest
    {
        protected abstract FileStream CreateFileStream(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite);

        [Fact]
        public void DisposedStream_ReadWrite_Throws()
        {
            var fs = CreateFileStream(GetTestFilePath(), FileMode.Create);
            fs.Dispose();
            Assert.Throws<ObjectDisposedException>(() => fs.Read(new Span<byte>(new byte[1])));
            Assert.Throws<ObjectDisposedException>(() => fs.Write(new Span<byte>(new byte[1])));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // Browser PNSE: Cannot wait on monitors
        public void EmptyFile_Read_Succeeds()
        {
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create))
            {
                // use a recognizable pattern
                var buffer = (byte[])TestBuffer.Clone();

                Assert.Equal(0, fs.Read(Span<byte>.Empty));
                Assert.Equal(0, fs.Read(new Span<byte>(buffer, 0, 1)));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, fs.Read(new Span<byte>(buffer, 0, buffer.Length)));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, fs.Read(new Span<byte>(buffer, buffer.Length - 1, 1)));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, fs.Read(new Span<byte>(buffer, buffer.Length / 2, buffer.Length - buffer.Length / 2)));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // Browser PNSE: Cannot wait on monitors
        public void NonEmptyFile_Read_GetsExpectedData()
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, TestBuffer);

            using (var fs = CreateFileStream(fileName, FileMode.Open))
            {
                var buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, fs.Read(new Span<byte>(buffer, 0, buffer.Length)));
                Assert.Equal(TestBuffer, buffer);

                // Larger than needed buffer, read into beginning, rest remains untouched
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, fs.Read(new Span<byte>(buffer)));
                Assert.Equal(TestBuffer, buffer.Take(TestBuffer.Length));
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length], buffer.Skip(TestBuffer.Length));

                // Larger than needed buffer, read into middle, beginning and end remain untouched
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, fs.Read(new Span<byte>(buffer, 2, buffer.Length - 2)));
                Assert.Equal(TestBuffer, buffer.Skip(2).Take(TestBuffer.Length));
                Assert.Equal(new byte[2], buffer.Take(2));
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length - 2], buffer.Skip(2 + TestBuffer.Length));
            }
        }

        [Fact]
        public void ReadOnly_Write_Throws()
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, TestBuffer);

            using (var fs = CreateFileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() => fs.Write(new Span<byte>(new byte[1])));
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Write(new Span<byte>(new byte[1]))); // Disposed checking happens first
            }
        }

        [Fact]
        public void WriteOnly_Read_Throws()
        {
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => fs.Read(new Span<byte>(new byte[1])));
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Read(new Span<byte>(new byte[1]))); // Disposed checking happens first
            }
        }

        [Fact]
        public void EmptyWrites_NoDataWritten()
        {
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(Span<byte>.Empty);
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // Browser PNSE: Cannot wait on monitors
        public void NonEmptyWrite_WritesExpectedData()
        {
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(new Span<byte>(TestBuffer));
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                fs.Position = 0;
                var buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, fs.Read(new Span<byte>(buffer)));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Fact]
        public void DisposedStream_ReadWriteAsync_Throws()
        {
            var fs = CreateFileStream(GetTestFilePath(), FileMode.Create);
            fs.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { fs.ReadAsync(new Memory<byte>(new byte[1])); });
            Assert.Throws<ObjectDisposedException>(() => { fs.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])); });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task EmptyFile_ReadAsync_Succeeds()
        {
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create))
            {
                // use a recognizable pattern
                var buffer = (byte[])TestBuffer.Clone();

                Assert.Equal(0, await fs.ReadAsync(Memory<byte>.Empty));
                Assert.Equal(0, await fs.ReadAsync(new Memory<byte>(buffer, 0, 1)));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await fs.ReadAsync(new Memory<byte>(buffer, 0, buffer.Length)));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await fs.ReadAsync(new Memory<byte>(buffer, buffer.Length - 1, 1)));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await fs.ReadAsync(new Memory<byte>(buffer, buffer.Length / 2, buffer.Length - buffer.Length / 2)));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task NonEmptyFile_ReadAsync_GetsExpectedData()
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, TestBuffer);

            using (var fs = CreateFileStream(fileName, FileMode.Open))
            {
                var buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(new Memory<byte>(buffer, 0, buffer.Length)));
                Assert.Equal(TestBuffer, buffer);

                // Larger than needed buffer, read into beginning, rest remains untouched
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(new Memory<byte>(buffer)));
                Assert.Equal(TestBuffer, buffer.Take(TestBuffer.Length));
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length], buffer.Skip(TestBuffer.Length));

                // Larger than needed buffer, read into middle, beginning and end remain untouched
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(new Memory<byte>(buffer, 2, buffer.Length - 2)));
                Assert.Equal(TestBuffer, buffer.Skip(2).Take(TestBuffer.Length));
                Assert.Equal(new byte[2], buffer.Take(2));
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length - 2], buffer.Skip(2 + TestBuffer.Length));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task NonEmptyFile_CustomMemoryManager_ReadAsync_GetsExpectedData()
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, TestBuffer);

            using (var fs = CreateFileStream(fileName, FileMode.Open))
            using (var buffer = new NativeMemoryManager(TestBuffer.Length))
            {
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(buffer.Memory));
                Assert.Equal<byte>(TestBuffer, buffer.Memory.ToArray());
            }
        }

        [Fact]
        public void ReadOnly_WriteAsync_Throws()
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, TestBuffer);

            using (var fs = CreateFileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() => { fs.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])); });
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { fs.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])); }); // Disposed checking happens first
            }
        }

        [Fact]
        public void WriteOnly_ReadAsync_Throws()
        {
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => { fs.ReadAsync(new Memory<byte>(new byte[1])); });
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { fs.ReadAsync(new Memory<byte>(new byte[1])); });// Disposed checking happens first
            }
        }

        [Fact]
        public async Task EmptyWriteAsync_NoDataWritten()
        {
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create))
            {
                await fs.WriteAsync(Memory<byte>.Empty);
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task NonEmptyWriteAsync_WritesExpectedData()
        {
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create))
            {
                await fs.WriteAsync(new Memory<byte>(TestBuffer));
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                fs.Position = 0;
                var buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(new Memory<byte>(buffer)));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task NonEmptyWriteAsync_CustomMemoryManager_WritesExpectedData()
        {
            using (var mem = new NativeMemoryManager(TestBuffer.Length))
            using (var fs = CreateFileStream(GetTestFilePath(), FileMode.Create))
            {
                new Memory<byte>(TestBuffer).CopyTo(mem.Memory);

                await fs.WriteAsync(mem.Memory);
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                fs.Position = 0;
                var buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(new Memory<byte>(buffer)));
                Assert.Equal(TestBuffer, buffer);
            }
        }
    }

    public class Sync_FileStream_ReadWrite_Span : FileStream_ReadWrite_Span
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access) =>
            new FileStream(path, mode, access, FileShare.None, bufferSize: 0x1000, FileOptions.None);
    }

    public class Async_FileStream_ReadWrite_Span : FileStream_ReadWrite_Span
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access) =>
            new FileStream(path, mode, access, FileShare.None, bufferSize: 0x1000, FileOptions.Asynchronous);
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class Async_NoBuffer_FileStream_ReadWrite_Span : FileStream_ReadWrite_Span
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access) =>
            new FileStream(path, mode, access, FileShare.None, bufferSize: 1, FileOptions.Asynchronous);
    }

    public sealed class Sync_DerivedFileStream_ReadWrite_Span : Sync_FileStream_ReadWrite_Span
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access) =>
            new DerivedFileStream(path, mode, access, FileShare.None, bufferSize: 0x1000, FileOptions.None);

        [Fact]
        public void CallSpanReadWriteOnDerivedFileStream_ArrayMethodsUsed()
        {
            using (var fs = (DerivedFileStream)CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite))
            {
                Assert.False(fs.WriteArrayInvoked);
                Assert.False(fs.ReadArrayInvoked);

                fs.Write(new ReadOnlySpan<byte>(new byte[1]));
                Assert.True(fs.WriteArrayInvoked);
                Assert.False(fs.ReadArrayInvoked);

                fs.Position = 0;
                fs.Read(new Span<byte>(new byte[1]));
                Assert.True(fs.WriteArrayInvoked);
                Assert.True(fs.ReadArrayInvoked);
            }
        }

        [Fact]
        public async Task CallMemoryReadWriteAsyncOnDerivedFileStream_ArrayMethodsUsed()
        {
            using (var fs = (DerivedFileStream)CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite))
            {
                Assert.False(fs.WriteAsyncArrayInvoked);
                Assert.False(fs.ReadAsyncArrayInvoked);

                await fs.WriteAsync(new ReadOnlyMemory<byte>(new byte[1]));
                Assert.True(fs.WriteAsyncArrayInvoked);
                Assert.False(fs.ReadAsyncArrayInvoked);

                fs.Position = 0;
                await fs.ReadAsync(new Memory<byte>(new byte[1]));
                Assert.True(fs.WriteAsyncArrayInvoked);
                Assert.True(fs.ReadAsyncArrayInvoked);
            }
        }
    }

    public sealed class Async_DerivedFileStream_ReadWrite_Span : Async_FileStream_ReadWrite_Span
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access) =>
            new DerivedFileStream(path, mode, access, FileShare.None, bufferSize: 0x1000, FileOptions.Asynchronous);
    }

    internal sealed class DerivedFileStream : FileStream
    {
        public bool ReadArrayInvoked = false, WriteArrayInvoked = false;
        public bool ReadAsyncArrayInvoked = false, WriteAsyncArrayInvoked = false;

        public DerivedFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) :
            base(path, mode, access, share, bufferSize, options)
        {
        }

        public override int Read(byte[] array, int offset, int count)
        {
            ReadArrayInvoked = true;
            return base.Read(array, offset, count);
        }

        public override void Write(byte[] array, int offset, int count)
        {
            WriteArrayInvoked = true;
            base.Write(array, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadAsyncArrayInvoked = true;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            WriteAsyncArrayInvoked = true;
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}
