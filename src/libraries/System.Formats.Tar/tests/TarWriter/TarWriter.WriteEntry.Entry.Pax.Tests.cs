﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to PAX format.
    public class TarWriter_WriteEntry_Pax_Tests : TarTestsBase
    {
        [Fact]
        public void Write_V7RegularFileEntry_As_RegularFileEntry()
        {
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format: TarEntryFormat.Pax, leaveOpen: true))
            {
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);

                // Should be written in the format of the entry
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(TarEntryFormat.V7, entry.Format);
                Assert.True(entry is V7TarEntry);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Fact]
        public void WriteRegularFile()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                writer.WriteEntry(regularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry regularFile = reader.GetNextEntry() as PaxTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
        }

        [Fact]
        public void WriteHardLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry hardLink = new PaxTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                writer.WriteEntry(hardLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry hardLink = reader.GetNextEntry() as PaxTarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public void WriteSymbolicLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry symbolicLink = new PaxTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                writer.WriteEntry(symbolicLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry symbolicLink = reader.GetNextEntry() as PaxTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public void WriteDirectory()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry directory = new PaxTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                writer.WriteEntry(directory);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry directory = reader.GetNextEntry() as PaxTarEntry;
                VerifyDirectory(directory);
            }
        }

        [Fact]
        public void WriteCharacterDevice()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry charDevice = new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDevice);
                VerifyCharacterDevice(charDevice);
                writer.WriteEntry(charDevice);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry charDevice = reader.GetNextEntry() as PaxTarEntry;
                VerifyCharacterDevice(charDevice);
            }
        }

        [Fact]
        public void WriteBlockDevice()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry blockDevice = new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDevice);
                VerifyBlockDevice(blockDevice);
                writer.WriteEntry(blockDevice);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry blockDevice = reader.GetNextEntry() as PaxTarEntry;
                VerifyBlockDevice(blockDevice);
            }
        }

        [Fact]
        public void WriteFifo()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry fifo = new PaxTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifo);
                VerifyFifo(fifo);
                writer.WriteEntry(fifo);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry fifo = reader.GetNextEntry() as PaxTarEntry;
                VerifyFifo(fifo);
            }
        }

        [Fact]
        public void WritePaxAttributes_CustomAttribute()
        {
            string expectedKey = "MyExtendedAttributeKey";
            string expectedValue = "MyExtendedAttributeValue";

            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes.Add(expectedKey, expectedValue);

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName, extendedAttributes);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                writer.WriteEntry(regularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry regularFile = reader.GetNextEntry() as PaxTarEntry;
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

        [Fact]
        public void WritePaxAttributes_Timestamps_AutomaticallyAdded()
        {
            DateTimeOffset minimumTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                writer.WriteEntry(regularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry regularFile = reader.GetNextEntry() as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 4);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaMTime, minimumTime);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaATime, minimumTime);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaCTime, minimumTime);
            }
        }

        [Fact]
        public void WritePaxAttributes_Timestamps_UserProvided()
        {
            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes.Add(PaxEaATime, GetTimestampStringFromDateTimeOffset(TestAccessTime));
            extendedAttributes.Add(PaxEaCTime, GetTimestampStringFromDateTimeOffset(TestChangeTime));

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName, extendedAttributes);
                regularFile.ModificationTime = TestModificationTime;
                writer.WriteEntry(regularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry regularFile = reader.GetNextEntry() as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 4);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaMTime, TestModificationTime);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaATime, TestAccessTime);
                VerifyExtendedAttributeTimestamp(regularFile, PaxEaCTime, TestChangeTime);
            }
        }

        [Fact]
        public void WritePaxAttributes_LongGroupName_LongUserName()
        {
            string userName = "IAmAUserNameWhoseLengthIsWayBeyondTheThirtyTwoByteLimit";
            string groupName = "IAmAGroupNameWhoseLengthIsWayBeyondTheThirtyTwoByteLimit";

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                regularFile.UserName = userName;
                regularFile.GroupName = groupName;
                writer.WriteEntry(regularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry regularFile = reader.GetNextEntry() as PaxTarEntry;
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
        public void WritePaxAttributes_Name_AutomaticallyAdded()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                writer.WriteEntry(regularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry regularFile = reader.GetNextEntry() as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(regularFile.ExtendedAttributes.Count, 4);
                Assert.Contains(PaxEaName, regularFile.ExtendedAttributes);
            }
        }

        [Fact]
        public void WritePaxAttributes_LongLinkName_AutomaticallyAdded()
        {
            using MemoryStream archiveStream = new MemoryStream();

            string longSymbolicLinkName = new string('a', 101);
            string longHardLinkName = new string('b', 101);
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry symlink = new PaxTarEntry(TarEntryType.SymbolicLink, "symlink");
                symlink.LinkName = longSymbolicLinkName;
                writer.WriteEntry(symlink);

                PaxTarEntry hardlink = new PaxTarEntry(TarEntryType.HardLink, "hardlink");
                hardlink.LinkName = longHardLinkName;
                writer.WriteEntry(hardlink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry symlink = reader.GetNextEntry() as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(symlink.ExtendedAttributes.Count, 5);

                Assert.Contains(PaxEaName, symlink.ExtendedAttributes);
                Assert.Equal("symlink", symlink.ExtendedAttributes[PaxEaName]);
                Assert.Contains(PaxEaLinkName, symlink.ExtendedAttributes);
                Assert.Equal(longSymbolicLinkName, symlink.ExtendedAttributes[PaxEaLinkName]);

                PaxTarEntry hardlink = reader.GetNextEntry() as PaxTarEntry;

                AssertExtensions.GreaterThanOrEqualTo(hardlink.ExtendedAttributes.Count, 5);

                Assert.Contains(PaxEaName, hardlink.ExtendedAttributes);
                Assert.Equal("hardlink", hardlink.ExtendedAttributes[PaxEaName]);
                Assert.Contains(PaxEaLinkName, hardlink.ExtendedAttributes);
                Assert.Equal(longHardLinkName, hardlink.ExtendedAttributes[PaxEaLinkName]);
            }
        }
    }
}
