// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarEntry_ExtractToFile_Tests : TarTestsBase
    {
        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task ExtractToFileAsync_Cancel(TarEntryFormat format)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            return Assert.ThrowsAsync<TaskCanceledException>(() => entry.ExtractToFileAsync("dir", overwrite: true, cs.Token));
        }

        [Theory]
        [MemberData(nameof(GetFormatBooleanData))]
        public async Task Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws(TarEntryFormat format, bool async)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            await Assert.ThrowsAsync<IOException>(() => ExtractToFile(entry, root.Path, overwrite: false, async));

            Assert.False(File.Exists(fullPath));
        }

        [Theory]
        [MemberData(nameof(GetFormatBooleanData))]
        public async Task Constructor_Name_FullPath_DestinationDirectory_Match_AdditionalSubdirectory_Throws(TarEntryFormat format, bool async)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "dir", "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            await Assert.ThrowsAsync<IOException>(() => ExtractToFile(entry, root.Path, overwrite: false, async));

            Assert.False(File.Exists(fullPath));
        }

        [Theory]
        [MemberData(nameof(GetFormatBooleanData))]
        public async Task Constructor_Name_FullPath_DestinationDirectory_Match(TarEntryFormat format, bool async)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            await ExtractToFile(entry, fullPath, overwrite: false, async);

            Assert.True(File.Exists(fullPath));
        }

        [Theory]
        [MemberData(nameof(GetFormatsAndLinksAndBooleanData))]
        public async Task ExtractToFile_Link_Throws(TarEntryFormat format, TarEntryType entryType, bool async)
        {
            using TempDirectory root = new TempDirectory();
            string fileName = "mylink";

            string linkTarget = PlatformDetection.IsWindows ? @"C:\Windows\system32\notepad.exe" : "/usr/bin/nano";

            TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, fileName);
            entry.LinkName = linkTarget;

            await Assert.ThrowsAsync<InvalidOperationException>(() => ExtractToFile(entry, fileName, overwrite: false, async));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }

        [Theory]
        [MemberData(nameof(GetFormatsAndFilesAndBooleanData))]
        public async Task Extract(TarEntryFormat format, TarEntryType entryType, bool async)
        {
            using TempDirectory root = new TempDirectory();

            (string entryName, string destination, TarEntry entry) = Prepare_Extract(root, format, entryType);

            await ExtractToFile(entry, destination, overwrite: true, async);

            Verify_Extract(destination, entry, entryType);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux | TestPlatforms.OSX)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(256, false)]
        [InlineData(4095, false)]
        [InlineData(4096, false)]
        [InlineData(8192, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(256, true)]
        [InlineData(4095, true)]
        [InlineData(4096, true)]
        [InlineData(8192, true)]
        public async Task ExtractBufferedData_RestoresModificationTime(int length, bool async)
        {
            using TempDirectory root = new TempDirectory();
            string destination = Path.Join(root.Path, "file.txt");
            byte[] expected = Enumerable.Range(0, length).Select(value => (byte)(value % 251)).ToArray();
            using MemoryStream source = new MemoryStream(expected);
            PaxTarEntry entry = new(TarEntryType.RegularFile, "file.txt")
            {
                DataStream = source,
                ModificationTime = TestModificationTime
            };

            await ExtractToFile(entry, destination, overwrite: false, async);

            Assert.Equal(expected, File.ReadAllBytes(destination));
            Assert.Equal(TestModificationTime.UtcDateTime, File.GetLastWriteTimeUtc(destination));
        }
    }
}
