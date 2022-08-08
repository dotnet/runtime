// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class GnuTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new GnuTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.V7RegularFile, InitialEntryName));

            // These are specific to GNU, but currently the user cannot create them manually
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.TapeVolume, InitialEntryName));

            // The user should not create these entries manually
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.LongPath, InitialEntryName));
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            GnuTarEntry regularFile = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            GnuTarEntry directory = new GnuTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            GnuTarEntry hardLink = new GnuTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            GnuTarEntry symbolicLink = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            GnuTarEntry blockDevice = new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            GnuTarEntry characterDevice = new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            GnuTarEntry fifo = new GnuTarEntry(TarEntryType.Fifo, InitialEntryName);
            SetFifo(fifo);
            VerifyFifo(fifo);
        }
    }
}
