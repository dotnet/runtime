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
    }
}
