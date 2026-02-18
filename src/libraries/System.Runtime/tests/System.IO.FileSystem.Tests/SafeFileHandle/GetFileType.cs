// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace System.IO.Tests
{
    public class SafeFileHandle_GetFileType : FileSystemTest
    {
        [Fact]
        public void GetFileType_RegularFile()
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, "test");

            using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read);
            Assert.Equal(FileType.RegularFile, handle.GetFileType());
        }

        [Fact]
        public void GetFileType_Directory()
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);

            if (OperatingSystem.IsWindows())
            {
                IntPtr hFile = Interop.Kernel32.CreateFile(
                    path,
                    Interop.Kernel32.GenericOperations.GENERIC_READ,
                    FileShare.ReadWrite,
                    null,
                    FileMode.Open,
                    Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero);

                using SafeFileHandle handle = new SafeFileHandle(hFile, ownsHandle: true);
                Assert.False(handle.IsInvalid);
                Assert.Equal(FileType.Directory, handle.GetFileType());
            }
            else
            {
                IntPtr fd = Interop.Sys.Open(path, Interop.Sys.OpenFlags.O_RDONLY, 0);
                using SafeFileHandle handle = new SafeFileHandle(fd, ownsHandle: true);
                Assert.False(handle.IsInvalid);
                Assert.Equal(FileType.Directory, handle.GetFileType());
            }
        }

        [Fact]
        public void GetFileType_NullDevice()
        {
            using SafeFileHandle handle = File.OpenHandle(
                OperatingSystem.IsWindows() ? "NUL" : "/dev/null",
                FileMode.Open,
                FileAccess.Write);

            Assert.Equal(FileType.CharacterDevice, handle.GetFileType());
        }

        [Fact]
        public void GetFileType_AnonymousPipe()
        {
            using AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out);
            using SafeFileHandle serverHandle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            
            Assert.Equal(FileType.Pipe, serverHandle.GetFileType());

            using SafeFileHandle clientHandle = new SafeFileHandle(server.ClientSafePipeHandle.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(FileType.Pipe, clientHandle.GetFileType());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetFileType_NamedPipe_Windows()
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetFileType_NamedPipe_Unix()
        {
            string pipePath = GetTestFilePath();
            Assert.Equal(0, Interop.Sys.MkFifo(pipePath, (int)UnixFileMode.UserRead | (int)UnixFileMode.UserWrite));

            Task readerTask = Task.Run(() =>
            {
                using SafeFileHandle reader = File.OpenHandle(pipePath, FileMode.Open, FileAccess.Read);
                Assert.Equal(FileType.Pipe, reader.GetFileType());
            });

            using SafeFileHandle writer = File.OpenHandle(pipePath, FileMode.Open, FileAccess.Write);
            Assert.Equal(FileType.Pipe, writer.GetFileType());

            readerTask.Wait();
        }

        [Fact]
        public void GetFileType_Socket()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(listener.LocalEndPoint);

            using Socket server = listener.Accept();

            using SafeFileHandle serverHandle = new SafeFileHandle(server.Handle, ownsHandle: false);
            using SafeFileHandle clientHandle = new SafeFileHandle(client.Handle, ownsHandle: false);

            Assert.Equal(FileType.Socket, serverHandle.GetFileType());
            Assert.Equal(FileType.Socket, clientHandle.GetFileType());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetFileType_ConsoleInput()
        {
            if (!Console.IsInputRedirected)
            {
                using SafeFileHandle handle = new SafeFileHandle(Console.OpenStandardInput().SafeFileHandle.DangerousGetHandle(), ownsHandle: false);
                FileType type = handle.GetFileType();
                
                Assert.True(type == FileType.CharacterDevice || type == FileType.Pipe || type == FileType.RegularFile,
                    $"Expected CharacterDevice, Pipe, or RegularFile but got {type}");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetFileType_SymbolicLink_Unix()
        {
            string targetPath = GetTestFilePath();
            string linkPath = GetTestFilePath();
            File.WriteAllText(targetPath, "test");
            File.CreateSymbolicLink(linkPath, targetPath);

            IntPtr fd = Interop.Sys.Open(linkPath, Interop.Sys.OpenFlags.O_RDONLY | Interop.Sys.OpenFlags.O_NOFOLLOW, 0);
            using SafeFileHandle handle = new SafeFileHandle(fd, ownsHandle: true);
            
            if (!handle.IsInvalid)
            {
                Assert.Equal(FileType.SymbolicLink, handle.GetFileType());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetFileType_SymbolicLink_Windows()
        {
            string targetPath = GetTestFilePath();
            string linkPath = GetTestFilePath();
            File.WriteAllText(targetPath, "test");
            File.CreateSymbolicLink(linkPath, targetPath);

            IntPtr hFile = Interop.Kernel32.CreateFile(
                linkPath,
                Interop.Kernel32.GenericOperations.GENERIC_READ,
                FileShare.ReadWrite,
                null,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_OPEN_REPARSE_POINT | Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            using SafeFileHandle handle = new SafeFileHandle(hFile, ownsHandle: true);
            if (!handle.IsInvalid)
            {
                Assert.Equal(FileType.SymbolicLink, handle.GetFileType());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.Wasi)]
        public void GetFileType_BlockDevice_Unix()
        {
            string[] possibleBlockDevices = { "/dev/sda", "/dev/loop0", "/dev/vda", "/dev/nvme0n1" };

            string blockDevice = null;
            foreach (string device in possibleBlockDevices)
            {
                if (File.Exists(device))
                {
                    blockDevice = device;
                    break;
                }
            }

            if (blockDevice == null)
            {
                throw new SkipTestException("No accessible block device found for testing");
            }

            try
            {
                IntPtr fd = Interop.Sys.Open(blockDevice, Interop.Sys.OpenFlags.O_RDONLY, 0);
                if (fd == (IntPtr)(-1))
                {
                    throw new SkipTestException($"Could not open {blockDevice}");
                }

                using SafeFileHandle handle = new SafeFileHandle(fd, ownsHandle: true);
                Assert.Equal(FileType.BlockDevice, handle.GetFileType());
            }
            catch (UnauthorizedAccessException)
            {
                throw new SkipTestException("Insufficient privileges to open block device");
            }
        }

        [Fact]
        public void GetFileType_ClosedHandle_ThrowsObjectDisposedException()
        {
            SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create);
            handle.Dispose();

            Assert.Throws<ObjectDisposedException>(() => handle.GetFileType());
        }

        [Fact]
        public void GetFileType_CachesResult()
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, "test");

            using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read);
            
            FileType firstCall = handle.GetFileType();
            FileType secondCall = handle.GetFileType();

            Assert.Equal(firstCall, secondCall);
            Assert.Equal(FileType.RegularFile, firstCall);
        }
    }
}
