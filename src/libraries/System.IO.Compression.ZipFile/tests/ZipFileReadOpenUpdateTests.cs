// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class ZipFileTest_ReadOpenUpdate : ZipFileTestBase
    {
        [Fact]
        public void ReadStreamOps()
        {
            using (ZipArchive archive = ZipFile.OpenRead(zfile("normal.zip")))
            {
                foreach (ZipArchiveEntry e in archive.Entries)
                {
                    using (Stream s = e.Open())
                    {
                        Assert.True(s.CanRead, "Can read to read archive");
                        Assert.False(s.CanWrite, "Can't write to read archive");
                        Assert.False(s.CanSeek, "Can't seek on archive");
                        Assert.Equal(LengthOfUnseekableStream(s), e.Length);
                    }
                }
            }
        }

        [Fact]
        public void UpdateReadTwice()
        {
            using (TempFile testArchive = CreateTempCopyFile(zfile("small.zip"), GetTestFilePath()))
            using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.Entries[0];
                string contents1, contents2;
                using (StreamReader s = new StreamReader(entry.Open()))
                {
                    contents1 = s.ReadToEnd();
                }
                using (StreamReader s = new StreamReader(entry.Open()))
                {
                    contents2 = s.ReadToEnd();
                }
                Assert.Equal(contents1, contents2);
            }
        }

        [Fact]
        public async Task UpdateAddFile()
        {
            //add file
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
                {
                    await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
                }
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
            }

            //add file and read entries before
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
                {
                    var x = archive.Entries;

                    await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
                }
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
            }

            //add file and read entries after
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
                {
                    await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");

                    var x = archive.Entries;
                }
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
            }
        }

        private static async Task UpdateArchive(ZipArchive archive, string installFile, string entryName)
        {
            string fileName = installFile;
            ZipArchiveEntry e = archive.CreateEntry(entryName);

            var file = FileData.GetFile(fileName);
            e.LastWriteTime = file.LastModifiedDate;

            using (var stream = await StreamHelpers.CreateTempCopyStream(fileName))
            {
                using (Stream es = e.Open())
                {
                    es.SetLength(0);
                    stream.CopyTo(es);
                }
            }
        }
    }
}

