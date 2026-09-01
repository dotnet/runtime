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
        public static IEnumerable<object[]> GetFormatAndBooleanAndBooleanData() => GetDataAndBooleanData(new[]
        {
            new object[] { TarEntryFormat.V7, false },
            new object[] { TarEntryFormat.V7, true },
            new object[] { TarEntryFormat.Ustar, false },
            new object[] { TarEntryFormat.Ustar, true },
            new object[] { TarEntryFormat.Pax, false },
            new object[] { TarEntryFormat.Pax, true },
            new object[] { TarEntryFormat.Gnu, false },
            new object[] { TarEntryFormat.Gnu, true }
        });

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task ThrowIf_AddFile_AfterDispose(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream);
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
        [MemberData(nameof(GetBooleanData))]
        public async Task FileName_NullOrEmpty(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async);
                TarWriter writer = writerHolder;

                await Assert.ThrowsAsync<ArgumentNullException>(() => WriteEntry(writer, null, "entryName", async));
                await Assert.ThrowsAsync<ArgumentException>(() => WriteEntry(writer, string.Empty, "entryName", async));
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
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
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Pax, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, file1Path, null, async);
                await WriteEntry(writer, file2Path, string.Empty, async);
                        }

            archiveStream.Seek(0, SeekOrigin.Begin);
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry first = await GetNextEntry(reader, async: async);
                Assert.NotNull(first);
                Assert.Equal(file1Name, first.Name);

                TarEntry second = await GetNextEntry(reader, async: async);
                Assert.NotNull(second);
                Assert.Equal(file2Name, second.Name);

                Assert.Null(await GetNextEntry(reader, async: async));
                        }
        }

        [Theory]
        [MemberData(nameof(GetFormatBooleanData))]
        public async Task Add_File(TarEntryFormat format, bool async)
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
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archive, async, format, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, filePath, fileName, async);
            }

            archive.Seek(0, SeekOrigin.Begin);
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archive, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry entry = await GetNextEntry(reader, async: async);
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

                Assert.Null(await GetNextEntry(reader, async: async));
            }
        }

        [Theory]
        [MemberData(nameof(GetFormatAndBooleanAndBooleanData))]
        public async Task Add_Directory(TarEntryFormat format, bool withContents, bool async)
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
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archive, async, format, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, dirPath, dirName, async);
            }

            archive.Seek(0, SeekOrigin.Begin);
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archive, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry entry = await GetNextEntry(reader, async: async);
                Assert.Equal(format, entry.Format);

                Assert.NotNull(entry);
                Assert.Equal(dirName, entry.Name);
                Assert.Equal(TarEntryType.Directory, entry.EntryType);
                Assert.Null(entry.DataStream);

                VerifyPlatformSpecificMetadata(dirPath, entry);

                Assert.Null(await GetNextEntry(reader, async: async));
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [MemberData(nameof(GetFormatAndBooleanAndBooleanData))]
        public async Task Add_SymbolicLink(TarEntryFormat format, bool createTarget, bool async)
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
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archive, async, format, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, linkPath, linkName, async);
            }

            archive.Seek(0, SeekOrigin.Begin);
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archive, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry entry = await GetNextEntry(reader, async: async);
                Assert.Equal(format, entry.Format);

                Assert.NotNull(entry);
                Assert.Equal(linkName, entry.Name);
                Assert.Equal(targetName, entry.LinkName);
                Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
                Assert.Null(entry.DataStream);

                VerifyPlatformSpecificMetadata(linkPath, entry);

                Assert.Null(await GetNextEntry(reader, async: async));
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

            // Create linked files (file1.txt, linked1.txt) and (file2.txt, linked2.txt).
            string file1 = Path.Join(root.Path, "file1.txt");
            File.WriteAllText(file1, "content1");
            string linked1 = Path.Join(root.Path, "linked1.txt");
            File.CreateHardLink(linked1, file1);
            string file2 = Path.Join(root.Path, "file2.txt");
            File.WriteAllText(file2, "content2");
            string linked2 = Path.Join(root.Path, "linked2.txt");
            File.CreateHardLink(linked2, file2);
            // Write to archive. Place the second pair in different directories.

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
            // Verify archive contents
            using (TarReader reader = new TarReader(archive))
            {
                // First file
                TarEntry entry1 = reader.GetNextEntry();
                Assert.NotNull(entry1);
                Assert.Equal("file1.txt", entry1.Name);
                Assert.True(entry1.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile);
                Assert.NotNull(entry1.DataStream);

                // Second entry: hard link or regular file depending on mode
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

                // Third entry
                TarEntry entry3 = reader.GetNextEntry();
                Assert.NotNull(entry3);
                Assert.Equal("dir1/file2.txt", entry3.Name);
                Assert.True(entry3.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile);
                Assert.NotNull(entry3.DataStream);

                // Fourth entry: hard link or regular file depending on mode
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

                Assert.Null(reader.GetNextEntry()); // If the dir had contents, they should've been excluded
            }
        }
    }
}
