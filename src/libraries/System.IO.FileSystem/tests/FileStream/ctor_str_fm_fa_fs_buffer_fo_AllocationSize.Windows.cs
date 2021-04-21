// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize_Windows : FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize
    {
        protected override long AllocationSize => 10;
        
        protected override long InitialLength => 0; // Windows modifies AllocationSize, but not EndOfFile (file length)

        [Theory]
        [InlineData(FileMode.Create, 0L)]
        [InlineData(FileMode.Create, -1L)]
        [InlineData(FileMode.CreateNew, 0L)]
        [InlineData(FileMode.CreateNew, -1L)]
        [InlineData(FileMode.OpenOrCreate, 0L)]
        [InlineData(FileMode.OpenOrCreate, -1L)]
        public unsafe void WhenFileIsCreatedWithoutAllocationSizeSpecifiedTheAllocationSizeIsNotSet(FileMode mode, long allocationSize)
        {
            using (var fs = new FileStream(GetPathToNonExistingFile(), mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Interop.Kernel32.FILE_STANDARD_INFO info;

                Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fs.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));
                Assert.Equal(0, info.AllocationSize);
                Assert.Equal(0, info.EndOfFile);

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
        public unsafe void WhenExistingFileIsBeingOpenedWithAllocationSizeSpecifiedTheAllocationSizeIsNotChanged(FileMode mode, long allocationSize)
        {
            const int initialSize = 1;
            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);
            long initialAllocationSize;

            using (var fs = new FileStream(filePath, mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None)) // allocationSize NOT provided
            {
                Interop.Kernel32.FILE_STANDARD_INFO info;

                Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fs.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));
                initialAllocationSize = info.AllocationSize; // just read it to ensure it's not being changed
            }

            using (var fs = new FileStream(filePath, mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Interop.Kernel32.FILE_STANDARD_INFO info;

                Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fs.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));
                Assert.Equal(initialAllocationSize, info.AllocationSize); // it has NOT been changed
                Assert.Equal(initialSize, info.EndOfFile);

                Assert.Equal(initialSize, fs.Length);
                Assert.Equal(mode == FileMode.Append ? initialSize : 0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.OpenOrCreate)]
        public unsafe void WhenFileIsCreatedWithAllocationSizeSpecifiedTheAllocationSizeIsSet(FileMode mode)
        {
            const long allocationSize = 123;

            using (var fs = new FileStream(GetPathToNonExistingFile(), mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Interop.Kernel32.FILE_STANDARD_INFO info;

                Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fs.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));
                Assert.True(info.AllocationSize >= allocationSize); // Windows might allocate MORE than we have requested
                Assert.Equal(0, info.EndOfFile); // Windows modifies AllocationSize, but not EndOfFile (file length)

                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.OpenOrCreate)]
        public void WhenFileStreamFailsToPreallocateDiskSpaceTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = 1024L * 1024L * 1024L * 1024L; // 1 TB

            string filePath = GetPathToNonExistingFile();

            IOException ex = Assert.Throws<IOException>(() => new FileStream(filePath, mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, tooMuch));
            Assert.Contains("disk was full", ex.Message);
            Assert.Contains(filePath, ex.Message);
            Assert.Contains(AllocationSize.ToString(), ex.Message);

            Assert.False(File.Exists(filePath)); // ensure it was NOT created
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        public unsafe void WhenFileIsTruncatedWithoutAllocationSizeSpecifiedTheAllocationSizeIsNotSet(int allocationSize)
        {
            const int initialSize = 10_000;

            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            using (var fs = new FileStream(filePath, FileMode.Truncate, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Interop.Kernel32.FILE_STANDARD_INFO info;

                Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fs.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));
                Assert.Equal(0, info.AllocationSize);
                Assert.Equal(0, info.EndOfFile);

                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Fact]
        public unsafe void WhenFileIsTruncatedWithAllocationSizeSpecifiedTheAllocationSizeIsSet()
        {
            const int initialSize = 10_000; // this must be more than 4kb which seems to be minimum allocaiton size on Windows
            const long allocationSize = 100;

            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            using (var fs = new FileStream(filePath, FileMode.Truncate, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, allocationSize))
            {
                Interop.Kernel32.FILE_STANDARD_INFO info;

                Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fs.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));
                Assert.True(info.AllocationSize >= allocationSize);
                Assert.True(info.AllocationSize < initialSize); // less than initial file size (file got truncated)
                Assert.Equal(0, info.EndOfFile);

                Assert.Equal(0, fs.Length);
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
