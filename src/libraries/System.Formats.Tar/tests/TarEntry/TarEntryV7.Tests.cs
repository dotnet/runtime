﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class V7TarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new V7TarEntry(TarEntryType.V7RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.V7RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.BlockDevice, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.CharacterDevice, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.Fifo, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.RegularFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.TapeVolume, InitialEntryName));
        }

        [Fact]
        public void Constructor_ConversionFromUstar()
        {
            UstarTarEntry ustar = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedUstar = new V7TarEntry(ustar);

            Assert.Equal(TarEntryFormat.V7, convertedUstar.Format);
            Assert.Equal(TarEntryType.V7RegularFile, convertedUstar.EntryType);
            Assert.Equal(InitialEntryName, convertedUstar.Name);
        }


        [Fact]
        public void Constructor_ConversionFromPax()
        {
            PaxTarEntry pax = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedPax = new V7TarEntry(pax);

            Assert.Equal(TarEntryFormat.V7, convertedPax.Format);
            Assert.Equal(TarEntryType.V7RegularFile, convertedPax.EntryType);
            Assert.Equal(InitialEntryName, convertedPax.Name);
        }


        [Fact]
        public void Constructor_ConversionFromGnu()
        {
            GnuTarEntry gnu = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedGnu = new V7TarEntry(gnu);

            Assert.Equal(TarEntryFormat.V7, convertedGnu.Format);
            Assert.Equal(TarEntryType.V7RegularFile, convertedGnu.EntryType);
            Assert.Equal(InitialEntryName, convertedGnu.Name);
        }

        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Ustar()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.Fifo, InitialEntryName)));
        }

        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Pax()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.Fifo, InitialEntryName)));

            // PaxGlobalExtendedAttributesTarEntry is a TarEntry but cannot be converted
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxGlobalExtendedAttributesTarEntry(new Dictionary<string, string>())));
        }

        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Gnu()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.Fifo, InitialEntryName)));
        }

        [Fact]
        public void SupportedEntryType_V7RegularFile()
        {
            V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            SetRegularFile(oldRegularFile);
            VerifyRegularFile(oldRegularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, fullPath);

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

            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, fullPath);

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

            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, fullPath);

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

            V7TarEntry entry = new V7TarEntry(entryType, fileName);
            entry.LinkName = linkTarget;

            Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(fileName, overwrite: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }
    }
}
