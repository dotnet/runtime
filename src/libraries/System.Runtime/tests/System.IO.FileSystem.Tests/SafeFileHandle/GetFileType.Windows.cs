// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class SafeFileHandle_GetFileType_Windows : FileSystemTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public unsafe void GetFileType_Directory(bool usePublicAPI)
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);

            using SafeFileHandle handle = usePublicAPI
                ? File.OpenHandle(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    (FileOptions)Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS)
                : Interop.Kernel32.CreateFile(
                    path,
                    Interop.Kernel32.GenericOperations.GENERIC_READ,
                    FileShare.ReadWrite,
                    null,
                    FileMode.Open,
                    Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero);

            Assert.False(handle.IsInvalid);
            Assert.Equal(FileHandleType.Directory, handle.Type);
        }

        [Fact]
        public async Task GetFileType_NamedPipe()
        {
            string pipeName = Path.GetRandomFileName();
            using NamedPipeServerStream server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            Task serverTask = server.WaitForConnectionAsync();

            using NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None);
            client.Connect();
            await serverTask;

            using SafeFileHandle serverHandle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(FileHandleType.Pipe, serverHandle.Type);

            using SafeFileHandle clientHandle = new SafeFileHandle(client.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(FileHandleType.Pipe, clientHandle.Type);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileStream_WriteOnlyNamedPipe_CanSeek_IsAsync(bool isAsync)
        {
            int openMode = (int)Interop.Kernel32.PipeOptions.PIPE_ACCESS_OUTBOUND;
            if (isAsync)
            {
                openMode |= Interop.Kernel32.FileOperations.FILE_FLAG_OVERLAPPED;
            }

            Interop.Kernel32.SECURITY_ATTRIBUTES securityAttributes = Interop.Kernel32.SECURITY_ATTRIBUTES.Create();
            using SafeFileHandle handle = Interop.Kernel32.CreateNamedPipeFileHandle(
                $@"\\.\pipe\{Guid.NewGuid():N}",
                openMode,
                pipeMode: (int)Interop.Kernel32.PipeOptions.PIPE_TYPE_BYTE,
                maxInstances: 1,
                outBufferSize: 0,
                inBufferSize: 0,
                defaultTimeout: 0,
                ref securityAttributes);
            Assert.False(handle.IsInvalid);

            using FileStream stream = new(handle, FileAccess.Write, bufferSize: 4096, isAsync);
            Assert.False(stream.CanSeek);
            Assert.Equal(isAsync, stream.IsAsync);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetFileType_ConsoleInput()
        {
            using SafeFileHandle handle = Console.OpenStandardInputHandle();
            FileHandleType type = handle.Type;

            if (Console.IsInputRedirected)
            {
                Assert.True(type == FileHandleType.Pipe || type == FileHandleType.RegularFile, $"Expected Pipe or RegularFile but got {type}");
            }
            else
            {
                Assert.Equal(FileHandleType.CharacterDevice, type);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public unsafe void GetFileType_SymbolicLink()
        {
            string targetPath = GetTestFilePath();
            string linkPath = GetTestFilePath();
            File.WriteAllText(targetPath, "test");
            File.CreateSymbolicLink(linkPath, targetPath);

            using SafeFileHandle handle = Interop.Kernel32.CreateFile(
                linkPath,
                Interop.Kernel32.GenericOperations.GENERIC_READ,
                FileShare.ReadWrite,
                null,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_OPEN_REPARSE_POINT | Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (!handle.IsInvalid)
            {
                Assert.Equal(FileHandleType.SymbolicLink, handle.Type);
            }
        }
    }
}
