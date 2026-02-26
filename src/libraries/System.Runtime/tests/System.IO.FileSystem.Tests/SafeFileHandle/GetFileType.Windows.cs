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
        [Fact]
        public void GetFileType_Directory()
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);

            using SafeFileHandle handle = Interop.Kernel32.CreateFile(
                path,
                Interop.Kernel32.GenericOperations.GENERIC_READ,
                FileShare.ReadWrite,
                null,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            Assert.False(handle.IsInvalid);
            Assert.Equal(FileType.Directory, handle.GetFileType());
        }

        [Fact]
        public void GetFileType_NamedPipe()
        {
            string pipeName = Path.GetRandomFileName();
            using NamedPipeServerStream server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            
            Task serverTask = Task.Run(async () => await server.WaitForConnectionAsync());

            using NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None);
            client.Connect();
            serverTask.Wait();

            using SafeFileHandle serverHandle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(FileType.Pipe, serverHandle.GetFileType());

            using SafeFileHandle clientHandle = new SafeFileHandle(client.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(FileType.Pipe, clientHandle.GetFileType());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetFileType_ConsoleInput()
        {
            if (!Console.IsInputRedirected)
            {
                using SafeFileHandle handle = Console.OpenStandardInputHandle();
                FileType type = handle.GetFileType();
                
                Assert.True(type == FileType.CharacterDevice || type == FileType.Pipe || type == FileType.RegularFile,
                    $"Expected CharacterDevice, Pipe, or RegularFile but got {type}");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void GetFileType_SymbolicLink()
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
                Assert.Equal(FileType.SymbolicLink, handle.GetFileType());
            }
        }
    }
}
