// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests that are independent of the archive format.
    public class TarWriter_WriteEntry_Tests : TarTestsBase
    {
        [Fact]
        public void WriteEntry_AfterDispose_Throws()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream);
            writer.Dispose();

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Throws<ObjectDisposedException>(() => writer.WriteEntry(entry));
        }

        [Fact]
        public void WriteEntry_FromUnseekableStream_AdvanceDataStream_WriteFromThatPosition()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream unseekable = new WrappedStream(source, canRead: true, canWrite: true, canSeek: false);

            using MemoryStream destination = new MemoryStream();

            using (TarReader reader = new TarReader(unseekable))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);
                entry.DataStream.ReadByte(); // Advance one byte, now the expected string would be "ello file"

                using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    writer.WriteEntry(entry);
                }
            }

            destination.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(destination))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);

                using (StreamReader streamReader = new StreamReader(entry.DataStream, leaveOpen: true))
                {
                    string contents = streamReader.ReadLine();
                    Assert.Equal("ello file", contents);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void WriteEntry_RespectDefaultWriterFormat(TarEntryFormat expectedFormat)
        {
            using TempDirectory root = new TempDirectory();

            string path = Path.Join(root.Path, "file.txt");
            File.Create(path).Dispose();

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, expectedFormat, leaveOpen: true))
            {
                writer.WriteEntry(path, "file.txt");
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(expectedFormat, entry.Format);

                Type expectedType = GetTypeForFormat(expectedFormat);

                Assert.Equal(expectedType, entry.GetType());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void ReadAndWriteMultipleGlobalExtendedAttributesEntries(TarEntryFormat format)
        {
            Dictionary<string, string> attrs = new Dictionary<string, string>()
            {
                { "hello", "world" },
                { "dotnet", "runtime" }
            };

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                PaxGlobalExtendedAttributesTarEntry gea1 = new PaxGlobalExtendedAttributesTarEntry(attrs);
                writer.WriteEntry(gea1);

                TarEntry entry1 = ConstructEntry(format, "dir1");
                writer.WriteEntry(entry1);

                PaxGlobalExtendedAttributesTarEntry gea2 = new PaxGlobalExtendedAttributesTarEntry(attrs);
                writer.WriteEntry(gea2);

                TarEntry entry2 = ConstructEntry(format, "dir2");
                writer.WriteEntry(entry2);
            }

            archiveStream.Position = 0;

            using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                VerifyGlobalExtendedAttributesEntry(reader.GetNextEntry(), attrs);
                VerifyDirEntry(reader.GetNextEntry(), format, "dir1");
                VerifyGlobalExtendedAttributesEntry(reader.GetNextEntry(), attrs);
                VerifyDirEntry(reader.GetNextEntry(), format, "dir2");
                Assert.Null(reader.GetNextEntry());
            }
        }

        private TarEntry ConstructEntry(TarEntryFormat format, string name) =>
            format switch
            {
                TarEntryFormat.V7 => new V7TarEntry(TarEntryType.Directory, name),
                TarEntryFormat.Ustar => new UstarTarEntry(TarEntryType.Directory, name),
                TarEntryFormat.Pax => new PaxTarEntry(TarEntryType.Directory, name),
                TarEntryFormat.Gnu => new GnuTarEntry(TarEntryType.Directory, name),
                _ => throw new Exception($"Unexpected format {format}"),
            };

        private void VerifyDirEntry(TarEntry entry, TarEntryFormat format, string name)
        {
            Assert.NotNull(entry);
            Assert.Equal(format, entry.Format);
            Assert.Equal(TarEntryType.Directory, entry.EntryType);
            Assert.Equal(name, entry.Name);
        }

        private void VerifyGlobalExtendedAttributesEntry(TarEntry entry, Dictionary<string, string> attrs)
        {
            PaxGlobalExtendedAttributesTarEntry gea = entry as PaxGlobalExtendedAttributesTarEntry;
            Assert.NotNull(gea);
            Assert.Equal(attrs.Count, gea.GlobalExtendedAttributes.Count);

            foreach ((string key, string value) in attrs)
            {
                Assert.Contains(key, gea.GlobalExtendedAttributes);
                Assert.Equal(value, gea.GlobalExtendedAttributes[key]);
            }
        }
    }
}
