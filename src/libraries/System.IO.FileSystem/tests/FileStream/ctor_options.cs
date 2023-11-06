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
    [Collection(nameof(DisableParallelization))]
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

        protected virtual FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize, UnixFileMode unixFileMode)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = access,
                        Share = share,
                        BufferSize = bufferSize,
                        Options = options,
                        PreallocationSize = preallocationSize,
                        UnixCreateMode = unixFileMode
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
        public void PreallocationSizeThrowsForFileModesThatOpenExistingFiles(FileMode mode)
        {
            Assert.Throws<ArgumentException>(
                () => CreateFileStream(GetTestFilePath(), mode, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 20));
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        public void PreallocationSizeThrowsForReadOnlyAccess(FileMode mode)
        {
            Assert.Throws<ArgumentException>(
                () => CreateFileStream(GetTestFilePath(), mode, FileAccess.Read, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 20));
        }

        [Theory]
        [InlineData(FileMode.Create, false)]
        [InlineData(FileMode.Create, true)]
        [InlineData(FileMode.CreateNew, false)]
        [InlineData(FileMode.Append, false)]
        [InlineData(FileMode.Append, true)]
        [InlineData(FileMode.Open, true)]
        [InlineData(FileMode.OpenOrCreate, true)]
        [InlineData(FileMode.OpenOrCreate, false)]
        [InlineData(FileMode.Truncate, true)]
        public void ZeroPreallocationSizeDoesNotAllocate(FileMode mode, bool createFile)
        {
            string filename = GetTestFilePath();

            if (createFile)
            {
                File.WriteAllText(filename, "");
            }

            using (FileStream fs = CreateFileStream(filename, mode, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize: 0))
            {
                Assert.Equal(0, fs.Length);
                if (IsGetAllocatedSizeImplemented)
                {
                    Assert.Equal(0, GetAllocatedSize(fs));
                }
                Assert.Equal(0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileAccess.Write, FileMode.Create)]
        [InlineData(FileAccess.Write, FileMode.CreateNew)]
        [InlineData(FileAccess.ReadWrite, FileMode.Create)]
        [InlineData(FileAccess.ReadWrite, FileMode.CreateNew)]
        public void PreallocationSize(FileAccess access, FileMode mode)
        {
            const long preallocationSize = 123;

            using (var fs = CreateFileStream(GetTestFilePath(), mode, access, FileShare.None, bufferSize: 1, FileOptions.None, preallocationSize))
            {
                Assert.Equal(0, fs.Length);
                if (IsGetAllocatedSizeImplemented)
                {
                    if (SupportsPreallocation)
                    {
                        Assert.True(GetAllocatedSize(fs) >= preallocationSize);
                    }
                    else
                    {
                        Assert.Equal(0, GetAllocatedSize(fs));
                    }
                }
                Assert.Equal(0, fs.Position);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/92624", TestPlatforms.Windows)]
        // macOS fcntl doc does not mention ENOSPC error: https://developer.apple.com/library/archive/documentation/System/Conceptual/ManPages_iPhoneOS/man2/fcntl.2.html
        // But depending on the OS version, it might actually return it.
        // Since we don't want to have unstable tests, it's better to not run it on macOS at all.
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        public void WhenDiskIsFullTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = 1024L * 1024L * 1024L * 1024L * 1024L * 1024L; // 1 Exbibyte .. we are assuming this is not available
            string filePath = GetTestFilePath();

            try
            {
                // not using Assert.Throws because in the event of failure, we want to dispose, so the next test isn't affected
                using FileStream fs = CreateFileStream(filePath, mode, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None, tooMuch);
                Assert.Fail($"Expected to throw IOException, {fs.Length}");
            }
            catch (IOException ex)
            {
                Assert.Contains(filePath, ex.Message);
                Assert.Contains(tooMuch.ToString(), ex.Message);

                // ensure it was NOT created
                Assert.False(File.Exists(filePath));
            }
        }
    }
}
