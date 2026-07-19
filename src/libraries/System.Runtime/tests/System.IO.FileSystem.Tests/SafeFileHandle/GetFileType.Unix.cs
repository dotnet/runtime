// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public class SafeFileHandle_GetFileType_Unix : FileSystemTest
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
    }
}
