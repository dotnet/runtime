// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_ExtractToDirectory_Stream_Tests : TarFile_ExtractToDirectory_Tests
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
            Assert.Throws<ArgumentException>(() => TarFile.ExtractToDirectory(unreadable, destinationDirectoryName: "path", overwriteFiles: false));
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

        [Theory]
        [InlineData(512)]
        [InlineData(512 + 1)]
        [InlineData(512 + 512 - 1)]
        public void Extract_UnseekableStream_BlockAlignmentPadding_DoesNotAffectNextEntries(int contentSize)
        {
            byte[] fileContents = new byte[contentSize];
            Array.Fill<byte>(fileContents, 0x1);

            using var archive = new MemoryStream();
            using (var compressor = new GZipStream(archive, CompressionMode.Compress, leaveOpen: true))
            {
                using var writer = new TarWriter(compressor);
                var entry1 = new PaxTarEntry(TarEntryType.RegularFile, "file");
                entry1.DataStream = new MemoryStream(fileContents);
                writer.WriteEntry(entry1);

                var entry2 = new PaxTarEntry(TarEntryType.RegularFile, "next-file");
                writer.WriteEntry(entry2);
            }

            archive.Position = 0;
            using var decompressor = new GZipStream(archive, CompressionMode.Decompress);
            using var reader = new TarReader(decompressor);

            using TempDirectory destination = new TempDirectory();
            TarFile.ExtractToDirectory(decompressor, destination.Path, overwriteFiles: true);

            Assert.Equal(2, Directory.GetFileSystemEntries(destination.Path, "*", SearchOption.AllDirectories).Count());
        }

        [Fact]
        public void PaxNameCollision_DedupInExtendedAttributes()
        {
            using TempDirectory root = new();

            string sharedRootFolders = Path.Join(root.Path, "folder with spaces", new string('a', 100));
            string path1 = Path.Join(sharedRootFolders, "entry 1 with spaces.txt");
            string path2 = Path.Join(sharedRootFolders, "entry 2 with spaces.txt");

            using MemoryStream stream = new();
            using (TarWriter writer = new(stream, TarEntryFormat.Pax, leaveOpen: true))
            {
                // Paths don't fit in the standard 'name' field, but they differ in the filename,
                // which is fully stored as an extended attribute
                PaxTarEntry entry1 = new(TarEntryType.RegularFile, path1);
                writer.WriteEntry(entry1);
                PaxTarEntry entry2 = new(TarEntryType.RegularFile, path2);
                writer.WriteEntry(entry2);
            }
            stream.Position = 0;

            TarFile.ExtractToDirectory(stream, root.Path, overwriteFiles: true);

            Assert.True(File.Exists(path1));
            Assert.True(Path.Exists(path2));
        }

        [Theory]
        [MemberData(nameof(GetExactRootDirMatchCases))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public void ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws(TarEntryFormat format, TarEntryType entryType, string fileName)
        {
            ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal(format, entryType, fileName, inverted: false);
            ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal(format, entryType, fileName, inverted: true);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public void ExtractToDirectory_ExactRootDirMatch_Directory_Relative_Throws()
        {
            string entryFolderName = "folder";
            string destinationFolderName = "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            // Relative segments should not change the final destination folder
            string dirPath1 = Path.Join(entryFolderPath, "..", "folder");
            string dirPath2 = Path.Join(entryFolderPath, "..", "folder" + Path.DirectorySeparatorChar);

            ExtractRootDirMatch_Verify_Throws(TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, dirPath1, linkTargetPath: null);
            ExtractRootDirMatch_Verify_Throws(TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, dirPath2, linkTargetPath: null);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public void ExtractToDirectory_ExactRootDirMatch_HardLinks_Throws(TarEntryFormat format)
        {
            ExtractToDirectory_ExactRootDirMatch_Links_Throws(format, TarEntryType.HardLink, inverted: false);
            ExtractToDirectory_ExactRootDirMatch_Links_Throws(format, TarEntryType.HardLink, inverted: true);
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void ExtractToDirectory_ExactRootDirMatch_SymLinks_Throws(TarEntryFormat format)
        {
            ExtractToDirectory_ExactRootDirMatch_Links_Throws(format, TarEntryType.SymbolicLink, inverted: false);
            ExtractToDirectory_ExactRootDirMatch_Links_Throws(format, TarEntryType.SymbolicLink, inverted: true);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_ExactRootDirMatch_SymLinks_TargetOutside_Throws()
        {
            string entryFolderName = "folder";
            string destinationFolderName = "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            string linkPath = Path.Join(entryFolderPath, "link");

            // Links target outside the destination path should not be allowed
            // Ensure relative segments do not go around this restriction
            string linkTargetPath1 = Path.Join(entryFolderPath, "..", entryFolderName);
            string linkTargetPath2 = Path.Join(entryFolderPath, "..", entryFolderName + Path.DirectorySeparatorChar);

            ExtractRootDirMatch_Verify_Throws(TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, linkPath, linkTargetPath1);
            ExtractRootDirMatch_Verify_Throws(TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, linkPath, linkTargetPath2);
        }

        private void ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal(TarEntryFormat format, TarEntryType entryType, string fileName, bool inverted)
        {
            // inverted == false:
            //   destination: folderSibling/
            //   entry folder: folder/ (does not match destination)

            // inverted == true:
            //   destination: folder/
            //   entry folder: folderSibling/ (does not match destination)

            string entryFolderName = inverted ? "folderSibling" : "folder";
            string destinationFolderName = inverted ? "folder" : "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            string filePath = Path.Join(entryFolderPath, fileName);

            ExtractRootDirMatch_Verify_Throws(format, entryType, destinationFolderPath, filePath, linkTargetPath: null);
        }

        private void ExtractToDirectory_ExactRootDirMatch_Links_Throws(TarEntryFormat format, TarEntryType entryType, bool inverted)
        {
            // inverted == false:
            //   destination: folderSibling/
            //   entry folder: folder/ (does not match destination)
            //   link entry file path: folder/link (does not match destination, should not be extracted)

            // inverted == true:
            //   destination: folder/
            //   entry folder: folderSibling/ (does not match destination)
            //   link entry file path: folderSibling/link (does not match destination, should not be extracted)

            string entryFolderName = inverted ? "folderSibling" : "folder";
            string destinationFolderName = inverted ? "folder" : "folderSibling";

            string linkTargetFileName = "file.txt";
            string linkFileName = "link";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            string linkPath = Path.Join(entryFolderPath, linkFileName);
            string linkTargetPath = Path.Join(destinationFolderPath, linkTargetFileName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);
            File.Create(linkTargetPath).Dispose();

            ExtractRootDirMatch_Verify_Throws(format, entryType, destinationFolderPath, linkPath, linkTargetPath);
        }

        private void ExtractRootDirMatch_Verify_Throws(TarEntryFormat format, TarEntryType entryType, string destinationFolderPath, string entryFilePath, string linkTargetPath)
        {
            using MemoryStream archive = new();
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, entryFilePath);
                MemoryStream dataStream = null;
                if (entryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                {
                    dataStream = new MemoryStream();
                    dataStream.Write(new byte[] { 0x1 });
                    entry.DataStream = dataStream;
                }
                if (entryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
                {
                    entry.LinkName = linkTargetPath;
                }
                writer.WriteEntry(entry);
                if (dataStream != null)
                {
                    dataStream.Dispose();
                }
            }
            archive.Position = 0;

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(archive, destinationFolderPath, overwriteFiles: false));
            Assert.False(File.Exists(entryFilePath), $"File should not exist: {entryFilePath}");
        }
    }
}
