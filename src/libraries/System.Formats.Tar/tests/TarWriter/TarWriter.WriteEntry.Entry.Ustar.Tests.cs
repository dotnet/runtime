// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to Ustar format.
    public class TarWriter_WriteEntry_Ustar_Tests : TarWriter_WriteEntry_Base
    {
        [Fact]
        public void WriteEntry_Null_Throws() =>
            WriteEntry_Null_Throws_Internal(TarEntryFormat.Ustar);

        [Fact]
        public void WriteRegularFile()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry regularFile = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                writer.WriteEntry(regularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry regularFile = reader.GetNextEntry() as UstarTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
        }

        [Fact]
        public void WriteHardLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry hardLink = new UstarTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                writer.WriteEntry(hardLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry hardLink = reader.GetNextEntry() as UstarTarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public void WriteSymbolicLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry symbolicLink = new UstarTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                writer.WriteEntry(symbolicLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry symbolicLink = reader.GetNextEntry() as UstarTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public void WriteDirectory()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry directory = new UstarTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                writer.WriteEntry(directory);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry directory = reader.GetNextEntry() as UstarTarEntry;
                VerifyDirectory(directory);
            }
        }

        [Fact]
        public void WriteCharacterDevice()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry charDevice = new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDevice);
                VerifyCharacterDevice(charDevice);
                writer.WriteEntry(charDevice);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry charDevice = reader.GetNextEntry() as UstarTarEntry;
                VerifyCharacterDevice(charDevice);
            }
        }

        [Fact]
        public void WriteBlockDevice()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry blockDevice = new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDevice);
                VerifyBlockDevice(blockDevice);
                writer.WriteEntry(blockDevice);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry blockDevice = reader.GetNextEntry() as UstarTarEntry;
                VerifyBlockDevice(blockDevice);
            }
        }

        [Fact]
        public void WriteFifo()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry fifo = new UstarTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifo);
                VerifyFifo(fifo);
                writer.WriteEntry(fifo);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry fifo = reader.GetNextEntry() as UstarTarEntry;
                VerifyFifo(fifo);
            }
        }
    }
}
