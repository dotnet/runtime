// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStream_ctor_options_as_base : FileStream_ctor_str_fm_fa_fs_buffer_fo
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

        protected FileStreamOptions GetOptions(FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
            => new FileStreamOptions
            {
                Mode = mode,
                Access = access,
                Share = share,
                Options = options,
                PreallocationSize = preallocationSize
            };
    }

    [CollectionDefinition("NoParallelTests", DisableParallelization = true)]
    public partial class NoParallelTests { }

    // Don't run in parallel as the WhenDiskIsFullTheErrorMessageContainsAllDetails test
    // consumes entire available free space on the disk (only on Linux, this is how posix_fallocate works)
    // and if we try to run other disk-writing test in the meantime we are going to get "No space left on device" exception.
    [Collection("NoParallelTests")]
    public partial class FileStream_ctor_options_as : FileStream_ctor_options_as_base
    {
        [Fact]
        public virtual void NegativePreallocationSizeThrows()
        {
            string filePath = GetPathToNonExistingFile();
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new FileStream(filePath, GetOptions(FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.None, -1)));
        }

        [Theory]
        [InlineData(FileMode.Append)]
        [InlineData(FileMode.Open)]
        [InlineData(FileMode.OpenOrCreate)]
        public void PreallocationSizeThrowsForFileModeOpenAndAppend(FileMode mode)
        {
            const int initialSize = 10;
            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            Assert.Throws<ArgumentException>(
                () => new FileStream(filePath, GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize: 20)));
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.Truncate)]
        public void PreallocationSize(FileMode mode)
        {
            const long preallocationSize = 123;

            string filePath = GetPathToNonExistingFile();
            if (mode == FileMode.Truncate)
            {
                const int initialSize = 10;
                File.WriteAllBytes(filePath, new byte[initialSize]);
            }

            using (var fs = new FileStream(filePath, GetOptions(mode, FileAccess.ReadWrite, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(0, fs.Length);
                Assert.True(GetAllocatedSize(fs) > preallocationSize);
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
        [InlineData(FileMode.Truncate)]
        public void WhenDiskIsFullTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = 1024L * 1024L * 1024L * 1024L; // 1 TB

            string filePath = GetPathToNonExistingFile();

            IOException ex = Assert.Throws<IOException>(() => new FileStream(filePath, GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, tooMuch)));
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
}
