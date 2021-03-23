// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStreamStandaloneConformanceTests : StandaloneStreamConformanceTests
    {
        protected abstract FileOptions Options { get; }
        protected abstract int BufferSize { get; }

        private Task<Stream> CreateStream(byte[] initialData, FileAccess access)
        {
            string path = GetTestFilePath();
            if (initialData != null)
            {
                File.WriteAllBytes(path, initialData);
            }

            return Task.FromResult<Stream>(new FileStream(path, FileMode.OpenOrCreate, access, FileShare.None, BufferSize, Options));
        }

        protected override Task<Stream> CreateReadOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Read);
        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.ReadWrite);
        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Write);

        protected override bool NopFlushCompletesSynchronously => OperatingSystem.IsWindows();

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public async Task FileOffsetIsPreservedWhenFileStreamIsCreatedFromSafeFileHandle_Reads(ReadWriteMode mode)
        {
            byte[] initialData = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            using FileStream stream = (FileStream)await CreateReadOnlyStreamCore(initialData);
            byte[] buffer = new byte[5];
            int bytesRead = await ReadAsync(mode, stream, buffer, 0, buffer.Length);

            Assert.Equal(bytesRead, stream.Position);

            using FileStream createdFromHandle = new FileStream(stream.SafeFileHandle, FileAccess.Read);

            Assert.Equal(bytesRead, stream.Position); // accessing SafeFileHandle must not change the position
            Assert.Equal(stream.Position, createdFromHandle.Position); // but it should sync the offset with OS
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public async Task FileOffsetIsPreservedWhenFileStreamIsCreatedFromSafeFileHandle_Writes(ReadWriteMode mode)
        {
            using FileStream stream = (FileStream)await CreateWriteOnlyStreamCore(Array.Empty<byte>());
            byte[] buffer = new byte[] { 0, 1, 2, 3, 4 };
            await WriteAsync(mode, stream, buffer, 0, buffer.Length);

            Assert.Equal(buffer.Length, stream.Position);

            using FileStream createdFromHandle = new FileStream(stream.SafeFileHandle, FileAccess.Write);

            Assert.Equal(buffer.Length, stream.Position); 
            Assert.Equal(stream.Position, createdFromHandle.Position);
        }

        [Theory]
        [MemberData(nameof(AllReadWriteModes))]
        public async Task WriteAsyncStartsWherePreviousReadAsyncHasFinished(ReadWriteMode mode)
        {
            if (mode == ReadWriteMode.SyncByte)
            {
                // it reads a single byte even if buffer.Length > 1
                return;
            }

            byte[] initialData = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] readBuffer = new byte[initialData.Length * 2]; // the edge case: reading more than available
            byte[] writeBuffer = new byte[] { 10, 11, 12, 13, 14, 15 };
            string filePath;

            using (FileStream stream = (FileStream)await CreateReadWriteStreamCore(initialData))
            {
                filePath = stream.Name;

                int bytesRead = await ReadAsync(mode, stream, readBuffer, 0, readBuffer.Length);

                Assert.Equal(bytesRead, initialData.Length);
                Assert.Equal(initialData.Length, stream.Position);
                Assert.Equal(stream.Position, stream.Length);

                await WriteAsync(mode, stream, writeBuffer, 0, writeBuffer.Length);

                Assert.Equal(initialData.Length + writeBuffer.Length, stream.Position);
                Assert.Equal(stream.Position, stream.Length);
            }

            byte[] allBytes = File.ReadAllBytes(filePath);
            Assert.Equal(initialData.Concat(writeBuffer), allBytes);
        }
    }

    public class UnbufferedSyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.None;
        protected override int BufferSize => 1;
    }

    public class BufferedSyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.None;
        protected override int BufferSize => 10;
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [PlatformSpecific(~TestPlatforms.Browser)] // copied from base class due to https://github.com/xunit/xunit/issues/2186
    public class UnbufferedAsyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.Asynchronous;
        protected override int BufferSize => 1;
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [PlatformSpecific(~TestPlatforms.Browser)] // copied from base class due to https://github.com/xunit/xunit/issues/2186
    public class BufferedAsyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.Asynchronous;
        protected override int BufferSize => 10;
    }

    public class AnonymousPipeFileStreamConnectedConformanceTests : ConnectedStreamConformanceTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            var server = new AnonymousPipeServerStream(PipeDirection.Out);

            var fs1 = new FileStream(new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Write);
            var fs2 = new FileStream(new SafeFileHandle(server.ClientSafePipeHandle.DangerousGetHandle(), true), FileAccess.Read);

            server.SafePipeHandle.SetHandleAsInvalid();
            server.ClientSafePipeHandle.SetHandleAsInvalid();

            return Task.FromResult<StreamPair>((fs1, fs2));
        }

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool FullyCancelableOperations => false;
        protected override bool BlocksOnZeroByteReads => OperatingSystem.IsWindows();
        protected override bool SupportsConcurrentBidirectionalUse => false;
    }

    public class NamedPipeFileStreamConnectedConformanceTests : ConnectedStreamConformanceTests
    {
        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            string name = FileSystemTest.GetNamedPipeServerStreamName();

            var server = new NamedPipeServerStream(name, PipeDirection.In);
            var client = new NamedPipeClientStream(".", name, PipeDirection.Out);

            await WhenAllOrAnyFailed(server.WaitForConnectionAsync(), client.ConnectAsync());

            var fs1 = new FileStream(new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Read);
            var fs2 = new FileStream(new SafeFileHandle(client.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Write);

            server.SafePipeHandle.SetHandleAsInvalid();
            client.SafePipeHandle.SetHandleAsInvalid();

            return (fs1, fs2);
        }

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool FullyCancelableOperations => false;
        protected override bool BlocksOnZeroByteReads => OperatingSystem.IsWindows();
        protected override bool SupportsConcurrentBidirectionalUse => false;
    }
}
