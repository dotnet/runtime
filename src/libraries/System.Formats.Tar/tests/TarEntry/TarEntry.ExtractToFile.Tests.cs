// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarEntry_ExtractToFile_Tests : TarTestsBase
    {
        [Fact]
        public void ExtractToFile_MismatchedSizeField_DoesNotPreallocateDeclaredSize()
        {
            // Craft an entry whose header declares a size much larger than the archive
            // actually contains. If the declared size were used verbatim for preallocation,
            // extraction could attempt to reserve an arbitrarily large amount of disk space
            // (disk-exhaustion) even though almost no real data backs it. Instead, extraction
            // should only preallocate/write as much data as is actually available, producing
            // a truncated file rather than reserving the bogus declared size.
            const long HugeDeclaredSize = 500_000_000; // 500 MB declared, far exceeding the tiny real archive.
            byte[] actualData = "small"u8.ToArray();

            byte[] archive = BuildRawPaxArchiveWithSizeOverride("file.bin", "file.bin", actualData, HugeDeclaredSize, HugeDeclaredSize);

            long apiLength;
            using (var scanStream = new MemoryStream(archive))
            using (var reader = new TarReader(scanStream))
            {
                TarEntry entry = reader.GetNextEntry(copyData: false);
                Assert.NotNull(entry);
                apiLength = entry.Length;
                Assert.Equal(HugeDeclaredSize, apiLength);
            }

            using TempDirectory root = new TempDirectory();
            string destination = Path.Join(root.Path, "file.bin");

            using var extractStream = new MemoryStream(archive);
            using var extractReader = new TarReader(extractStream);
            TarEntry entryToExtract = extractReader.GetNextEntry(copyData: false);
            Assert.NotNull(entryToExtract);

            // Should complete without hanging or exhausting disk trying to preallocate 500 MB
            // for a few bytes of real data; the resulting file is truncated to the real data available.
            entryToExtract.ExtractToFile(destination, overwrite: false);

            long extractedSize = new FileInfo(destination).Length;
            Assert.True(extractedSize < HugeDeclaredSize);
        }

        [Theory]
        [InlineData(10, 100_000)] // declared size far larger than the entire remaining archive: extraction is bounded by available data, not the bogus declared size
        [InlineData(100, 25)]     // declared size smaller than actual data: gets truncated to the declared size
        public void ExtractToFile_MismatchedSizeField_MatchesAvailableData(int dataSize, long headerSizeField)
        {
            byte[] actualData = new byte[dataSize];
            Array.Fill<byte>(actualData, (byte)'X');

            byte[] archive = BuildRawPaxArchiveWithSizeOverride("file.bin", "file.bin", actualData, headerSizeField, headerSizeField);

            using TempDirectory root = new TempDirectory();
            string destination = Path.Join(root.Path, "file.bin");

            using var extractStream = new MemoryStream(archive);
            using var reader = new TarReader(extractStream);
            TarEntry entry = reader.GetNextEntry(copyData: false);
            Assert.NotNull(entry);

            entry.ExtractToFile(destination, overwrite: false);
            long extractedSize = new FileInfo(destination).Length;

            if (headerSizeField <= dataSize)
            {
                Assert.Equal(headerSizeField, extractedSize);
            }
            else
            {
                // Not enough real data to satisfy the declared size: extraction stops once the
                // underlying archive stream is exhausted, and never reaches the huge declared size.
                Assert.True(extractedSize < headerSizeField);
                Assert.True(extractedSize <= archive.Length);
            }
        }


        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => entry.ExtractToFile(root.Path, overwrite: false));

            Assert.False(File.Exists(fullPath));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_Name_FullPath_DestinationDirectory_Match_AdditionalSubdirectory_Throws(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "dir", "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => entry.ExtractToFile(root.Path, overwrite: false));

            Assert.False(File.Exists(fullPath));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_Name_FullPath_DestinationDirectory_Match(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            entry.ExtractToFile(fullPath, overwrite: false);

            Assert.True(File.Exists(fullPath));
        }

        [Theory]
        [MemberData(nameof(GetFormatsAndLinks))]
        public void ExtractToFile_Link_Throws(TarEntryFormat format, TarEntryType entryType)
        {
            using TempDirectory root = new TempDirectory();
            string fileName = "mylink";
            string fullPath = Path.Join(root.Path, fileName);

            string linkTarget = PlatformDetection.IsWindows ? @"C:\Windows\system32\notepad.exe" : "/usr/bin/nano";

            TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, fileName);
            entry.LinkName = linkTarget;

            Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(fileName, overwrite: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }

        [Theory]
        [MemberData(nameof(GetFormatsAndFiles))]
        public void Extract(TarEntryFormat format, TarEntryType entryType)
        {
            using TempDirectory root = new TempDirectory();

            (string entryName, string destination, TarEntry entry) = Prepare_Extract(root, format, entryType);

            entry.ExtractToFile(destination, overwrite: true);

            Verify_Extract(destination, entry, entryType);
        }
    }
}
