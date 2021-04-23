// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_str_fm_fa_fs_buffer_fo_as : FileStream_ctor_str_fm_fa_fs_buffer_fo_as_base
    {
        protected override long AllocationSize => 10;

        protected override long InitialLength => 0; // Windows modifies AllocationSize, but not EndOfFile (file length)

        private long GetExpectedFileLength(long allocationSize) => 0; // Windows modifies AllocationSize, but not EndOfFile (file length)

        private unsafe long GetActualAllocationSize(FileStream fileStream)
        {
            Interop.Kernel32.FILE_STANDARD_INFO info;

            Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fileStream.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));

            return info.AllocationSize;
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalTheory(nameof(IsFat32))]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.OpenOrCreate)]
        public void WhenFileIsTooLargeTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = uint.MaxValue + 1L; // more than FAT32 max size

            string filePath = GetPathToNonExistingFile();
            Assert.StartsWith(Path.GetTempPath(), filePath); // this is what IsFat32 method relies on

            IOException ex = Assert.Throws<IOException>(() => new FileStream(filePath, mode, FileAccess.Write, FileShare.None, c_DefaultBufferSize, FileOptions.None, tooMuch));
            Assert.Contains("file was too large", ex.Message);
            Assert.Contains(filePath, ex.Message);
            Assert.Contains(tooMuch.ToString(), ex.Message);

            Assert.False(File.Exists(filePath)); // ensure it was NOT created
        }

        public static bool IsFat32
        {
            get
            {
                string testDirectory = Path.GetTempPath(); // logic taken from FileCleanupTestBase, can't call the property here as it's not static

                var volumeNameBufffer = new StringBuilder(250);
                var fileSystemNameBuffer = new StringBuilder(250);

                if (GetVolumeInformation(
                    Path.GetPathRoot(testDirectory),
                    volumeNameBufffer,
                    volumeNameBufffer.Capacity,
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
