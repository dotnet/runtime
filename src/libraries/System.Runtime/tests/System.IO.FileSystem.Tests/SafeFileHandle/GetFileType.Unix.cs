// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public partial class SafeFileHandle_GetFileType_Unix : FileSystemTest
    {
        [Fact]
        public void GetFileType_Directory()
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);

            using SafeFileHandle handle = Interop.Sys.Open(path, Interop.Sys.OpenFlags.O_RDONLY, 0);
            Assert.False(handle.IsInvalid);
            Assert.Equal(FileHandleType.Directory, handle.Type);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "iOS/tvOS do not support creating FIFOs (named pipes) with mkfifo")]
        public async Task GetFileType_NamedPipe()
        {
            string pipePath = GetTestFilePath();
            Assert.Equal(0, Interop.Sys.MkFifo(pipePath, (int)UnixFileMode.UserRead | (int)UnixFileMode.UserWrite));

            // The reader blocks until a writer opens the pipe, so run it in a separate task.
            Task readerTask = Task.Run(() =>
            {
                using SafeFileHandle reader = File.OpenHandle(pipePath, FileMode.Open, FileAccess.Read);
                Assert.Equal(FileHandleType.Pipe, reader.Type);
            });

            using SafeFileHandle writer = File.OpenHandle(pipePath, FileMode.Open, FileAccess.Write);
            Assert.Equal(FileHandleType.Pipe, writer.Type);

            await readerTask;
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void GetFileType_SymbolicLink()
        {
            string targetPath = GetTestFilePath();
            string linkPath = GetTestFilePath();
            File.WriteAllText(targetPath, "test");
            File.CreateSymbolicLink(linkPath, targetPath);

            using SafeFileHandle handle = Interop.Sys.Open(linkPath, Interop.Sys.OpenFlags.O_RDONLY | Interop.Sys.OpenFlags.O_NOFOLLOW, 0);

            if (!handle.IsInvalid)
            {
                Assert.Equal(FileHandleType.SymbolicLink, handle.Type);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.Wasi)]
        public void GetFileType_BlockDevice()
        {
            string[] possibleBlockDevices = { "/dev/sda", "/dev/loop0", "/dev/vda", "/dev/nvme0n1" };

            string? blockDevice = null;
            foreach (string device in possibleBlockDevices)
            {
                if (File.Exists(device))
                {
                    blockDevice = device;
                    break;
                }
            }

            if (blockDevice is null)
            {
                throw new SkipTestException("No accessible block device found for testing");
            }

            try
            {
                using SafeFileHandle handle = Interop.Sys.Open(blockDevice, Interop.Sys.OpenFlags.O_RDONLY, 0);
                if (handle.IsInvalid)
                {
                    throw new SkipTestException($"Could not open {blockDevice}");
                }

                Assert.Equal(FileHandleType.BlockDevice, handle.Type);
            }
            catch (UnauthorizedAccessException)
            {
                throw new SkipTestException("Insufficient privileges to open block device");
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void WriteCanUseFileDescriptorAboveCurrentLimit()
        {
            RemoteExecutor.Invoke(() =>
            {
                const ulong MaximumFileDescriptorLimit = 4_096;

                Assert.Equal(0, Interop.Sys.GetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, out Interop.Sys.RLimit limits));

                limits.CurrentLimit = Math.Min(limits.CurrentLimit, MaximumFileDescriptorLimit);
                Assert.InRange(limits.CurrentLimit, 2UL, (ulong)int.MaxValue);
                Assert.Equal(0, Interop.Sys.SetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, ref limits));

                int maxAllowedFileDescriptor = checked((int)limits.CurrentLimit - 1);
                string filePath = Path.GetTempFileName();

                try
                {
                    using (SafeFileHandle fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        Assert.Equal(maxAllowedFileDescriptor, dup2((int)fileHandle.DangerousGetHandle(), maxAllowedFileDescriptor));

                        limits.CurrentLimit--;
                        Assert.Equal(0, Interop.Sys.SetRLimit(Interop.Sys.RlimitResources.RLIMIT_NOFILE, ref limits));

                        using (SafeFileHandle duplicatedHandle = new(maxAllowedFileDescriptor, ownsHandle: true))
                        {
                            RandomAccess.Write(duplicatedHandle, new byte[] { 1 }, fileOffset: 0);
                        }

                        byte[] buffer = new byte[1];
                        Assert.Equal(1, RandomAccess.Read(fileHandle, buffer, fileOffset: 0));
                        Assert.Equal(new byte[] { 1 }, buffer);
                    }
                }
                finally
                {
                    File.Delete(filePath);
                }

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [LibraryImport("libc", SetLastError = true)]
        private static partial int dup2(int oldFileDescriptor, int newFileDescriptor);
    }
}
