// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStream_ctor_str_fm_fa_fs_buffer_fo_as_base : FileStream_ctor_str_fm_fa_fs_buffer_fo
    {
        protected abstract long AllocationSize { get; }

        protected override FileStream CreateFileStream(string path, FileMode mode)
            => new FileStream(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, allocationSize: AllocationSize);

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
            => new FileStream(path, mode, access, allocationSize: AllocationSize);

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => new FileStream(path, mode, access, share, bufferSize, options, allocationSize: AllocationSize);
    }

    public class FileStream_ctor_str_fm_fa_fs_buffer_fo_as_zero : FileStream_ctor_str_fm_fa_fs_buffer_fo_as_base
    {
        protected override long AllocationSize => 0; // specifying 0 should have no effect

        protected override long InitialLength => 0;
    }

    public class FileStream_ctor_str_fm_fa_fs_buffer_fo_as_negative : FileStream_ctor_str_fm_fa_fs_buffer_fo_as_base
    {
        protected override long AllocationSize => -1; // specifying negative value should have no effect

        protected override long InitialLength => 0;
    }

    [CollectionDefinition("NoParallelTests", DisableParallelization = true)]
    public partial class NoParallelTests { }

    // Don't run in parallel as the WhenDiskIsFullTheErrorMessageContainsAllDetails test
    // consumes entire available free space on the disk (only on Linux, this is how posix_fallocate works)
    // and if we try to run other disk-writing test in the meantime we are going to get "No space left on device" exception.
    [Collection("NoParallelTests")]
    public partial class FileStream_ctor_str_fm_fa_fs_buffer_fo_as : FileStream_ctor_str_fm_fa_fs_buffer_fo_as_base
    {
        [Theory]
        [InlineData(FileMode.Create, 0L)]
        [InlineData(FileMode.Create, -1L)]
        [InlineData(FileMode.CreateNew, 0L)]
        [InlineData(FileMode.CreateNew, -1L)]
        [InlineData(FileMode.OpenOrCreate, 0L)]
        [InlineData(FileMode.OpenOrCreate, -1L)]
        public void WhenFileIsCreatedWithoutAllocationSizeSpecifiedTheAllocationSizeIsNotSet(FileMode mode, long allocationSize)
        {
            using (var fs = new FileStream(GetPathToNonExistingFile(), mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Assert.Equal(0, GetActualAllocationSize(fs));
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileMode.Open, 0L)]
        [InlineData(FileMode.Open, -1L)]
        [InlineData(FileMode.Open, 1L)]
        [InlineData(FileMode.OpenOrCreate, 0L)]
        [InlineData(FileMode.OpenOrCreate, -1L)]
        [InlineData(FileMode.OpenOrCreate, 1L)]
        [InlineData(FileMode.Append, 0L)]
        [InlineData(FileMode.Append, -1L)]
        [InlineData(FileMode.Append, 1L)]
        public void WhenExistingFileIsBeingOpenedWithAllocationSizeSpecifiedTheAllocationSizeIsNotChanged(FileMode mode, long allocationSize)
        {
            const int initialSize = 1;
            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);
            long initialAllocationSize;

            using (var fs = new FileStream(filePath, mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None)) // allocationSize NOT provided
            {
                initialAllocationSize = GetActualAllocationSize(fs); // just read it to ensure it's not being changed
            }

            using (var fs = new FileStream(filePath, mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Assert.Equal(initialAllocationSize, GetActualAllocationSize(fs)); // it has NOT been changed
                Assert.Equal(initialSize, fs.Length);
                Assert.Equal(mode == FileMode.Append ? initialSize : 0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.OpenOrCreate)]
        public void WhenFileIsCreatedWithAllocationSizeSpecifiedTheAllocationSizeIsSet(FileMode mode)
        {
            const long allocationSize = 123;

            using (var fs = new FileStream(GetPathToNonExistingFile(), mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                // OS might allocate MORE than we have requested
                Assert.True(GetActualAllocationSize(fs) >= allocationSize, $"Provided {allocationSize}, actual: {GetActualAllocationSize(fs)}");
                Assert.Equal(GetExpectedFileLength(allocationSize), fs.Length);
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

            IOException ex = Assert.Throws<IOException>(() => new FileStream(filePath, mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, tooMuch));
            Assert.Contains(filePath, ex.Message);
            Assert.Contains(tooMuch.ToString(), ex.Message);

            Assert.False(File.Exists(filePath)); // ensure it was NOT created (provided OOTB by Windows, emulated on Unix)
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        public void WhenFileIsTruncatedWithoutAllocationSizeSpecifiedTheAllocationSizeIsNotSet(int allocationSize)
        {
            const int initialSize = 10_000;

            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            using (var fs = new FileStream(filePath, FileMode.Truncate, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Assert.Equal(0, GetActualAllocationSize(fs));
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Fact]
        public void WhenFileIsTruncatedWithAllocationSizeSpecifiedTheAllocationSizeIsSet()
        {
            const int initialSize = 10_000; // this must be more than 4kb which seems to be minimum allocaiton size on Windows
            const long allocationSize = 100;

            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            using (var fs = new FileStream(filePath, FileMode.Truncate, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Assert.True(GetActualAllocationSize(fs) >= allocationSize, $"Provided {allocationSize}, actual: {GetActualAllocationSize(fs)}");
                // less than initial file size (file got truncated)
                Assert.True(GetActualAllocationSize(fs) < initialSize, $"initialSize {initialSize}, actual: {GetActualAllocationSize(fs)}");
                Assert.Equal(GetExpectedFileLength(allocationSize), fs.Length);
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
