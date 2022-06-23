// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class UstarTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new UstarTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.V7RegularFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.TapeVolume, InitialEntryName));
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            UstarTarEntry regularFile = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            UstarTarEntry directory = new UstarTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            UstarTarEntry hardLink = new UstarTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            UstarTarEntry symbolicLink = new UstarTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            UstarTarEntry blockDevice = new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            UstarTarEntry characterDevice = new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            UstarTarEntry fifo = new UstarTarEntry(TarEntryType.Fifo, InitialEntryName);
            SetFifo(fifo);
            VerifyFifo(fifo);
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => entry.ExtractToFile(root.Path, overwrite: false));

            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Match_AdditionalSubdirectory_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "dir", "file.txt");

            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => entry.ExtractToFile(root.Path, overwrite: false));

            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Match()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "file.txt");

            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            entry.ExtractToFile(fullPath, overwrite: false);

            Assert.True(File.Exists(fullPath));
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public void ExtractToFile_Link_Throws(TarEntryType entryType)
        {
            using TempDirectory root = new TempDirectory();
            string fileName = "mylink";
            string fullPath = Path.Join(root.Path, fileName);

            string linkTarget = PlatformDetection.IsWindows ? @"C:\Windows\system32\notepad.exe" : "/usr/bin/nano";

            UstarTarEntry entry = new UstarTarEntry(entryType, fileName);
            entry.LinkName = linkTarget;

            Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(fileName, overwrite: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }
    }
}
