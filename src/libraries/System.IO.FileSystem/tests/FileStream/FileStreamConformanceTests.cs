// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStreamStandaloneConformanceTests : StandaloneStreamConformanceTests
    {
        protected abstract FileOptions Options { get; }
        protected abstract int BufferSize { get; }

        private FileStream CreateStream(byte[] initialData, FileAccess access)
        {
            string path = GetTestFilePath();
            if (initialData != null)
            {
                File.WriteAllBytes(path, initialData);
            }

            return new FileStream(path, FileMode.OpenOrCreate, access, FileShare.None, BufferSize, Options);
        }

        protected override Stream CreateReadOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Read);
        protected override Stream CreateReadWriteStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.ReadWrite);
        protected override Stream CreateWriteOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Write);

        protected override bool NopFlushCompletesSynchronously => OperatingSystem.IsWindows();
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
    public class UnbufferedAsyncFileStreamStandaloneConformanceTests : FileStreamStandaloneConformanceTests
    {
        protected override FileOptions Options => FileOptions.Asynchronous;
        protected override int BufferSize => 1;
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
