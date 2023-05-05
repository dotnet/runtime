// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class PaxTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new PaxTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<ArgumentException>(() => new PaxTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.V7RegularFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.TapeVolume, InitialEntryName));

            // The user should not be creating these entries manually in pax
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
        }


        [Theory]
        [InlineData("\n", "value")]
        [InlineData("=", "value")]
        [InlineData("key", "\n")]
        [InlineData("\nkey", "value")]
        [InlineData("k\ney", "value")]
        [InlineData("key\n", "value")]
        [InlineData("=key", "value")]
        [InlineData("ke=y", "value")]
        [InlineData("key=", "value")]
        [InlineData("key", "\nvalue")]
        [InlineData("key", "val\nue")]
        [InlineData("key", "value\n")]
        [InlineData("key=", "value\n")]
        [InlineData("key\n", "value\n")]
        public void Disallowed_ExtendedAttributes_SeparatorCharacters(string key, string value)
        {
            Dictionary<string, string> extendedAttribute = new Dictionary<string, string>() { { key, value } };

            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName, extendedAttribute));
            Assert.Throws<ArgumentException>(() => new PaxGlobalExtendedAttributesTarEntry(extendedAttribute));
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            PaxTarEntry directory = new PaxTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            PaxTarEntry hardLink = new PaxTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            PaxTarEntry symbolicLink = new PaxTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            PaxTarEntry blockDevice = new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            PaxTarEntry characterDevice = new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            PaxTarEntry fifo = new PaxTarEntry(TarEntryType.Fifo, InitialEntryName);
            SetFifo(fifo);
            VerifyFifo(fifo);
        }
    }
}
