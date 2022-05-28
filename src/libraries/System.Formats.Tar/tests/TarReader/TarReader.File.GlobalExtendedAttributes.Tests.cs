// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_GlobalExtendedAttributes_Tests : TarReader_File_Tests_Base
    {
        [Fact]
        public void Read_Archive_File()
        {
            string testCaseName = "file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            PaxTarEntry file = reader.GetNextEntry() as PaxTarEntry;

            VerifyRegularFileEntry(file, TarFormat.Pax, "file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_File_HardLink()
        {
            string testCaseName = "file_hardlink";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry file = reader.GetNextEntry();

            VerifyRegularFileEntry(file, TarFormat.Pax, "file.txt", $"Hello {testCaseName}");

            TarEntry hardLink = reader.GetNextEntry();
            // The 'tar' tool detects hardlinks as regular files and saves them as such in the archives, for all formats
            VerifyRegularFileEntry(hardLink, TarFormat.Pax, "hardlink.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_File_SymbolicLink()
        {
            string testCaseName = "file_symlink";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry file = reader.GetNextEntry();

            VerifyRegularFileEntry(file, TarFormat.Pax, "file.txt", $"Hello {testCaseName}");

            TarEntry symbolicLink = reader.GetNextEntry();
            VerifySymbolicLinkEntry(symbolicLink, TarFormat.Pax, "link.txt", "file.txt");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_Folder_File()
        {
            string testCaseName = "folder_file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry directory = reader.GetNextEntry();

            VerifyDirectoryEntry(directory, TarFormat.Pax, "folder/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, TarFormat.Pax, "folder/file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_Folder_File_Utf8()
        {
            string testCaseName = "folder_file_utf8";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry directory = reader.GetNextEntry();

            VerifyDirectoryEntry(directory, TarFormat.Pax, "földër/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, TarFormat.Pax, "földër/áöñ.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_Folder_Subfolder_File()
        {
            string testCaseName = "folder_subfolder_file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry parent = reader.GetNextEntry();

            VerifyDirectoryEntry(parent, TarFormat.Pax, "parent/");

            TarEntry child = reader.GetNextEntry();
            VerifyDirectoryEntry(child, TarFormat.Pax, "parent/child/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, TarFormat.Pax, "parent/child/file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_FolderSymbolicLink_Folder_Subfolder_File()
        {
            string testCaseName = "foldersymlink_folder_subfolder_file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry childlink = reader.GetNextEntry();

            VerifySymbolicLinkEntry(childlink, TarFormat.Pax, "childlink", "parent/child");

            TarEntry parent = reader.GetNextEntry();
            VerifyDirectoryEntry(parent, TarFormat.Pax, "parent/");

            TarEntry child = reader.GetNextEntry();
            VerifyDirectoryEntry(child, TarFormat.Pax, "parent/child/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, TarFormat.Pax, "parent/child/file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_Many_Small_Files()
        {
            string testCaseName = "many_small_files";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            List<TarEntry> entries = new List<TarEntry>();
            TarEntry entry;
            bool isFirstEntry = true;
            while ((entry = reader.GetNextEntry()) != null)
            {
                if (isFirstEntry)
                {
                    isFirstEntry = false;
                }
                entries.Add(entry);
            }

            int directoriesCount = entries.Count(e => e.EntryType == TarEntryType.Directory);
            Assert.Equal(10, directoriesCount);

            for (int i = 0; i < 10; i++)
            {
                int filesCount = entries.Count(e => e.EntryType == TarEntryType.RegularFile && e.Name.StartsWith($"{i}/"));
                Assert.Equal(10, filesCount);
            }
        }

        [Fact]
        public void Read_Archive_LongPath_Splitable_Under255()
        {
            string testCaseName = "longpath_splitable_under255";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry directory = reader.GetNextEntry();

            VerifyDirectoryEntry(directory, TarFormat.Pax,
                "00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, TarFormat.Pax,
                $"00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999.txt",
                $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_SpecialFiles()
        {
            string testCaseName = "specialfiles";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            PosixTarEntry blockDevice = reader.GetNextEntry() as PosixTarEntry;

            VerifyBlockDeviceEntry(blockDevice, TarFormat.Pax, AssetBlockDeviceFileName);

            PosixTarEntry characterDevice = reader.GetNextEntry() as PosixTarEntry;
            VerifyCharacterDeviceEntry(characterDevice, TarFormat.Pax, AssetCharacterDeviceFileName);

            PosixTarEntry fifo = reader.GetNextEntry() as PosixTarEntry;
            VerifyFifoEntry(fifo, TarFormat.Pax, "fifofile");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_File_LongSymbolicLink()
        {
            string testCaseName = "file_longsymlink";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry directory = reader.GetNextEntry();

            VerifyDirectoryEntry(directory, TarFormat.Pax,
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, TarFormat.Pax,
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999000000000011111111112222222222333333333344444444445.txt",
            $"Hello {testCaseName}");

            TarEntry symbolicLink = reader.GetNextEntry();
            VerifySymbolicLinkEntry(symbolicLink, TarFormat.Pax,
            "link.txt",
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999000000000011111111112222222222333333333344444444445.txt");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_LongFileName_Over100_Under255()
        {
            string testCaseName = "longfilename_over100_under255";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry file = reader.GetNextEntry();

            VerifyRegularFileEntry(file, TarFormat.Pax,
                "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444.txt",
                $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Fact]
        public void Read_Archive_LongPath_Over255()
        {
            string testCaseName = "longpath_over255";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax_gea, testCaseName);

            using TarReader reader = new TarReader(ms);

            IReadOnlyDictionary<string, string> gea = VerifyGlobalExtendedAttributes(reader);

            TarEntry directory = reader.GetNextEntry();

            VerifyDirectoryEntry(directory, TarFormat.Pax,
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, TarFormat.Pax,
            "000000000011111111112222222222333333333344444444445555555555666666666677777777778888888888999999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999000000000011111111112222222222333333333344444444445.txt",
            $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        private IReadOnlyDictionary<string, string> VerifyGlobalExtendedAttributes(TarReader reader)
        {
            PaxGlobalExtendedAttributesTarEntry gea = reader.GetNextEntry() as PaxGlobalExtendedAttributesTarEntry;
            Assert.NotNull(gea);
            Assert.Equal(TarEntryType.GlobalExtendedAttributes, gea.EntryType);
            Assert.Equal(TarFormat.Pax, gea.Format);

            // Format: %d/GlobalHead.%p.%n, where:
            // - %d is the tmp path (platform dependent, and if too long, gets truncated to just '/tmp')
            // - %p is current process ID
            // - %n is the sequence number, which is always 1 for the first entry of the asset archive files
            Assert.Matches(@".+\/GlobalHead\.\d+\.1", gea.Name);

            Assert.True(gea.GlobalExtendedAttributes.Any());
            Assert.Contains(AssetPaxGeaKey, gea.GlobalExtendedAttributes);
            Assert.Equal(AssetPaxGeaValue, gea.GlobalExtendedAttributes[AssetPaxGeaKey]);

            return gea.GlobalExtendedAttributes;
        }
    }
}
