// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_WriteEntry_File_Tests : TarWriter_File_Base
    {
        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task ThrowIf_AddFile_AfterDispose(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, async);
            await DisposeTarWriter(writer, async);

            if (async)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(() => writer.WriteEntryAsync("fileName", "entryName"));
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => writer.WriteEntry("fileName", "entryName"));
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task FileName_NullOrEmpty(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteEntryAsync(null, "entryName"));
                    await Assert.ThrowsAsync<ArgumentException>(() => writer.WriteEntryAsync(string.Empty, "entryName"));
                }
                else
                {
                    Assert.Throws<ArgumentNullException>(() => writer.WriteEntry(null, "entryName"));
                    Assert.Throws<ArgumentException>(() => writer.WriteEntry(string.Empty, "entryName"));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task EntryName_NullOrEmpty(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string file1Name = "file1.txt";
            string file2Name = "file2.txt";

            string file1Path = Path.Join(root.Path, file1Name);
            string file2Path = Path.Join(root.Path, file2Name);

            File.Create(file1Path).Dispose();
            File.Create(file2Path).Dispose();

            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, async, TarEntryFormat.Pax, leaveOpen: true);
            try
            {
                await WriteEntry(writer, file1Path, null, async);
                await WriteEntry(writer, file2Path, string.Empty, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Seek(0, SeekOrigin.Begin);
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                TarEntry first = await GetNextEntry(reader, async);
                Assert.NotNull(first);
                Assert.Equal(file1Name, first.Name);

                TarEntry second = await GetNextEntry(reader, async);
                Assert.NotNull(second);
                Assert.Equal(file2Name, second.Name);

                Assert.Null(await GetNextEntry(reader, async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Add_File(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
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
                TarWriter writer = await CreateTarWriter(archive, async, format, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, filePath, fileName, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archive.Seek(0, SeekOrigin.Begin);
                TarReader reader = await CreateTarReader(archive, async);
                try
                {
                    TarEntry entry = await GetNextEntry(reader, async);
                    Assert.NotNull(entry);
                    Assert.Equal(format, entry.Format);
                    Assert.Equal(fileName, entry.Name);
                    TarEntryType expectedEntryType = GetRegularFileEntryTypeForFormat(format);
                    Assert.Equal(expectedEntryType, entry.EntryType);
                    Assert.True(entry.Length > 0);
                    Assert.NotNull(entry.DataStream);

                    entry.DataStream.Seek(0, SeekOrigin.Begin);
                    using StreamReader dataReader = new StreamReader(entry.DataStream);
                    string dataContents = dataReader.ReadLine();

                    Assert.Equal(fileContents, dataContents);

                    VerifyPlatformSpecificMetadata(filePath, entry);

                    Assert.Null(await GetNextEntry(reader, async));
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
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
        public async Task Add_Directory(TarEntryFormat format, bool withContents)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory root = new TempDirectory();
                string dirName = "dir";
                string dirPath = Path.Join(root.Path, dirName);
                Directory.CreateDirectory(dirPath);

                if (withContents)
                {
                    File.Create(Path.Join(dirPath, "file.txt")).Dispose();
                }

                using MemoryStream archive = new MemoryStream();
                TarWriter writer = await CreateTarWriter(archive, async, format, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, dirPath, dirName, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archive.Seek(0, SeekOrigin.Begin);
                TarReader reader = await CreateTarReader(archive, async);
                try
                {
                    TarEntry entry = await GetNextEntry(reader, async);
                    Assert.Equal(format, entry.Format);

                    Assert.NotNull(entry);
                    Assert.Equal(dirName, entry.Name);
                    Assert.Equal(TarEntryType.Directory, entry.EntryType);
                    Assert.Null(entry.DataStream);

                    VerifyPlatformSpecificMetadata(dirPath, entry);

                    Assert.Null(await GetNextEntry(reader, async));
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
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
        public async Task Add_SymbolicLink(TarEntryFormat format, bool createTarget)
        {
            foreach (bool async in Booleans)
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
                TarWriter writer = await CreateTarWriter(archive, async, format, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, linkPath, linkName, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archive.Seek(0, SeekOrigin.Begin);
                TarReader reader = await CreateTarReader(archive, async);
                try
                {
                    TarEntry entry = await GetNextEntry(reader, async);
                    Assert.Equal(format, entry.Format);

                    Assert.NotNull(entry);
                    Assert.Equal(linkName, entry.Name);
                    Assert.Equal(targetName, entry.LinkName);
                    Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
                    Assert.Null(entry.DataStream);

                    VerifyPlatformSpecificMetadata(linkPath, entry);

                    Assert.Null(await GetNextEntry(reader, async));
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateHardLinks))]
        [InlineData(TarEntryFormat.V7, TarHardLinkMode.PreserveLink)]
        [InlineData(TarEntryFormat.Ustar, TarHardLinkMode.PreserveLink)]
        [InlineData(TarEntryFormat.Pax, TarHardLinkMode.PreserveLink)]
        [InlineData(TarEntryFormat.Gnu, TarHardLinkMode.PreserveLink)]
        [InlineData(TarEntryFormat.V7, TarHardLinkMode.CopyContents)]
        [InlineData(TarEntryFormat.Ustar, TarHardLinkMode.CopyContents)]
        [InlineData(TarEntryFormat.Pax, TarHardLinkMode.CopyContents)]
        [InlineData(TarEntryFormat.Gnu, TarHardLinkMode.CopyContents)]
        public void WriteEntry_HardLinks(TarEntryFormat format, TarHardLinkMode linkMode)
        {
            using TempDirectory root = new TempDirectory();

            string file1 = Path.Join(root.Path, "file1.txt");
            File.WriteAllText(file1, "content1");
            string linked1 = Path.Join(root.Path, "linked1.txt");
            File.CreateHardLink(linked1, file1);
            string file2 = Path.Join(root.Path, "file2.txt");
            File.WriteAllText(file2, "content2");
            string linked2 = Path.Join(root.Path, "linked2.txt");
            File.CreateHardLink(linked2, file2);

            using MemoryStream archive = new MemoryStream();
            TarWriterOptions options = new TarWriterOptions() { Format = format, HardLinkMode = linkMode };
            using (TarWriter writer = new TarWriter(archive, options, leaveOpen: true))
            {
                writer.WriteEntry(file1, "file1.txt");
                writer.WriteEntry(linked1, "linked1.txt");
                writer.WriteEntry(file2, "dir1/file2.txt");
                writer.WriteEntry(linked2, "dir2/linked2.txt");
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry1 = reader.GetNextEntry();
                Assert.NotNull(entry1);
                Assert.Equal("file1.txt", entry1.Name);
                Assert.True(entry1.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile);
                Assert.NotNull(entry1.DataStream);

                TarEntry entry2 = reader.GetNextEntry();
                Assert.NotNull(entry2);
                Assert.Equal("linked1.txt", entry2.Name);
                if (linkMode == TarHardLinkMode.PreserveLink)
                {
                    Assert.Equal(TarEntryType.HardLink, entry2.EntryType);
                    Assert.Equal("file1.txt", entry2.LinkName);
                    Assert.Null(entry2.DataStream);
                }
                else
                {
                    Assert.True(entry2.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile);
                    Assert.NotNull(entry2.DataStream);
                }

                TarEntry entry3 = reader.GetNextEntry();
                Assert.NotNull(entry3);
                Assert.Equal("dir1/file2.txt", entry3.Name);
                Assert.True(entry3.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile);
                Assert.NotNull(entry3.DataStream);

                TarEntry entry4 = reader.GetNextEntry();
                Assert.NotNull(entry4);
                Assert.Equal("dir2/linked2.txt", entry4.Name);
                if (linkMode == TarHardLinkMode.PreserveLink)
                {
                    Assert.Equal(TarEntryType.HardLink, entry4.EntryType);
                    Assert.Equal("dir1/file2.txt", entry4.LinkName);
                    Assert.Null(entry4.DataStream);
                }
                else
                {
                    Assert.True(entry4.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile);
                    Assert.NotNull(entry4.DataStream);
                }

                Assert.Null(reader.GetNextEntry());
            }
        }
    }
}
