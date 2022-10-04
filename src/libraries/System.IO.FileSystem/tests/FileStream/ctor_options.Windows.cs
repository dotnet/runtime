// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
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

        [ConditionalTheory(nameof(IsFat32))]
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

        public static bool IsFat32
        {
            get
            {
                string testDirectory = Path.GetTempPath(); // logic taken from FileCleanupTestBase, can't call the property here as it's not static

                var volumeNameBuffer = new StringBuilder(250);
                var fileSystemNameBuffer = new StringBuilder(250);

                if (GetVolumeInformation(
                    Path.GetPathRoot(testDirectory),
                    volumeNameBuffer,
                    volumeNameBuffer.Capacity,
                    out uint _,
                    out uint _,
                    out uint _,
                    fileSystemNameBuffer,
                    fileSystemNameBuffer.Capacity
                    ))
                {
                    return fileSystemNameBuffer.ToString().Equals("FAT32", StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
        }

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool GetVolumeInformation(
           string rootPathName,
           StringBuilder volumeNameBuffer,
           int volumeNameSize,
           out uint volumeSerialNumber,
           out uint maximumComponentLength,
           out uint fileSystemFlags,
           StringBuilder fileSystemNameBuffer,
           int fileSystemNameSize);
    }
}
