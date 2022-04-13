// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to V7 format.
    public class TarWriter_WriteEntry_V7_Tests : TarTestsBase
    {
        [Fact]
        public void ThrowIf_WriteEntry_UnsupportedFile()
        {
            // Verify that entry types that can be manually constructed in other types, cannot be inserted in a v7 writer
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, archiveFormat: TarFormat.V7, leaveOpen: true))
            {
                // Entry types supported in ustar but not in v7
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new UstarTarEntry(TarEntryType.Fifo, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName)));

                // Entry types supported in pax but not in v7
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new PaxTarEntry(TarEntryType.Fifo, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName)));

                // Entry types supported in gnu but not in v7
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new GnuTarEntry(TarEntryType.Fifo, InitialEntryName)));
                Assert.Throws<NotSupportedException>(() => writer.WriteEntry(new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName)));
            }
            // Verify nothing was written, not even the empty records
            Assert.Equal(0, archiveStream.Length);
        }


        [Fact]
        public void WriteRegularFile()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.V7, leaveOpen: true))
            {
                V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                SetRegularFile(oldRegularFile);
                VerifyRegularFile(oldRegularFile, isWritable: true);
                writer.WriteEntry(oldRegularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry oldRegularFile = reader.GetNextEntry() as V7TarEntry;
                VerifyRegularFile(oldRegularFile, isWritable: false);
            }
        }

        [Fact]
        public void WriteHardLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.V7, leaveOpen: true))
            {
                V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                writer.WriteEntry(hardLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry hardLink = reader.GetNextEntry() as V7TarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public void WriteSymbolicLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.V7, leaveOpen: true))
            {
                V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                writer.WriteEntry(symbolicLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry symbolicLink = reader.GetNextEntry() as V7TarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public void WriteDirectory()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.V7, leaveOpen: true))
            {
                V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                writer.WriteEntry(directory);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry directory = reader.GetNextEntry() as V7TarEntry;
                VerifyDirectory(directory);
            }
        }
    }
}
