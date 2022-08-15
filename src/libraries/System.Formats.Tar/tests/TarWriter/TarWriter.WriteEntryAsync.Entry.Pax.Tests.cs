// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to PAX format.
    public class TarWriter_WriteEntryAsync_Pax_Tests : TarWriter_WriteEntry_Base
    {
        [Fact]
        public Task WriteEntry_Null_Throws_Async() =>
            WriteEntry_Null_Throws_Async_Internal(TarEntryFormat.Pax);

        [Fact]
        public async Task WriteRegularFile_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                    SetRegularFile(regularFile);
                    VerifyRegularFile(regularFile, isWritable: true);
                    await writer.WriteEntryAsync(regularFile);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry regularFile = await reader.GetNextEntryAsync() as PaxTarEntry;
                    VerifyRegularFile(regularFile, isWritable: false);
                }
            }
        }

        [Fact]
        public async Task WriteHardLink_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry hardLink = new PaxTarEntry(TarEntryType.HardLink, InitialEntryName);
                    SetHardLink(hardLink);
                    VerifyHardLink(hardLink);
                    await writer.WriteEntryAsync(hardLink);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry hardLink = await reader.GetNextEntryAsync() as PaxTarEntry;
                    VerifyHardLink(hardLink);
                }
            }
        }

        [Fact]
        public async Task WriteSymbolicLink_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry symbolicLink = new PaxTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                    SetSymbolicLink(symbolicLink);
                    VerifySymbolicLink(symbolicLink);
                    await writer.WriteEntryAsync(symbolicLink);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry symbolicLink = await reader.GetNextEntryAsync() as PaxTarEntry;
                    VerifySymbolicLink(symbolicLink);
                }
            }
        }

        [Fact]
        public async Task WriteDirectory_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry directory = new PaxTarEntry(TarEntryType.Directory, InitialEntryName);
                    SetDirectory(directory);
                    VerifyDirectory(directory);
                    await writer.WriteEntryAsync(directory);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry directory = await reader.GetNextEntryAsync() as PaxTarEntry;
                    VerifyDirectory(directory);
                }
            }
        }

        [Fact]
        public async Task WriteCharacterDevice_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry charDevice = new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                    SetCharacterDevice(charDevice);
                    VerifyCharacterDevice(charDevice);
                    await writer.WriteEntryAsync(charDevice);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry charDevice = await reader.GetNextEntryAsync() as PaxTarEntry;
                    VerifyCharacterDevice(charDevice);
                }
            }
        }

        [Fact]
        public async Task WriteBlockDevice_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry blockDevice = new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                    SetBlockDevice(blockDevice);
                    VerifyBlockDevice(blockDevice);
                    await writer.WriteEntryAsync(blockDevice);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry blockDevice = await reader.GetNextEntryAsync() as PaxTarEntry;
                    VerifyBlockDevice(blockDevice);
                }
            }
        }

        [Fact]
        public async Task WriteFifo_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry fifo = new PaxTarEntry(TarEntryType.Fifo, InitialEntryName);
                    SetFifo(fifo);
                    VerifyFifo(fifo);
                    await writer.WriteEntryAsync(fifo);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry fifo = await reader.GetNextEntryAsync() as PaxTarEntry;
                    VerifyFifo(fifo);
                }
            }
        }

        [Fact]
        public async Task WritePaxAttributes_CustomAttribute_Async()
        {
            string expectedKey = "MyExtendedAttributeKey";
            string expectedValue = "MyExtendedAttributeValue";

            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes.Add(expectedKey, expectedValue);

            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName, extendedAttributes);
                    SetRegularFile(regularFile);
                    VerifyRegularFile(regularFile, isWritable: true);
                    await writer.WriteEntryAsync(regularFile);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry regularFile = await reader.GetNextEntryAsync() as PaxTarEntry;
                    VerifyRegularFile(regularFile, isWritable: false);

                    Assert.NotNull(regularFile.ExtendedAttributes);

                    // path, mtime, atime and ctime are always collected by default
                    AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 5);

                    Assert.Contains(PaxEaName, regularFile.ExtendedAttributes);
                    Assert.Contains(PaxEaMTime, regularFile.ExtendedAttributes);
                    Assert.Contains(PaxEaATime, regularFile.ExtendedAttributes);
                    Assert.Contains(PaxEaCTime, regularFile.ExtendedAttributes);

                    Assert.Contains(expectedKey, regularFile.ExtendedAttributes);
                    Assert.Equal(expectedValue, regularFile.ExtendedAttributes[expectedKey]);
                }
            }
        }

        [Fact]
        public async Task WritePaxAttributes_Timestamps_AutomaticallyAdded_Async()
        {
            DateTimeOffset minimumTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                    await writer.WriteEntryAsync(regularFile);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry regularFile = await reader.GetNextEntryAsync() as PaxTarEntry;

                    AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 4);
                    VerifyExtendedAttributeTimestamp(regularFile, PaxEaMTime, minimumTime);
                    VerifyExtendedAttributeTimestamp(regularFile, PaxEaATime, minimumTime);
                    VerifyExtendedAttributeTimestamp(regularFile, PaxEaCTime, minimumTime);
                }
            }
        }

        [Fact]
        public async Task WritePaxAttributes_Timestamps_UserProvided_Async()
        {
            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes.Add(PaxEaATime, GetTimestampStringFromDateTimeOffset(TestAccessTime));
            extendedAttributes.Add(PaxEaCTime, GetTimestampStringFromDateTimeOffset(TestChangeTime));

            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                {
                    PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName, extendedAttributes);
                    regularFile.ModificationTime = TestModificationTime;
                    await writer.WriteEntryAsync(regularFile);
                }

                archiveStream.Position = 0;
                await using (TarReader reader = new TarReader(archiveStream))
                {
                    PaxTarEntry regularFile = await reader.GetNextEntryAsync() as PaxTarEntry;

                    AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 4);
                    VerifyExtendedAttributeTimestamp(regularFile, PaxEaMTime, TestModificationTime);
                    VerifyExtendedAttributeTimestamp(regularFile, PaxEaATime, TestAccessTime);
                    VerifyExtendedAttributeTimestamp(regularFile, PaxEaCTime, TestChangeTime);
                }
            }
        }

        [Fact]
        public async Task WritePaxAttributes_LongGroupName_LongUserName_Async()
        {
            string userName = "IAmAUserNameWhoseLengthIsWayBeyondTheThirtyTwoByteLimit";
            string groupName = "IAmAGroupNameWhoseLengthIsWayBeyondTheThirtyTwoByteLimit";

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                regularFile.UserName = userName;
                regularFile.GroupName = groupName;
                await writer.WriteEntryAsync(regularFile);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry regularFile = await reader.GetNextEntryAsync() as PaxTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);

                Assert.NotNull(regularFile.ExtendedAttributes);

                // path, mtime, atime and ctime are always collected by default
                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 6);

                Assert.Contains(PaxEaName, regularFile.ExtendedAttributes);
                Assert.Contains(PaxEaMTime, regularFile.ExtendedAttributes);
                Assert.Contains(PaxEaATime, regularFile.ExtendedAttributes);
                Assert.Contains(PaxEaCTime, regularFile.ExtendedAttributes);

                Assert.Contains(PaxEaUName, regularFile.ExtendedAttributes);
                Assert.Equal(userName, regularFile.ExtendedAttributes[PaxEaUName]);

                Assert.Contains(PaxEaGName, regularFile.ExtendedAttributes);
                Assert.Equal(groupName, regularFile.ExtendedAttributes[PaxEaGName]);

                // They should also get exposed via the regular properties
                Assert.Equal(groupName, regularFile.GroupName);
                Assert.Equal(userName, regularFile.UserName);
            }
        }

        [Fact]
        public async Task WritePaxAttributes_Name_AutomaticallyAdded_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                await writer.WriteEntryAsync(regularFile);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry regularFile = await reader.GetNextEntryAsync() as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 4);
                Assert.Contains(PaxEaName, regularFile.ExtendedAttributes);
            }
        }

        [Fact]
        public async Task WritePaxAttributes_LongLinkName_AutomaticallyAdded_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();

            string longSymbolicLinkName = new string('a', 101);
            string longHardLinkName = new string('b', 101);
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry symlink = new PaxTarEntry(TarEntryType.SymbolicLink, "symlink");
                symlink.LinkName = longSymbolicLinkName;
                await writer.WriteEntryAsync(symlink);

                PaxTarEntry hardlink = new PaxTarEntry(TarEntryType.HardLink, "hardlink");
                hardlink.LinkName = longHardLinkName;
                await writer.WriteEntryAsync(hardlink);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry symlink = await reader.GetNextEntryAsync() as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(symlink.ExtendedAttributes.Count, 5);

                Assert.Contains(PaxEaName, symlink.ExtendedAttributes);
                Assert.Equal("symlink", symlink.ExtendedAttributes[PaxEaName]);
                Assert.Contains(PaxEaLinkName, symlink.ExtendedAttributes);
                Assert.Equal(longSymbolicLinkName, symlink.ExtendedAttributes[PaxEaLinkName]);

                PaxTarEntry hardlink = await reader.GetNextEntryAsync() as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(hardlink.ExtendedAttributes.Count, 5);

                Assert.Contains(PaxEaName, hardlink.ExtendedAttributes);
                Assert.Equal("hardlink", hardlink.ExtendedAttributes[PaxEaName]);
                Assert.Contains(PaxEaLinkName, hardlink.ExtendedAttributes);
                Assert.Equal(longHardLinkName, hardlink.ExtendedAttributes[PaxEaLinkName]);
            }
        }

        [Fact]
        public async Task Add_Empty_GlobalExtendedAttributes_Async()
        {
            using MemoryStream archive = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archive, leaveOpen: true))
            {
                PaxGlobalExtendedAttributesTarEntry gea = new PaxGlobalExtendedAttributesTarEntry(new Dictionary<string, string>());
                await writer.WriteEntryAsync(gea);
            }

            archive.Seek(0, SeekOrigin.Begin);
            await using (TarReader reader = new TarReader(archive))
            {
                PaxGlobalExtendedAttributesTarEntry gea = await reader.GetNextEntryAsync() as PaxGlobalExtendedAttributesTarEntry;
                Assert.NotNull(gea);
                Assert.Equal(TarEntryFormat.Pax, gea.Format);
                Assert.Equal(TarEntryType.GlobalExtendedAttributes, gea.EntryType);

                Assert.Equal(0, gea.GlobalExtendedAttributes.Count);

                Assert.Null(await reader.GetNextEntryAsync());
            }
        }

        [Fact]
        // Y2K38 will happen one second after "2038/19/01 03:14:07 +00:00". This timestamp represents the seconds since the Unix epoch with a
        // value of int.MaxValue: 2,147,483,647.
        // The fixed size fields for mtime, atime and ctime can fit 12 ASCII characters, but the last character is reserved for an ASCII space.
        // All our entry types should survive the Epochalypse because we internally use long to represent the seconds since Unix epoch, not int.
        // So if the max allowed value is 77,777,777,777 in octal, then the max allowed seconds since the Unix epoch are 8,589,934,591, which
        // is way past int MaxValue, but still within the long limits. That number represents the date "2242/16/03 12:56:32 +00:00".
        public async Task WriteTimestampsBeyondEpochalypseInPax_Async()
        {
            DateTimeOffset epochalypse = new DateTimeOffset(2038, 1, 19, 3, 14, 8, TimeSpan.Zero);
            string strEpochalypse = GetTimestampStringFromDateTimeOffset(epochalypse);

            Dictionary<string, string> ea = new Dictionary<string, string>()
            {
                { PaxEaATime, strEpochalypse },
                { PaxEaCTime, strEpochalypse }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.Directory, "dir", ea);

            entry.ModificationTime = epochalypse;
            Assert.Equal(epochalypse, entry.ModificationTime);

            Assert.Contains(PaxEaATime, entry.ExtendedAttributes);
            DateTimeOffset atime = GetDateTimeOffsetFromTimestampString(entry.ExtendedAttributes, PaxEaATime);
            Assert.Equal(epochalypse, atime);

            Assert.Contains(PaxEaCTime, entry.ExtendedAttributes);
            DateTimeOffset ctime = GetDateTimeOffsetFromTimestampString(entry.ExtendedAttributes, PaxEaCTime);
            Assert.Equal(epochalypse, ctime);

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry readEntry = await reader.GetNextEntryAsync() as PaxTarEntry;
                Assert.NotNull(readEntry);

                Assert.Equal(epochalypse, readEntry.ModificationTime);

                Assert.Contains(PaxEaATime, readEntry.ExtendedAttributes);
                DateTimeOffset actualATime = GetDateTimeOffsetFromTimestampString(readEntry.ExtendedAttributes, PaxEaATime);
                Assert.Equal(epochalypse, actualATime);

                Assert.Contains(PaxEaCTime, readEntry.ExtendedAttributes);
                DateTimeOffset actualCTime = GetDateTimeOffsetFromTimestampString(readEntry.ExtendedAttributes, PaxEaCTime);
                Assert.Equal(epochalypse, actualCTime);
            }
        }

        [Fact]
        // The fixed size fields for mtime, atime and ctime can fit 12 ASCII characters, but the last character is reserved for an ASCII space.
        // We internally use long to represent the seconds since Unix epoch, not int.
        // If the max allowed value is 77,777,777,777 in octal, then the max allowed seconds since the Unix epoch are 8,589,934,591,
        // which represents the date "2242/03/16 12:56:32 +00:00".
        // Pax should survive after this date because it stores the timestamps in the extended attributes dictionary
        // without size restrictions.
        public async Task WriteTimestampsBeyondOctalLimitInPax_Async()
        {
            DateTimeOffset overLimitTimestamp = new DateTimeOffset(2242, 3, 16, 12, 56, 33, TimeSpan.Zero); // One second past the octal limit

            string strOverLimitTimestamp = GetTimestampStringFromDateTimeOffset(overLimitTimestamp);

            Dictionary<string, string> ea = new Dictionary<string, string>()
            {
                { PaxEaATime, strOverLimitTimestamp },
                { PaxEaCTime, strOverLimitTimestamp }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.Directory, "dir", ea);

            entry.ModificationTime = overLimitTimestamp;
            Assert.Equal(overLimitTimestamp, entry.ModificationTime);

            Assert.Contains(PaxEaATime, entry.ExtendedAttributes);
            DateTimeOffset atime = GetDateTimeOffsetFromTimestampString(entry.ExtendedAttributes, PaxEaATime);
            Assert.Equal(overLimitTimestamp, atime);

            Assert.Contains(PaxEaCTime, entry.ExtendedAttributes);
            DateTimeOffset ctime = GetDateTimeOffsetFromTimestampString(entry.ExtendedAttributes, PaxEaCTime);
            Assert.Equal(overLimitTimestamp, ctime);

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry readEntry = await reader.GetNextEntryAsync() as PaxTarEntry;
                Assert.NotNull(readEntry);

                Assert.Equal(overLimitTimestamp, readEntry.ModificationTime);

                Assert.Contains(PaxEaATime, readEntry.ExtendedAttributes);
                DateTimeOffset actualATime = GetDateTimeOffsetFromTimestampString(readEntry.ExtendedAttributes, PaxEaATime);
                Assert.Equal(overLimitTimestamp, actualATime);

                Assert.Contains(PaxEaCTime, readEntry.ExtendedAttributes);
                DateTimeOffset actualCTime = GetDateTimeOffsetFromTimestampString(readEntry.ExtendedAttributes, PaxEaCTime);
                Assert.Equal(overLimitTimestamp, actualCTime);
            }
        }
    }
}
