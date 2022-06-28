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
            using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
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
            using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
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

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Extract_SymbolicLinkEntry_TargetInsideDirectory(TarEntryFormat format) => Extract_LinkEntry_TargetInsideDirectory_Internal(TarEntryType.SymbolicLink, format, null);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Extract_HardLinkEntry_TargetInsideDirectory(TarEntryFormat format) => Extract_LinkEntry_TargetInsideDirectory_Internal(TarEntryType.HardLink, format, null);

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Extract_SymbolicLinkEntry_TargetInsideDirectory_LongBaseDir(TarEntryFormat format) => Extract_LinkEntry_TargetInsideDirectory_Internal(TarEntryType.SymbolicLink, format, new string('a', 99));

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Extract_HardLinkEntry_TargetInsideDirectory_LongBaseDir(TarEntryFormat format) => Extract_LinkEntry_TargetInsideDirectory_Internal(TarEntryType.HardLink, format, new string('a', 99));

        // This test would not pass for the V7 and Ustar formats in some OSs like MacCatalyst, tvOSSimulator and OSX, because the TempDirectory gets created in
        // a folder with a path longer than 100 bytes, and those tar formats have no way of handling pathnames and linknames longer than that length.
        // The rest of the OSs create the TempDirectory in a path that does not surpass the 100 bytes, so the 'subfolder' parameter gives a chance to extend
        // the base directory past that length, to ensure this scenario is tested everywhere.
        private void Extract_LinkEntry_TargetInsideDirectory_Internal(TarEntryType entryType, TarEntryFormat format, string subfolder)
        {
            using TempDirectory root = new TempDirectory();

            string baseDir = string.IsNullOrEmpty(subfolder) ? root.Path : Path.Join(root.Path, subfolder);
            Directory.CreateDirectory(baseDir);

            string linkName = "link";
            string targetName = "target";
            string targetPath = Path.Join(baseDir, targetName);

            File.Create(targetPath).Dispose();

            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                TarEntry entry= InvokeTarEntryCreationConstructor(format, entryType, linkName);
                entry.LinkName = targetPath;
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);

            TarFile.ExtractToDirectory(archive, baseDir, overwriteFiles: false);

            Assert.Equal(2, Directory.GetFileSystemEntries(baseDir).Count());
        }
    }
}
