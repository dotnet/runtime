// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_options_as
    {
        [Theory]
        [InlineData(@"\\?\")]
        [InlineData(@"\??\")]
        [InlineData("")]
        public void ExtendedPathsAreSupported(string prefix)
        {
            const long preallocationSize = 123;

            string filePath = prefix + Path.GetFullPath(GetPathToNonExistingFile());

            using (var fs = new FileStream(filePath, GetOptions(FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(preallocationSize, fs.Length);
            }
        }

        [Fact]
        public async Task PreallocationSizeIsIgnoredForNonSeekableFiles()
        {
            string pipeName = GetNamedPipeServerStreamName();
            string pipePath = Path.GetFullPath($@"\\.\pipe\{pipeName}");

            FileStreamOptions options = new() { Mode = FileMode.Open, Access = FileAccess.Write, Share = FileShare.None, PreallocationSize = 123 };

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.In))
            using (var clienStream = new FileStream(pipePath, options))
            {
                await server.WaitForConnectionAsync();

                Assert.False(clienStream.CanSeek);
            }
        }

        [ConditionalTheory(nameof(IsFat32))]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.OpenOrCreate)]
        public void WhenFileIsTooLargeTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = uint.MaxValue + 1L; // more than FAT32 max size

            string filePath = GetPathToNonExistingFile();
            Assert.StartsWith(Path.GetTempPath(), filePath); // this is what IsFat32 method relies on

            IOException ex = Assert.Throws<IOException>(() => new FileStream(filePath, GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, tooMuch)));
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
