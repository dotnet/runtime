// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_WriteEntryAsync_File_Tests : TarWriter_File_Base
    {
        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndOnUnixAndSuperUser))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Add_Fifo_Async(TarEntryFormat format)
        {
            RemoteExecutor.Invoke(async (string strFormat) =>
            {
                TarEntryFormat expectedFormat = Enum.Parse<TarEntryFormat>(strFormat);

                using (TempDirectory root = new TempDirectory())
                {
                    string fifoName = "fifofile";
                    string fifoPath = Path.Join(root.Path, fifoName);

                    Interop.CheckIo(Interop.Sys.MkFifo(fifoPath, (int)DefaultFileMode));

                    await using (MemoryStream archive = new MemoryStream())
                    {
                        await using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                        {
                            await writer.WriteEntryAsync(fileName: fifoPath, entryName: fifoName);
                        }

                        archive.Seek(0, SeekOrigin.Begin);
                        await using (TarReader reader = new TarReader(archive))
                        {
                            PosixTarEntry entry = await reader.GetNextEntryAsync() as PosixTarEntry;
                            Assert.Equal(expectedFormat, entry.Format);

                            Assert.NotNull(entry);
                            Assert.Equal(fifoName, entry.Name);
                            Assert.Equal(DefaultLinkName, entry.LinkName);
                            Assert.Equal(TarEntryType.Fifo, entry.EntryType);
                            Assert.Null(entry.DataStream);

                            VerifyPlatformSpecificMetadata(fifoPath, entry);

                            Assert.Null(await reader.GetNextEntryAsync());
                        }
                    }
                }
            }, format.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndOnUnixAndSuperUser))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Add_BlockDevice_Async(TarEntryFormat format)
        {
            RemoteExecutor.Invoke(async (string strFormat) =>
            {
                TarEntryFormat expectedFormat = Enum.Parse<TarEntryFormat>(strFormat);

                using (TempDirectory root = new TempDirectory())
                {
                    string blockDevicePath = Path.Join(root.Path, AssetBlockDeviceFileName);

                    // Creating device files needs elevation
                    Interop.CheckIo(Interop.Sys.CreateBlockDevice(blockDevicePath, (int)DefaultFileMode, TestBlockDeviceMajor, TestBlockDeviceMinor));

                    await using (MemoryStream archive = new MemoryStream())
                    {
                        await using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                        {
                            await writer.WriteEntryAsync(fileName: blockDevicePath, entryName: AssetBlockDeviceFileName);
                        }

                        archive.Seek(0, SeekOrigin.Begin);
                        await using (TarReader reader = new TarReader(archive))
                        {
                            PosixTarEntry entry = await reader.GetNextEntryAsync() as PosixTarEntry;
                            Assert.Equal(expectedFormat, entry.Format);

                            Assert.NotNull(entry);
                            Assert.Equal(AssetBlockDeviceFileName, entry.Name);
                            Assert.Equal(DefaultLinkName, entry.LinkName);
                            Assert.Equal(TarEntryType.BlockDevice, entry.EntryType);
                            Assert.Null(entry.DataStream);

                            VerifyPlatformSpecificMetadata(blockDevicePath, entry);

                            Assert.Equal(TestBlockDeviceMajor, entry.DeviceMajor);
                            Assert.Equal(TestBlockDeviceMinor, entry.DeviceMinor);

                            Assert.Null(await reader.GetNextEntryAsync());
                        }
                    }
                }
            }, format.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndOnUnixAndSuperUser))]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Add_CharacterDevice_Async(TarEntryFormat format)
        {
            RemoteExecutor.Invoke(async (string strFormat) =>
            {
                using (TempDirectory root = new TempDirectory())
                {
                    TarEntryFormat expectedFormat = Enum.Parse<TarEntryFormat>(strFormat);
                    string characterDevicePath = Path.Join(root.Path, AssetCharacterDeviceFileName);

                    // Creating device files needs elevation
                    Interop.CheckIo(Interop.Sys.CreateCharacterDevice(characterDevicePath, (int)DefaultFileMode, TestCharacterDeviceMajor, TestCharacterDeviceMinor));

                    await using (MemoryStream archive = new MemoryStream())
                    {
                        await using (TarWriter writer = new TarWriter(archive, expectedFormat, leaveOpen: true))
                        {
                            await writer.WriteEntryAsync(fileName: characterDevicePath, entryName: AssetCharacterDeviceFileName);
                        }

                        archive.Seek(0, SeekOrigin.Begin);
                        await using (TarReader reader = new TarReader(archive))
                        {
                            PosixTarEntry entry = await reader.GetNextEntryAsync() as PosixTarEntry;
                            Assert.Equal(expectedFormat, entry.Format);

                            Assert.NotNull(entry);
                            Assert.Equal(AssetCharacterDeviceFileName, entry.Name);
                            Assert.Equal(DefaultLinkName, entry.LinkName);
                            Assert.Equal(TarEntryType.CharacterDevice, entry.EntryType);
                            Assert.Null(entry.DataStream);

                            VerifyPlatformSpecificMetadata(characterDevicePath, entry);

                            Assert.Equal(TestCharacterDeviceMajor, entry.DeviceMajor);
                            Assert.Equal(TestCharacterDeviceMinor, entry.DeviceMinor);

                            Assert.Null(await reader.GetNextEntryAsync());
                        }
                    }
                }
            }, format.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }
    }
}
