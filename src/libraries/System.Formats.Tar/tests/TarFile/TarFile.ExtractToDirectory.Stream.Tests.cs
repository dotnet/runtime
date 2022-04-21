// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_ExtractToDirectory_Stream_Tests : TarTestsBase
    {
        [Fact]
        public void NullStream_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TarFile.ExtractToDirectory(source: null, destinationDirectoryName: "path", overwriteFiles: false));
        }

        [Fact]
        public void InvalidPath_Throws()
        {
            using MemoryStream archive = new MemoryStream();
            Assert.Throws<ArgumentNullException>(() => TarFile.ExtractToDirectory(archive, destinationDirectoryName: null, overwriteFiles: false));
            Assert.Throws<ArgumentException>(() => TarFile.ExtractToDirectory(archive, destinationDirectoryName: string.Empty, overwriteFiles: false));
        }

        [Fact]
        public void UnreadableStream_Throws()
        {
            using MemoryStream archive = new MemoryStream();
            using WrappedStream unreadable = new WrappedStream(archive, canRead: false, canWrite: true, canSeek: true);
            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(unreadable, destinationDirectoryName: "path", overwriteFiles: false));
        }

        [Fact]
        public void NonExistentDirectory_Throws()
        {
            using TempDirectory root = new TempDirectory();
            string dirPath = Path.Join(root.Path, "dir");

            using MemoryStream archive = new MemoryStream();
            Assert.Throws<DirectoryNotFoundException>(() => TarFile.ExtractToDirectory(archive, destinationDirectoryName: dirPath, overwriteFiles: false));
        }

        [Fact]
        public void ExtractEntry_ManySubfolderSegments_NoPrecedingDirectoryEntries()
        {
            using TempDirectory root = new TempDirectory();

            string firstSegment = "a";
            string secondSegment = Path.Join(firstSegment, "b");
            string fileWithTwoSegments = Path.Join(secondSegment, "c.txt");

            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarFormat.Ustar, leaveOpen: true))
            {
                // No preceding directory entries for the segments
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fileWithTwoSegments);

                entry.DataStream = new MemoryStream();
                entry.DataStream.Write(new byte[] { 0x1 });
                entry.DataStream.Seek(0, SeekOrigin.Begin);

                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            TarFile.ExtractToDirectory(archive, root.Path, overwriteFiles: false);

            Assert.True(Directory.Exists(Path.Join(root.Path, firstSegment)));
            Assert.True(Directory.Exists(Path.Join(root.Path, secondSegment)));
            Assert.True(File.Exists(Path.Join(root.Path, fileWithTwoSegments)));
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public void Extract_LinkEntry_TargetOutsideDirectory(TarEntryType entryType)
        {
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry entry = new UstarTarEntry(entryType, "link");
                entry.LinkName = PlatformDetection.IsWindows ? @"C:\Windows\System32\notepad.exe" : "/usr/bin/nano";
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);

            using TempDirectory root = new TempDirectory();

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(archive, root.Path, overwriteFiles: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void Extract_SymbolicLinkEntry_TargetInsideDirectory() => Extract_LinkEntry_TargetInsideDirectory_Internal(TarEntryType.SymbolicLink);

        [Fact]
        public void Extract_HardLinkEntry_TargetInsideDirectory() => Extract_LinkEntry_TargetInsideDirectory_Internal(TarEntryType.HardLink);

        private void Extract_LinkEntry_TargetInsideDirectory_Internal(TarEntryType entryType)
        {
            using TempDirectory root = new TempDirectory();

            string linkName = "link";
            string targetName = "target";
            string targetPath = Path.Join(root.Path, targetName);

            File.Create(targetPath).Dispose();

            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry entry = new UstarTarEntry(entryType, linkName);
                entry.LinkName = targetPath;
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);

            TarFile.ExtractToDirectory(archive, root.Path, overwriteFiles: false);

            Assert.Equal(2, Directory.GetFileSystemEntries(root.Path).Count());
        }
    }
}
