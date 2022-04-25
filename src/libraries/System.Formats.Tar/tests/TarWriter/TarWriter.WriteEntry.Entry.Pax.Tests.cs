// Licensed to the .NET Foundation under one or more agreements.
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
            using (TarWriter writer = new TarWriter(archive, archiveFormat: TarFormat.Pax, leaveOpen: true))
            {
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);

                // Should be written as RegularFile
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                PaxTarEntry entry = reader.GetNextEntry() as PaxTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Fact]
        public void WriteRegularFile()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
                Assert.True(regularFile.ExtendedAttributes.Count >= 5);

                Assert.Contains("path", regularFile.ExtendedAttributes);
                Assert.Contains("mtime", regularFile.ExtendedAttributes);
                Assert.Contains("atime", regularFile.ExtendedAttributes);
                Assert.Contains("ctime", regularFile.ExtendedAttributes);

                Assert.Contains(expectedKey, regularFile.ExtendedAttributes);
                Assert.Equal(expectedValue, regularFile.ExtendedAttributes[expectedKey]);
            }
        }

        [Fact]
        public void WritePaxAttributes_Timestamps()
        {
            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes.Add("atime", ConvertDateTimeOffsetToDouble(TestAccessTime).ToString("F6", CultureInfo.InvariantCulture));
            extendedAttributes.Add("ctime", ConvertDateTimeOffsetToDouble(TestChangeTime).ToString("F6", CultureInfo.InvariantCulture));

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
                Assert.True(regularFile.ExtendedAttributes.Count >= 4);

                Assert.Contains("path", regularFile.ExtendedAttributes);
                VerifyExtendedAttributeTimestamp(regularFile, "mtime", TestModificationTime);
                VerifyExtendedAttributeTimestamp(regularFile, "atime", TestAccessTime);
                VerifyExtendedAttributeTimestamp(regularFile, "ctime", TestChangeTime);
            }
        }

        [Fact]
        public void WritePaxAttributes_LongGroupName_LongUserName()
        {
            string userName = "IAmAUserNameWhoseLengthIsWayBeyondTheThirtyTwoByteLimit";
            string groupName = "IAmAGroupNameWhoseLengthIsWayBeyondTheThirtyTwoByteLimit";

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Pax, leaveOpen: true))
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
                Assert.True(regularFile.ExtendedAttributes.Count >= 6);

                Assert.Contains("path", regularFile.ExtendedAttributes);
                Assert.Contains("mtime", regularFile.ExtendedAttributes);
                Assert.Contains("atime", regularFile.ExtendedAttributes);
                Assert.Contains("ctime", regularFile.ExtendedAttributes);

                Assert.Contains("uname", regularFile.ExtendedAttributes);
                Assert.Equal(userName, regularFile.ExtendedAttributes["uname"]);

                Assert.Contains("gname", regularFile.ExtendedAttributes);
                Assert.Equal(groupName, regularFile.ExtendedAttributes["gname"]);

                // They should also get exposed via the regular properties
                Assert.Equal(groupName, regularFile.GroupName);
                Assert.Equal(userName, regularFile.UserName);
            }
        }
    }
}
