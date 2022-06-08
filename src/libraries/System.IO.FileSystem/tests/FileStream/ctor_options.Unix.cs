// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_options
    {
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [MemberData(nameof(TestUnixFileModes))]
        public void CreateWithUnixFileMode(UnixFileMode mode)
        {
            string filename = GetTestFilePath();
            FileStream fs = CreateFileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 0, mode);
            fs.Dispose();

            UnixFileMode platformFilter = PlatformDetection.IsBsdLike
                                            ? (UnixFileMode.SetGroup | UnixFileMode.SetUser | UnixFileMode.StickyBit)
                                            : UnixFileMode.None;
            UnixFileMode expectedMode = mode & ~GetUmask() & ~platformFilter;
            UnixFileMode actualMode = File.GetUnixFileMode(filename);
            Assert.Equal(expectedMode, actualMode);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void CreateDoesntChangeExistingMode()
        {
            // Create file as writable for user only.
            const UnixFileMode mode = UnixFileMode.UserWrite;
            string filename = GetTestFilePath();
            using FileStream fs = CreateFileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 0, mode);
            fs.Dispose();

            // Now open with AllAccess.
            using FileStream fs2 = CreateFileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 0, AllAccess);
            UnixFileMode actualMode = File.GetUnixFileMode(filename);
            Assert.Equal(mode, actualMode);
        }

        private static long GetAllocatedSize(FileStream fileStream)
        {
            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            // Call 'stat' to get the number of blocks, and size of blocks.
            using var px = Process.Start(new ProcessStartInfo
            {
                FileName = "stat",
                ArgumentList = { PlatformDetection.IsBsdLike ? "-f"    : "-c",
                                 PlatformDetection.IsBsdLike ? "%b %k" : "%b %B",
                                 fileStream.Name },
                RedirectStandardOutput = true
            });
            string stdout = px.StandardOutput.ReadToEnd();

            string[] parts = stdout.Split(' ');
            return long.Parse(parts[0]) * long.Parse(parts[1]);
        }

        private static bool SupportsPreallocation =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        // Mobile platforms don't support Process.Start.
        private static bool IsGetAllocatedSizeImplemented => !PlatformDetection.IsMobile;
    }
}
