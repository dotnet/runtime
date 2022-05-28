// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_Tests : TarReader_File_Tests_Base
    {
        [Theory]
        [InlineData(TarFormat.V7, TestTarFormat.v7)]
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(TarFormat.V7, TestTarFormat.v7)]
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_HardLink(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "file_hardlink";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "file.txt", $"Hello {testCaseName}");

            // The 'tar' unix tool detects hardlinks as regular files and saves them as such in the archives, for all formats
            TarEntry hardLink = reader.GetNextEntry();
            VerifyRegularFileEntry(hardLink, format, "hardlink.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(TarFormat.V7, TestTarFormat.v7)]
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_SymbolicLink(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "file_symlink";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "file.txt", $"Hello {testCaseName}");

            TarEntry symbolicLink = reader.GetNextEntry();
            VerifySymbolicLinkEntry(symbolicLink, format, "link.txt", "file.txt");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(TarFormat.V7, TestTarFormat.v7)]
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_File(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "folder_file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry directory = reader.GetNextEntry();
            VerifyDirectoryEntry(directory, format, "folder/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "folder/file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(TarFormat.V7, TestTarFormat.v7)]
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_File_Utf8(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "folder_file_utf8";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry directory = reader.GetNextEntry();
            VerifyDirectoryEntry(directory, format, "földër/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "földër/áöñ.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(TarFormat.V7, TestTarFormat.v7)]
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_Subfolder_File(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "folder_subfolder_file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry parent = reader.GetNextEntry();
            VerifyDirectoryEntry(parent, format, "parent/");

            TarEntry child = reader.GetNextEntry();
            VerifyDirectoryEntry(child, format, "parent/child/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "parent/child/file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(TarFormat.V7, TestTarFormat.v7)]
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_FolderSymbolicLink_Folder_Subfolder_File(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "foldersymlink_folder_subfolder_file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry childlink = reader.GetNextEntry();
            VerifySymbolicLinkEntry(childlink, format, "childlink", "parent/child");

            TarEntry parent = reader.GetNextEntry();
            VerifyDirectoryEntry(parent, format, "parent/");

            TarEntry child = reader.GetNextEntry();
            VerifyDirectoryEntry(child, format, "parent/child/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "parent/child/file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(TarFormat.V7, TestTarFormat.v7)]
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Many_Small_Files(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "many_small_files";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            List<TarEntry> entries = new List<TarEntry>();
            TarEntry entry;
            while ((entry = reader.GetNextEntry()) != null)
            {
                Assert.Equal(format, entry.Format);
                entries.Add(entry);
            }

            int directoriesCount = entries.Count(e => e.EntryType == TarEntryType.Directory);
            Assert.Equal(10, directoriesCount);

            TarEntryType regularFileEntryType = format == TarFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
            for (int i = 0; i < 10; i++)
            {
                int filesCount = entries.Count(e => e.EntryType == regularFileEntryType && e.Name.StartsWith($"{i}/"));
                Assert.Equal(10, filesCount);
            }
        }

        [Theory]
        // V7 does not support longer filenames
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongPath_Splitable_Under255(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "longpath_splitable_under255";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry directory = reader.GetNextEntry();
            VerifyDirectoryEntry(directory, format, "00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, $"00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        // V7 does not support block devices, character devices or fifos
        [InlineData(TarFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_SpecialFiles(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "specialfiles";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            PosixTarEntry blockDevice = reader.GetNextEntry() as PosixTarEntry;
            VerifyBlockDeviceEntry(blockDevice, format, AssetBlockDeviceFileName);

            PosixTarEntry characterDevice = reader.GetNextEntry() as PosixTarEntry;
            VerifyCharacterDeviceEntry(characterDevice, format, AssetCharacterDeviceFileName);

            PosixTarEntry fifo = reader.GetNextEntry() as PosixTarEntry;
            VerifyFifoEntry(fifo, format, "fifofile");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        // Neither V7 not Ustar can handle links with long target filenames
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_LongSymbolicLink(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "file_longsymlink";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry directory = reader.GetNextEntry();
            VerifyDirectoryEntry(directory, format,
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format,
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999000000000011111111112222222222333333333344444444445.txt",
            $"Hello {testCaseName}");

            TarEntry symbolicLink = reader.GetNextEntry();
            VerifySymbolicLinkEntry(symbolicLink, format,
            "link.txt",
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999000000000011111111112222222222333333333344444444445.txt");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        // Neither V7 not Ustar can handle a path that does not have separators that can be split under 100 bytes
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongFileName_Over100_Under255(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "longfilename_over100_under255";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        // Neither V7 not Ustar can handle path lenghts waaaay beyond name+prefix length
        [InlineData(TarFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongPath_Over255(TarFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "longpath_over255";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry directory = reader.GetNextEntry();
            VerifyDirectoryEntry(directory, format,
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format,
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999000000000011111111112222222222333333333344444444445.txt",
            $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }
    }
}
