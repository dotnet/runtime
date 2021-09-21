// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace System.IO.Tests
{
    // Don't run in parallel as the WhenDiskIsFullTheErrorMessageContainsAllDetails test
    // consumes entire available free space on the disk (only on Linux, this is how posix_fallocate works)
    // and if we try to run other disk-writing test in the meantime we are going to get "No space left on device" exception.
    [Collection("NoParallelTests")]
    public partial class FileStream_ctor_options : FileStream_ctor_str_fm_fa_fs_buffer_fo
    {
        protected override string GetExpectedParamName(string paramName) => "value";

        protected override FileStream CreateFileStream(string path, FileMode mode)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite
                    });

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = access
                    });

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = access,
                        Share = share,
                        BufferSize = bufferSize,
                        Options = options
                    });

        protected virtual FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = access,
                        Share = share,
                        BufferSize = bufferSize,
                        Options = options,
                        PreallocationSize = preallocationSize
                    });

        [Fact]
        public virtual void NegativePreallocationSizeThrows()
        {
            string filePath = GetTestFilePath();
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateFileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: -1));
        }

        [Theory]
        [InlineData(FileMode.Append)]
        [InlineData(FileMode.Open)]
        [InlineData(FileMode.OpenOrCreate)]
        [InlineData(FileMode.Truncate)]
        public void PreallocationSizeThrowsForExistingFiles(FileMode mode)
        {
            const int initialSize = 10;
            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            Assert.Throws<ArgumentException>(
                () => CreateFileStream(filePath, mode, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 20));
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        public void PreallocationSize(FileMode mode)
        {
            const long preallocationSize = 123;

            using (var fs = CreateFileStream(GetPathToNonExistingFile(), mode, FileAccess.ReadWrite, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize))
            {
                Assert.Equal(0, fs.Length);
                if (SupportsPreallocation)
                {
                    Assert.True(GetAllocatedSize(fs) >= preallocationSize);
                }
                else
                {
                    Assert.Equal(0, GetAllocatedSize(fs));
                }
                Assert.Equal(0, fs.Position);
            }
        }

        [OuterLoop("Might allocate 1 TB file if there is enough space on the disk")]
        // macOS fcntl doc does not mention ENOSPC error: https://developer.apple.com/library/archive/documentation/System/Conceptual/ManPages_iPhoneOS/man2/fcntl.2.html
        // But depending on the OS version, it might actually return it.
        // Since we don't want to have unstable tests, it's better to not run it on macOS at all.
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        public void WhenDiskIsFullTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = 1024L * 1024L * 1024L * 1024L; // 1 TB

            string filePath = GetPathToNonExistingFile();

            IOException ex = Assert.Throws<IOException>(() => CreateFileStream(filePath, mode, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, tooMuch));
            Assert.Contains(filePath, ex.Message);
            Assert.Contains(tooMuch.ToString(), ex.Message);

            // ensure it was NOT created
            bool exists = File.Exists(filePath);
            if (exists)
            {
                File.Delete(filePath);
            }
            Assert.False(exists);
        }

        private string GetPathToNonExistingFile()
        {
            string filePath = GetTestFilePath();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return filePath;
        }
    }

    [CollectionDefinition("NoParallelTests", DisableParallelization = true)]
    public partial class NoParallelTests { }
}
