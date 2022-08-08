﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_WriteEntry_File_Tests : TarWriter_File_Base
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
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
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
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Add_File(TarEntryFormat format)
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
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(format, entry.Format);
                Assert.Equal(fileName, entry.Name);
                TarEntryType expectedEntryType = format is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
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
        [InlineData(TarEntryFormat.V7, false)]
        [InlineData(TarEntryFormat.V7, true)]
        [InlineData(TarEntryFormat.Ustar, false)]
        [InlineData(TarEntryFormat.Ustar, true)]
        [InlineData(TarEntryFormat.Pax, false)]
        [InlineData(TarEntryFormat.Pax, true)]
        [InlineData(TarEntryFormat.Gnu, false)]
        [InlineData(TarEntryFormat.Gnu, true)]
        public void Add_Directory(TarEntryFormat format, bool withContents)
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
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(format, entry.Format);

                Assert.NotNull(entry);
                Assert.Equal(dirName, entry.Name);
                Assert.Equal(TarEntryType.Directory, entry.EntryType);
                Assert.Null(entry.DataStream);

                VerifyPlatformSpecificMetadata(dirPath, entry);

                Assert.Null(reader.GetNextEntry()); // If the dir had contents, they should've been excluded
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.V7, false)]
        [InlineData(TarEntryFormat.V7, true)]
        [InlineData(TarEntryFormat.Ustar, false)]
        [InlineData(TarEntryFormat.Ustar, true)]
        [InlineData(TarEntryFormat.Pax, false)]
        [InlineData(TarEntryFormat.Pax, true)]
        [InlineData(TarEntryFormat.Gnu, false)]
        [InlineData(TarEntryFormat.Gnu, true)]
        public void Add_SymbolicLink(TarEntryFormat format, bool createTarget)
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
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(format, entry.Format);

                Assert.NotNull(entry);
                Assert.Equal(linkName, entry.Name);
                Assert.Equal(targetName, entry.LinkName);
                Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
                Assert.Null(entry.DataStream);

                VerifyPlatformSpecificMetadata(linkPath, entry);

                Assert.Null(reader.GetNextEntry());
            }
        }
    }
}
