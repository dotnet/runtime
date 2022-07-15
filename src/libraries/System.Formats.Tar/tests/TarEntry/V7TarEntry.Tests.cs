// Licensed to the .NET Foundation under one or more agreements.
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
    }
}
