// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public class DevicesPipesAndSockets : FileSystemTest
    {
        [Theory]
        [MemberData(nameof(DevicePath_FileOptions_TestData))]
        public void CharacterDevice_FileStream_Write(string devicePath, FileOptions fileOptions)
        {
            FileStreamOptions options = new() { Options = fileOptions, Access = FileAccess.Write, Share = FileShare.Write };
            using FileStream fs = new(devicePath, options);
            fs.Write("foo"u8);
        }

        [Theory]
        [MemberData(nameof(DevicePath_FileOptions_TestData))]
        public async Task CharacterDevice_FileStream_WriteAsync(string devicePath, FileOptions fileOptions)
        {
            FileStreamOptions options = new() { Options = fileOptions, Access = FileAccess.Write, Share = FileShare.Write };
            using FileStream fs = new(devicePath, options);
            await fs.WriteAsync("foo"u8.ToArray());
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public void CharacterDevice_WriteAllBytes(string devicePath)
        {
            File.WriteAllBytes(devicePath, "foo"u8.ToArray());
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public async Task CharacterDevice_WriteAllBytesAsync(string devicePath)
        {
            await File.WriteAllBytesAsync(devicePath, "foo"u8.ToArray());
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public void CharacterDevice_WriteAllText(string devicePath)
        {
            File.WriteAllText(devicePath, "foo");
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public async Task CharacterDevice_WriteAllTextAsync(string devicePath)
        {
            await File.WriteAllTextAsync(devicePath, "foo");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks UNIX sockets in our CI environment")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "iOS/tvOS blocks binding to UNIX sockets")]
        public async Task NamedPipe_ReadWrite()
        {
            string fifoPath = GetTestFilePath();
            Assert.Equal(0, mkfifo(fifoPath, 438 /* 666 in octal */ ));

            await Task.WhenAll(
                Task.Run(() => 
                {
                    using var fs = new FileStream(fifoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    ReadByte(fs, 42);
                }),
                Task.Run(() => 
                {
                    using var fs = new FileStream(fifoPath, FileMode.Open, FileAccess.Write, FileShare.Read);
                    WriteByte(fs, 42);
                }));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks UNIX sockets in our CI environment")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "iOS/tvOS blocks binding to UNIX sockets")]
        public async Task NamedPipe_ReadWrite_Async()
        {
            string fifoPath = GetTestFilePath();
            Assert.Equal(0, mkfifo(fifoPath, 438 /* 666 in octal */ ));

            await Task.WhenAll(
                Task.Run(async () => {
                    using var fs = new FileStream(fifoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    await ReadByteAsync(fs, 42);
                }),
                Task.Run(async () => 
                {
                    using var fs = new FileStream(fifoPath, FileMode.Open, FileAccess.Write, FileShare.Read);
                    await WriteByteAsync(fs, 42);
                }));
        }

        private const int AF_UNIX = 1;
        private const int SOCK_STREAM = 1;

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]
        public unsafe void SocketPair_ReadWrite()
        {
            int* ptr = stackalloc int[2];
            Assert.Equal(0, socketpair(AF_UNIX, SOCK_STREAM, 0, ptr));

            using var readFileStream = new FileStream(new SafeFileHandle((IntPtr)ptr[0], ownsHandle: true), FileAccess.Read);
            using var writeFileStream = new FileStream(new SafeFileHandle((IntPtr)ptr[1], ownsHandle: true), FileAccess.Write);

            Task.WhenAll(
                Task.Run(() => ReadByte(readFileStream, 42)),
                Task.Run(() => WriteByte(writeFileStream, 42))).GetAwaiter().GetResult();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]
        public void SocketPair_ReadWrite_Async()
        {
            unsafe
            {
                int* ptr = stackalloc int[2];
                Assert.Equal(0, socketpair(AF_UNIX, SOCK_STREAM, 0, ptr));

                using var readFileStream = new FileStream(new SafeFileHandle((IntPtr)ptr[0], ownsHandle: true), FileAccess.Read);
                using var writeFileStream = new FileStream(new SafeFileHandle((IntPtr)ptr[1], ownsHandle: true), FileAccess.Write);

                Task.WhenAll(
                    ReadByteAsync(readFileStream, 42),
                    WriteByteAsync(writeFileStream, 42)).GetAwaiter().GetResult();
            }
        }

        private static void ReadByte(FileStream fs, byte expected)
        {
            var buffer = new byte[1];
            Assert.Equal(1, fs.Read(buffer));
            Assert.Equal(expected, buffer[0]);
        }

        private static void WriteByte(FileStream fs, byte value)
        {
            fs.Write(new byte[] { value });
            fs.Flush();
        }

        private static async Task ReadByteAsync(FileStream fs, byte expected)
        {
            var buffer = new byte[1];
            Assert.Equal(1, await fs.ReadAsync(buffer));
            Assert.Equal(expected, buffer[0]);
        }

        private static async Task WriteByteAsync(FileStream fs, byte value)
        {
            await fs.WriteAsync(new byte[] { value });
            await fs.FlushAsync();
        }

        private static Lazy<IEnumerable<string>> AvailableDevicePaths = new Lazy<IEnumerable<string>>(() =>
        { 
            List<string> paths = new();
            FileStreamOptions options = new() { Access = FileAccess.Write, Share = FileShare.Write };

            foreach (string devicePath in new[] { "/dev/tty", "/dev/console", "/dev/null", "/dev/zero" })
            {
                if (!File.Exists(devicePath))
                {
                    continue;
                }

                try
                {
                    File.Open(devicePath, options).Dispose();
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        continue;
                    }

                    throw;
                }

                paths.Add(devicePath);
            }

            return paths;
        });

        public static IEnumerable<object[]> DevicePath_FileOptions_TestData()
        {
            foreach (string devicePath in AvailableDevicePaths.Value)
            {
                foreach (FileOptions options in new[] { FileOptions.None, FileOptions.Asynchronous })
                {
                    yield return new object[] { devicePath, options};
                }
            }
        }

        public static IEnumerable<object[]> DevicePath_TestData()
        {
            foreach (string devicePath in AvailableDevicePaths.Value)
            {
                yield return new object[] { devicePath };
            }
        }

        [DllImport("libc")]
        private static unsafe extern int socketpair(int domain, int type, int protocol, int* ptr);
    }
}
