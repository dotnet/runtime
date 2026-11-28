// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_options
    {
        private unsafe long GetAllocatedSize(FileStream fileStream)
        {
            Interop.Kernel32.FILE_STANDARD_INFO info;

            Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fileStream.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));

            return info.AllocationSize;
        }

        private static bool SupportsPreallocation => true;

        private static bool IsGetAllocatedSizeImplemented => true;

        [Theory]
        [InlineData(@"\\?\")]
        [InlineData(@"\??\")]
        [InlineData("")]
        public void ExtendedPathsAreSupported(string prefix)
        {
            const long preallocationSize = 123;

            string filePath = prefix + Path.GetFullPath(GetTestFilePath());

            using (var fs = CreateFileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, FileOptions.None, preallocationSize))
            {
                Assert.Equal(0, fs.Length);
                Assert.True(GetAllocatedSize(fs) >= preallocationSize);
            }
        }

        [ConditionalTheory(typeof(FileSystemTest), nameof(FileSystemTest.IsTempPathOnFat32))]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        public void WhenFileIsTooLargeTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = uint.MaxValue + 1L; // more than FAT32 max size

            string filePath = GetTestFilePath();
            Assert.StartsWith(Path.GetTempPath(), filePath); // this is what IsFat32 method relies on

            IOException ex = Assert.Throws<IOException>(() => CreateFileStream(filePath, mode, FileAccess.Write, FileShare.None, bufferSize: 4096, FileOptions.None, tooMuch));
            Assert.Contains(filePath, ex.Message);
            Assert.Contains(tooMuch.ToString(), ex.Message);

            Assert.False(File.Exists(filePath)); // ensure it was NOT created
        }
    }
}
