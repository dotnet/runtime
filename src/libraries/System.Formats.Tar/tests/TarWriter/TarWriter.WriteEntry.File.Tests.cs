// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_WriteEntry_File_Tests : TarTestsBase
    {
        [Fact]
        public void ThrowIf_AddFile_AfterDispose()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream);
            writer.Dispose();

            Assert.Throws<ObjectDisposedException>(() => writer.WriteEntry("fileName", "entryName"));
        }

        [Fact]
        public void FileName_NullOrEmpty()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using TarWriter writer = new TarWriter(archiveStream);

            Assert.Throws<ArgumentNullException>(() => writer.WriteEntry(null, "entryName"));
            Assert.Throws<ArgumentException>(() => writer.WriteEntry(string.Empty, "entryName"));
        }

        [Fact]
        public void EntryName_NullOrEmpty()
        {
            using TempDirectory root = new TempDirectory();

            string file1Name = "file1.txt";
            string file2Name = "file2.txt";

            string file1Path = Path.Join(root.Path, file1Name);
            string file2Path = Path.Join(root.Path, file2Name);

            File.Create(file1Path).Dispose();
            File.Create(file2Path).Dispose();

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                writer.WriteEntry(file1Path, null);
                writer.WriteEntry(file2Path, string.Empty);
            }

            archiveStream.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archiveStream))
            {
                TarEntry first = reader.GetNextEntry();
                Assert.NotNull(first);
                Assert.Equal(file1Name, first.Name);

                TarEntry second = reader.GetNextEntry();
                Assert.NotNull(second);
                Assert.Equal(file2Name, second.Name);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Theory]
        [InlineData(TarFormat.V7)]
        [InlineData(TarFormat.Ustar)]
        [InlineData(TarFormat.Pax)]
        [InlineData(TarFormat.Gnu)]
        public void Add_File(TarFormat format)
        {
            using TempDirectory root = new TempDirectory();
            string fileName = "file.txt";
            string filePath = Path.Join(root.Path, fileName);
            string fileContents = "Hello world";

            using (StreamWriter streamWriter = File.CreateText(filePath))
            {
                streamWriter.Write(fileContents);
            }

            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                writer.WriteEntry(fileName: filePath, entryName: fileName);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                Assert.Equal(TarFormat.Unknown, reader.Format);
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(format, reader.Format);
                Assert.Equal(fileName, entry.Name);
                TarEntryType expectedEntryType = format is TarFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
                Assert.Equal(expectedEntryType, entry.EntryType);
                Assert.True(entry.Length > 0);
                Assert.NotNull(entry.DataStream);

                entry.DataStream.Seek(0, SeekOrigin.Begin);
                using StreamReader dataReader = new StreamReader(entry.DataStream);
                string dataContents = dataReader.ReadLine();

                Assert.Equal(fileContents, dataContents);

                VerifyPlatformSpecificMetadata(filePath, entry);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Theory]
        [InlineData(TarFormat.V7, false)]
        [InlineData(TarFormat.V7, true)]
        [InlineData(TarFormat.Ustar, false)]
        [InlineData(TarFormat.Ustar, true)]
        [InlineData(TarFormat.Pax, false)]
        [InlineData(TarFormat.Pax, true)]
        [InlineData(TarFormat.Gnu, false)]
        [InlineData(TarFormat.Gnu, true)]
        public void Add_Directory(TarFormat format, bool withContents)
        {
            using TempDirectory root = new TempDirectory();
            string dirName = "dir";
            string dirPath = Path.Join(root.Path, dirName);
            Directory.CreateDirectory(dirPath);

            if (withContents)
            {
                // Add a file inside the directory, we need to ensure the contents
                // of the directory are ignored when using AddFile
                File.Create(Path.Join(dirPath, "file.txt")).Dispose();
            }

            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                writer.WriteEntry(fileName: dirPath, entryName: dirName);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                Assert.Equal(TarFormat.Unknown, reader.Format);
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(format, reader.Format);

                Assert.NotNull(entry);
                Assert.Equal(dirName, entry.Name);
                Assert.Equal(TarEntryType.Directory, entry.EntryType);
                Assert.Null(entry.DataStream);

                VerifyPlatformSpecificMetadata(dirPath, entry);

                Assert.Null(reader.GetNextEntry()); // If the dir had contents, they should've been excluded
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarFormat.V7, false)]
        [InlineData(TarFormat.V7, true)]
        [InlineData(TarFormat.Ustar, false)]
        [InlineData(TarFormat.Ustar, true)]
        [InlineData(TarFormat.Pax, false)]
        [InlineData(TarFormat.Pax, true)]
        [InlineData(TarFormat.Gnu, false)]
        [InlineData(TarFormat.Gnu, true)]
        public void Add_SymbolicLink(TarFormat format, bool createTarget)
        {
            using TempDirectory root = new TempDirectory();
            string targetName = "file.txt";
            string linkName = "link.txt";
            string targetPath = Path.Join(root.Path, targetName);
            string linkPath = Path.Join(root.Path, linkName);

            if (createTarget)
            {
                File.Create(targetPath).Dispose();
            }

            FileInfo linkInfo = new FileInfo(linkPath);
            linkInfo.CreateAsSymbolicLink(targetName);

            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                writer.WriteEntry(fileName: linkPath, entryName: linkName);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                Assert.Equal(TarFormat.Unknown, reader.Format);
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(format, reader.Format);

                Assert.NotNull(entry);
                Assert.Equal(linkName, entry.Name);
                Assert.Equal(targetName, entry.LinkName);
                Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
                Assert.Null(entry.DataStream);

                VerifyPlatformSpecificMetadata(linkPath, entry);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Add_PaxGlobalExtendedAttributes_NoEntries(bool withAttributes)
        {
            using MemoryStream archive = new MemoryStream();

            Dictionary<string, string> globalExtendedAttributes = new Dictionary<string, string>();

            if (withAttributes)
            {
                globalExtendedAttributes.Add("hello", "world");
            }

            using (TarWriter writer = new TarWriter(archive, globalExtendedAttributes, leaveOpen: true))
            {
            } // Dispose with no entries

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                // Unknown until reading first entry
                Assert.Equal(TarFormat.Unknown, reader.Format);
                Assert.Null(reader.GlobalExtendedAttributes);

                Assert.Null(reader.GetNextEntry());

                Assert.Equal(TarFormat.Pax, reader.Format);
                Assert.NotNull(reader.GlobalExtendedAttributes);

                int expectedCount = withAttributes ? 1 : 0;
                Assert.Equal(expectedCount, reader.GlobalExtendedAttributes.Count);

                if (expectedCount > 0)
                {
                    Assert.Equal("world", reader.GlobalExtendedAttributes["hello"]);
                }
            }
        }

        partial void VerifyPlatformSpecificMetadata(string filePath, TarEntry entry);
    }
}
