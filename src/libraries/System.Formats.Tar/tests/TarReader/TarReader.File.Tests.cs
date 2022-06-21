// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_Tests : TarTestsBase
    {
        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File(TarEntryFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "file";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format, "file.txt", $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_HardLink(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_SymbolicLink(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_File(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_File_Utf8(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_Subfolder_File(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_FolderSymbolicLink_Folder_Subfolder_File(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Many_Small_Files(TarEntryFormat format, TestTarFormat testFormat)
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

            TarEntryType regularFileEntryType = format == TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
            for (int i = 0; i < 10; i++)
            {
                int filesCount = entries.Count(e => e.EntryType == regularFileEntryType && e.Name.StartsWith($"{i}/"));
                Assert.Equal(10, filesCount);
            }
        }

        [Theory]
        // V7 does not support longer filenames
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongPath_Splitable_Under255(TarEntryFormat format, TestTarFormat testFormat)
        {
            string testCaseName = "longpath_splitable_under255";
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, testCaseName);

            using TarReader reader = new TarReader(ms);

            TarEntry directory = reader.GetNextEntry();
            VerifyDirectoryEntry(directory, format, "00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999/");

            TarEntry file = reader.GetNextEntry();
            VerifyRegularFileEntry(file, format,
                $"00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999/00000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999.txt",
                $"Hello {testCaseName}");

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        // V7 does not support block devices, character devices or fifos
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_SpecialFiles(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_LongSymbolicLink(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongFileName_Over100_Under255(TarEntryFormat format, TestTarFormat testFormat)
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
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongPath_Over255(TarEntryFormat format, TestTarFormat testFormat)
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

        protected void VerifyRegularFileEntry(TarEntry file, TarEntryFormat format, string expectedFileName, string expectedContents)
        {
            Assert.NotNull(file);
            Assert.Equal(format, file.Format);

            Assert.True(file.Checksum > 0);
            Assert.NotNull(file.DataStream);
            Assert.True(file.DataStream.Length > 0);
            Assert.True(file.DataStream.CanRead);
            Assert.True(file.DataStream.CanSeek);
            file.DataStream.Seek(0, SeekOrigin.Begin);
            using (StreamReader reader = new StreamReader(file.DataStream, leaveOpen: true))
            {
                string contents = reader.ReadLine();
                Assert.Equal(expectedContents, contents);
            }

            TarEntryType expectedEntryType = format == TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
            Assert.Equal(expectedEntryType, file.EntryType);

            Assert.Equal(AssetGid, file.Gid);
            Assert.Equal(file.Length, file.DataStream.Length);
            Assert.Equal(DefaultLinkName, file.LinkName);
            Assert.Equal(AssetMode, file.Mode);
            Assert.True(file.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, file.Name);
            Assert.Equal(AssetUid, file.Uid);

            if (file is PosixTarEntry posix)
            {
                Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
                Assert.Equal(AssetGName, posix.GroupName);
                Assert.Equal(AssetUName, posix.UserName);

                if (posix is PaxTarEntry pax)
                {
                    VerifyExtendedAttributes(pax);
                }
                else if (posix is GnuTarEntry gnu)
                {
                    VerifyGnuFields(gnu);
                }
            }
        }

        protected void VerifySymbolicLinkEntry(TarEntry symbolicLink, TarEntryFormat format, string expectedFileName, string expectedTargetName)
        {
            Assert.NotNull(symbolicLink);
            Assert.Equal(format, symbolicLink.Format);

            Assert.True(symbolicLink.Checksum > 0);
            Assert.Null(symbolicLink.DataStream);

            Assert.Equal(TarEntryType.SymbolicLink, symbolicLink.EntryType);

            Assert.Equal(AssetGid, symbolicLink.Gid);
            Assert.Equal(0, symbolicLink.Length);
            Assert.Equal(expectedTargetName, symbolicLink.LinkName);
            Assert.Equal(AssetSymbolicLinkMode, symbolicLink.Mode);
            Assert.True(symbolicLink.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, symbolicLink.Name);
            Assert.Equal(AssetUid, symbolicLink.Uid);

            if (symbolicLink is PosixTarEntry posix)
            {
                Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
                Assert.Equal(AssetGName, posix.GroupName);
                Assert.Equal(AssetUName, posix.UserName);
            }

            if (symbolicLink is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (symbolicLink is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        protected void VerifyDirectoryEntry(TarEntry directory, TarEntryFormat format, string expectedFileName)
        {
            Assert.NotNull(directory);
            Assert.Equal(format, directory.Format);

            Assert.True(directory.Checksum > 0);
            Assert.Null(directory.DataStream);

            Assert.Equal(TarEntryType.Directory, directory.EntryType);

            Assert.Equal(AssetGid, directory.Gid);
            Assert.Equal(0, directory.Length);
            Assert.Equal(DefaultLinkName, directory.LinkName);
            Assert.Equal(AssetMode, directory.Mode);
            Assert.True(directory.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, directory.Name);
            Assert.Equal(AssetUid, directory.Uid);

            if (directory is PosixTarEntry posix)
            {
                Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
                Assert.Equal(AssetGName, posix.GroupName);
                Assert.Equal(AssetUName, posix.UserName);
            }

            if (directory is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (directory is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        protected void VerifyBlockDeviceEntry(PosixTarEntry blockDevice, TarEntryFormat format, string expectedFileName)
        {
            Assert.NotNull(blockDevice);
            Assert.Equal(TarEntryType.BlockDevice, blockDevice.EntryType);
            Assert.Equal(format, blockDevice.Format);

            Assert.True(blockDevice.Checksum > 0);
            Assert.Null(blockDevice.DataStream);

            Assert.Equal(AssetGid, blockDevice.Gid);
            Assert.Equal(0, blockDevice.Length);
            Assert.Equal(DefaultLinkName, blockDevice.LinkName);
            Assert.Equal(AssetSpecialFileMode, blockDevice.Mode);
            Assert.True(blockDevice.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, blockDevice.Name);
            Assert.Equal(AssetUid, blockDevice.Uid);

            // TODO: Figure out why the numbers don't match https://github.com/dotnet/runtime/issues/68230
            // Assert.Equal(AssetBlockDeviceMajor, blockDevice.DeviceMajor);
            // Assert.Equal(AssetBlockDeviceMinor, blockDevice.DeviceMinor);
            // Remove these two temporary checks when the above is fixed
            Assert.True(blockDevice.DeviceMajor > 0);
            Assert.True(blockDevice.DeviceMinor > 0);
            Assert.Equal(AssetGName, blockDevice.GroupName);
            Assert.Equal(AssetUName, blockDevice.UserName);

            if (blockDevice is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (blockDevice is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        protected void VerifyCharacterDeviceEntry(PosixTarEntry characterDevice, TarEntryFormat format, string expectedFileName)
        {
            Assert.NotNull(characterDevice);
            Assert.Equal(TarEntryType.CharacterDevice, characterDevice.EntryType);
            Assert.Equal(format, characterDevice.Format);

            Assert.True(characterDevice.Checksum > 0);
            Assert.Null(characterDevice.DataStream);

            Assert.Equal(AssetGid, characterDevice.Gid);
            Assert.Equal(0, characterDevice.Length);
            Assert.Equal(DefaultLinkName, characterDevice.LinkName);
            Assert.Equal(AssetSpecialFileMode, characterDevice.Mode);
            Assert.True(characterDevice.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, characterDevice.Name);
            Assert.Equal(AssetUid, characterDevice.Uid);

            // TODO: Figure out why the numbers don't match https://github.com/dotnet/runtime/issues/68230
            //Assert.Equal(AssetBlockDeviceMajor, characterDevice.DeviceMajor);
            //Assert.Equal(AssetBlockDeviceMinor, characterDevice.DeviceMinor);
            // Remove these two temporary checks when the above is fixed
            Assert.True(characterDevice.DeviceMajor > 0);
            Assert.True(characterDevice.DeviceMinor > 0);
            Assert.Equal(AssetGName, characterDevice.GroupName);
            Assert.Equal(AssetUName, characterDevice.UserName);

            if (characterDevice is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (characterDevice is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        protected void VerifyFifoEntry(PosixTarEntry fifo, TarEntryFormat format, string expectedFileName)
        {
            Assert.NotNull(fifo);
            Assert.Equal(format, fifo.Format);

            Assert.True(fifo.Checksum > 0);
            Assert.Null(fifo.DataStream);

            Assert.Equal(TarEntryType.Fifo, fifo.EntryType);

            Assert.Equal(AssetGid, fifo.Gid);
            Assert.Equal(0, fifo.Length);
            Assert.Equal(DefaultLinkName, fifo.LinkName);
            Assert.Equal(AssetSpecialFileMode, fifo.Mode);
            Assert.True(fifo.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, fifo.Name);
            Assert.Equal(AssetUid, fifo.Uid);

            Assert.Equal(DefaultDeviceMajor, fifo.DeviceMajor);
            Assert.Equal(DefaultDeviceMinor, fifo.DeviceMinor);
            Assert.Equal(AssetGName, fifo.GroupName);
            Assert.Equal(AssetUName, fifo.UserName);

            if (fifo is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (fifo is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        private void VerifyExtendedAttributes(PaxTarEntry pax)
        {
            Assert.NotNull(pax.ExtendedAttributes);
            Assert.Equal(TarEntryFormat.Pax, pax.Format);
            AssertExtensions.GreaterThanOrEqualTo(pax.ExtendedAttributes.Count(), 3); // Expect to at least collect mtime, ctime and atime

            VerifyExtendedAttributeTimestamp(pax, PaxEaMTime, MinimumTime);
            VerifyExtendedAttributeTimestamp(pax, PaxEaATime, MinimumTime);
            VerifyExtendedAttributeTimestamp(pax, PaxEaCTime, MinimumTime);
        }

        private void VerifyGnuFields(GnuTarEntry gnu)
        {
            Assert.Equal(TarEntryFormat.Gnu, gnu.Format);
            AssertExtensions.GreaterThanOrEqualTo(gnu.AccessTime, DateTimeOffset.UnixEpoch);
            AssertExtensions.GreaterThanOrEqualTo(gnu.ChangeTime, DateTimeOffset.UnixEpoch);
        }
    }
}
