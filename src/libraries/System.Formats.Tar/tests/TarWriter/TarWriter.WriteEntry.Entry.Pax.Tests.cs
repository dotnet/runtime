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
    public class TarWriter_WriteEntry_Pax_Tests : TarWriter_WriteEntry_Base
    {
        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteEntry_Null_Throws(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: false);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteEntryAsync(null));
                }
                else
                {
                    Assert.Throws<ArgumentNullException>(() => writer.WriteEntry(null));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteRegularFile(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                await WriteEntry(writer, regularFile, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry regularFile = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteHardLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry hardLink = new PaxTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                await WriteEntry(writer, hardLink, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry hardLink = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifyHardLink(hardLink);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteSymbolicLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry symbolicLink = new PaxTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                await WriteEntry(writer, symbolicLink, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry symbolicLink = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteDirectory(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry directory = new PaxTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                await WriteEntry(writer, directory, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry directory = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifyDirectory(directory);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteCharacterDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry charDevice = new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDevice);
                VerifyCharacterDevice(charDevice);
                await WriteEntry(writer, charDevice, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry charDevice = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifyCharacterDevice(charDevice);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteBlockDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry blockDevice = new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDevice);
                VerifyBlockDevice(blockDevice);
                await WriteEntry(writer, blockDevice, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry blockDevice = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifyBlockDevice(blockDevice);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteFifo(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry fifo = new PaxTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifo);
                VerifyFifo(fifo);
                await WriteEntry(writer, fifo, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry fifo = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifyFifo(fifo);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WritePaxAttributes_CustomAttribute(bool async)
        {
            string expectedKey = "MyExtendedAttributeKey";
            string expectedValue = "MyExtendedAttributeValue";

            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes.Add(expectedKey, expectedValue);

            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName, extendedAttributes);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                await WriteEntry(writer, regularFile, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry regularFile = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);

                Assert.NotNull(regularFile.ExtendedAttributes);

                // path, mtime, atime and ctime are always collected by default
                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 3);

                Assert.Contains(PaxEaName, regularFile.ExtendedAttributes);
                Assert.Contains(PaxEaMTime, regularFile.ExtendedAttributes);

                Assert.Contains(expectedKey, regularFile.ExtendedAttributes);
                Assert.Equal(expectedValue, regularFile.ExtendedAttributes[expectedKey]);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WritePaxAttributes_Timestamps_AutomaticallyAdded(bool async)
        {
            DateTimeOffset minimumTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                await WriteEntry(writer, regularFile, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry regularFile = await GetNextEntry(reader, async: async) as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 2);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaMTime, minimumTime);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WritePaxAttributes_Timestamps_UserProvided(bool async)
        {
            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes.Add(PaxEaATime, GetTimestampStringFromDateTimeOffset(TestAccessTime));
            extendedAttributes.Add(PaxEaCTime, GetTimestampStringFromDateTimeOffset(TestChangeTime));

            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName, extendedAttributes);
                regularFile.ModificationTime = TestModificationTime;
                await WriteEntry(writer, regularFile, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry regularFile = await GetNextEntry(reader, async: async) as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 4);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaMTime, TestModificationTime);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaATime, TestAccessTime);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaCTime, TestChangeTime);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WritePaxAttributes_LongGroupName_LongUserName(bool async)
        {
            string userName = "IAmAUserNameWhoseLengthIsWayBeyondTheThirtyTwoByteLimit";
            string groupName = "IAmAGroupNameWhoseLengthIsWayBeyondTheThirtyTwoByteLimit";

            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                regularFile.UserName = userName;
                regularFile.GroupName = groupName;
                await WriteEntry(writer, regularFile, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry regularFile = await GetNextEntry(reader, async: async) as PaxTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);

                Assert.NotNull(regularFile.ExtendedAttributes);

                // path, mtime are always collected by default
                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 4);

                Assert.Contains(PaxEaName, regularFile.ExtendedAttributes);
                Assert.Contains(PaxEaMTime, regularFile.ExtendedAttributes);

                Assert.Contains(PaxEaUName, regularFile.ExtendedAttributes);
                Assert.Equal(userName, regularFile.ExtendedAttributes[PaxEaUName]);

                Assert.Contains(PaxEaGName, regularFile.ExtendedAttributes);
                Assert.Equal(groupName, regularFile.ExtendedAttributes[PaxEaGName]);

                // They should also get exposed via the regular properties
                Assert.Equal(groupName, regularFile.GroupName);
                Assert.Equal(userName, regularFile.UserName);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WritePaxAttributes_Name_AutomaticallyAdded(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                await WriteEntry(writer, regularFile, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry regularFile = await GetNextEntry(reader, async: async) as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 2);
                Assert.Contains(PaxEaName, regularFile.ExtendedAttributes);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WritePaxAttributes_LongLinkName_AutomaticallyAdded(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();

            string longSymbolicLinkName = new string('a', 101);
            string longHardLinkName = new string('b', 101);
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                PaxTarEntry symlink = new PaxTarEntry(TarEntryType.SymbolicLink, "symlink");
                symlink.LinkName = longSymbolicLinkName;
                await WriteEntry(writer, symlink, async);

                PaxTarEntry hardlink = new PaxTarEntry(TarEntryType.HardLink, "hardlink");
                hardlink.LinkName = longHardLinkName;
                await WriteEntry(writer, hardlink, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry symlink = await GetNextEntry(reader, async: async) as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(symlink.ExtendedAttributes.Count, 3);

                Assert.Contains(PaxEaName, symlink.ExtendedAttributes);
                Assert.Equal("symlink", symlink.ExtendedAttributes[PaxEaName]);
                Assert.Contains(PaxEaLinkName, symlink.ExtendedAttributes);
                Assert.Equal(longSymbolicLinkName, symlink.ExtendedAttributes[PaxEaLinkName]);

                PaxTarEntry hardlink = await GetNextEntry(reader, async: async) as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(hardlink.ExtendedAttributes.Count, 3);

                Assert.Contains(PaxEaName, hardlink.ExtendedAttributes);
                Assert.Equal("hardlink", hardlink.ExtendedAttributes[PaxEaName]);
                Assert.Contains(PaxEaLinkName, hardlink.ExtendedAttributes);
                Assert.Equal(longHardLinkName, hardlink.ExtendedAttributes[PaxEaLinkName]);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task Add_Empty_GlobalExtendedAttributes(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = CreateTarWriter(archive, leaveOpen: true);
            try
            {
                PaxGlobalExtendedAttributesTarEntry gea = new PaxGlobalExtendedAttributesTarEntry(new Dictionary<string, string>());
                Assert.Equal("PaxGlobalExtendedAttributesTarEntry", gea.Name);
                await WriteEntry(writer, gea, async);
                Assert.Matches(@".*/GlobalHead\.\d+\.\d+", gea.Name);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Seek(0, SeekOrigin.Begin);
            TarReader reader = CreateTarReader(archive);
            try
            {
                PaxGlobalExtendedAttributesTarEntry gea = await GetNextEntry(reader, async: async) as PaxGlobalExtendedAttributesTarEntry;
                Assert.NotNull(gea);
                Assert.Equal(TarEntryFormat.Pax, gea.Format);
                Assert.Equal(TarEntryType.GlobalExtendedAttributes, gea.EntryType);

                Assert.Equal(0, gea.GlobalExtendedAttributes.Count);

                Assert.Null(await GetNextEntry(reader, async: async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        public static IEnumerable<object[]> WriteTimestampsInPax_TheoryData()
        {
            foreach (object[] data in WriteTimeStamp_Pax_TheoryData())
            {
                DateTimeOffset timestamp = (DateTimeOffset)data[0];
                yield return new object[] { timestamp, false };
                yield return new object[] { timestamp, true };
            }
        }

        [Theory]
        [MemberData(nameof(WriteTimestampsInPax_TheoryData))]
        public async Task WriteTimestampsInPax(DateTimeOffset timestamp, bool async)
        {
            string strTimestamp = GetTimestampStringFromDateTimeOffset(timestamp);

            Dictionary<string, string> ea = new Dictionary<string, string>()
            {
                { PaxEaATime, strTimestamp },
                { PaxEaCTime, strTimestamp }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.Directory, "dir", ea);

            entry.ModificationTime = timestamp;
            Assert.Equal(timestamp, entry.ModificationTime);

            Assert.Contains(PaxEaATime, entry.ExtendedAttributes);
            DateTimeOffset atime = GetDateTimeOffsetFromTimestampString(entry.ExtendedAttributes, PaxEaATime);
            Assert.Equal(timestamp, atime);

            Assert.Contains(PaxEaCTime, entry.ExtendedAttributes);
            DateTimeOffset ctime = GetDateTimeOffsetFromTimestampString(entry.ExtendedAttributes, PaxEaCTime);
            Assert.Equal(timestamp, ctime);

            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, leaveOpen: true);
            try
            {
                await WriteEntry(writer, entry, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                PaxTarEntry readEntry = await GetNextEntry(reader, async: async) as PaxTarEntry;
                Assert.NotNull(readEntry);

                Assert.Equal(timestamp, readEntry.ModificationTime);

                Assert.Contains(PaxEaATime, readEntry.ExtendedAttributes);
                DateTimeOffset actualATime = GetDateTimeOffsetFromTimestampString(readEntry.ExtendedAttributes, PaxEaATime);
                Assert.Equal(timestamp, actualATime);

                Assert.Contains(PaxEaCTime, readEntry.ExtendedAttributes);
                DateTimeOffset actualCTime = GetDateTimeOffsetFromTimestampString(readEntry.ExtendedAttributes, PaxEaCTime);
                Assert.Equal(timestamp, actualCTime);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [InlineData(TarEntryType.HardLink, false)]
        [InlineData(TarEntryType.HardLink, true)]
        [InlineData(TarEntryType.SymbolicLink, false)]
        [InlineData(TarEntryType.SymbolicLink, true)]
        public async Task Write_LinkEntry_EmptyLinkName_Throws(TarEntryType entryType, bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, leaveOpen: false);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(new PaxTarEntry(entryType, "link")));
                }
                else
                {
                    Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(new PaxTarEntry(entryType, "link")));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }
    }
}