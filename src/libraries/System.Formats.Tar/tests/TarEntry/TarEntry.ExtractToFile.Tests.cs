// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarEntry_ExtractToFile_Tests : TarTestsBase
    {
        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => entry.ExtractToFile(root.Path, overwrite: false));

            Assert.False(File.Exists(fullPath));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_Name_FullPath_DestinationDirectory_Match_AdditionalSubdirectory_Throws(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "dir", "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => entry.ExtractToFile(root.Path, overwrite: false));

            Assert.False(File.Exists(fullPath));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Constructor_Name_FullPath_DestinationDirectory_Match(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "file.txt");

            TarEntry entry = InvokeTarEntryCreationConstructor(format, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format), fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            entry.ExtractToFile(fullPath, overwrite: false);

            Assert.True(File.Exists(fullPath));
        }

        [Theory]
        [MemberData(nameof(GetFormatsAndLinks))]
        public void ExtractToFile_Link_Throws(TarEntryFormat format, TarEntryType entryType)
        {
            using TempDirectory root = new TempDirectory();
            string fileName = "mylink";
            string fullPath = Path.Join(root.Path, fileName);

            string linkTarget = PlatformDetection.IsWindows ? @"C:\Windows\system32\notepad.exe" : "/usr/bin/nano";

            TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, fileName);
            entry.LinkName = linkTarget;

            Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(fileName, overwrite: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }
    }
}
