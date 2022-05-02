// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_WriteEntry_File_Tests : TarTestsBase
    {
        private static bool IsRemoteExecutorSupportedAndOnUnixAndSuperUser => RemoteExecutor.IsSupported && PlatformDetection.IsUnixAndSuperUser;

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndOnUnixAndSuperUser))]
        [InlineData(TarFormat.Ustar)]
        [InlineData(TarFormat.Pax)]
        [InlineData(TarFormat.Gnu)]
        public void Add_Fifo(TarFormat format)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                TarFormat expectedFormat = Enum.Parse<TarFormat>(strFormat);

                using TempDirectory root = new TempDirectory();
                string fifoName = "fifofile";
                string fifoPath = Path.Join(root.Path, fifoName);

                Interop.CheckIo(Interop.Sys.MkFifo(fifoPath, (int)DefaultMode));

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                {
                    writer.WriteEntry(fileName: fifoPath, entryName: fifoName);
                }

                archive.Seek(0, SeekOrigin.Begin);
                using (TarReader reader = new TarReader(archive))
                {
                    Assert.Equal(TarFormat.Unknown, reader.Format);
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.Equal(expectedFormat, reader.Format);

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

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndOnUnixAndSuperUser))]
        [InlineData(TarFormat.Ustar)]
        [InlineData(TarFormat.Pax)]
        [InlineData(TarFormat.Gnu)]
        public void Add_BlockDevice(TarFormat format)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                TarFormat expectedFormat = Enum.Parse<TarFormat>(strFormat);

                using TempDirectory root = new TempDirectory();
                string blockDevicePath = Path.Join(root.Path, AssetBlockDeviceFileName);

                // Creating device files needs elevation
                Interop.CheckIo(Interop.Sys.CreateBlockDevice(blockDevicePath, (int)DefaultMode, TestBlockDeviceMajor, TestBlockDeviceMinor));

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                {
                    writer.WriteEntry(fileName: blockDevicePath, entryName: AssetBlockDeviceFileName);
                }

                archive.Seek(0, SeekOrigin.Begin);
                using (TarReader reader = new TarReader(archive))
                {
                    Assert.Equal(TarFormat.Unknown, reader.Format);
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.Equal(expectedFormat, reader.Format);

                    Assert.NotNull(entry);
                    Assert.Equal(AssetBlockDeviceFileName, entry.Name);
                    Assert.Equal(DefaultLinkName, entry.LinkName);
                    Assert.Equal(TarEntryType.BlockDevice, entry.EntryType);
                    Assert.Null(entry.DataStream);

                    VerifyPlatformSpecificMetadata(blockDevicePath, entry);

                    // TODO: Fix how these values are collected, the numbers don't match even though https://github.com/dotnet/runtime/issues/68230
                    // they come from stat's dev and from the major/minor syscalls
                    // Assert.Equal(TestBlockDeviceMajor, entry.DeviceMajor);
                    // Assert.Equal(TestBlockDeviceMinor, entry.DeviceMinor);

                    Assert.Null(reader.GetNextEntry());
                }

            }, format.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndOnUnixAndSuperUser))]
        [InlineData(TarFormat.Ustar)]
        [InlineData(TarFormat.Pax)]
        [InlineData(TarFormat.Gnu)]
        public void Add_CharacterDevice(TarFormat format)
        {
            RemoteExecutor.Invoke((string strFormat) =>
            {
                TarFormat expectedFormat = Enum.Parse<TarFormat>(strFormat);
                using TempDirectory root = new TempDirectory();
                string characterDevicePath = Path.Join(root.Path, AssetCharacterDeviceFileName);

                // Creating device files needs elevation
                Interop.CheckIo(Interop.Sys.CreateCharacterDevice(characterDevicePath, (int)DefaultMode, TestCharacterDeviceMajor, TestCharacterDeviceMinor));

                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                {
                    writer.WriteEntry(fileName: characterDevicePath, entryName: AssetCharacterDeviceFileName);
                }

                archive.Seek(0, SeekOrigin.Begin);
                using (TarReader reader = new TarReader(archive))
                {
                    Assert.Equal(TarFormat.Unknown, reader.Format);
                    PosixTarEntry entry = reader.GetNextEntry() as PosixTarEntry;
                    Assert.Equal(expectedFormat, reader.Format);

                    Assert.NotNull(entry);
                    Assert.Equal(AssetCharacterDeviceFileName, entry.Name);
                    Assert.Equal(DefaultLinkName, entry.LinkName);
                    Assert.Equal(TarEntryType.CharacterDevice, entry.EntryType);
                    Assert.Null(entry.DataStream);

                    VerifyPlatformSpecificMetadata(characterDevicePath, entry);

                    // TODO: Fix how these values are collected, the numbers don't match even though https://github.com/dotnet/runtime/issues/68230
                    // they come from stat's dev and from the major/minor syscalls
                    // Assert.Equal(TestCharacterDeviceMajor, entry.DeviceMajor);
                    // Assert.Equal(TestCharacterDeviceMinor, entry.DeviceMinor);

                    Assert.Null(reader.GetNextEntry());
                }

            }, format.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        partial void VerifyPlatformSpecificMetadata(string filePath, TarEntry entry)
        {
            Interop.Sys.FileStatus status = default;
            status.Mode = default;
            status.Dev = default;
            Interop.CheckIo(Interop.Sys.LStat(filePath, out status));

            Assert.Equal((int)status.Uid, entry.Uid);
            Assert.Equal((int)status.Gid, entry.Gid);

            if (entry is PosixTarEntry posix)
            {
                Assert.Equal(DefaultGName, posix.GroupName);
                Assert.Equal(DefaultUName, posix.UserName);

                if (entry.EntryType is not TarEntryType.BlockDevice and not TarEntryType.CharacterDevice)
                {
                    Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                    Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
                }
            }

            if (entry.EntryType is not TarEntryType.Directory)
            {
                TarFileMode expectedMode = (TarFileMode)(status.Mode & 4095); // First 12 bits
                DateTimeOffset expectedMTime = DateTimeOffset.FromUnixTimeSeconds(status.MTime);
                DateTimeOffset expectedATime = DateTimeOffset.FromUnixTimeSeconds(status.ATime);
                DateTimeOffset expectedCTime = DateTimeOffset.FromUnixTimeSeconds(status.CTime);

                Assert.Equal(expectedMode, entry.Mode);
                Assert.Equal(expectedMTime, entry.ModificationTime);

                if (entry is PaxTarEntry pax)
                {
                    Assert.NotNull(pax.ExtendedAttributes);
                    Assert.True(pax.ExtendedAttributes.Count >= 4);
                    Assert.Contains("path", pax.ExtendedAttributes);
                    VerifyExtendedAttributeTimestamp(pax, "mtime");
                    VerifyExtendedAttributeTimestamp(pax, "atime");
                    VerifyExtendedAttributeTimestamp(pax, "ctime");
                }
                else if (entry is GnuTarEntry gnu)
                {
                    Assert.Equal(expectedATime, gnu.AccessTime);
                    Assert.Equal(expectedCTime, gnu.ChangeTime);
                }
            }
        }
    }
}
