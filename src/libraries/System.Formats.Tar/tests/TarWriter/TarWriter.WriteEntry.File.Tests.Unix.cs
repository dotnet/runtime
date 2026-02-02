// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_WriteEntry_File_Tests : TarWriter_File_Base
    {
        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Add_Fifo(TarEntryFormat format)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                TarEntryFormat expectedFormat = Enum.Parse<TarEntryFormat>(strFormat);

                using TempDirectory root = new TempDirectory();
                string fifoName = "fifofile";
                string fifoPath = Path.Join(root.Path, fifoName);

                Interop.CheckIo(Interop.Sys.MkFifo(fifoPath, (int)DefaultFileMode));

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                {
                    writer.WriteEntry(fileName: fifoPath, entryName: fifoName);
                }

                archive.Seek(0, SeekOrigin.Begin);
                using (TarReader reader = new TarReader(archive))
                {
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.Equal(expectedFormat, entry.Format);

                    Assert.NotNull(entry);
                    Assert.Equal(fifoName, entry.Name);
                    Assert.Equal(DefaultLinkName, entry.LinkName);
                    Assert.Equal(TarEntryType.Fifo, entry.EntryType);
                    Assert.Null(entry.DataStream);

                    VerifyPlatformSpecificMetadata(fifoPath, entry);

                    Assert.Null(reader.GetNextEntry());
                }

            }, format.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Add_BlockDevice(TarEntryFormat format)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                TarEntryFormat expectedFormat = Enum.Parse<TarEntryFormat>(strFormat);

                using TempDirectory root = new TempDirectory();
                string blockDevicePath = Path.Join(root.Path, AssetBlockDeviceFileName);

                // Creating device files needs elevation
                Interop.CheckIo(Interop.Sys.CreateBlockDevice(blockDevicePath, (int)DefaultFileMode, TestBlockDeviceMajor, TestBlockDeviceMinor));

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                {
                    writer.WriteEntry(fileName: blockDevicePath, entryName: AssetBlockDeviceFileName);
                }

                archive.Seek(0, SeekOrigin.Begin);
                using (TarReader reader = new TarReader(archive))
                {
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.Equal(expectedFormat, entry.Format);

                    Assert.NotNull(entry);
                    Assert.Equal(AssetBlockDeviceFileName, entry.Name);
                    Assert.Equal(DefaultLinkName, entry.LinkName);
                    Assert.Equal(TarEntryType.BlockDevice, entry.EntryType);
                    Assert.Null(entry.DataStream);

                    VerifyPlatformSpecificMetadata(blockDevicePath, entry);

                    Assert.Equal(TestBlockDeviceMajor, entry.DeviceMajor);
                    Assert.Equal(TestBlockDeviceMinor, entry.DeviceMinor);

                    Assert.Null(reader.GetNextEntry());
                }

            }, format.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Add_CharacterDevice(TarEntryFormat format)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                TarEntryFormat expectedFormat = Enum.Parse<TarEntryFormat>(strFormat);
                using TempDirectory root = new TempDirectory();
                string characterDevicePath = Path.Join(root.Path, AssetCharacterDeviceFileName);

                // Creating device files needs elevation
                Interop.CheckIo(Interop.Sys.CreateCharacterDevice(characterDevicePath, (int)DefaultFileMode, TestCharacterDeviceMajor, TestCharacterDeviceMinor));

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                {
                    writer.WriteEntry(fileName: characterDevicePath, entryName: AssetCharacterDeviceFileName);
                }

                archive.Seek(0, SeekOrigin.Begin);
                using (TarReader reader = new TarReader(archive))
                {
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.Equal(expectedFormat, entry.Format);

                    Assert.NotNull(entry);
                    Assert.Equal(AssetCharacterDeviceFileName, entry.Name);
                    Assert.Equal(DefaultLinkName, entry.LinkName);
                    Assert.Equal(TarEntryType.CharacterDevice, entry.EntryType);
                    Assert.Null(entry.DataStream);

                    VerifyPlatformSpecificMetadata(characterDevicePath, entry);

                    Assert.Equal(TestCharacterDeviceMajor, entry.DeviceMajor);
                    Assert.Equal(TestCharacterDeviceMinor, entry.DeviceMinor);

                    Assert.Null(reader.GetNextEntry());
                }

            }, format.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void CreateEntryFromFileOwnedByNonExistentGroup(TarEntryFormat f)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                using TempDirectory root = new TempDirectory();

                string fileName = "file.txt";
                string filePath = Path.Join(root.Path, fileName);
                File.Create(filePath).Dispose();

                string groupName = Path.GetRandomFileName()[0..6];
                int groupId = CreateGroup(groupName);

                try
                {
                    SetGroupAsOwnerOfFile(groupName, filePath);
                }
                finally
                {
                    DeleteGroup(groupName);
                }

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, Enum.Parse<TarEntryFormat>(strFormat), leaveOpen: true))
                {
                    writer.WriteEntry(filePath, fileName); // Should not throw
                }
                archive.Seek(0, SeekOrigin.Begin);

                using (TarReader reader = new TarReader(archive, leaveOpen: false))
                {
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.NotNull(entry);

                    Assert.Equal(string.Empty, entry.GroupName);
                    Assert.Equal(groupId, entry.Gid);

                    string extractedPath = Path.Join(root.Path, "extracted.txt");
                    entry.ExtractToFile(extractedPath, overwrite: false);
                    Assert.True(File.Exists(extractedPath));

                    Assert.Null(reader.GetNextEntry());
                }
            }, f.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void CreateEntryFromFileOwnedByNonExistentUser(TarEntryFormat f)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                using TempDirectory root = new TempDirectory();

                string fileName = "file.txt";
                string filePath = Path.Join(root.Path, fileName);
                File.Create(filePath).Dispose();

                string userName = Path.GetRandomFileName()[0..6];
                int userId = CreateUser(userName);

                try
                {
                    SetUserAsOwnerOfFile(userName, filePath);
                }
                finally
                {
                    DeleteUser(userName);
                }

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, Enum.Parse<TarEntryFormat>(strFormat), leaveOpen: true))
                {
                    writer.WriteEntry(filePath, fileName); // Should not throw
                }
                archive.Seek(0, SeekOrigin.Begin);

                using (TarReader reader = new TarReader(archive, leaveOpen: false))
                {
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.NotNull(entry);

                    Assert.Equal(string.Empty, entry.UserName);
                    Assert.Equal(userId, entry.Uid);

                    string extractedPath = Path.Join(root.Path, "extracted.txt");
                    entry.ExtractToFile(extractedPath, overwrite: false);
                    Assert.True(File.Exists(extractedPath));

                    Assert.Null(reader.GetNextEntry());
                }
            }, f.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void CreateEntryFromFileOwnedByNonExistentGroupAndUser(TarEntryFormat f)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                using TempDirectory root = new TempDirectory();

                string fileName = "file.txt";
                string filePath = Path.Join(root.Path, fileName);
                File.Create(filePath).Dispose();

                string groupName = Path.GetRandomFileName()[0..6];
                int groupId = CreateGroup(groupName);

                string userName = Path.GetRandomFileName()[0..6];
                int userId = CreateUser(userName);

                try
                {
                    SetGroupAsOwnerOfFile(groupName, filePath);
                }
                finally
                {
                    DeleteGroup(groupName);
                }

                try
                {
                    SetUserAsOwnerOfFile(userName, filePath);
                }
                finally
                {
                    DeleteUser(userName);
                }

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, Enum.Parse<TarEntryFormat>(strFormat), leaveOpen: true))
                {
                    writer.WriteEntry(filePath, fileName); // Should not throw
                }
                archive.Seek(0, SeekOrigin.Begin);

                using (TarReader reader = new TarReader(archive, leaveOpen: false))
                {
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.NotNull(entry);

                    Assert.Equal(string.Empty, entry.GroupName);
                    Assert.Equal(groupId, entry.Gid);

                    Assert.Equal(string.Empty, entry.UserName);
                    Assert.Equal(userId, entry.Uid);

                    string extractedPath = Path.Join(root.Path, "extracted.txt");
                    entry.ExtractToFile(extractedPath, overwrite: false);
                    Assert.True(File.Exists(extractedPath));

                    Assert.Null(reader.GetNextEntry());
                }
            }, f.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void WriteEntry_HardLinks(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            // Create linked files (file1.txt, linked1.txt) and (file2.txt, linked2.txt).
            string file1 = Path.Join(root.Path, "file1.txt");
            File.WriteAllText(file1, "content1");
            string linked1 = Path.Join(root.Path, "linked1.txt");
            File.CreateHardLink(linked1, file1);
            string file2 = Path.Join(root.Path, "file2.txt");
            File.WriteAllText(file2, "content2");
            string linked2 = Path.Join(root.Path, "linked2.txt");
            File.CreateHardLink(linked2, file2);

            // Write to archive. Place the second pair in different directories.
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                writer.WriteEntry(file1, "file1.txt");
                writer.WriteEntry(linked1, "linked1.txt");
                writer.WriteEntry(file2, "dir1/file2.txt");
                writer.WriteEntry(linked2, "dir2/linked2.txt");
            }

            // Verify archive contents
            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                // First file
                TarEntry entry1 = reader.GetNextEntry();
                Assert.NotNull(entry1);
                Assert.Equal("file1.txt", entry1.Name);
                Assert.True(entry1.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile);
                Assert.NotNull(entry1.DataStream);

                // Hard link to first file
                TarEntry entry2 = reader.GetNextEntry();
                Assert.NotNull(entry2);
                Assert.Equal("linked1.txt", entry2.Name);
                Assert.Equal(TarEntryType.HardLink, entry2.EntryType);
                Assert.Equal("file1.txt", entry2.LinkName);
                Assert.Null(entry2.DataStream);

                // Second file
                TarEntry entry3 = reader.GetNextEntry();
                Assert.Equal("dir1/file2.txt", entry3.Name);
                Assert.True(entry3.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile);
                Assert.NotNull(entry3.DataStream);

                // Hard link to second file
                TarEntry entry4 = reader.GetNextEntry();
                Assert.Equal("dir2/linked2.txt", entry4.Name);
                Assert.Equal(TarEntryType.HardLink, entry4.EntryType);
                Assert.Equal("dir1/file2.txt", entry4.LinkName);
                Assert.Null(entry4.DataStream);

                Assert.Null(reader.GetNextEntry());
            }
        }
    }
}
