// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_options
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsStartingProcessesSupported))]
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

        [Theory]
        [InlineData(FileMode.Append)]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.OpenOrCreate)]
        public void CreateDoesntChangeExistingUnixFileMode(FileMode fileMode)
        {
            // Create file as writable for user only.
            const UnixFileMode mode = UnixFileMode.UserWrite;
            string filename = GetTestFilePath();
            CreateFileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 0, mode).Dispose();

            // Now open with AllAccess.
            using FileStream fs = CreateFileStream(filename, fileMode, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 0, AllAccess);
            UnixFileMode actualMode = File.GetUnixFileMode(filename);
            Assert.Equal(mode, actualMode);
        }

        [Theory]
        [InlineData(FileMode.Append, true)]
        [InlineData(FileMode.Create, true)]
        [InlineData(FileMode.CreateNew, true)]
        [InlineData(FileMode.OpenOrCreate, true)]
        [InlineData(FileMode.Truncate, false)]
        [InlineData(FileMode.Open, false)]
        public void UnixCreateModeThrowsForNonCreatingFileModes(FileMode fileMode, bool canSetUnixCreateMode)
        {
            const UnixFileMode unixMode = UnixFileMode.UserWrite;
            string filename = GetTestFilePath();

            if (canSetUnixCreateMode)
            {
                CreateFileStream(filename, fileMode, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 0, unixMode).Dispose();

                UnixFileMode actualMode = File.GetUnixFileMode(filename);
                Assert.Equal(unixMode, actualMode);
            }
            else
            {
                Assert.Throws<ArgumentException>(() => CreateFileStream(filename, fileMode, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 0, unixMode));
            }
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
