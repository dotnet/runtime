// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStream_ctor_options_as_base : FileStream_ctor_str_fm_fa_fs_buffer_fo
    {
        protected abstract long PreallocationSize { get; }

        protected override FileStream CreateFileStream(string path, FileMode mode)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite,
                        PreallocationSize = PreallocationSize
                    });

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = access,
                        PreallocationSize = PreallocationSize
                    });

        protected FileStreamOptions GetOptions(FileMode mode, FileAccess access, FileShare share, FileOptions options, long preAllocationSize)
            => new FileStreamOptions
            {
                Mode = mode,
                Access = access,
                Share = share,
                Options = options,
                PreallocationSize = preAllocationSize
            };
    }

    public class FileStream_ctor_options_as_zero : FileStream_ctor_options_as_base
    {
        protected override long PreallocationSize => 0; // specifying 0 should have no effect

        protected override long InitialLength => 0;
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
        public void NegativePreallocationSizeThrows()
        {
            string filePath = GetPathToNonExistingFile();
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new FileStream(filePath, GetOptions(FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.None, -1)));
        }

        [Theory]
        [InlineData(FileMode.Create, 0L)]
        [InlineData(FileMode.CreateNew, 0L)]
        [InlineData(FileMode.OpenOrCreate, 0L)]
        public void WhenFileIsCreatedWithoutPreallocationSizeSpecifiedThePreallocationSizeIsNotSet(FileMode mode, long preallocationSize)
        {
            using (var fs = new FileStream(GetPathToNonExistingFile(), GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(0, GetActualPreallocationSize(fs));
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileMode.Open, 0L)]
        [InlineData(FileMode.Open, 1L)]
        [InlineData(FileMode.OpenOrCreate, 0L)]
        [InlineData(FileMode.OpenOrCreate, 1L)]
        [InlineData(FileMode.Append, 0L)]
        [InlineData(FileMode.Append, 1L)]
        public void WhenExistingFileIsBeingOpenedWithPreallocationSizeSpecifiedThePreallocationSizeIsNotChanged(FileMode mode, long preallocationSize)
        {
            const int initialSize = 1;
            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);
            long initialPreallocationSize;

            using (var fs = new FileStream(filePath, GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, 0))) // preallocationSize NOT provided
            {
                initialPreallocationSize = GetActualPreallocationSize(fs); // just read it to ensure it's not being changed
            }

            using (var fs = new FileStream(filePath, GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(initialPreallocationSize, GetActualPreallocationSize(fs)); // it has NOT been changed
                Assert.Equal(initialSize, fs.Length);
                Assert.Equal(mode == FileMode.Append ? initialSize : 0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.OpenOrCreate)]
        public void WhenFileIsCreatedWithPreallocationSizeSpecifiedThePreallocationSizeIsSet(FileMode mode)
        {
            const long preallocationSize = 123;

            using (var fs = new FileStream(GetPathToNonExistingFile(), GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize)))
            {
                // OS might allocate MORE than we have requested
                Assert.True(GetActualPreallocationSize(fs) >= preallocationSize, $"Provided {preallocationSize}, actual: {GetActualPreallocationSize(fs)}");
                Assert.Equal(GetExpectedFileLength(preallocationSize), fs.Length);
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
        [InlineData(FileMode.OpenOrCreate)]
        public void WhenDiskIsFullTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = 1024L * 1024L * 1024L * 1024L; // 1 TB

            string filePath = GetPathToNonExistingFile();

            IOException ex = Assert.Throws<IOException>(() => new FileStream(filePath, GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, tooMuch)));
            Assert.Contains(filePath, ex.Message);
            Assert.Contains(tooMuch.ToString(), ex.Message);

            // ensure it was NOT created (provided OOTB by Windows, emulated on Unix)
            bool exists = File.Exists(filePath);
            if (exists)
            {
                File.Delete(filePath);
            }
            Assert.False(exists);
        }

        [Fact]
        public void WhenFileIsTruncatedWithoutPreallocationSizeSpecifiedThePreallocationSizeIsNotSet()
        {
            const int initialSize = 10_000;

            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            using (var fs = new FileStream(filePath, GetOptions(FileMode.Truncate, FileAccess.Write, FileShare.None, FileOptions.None, 0)))
            {
                Assert.Equal(0, GetActualPreallocationSize(fs));
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Fact]
        public void WhenFileIsTruncatedWithPreallocationSizeSpecifiedThePreallocationSizeIsSet()
        {
            const int initialSize = 10_000; // this must be more than 4kb which seems to be minimum allocation size on Windows
            const long preallocationSize = 100;

            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            using (var fs = new FileStream(filePath, GetOptions(FileMode.Truncate, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.True(GetActualPreallocationSize(fs) >= preallocationSize, $"Provided {preallocationSize}, actual: {GetActualPreallocationSize(fs)}");
                // less than initial file size (file got truncated)
                Assert.True(GetActualPreallocationSize(fs) < initialSize, $"initialSize {initialSize}, actual: {GetActualPreallocationSize(fs)}");
                Assert.Equal(GetExpectedFileLength(preallocationSize), fs.Length);
                Assert.Equal(0, fs.Position);
            }
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
