// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_ReadTests : ZipFileTestBase
    {
        [Theory]
        [InlineData("normal.zip", "normal")]
        [InlineData("fake64.zip", "small")]
        [InlineData("empty.zip", "empty")]
        [InlineData("appended.zip", "small")]
        [InlineData("prepended.zip", "small")]
        [InlineData("emptydir.zip", "emptydir")]
        [InlineData("small.zip", "small")]
        [InlineData("unicode.zip", "unicode")]
        public static async Task ReadNormal(string zipFile, string zipFolder)
        {
            await IsZipSameAsDirAsync(zfile(zipFile), zfolder(zipFolder), ZipArchiveMode.Read);
        }

        [Theory]
        [InlineData("normal.zip", "normal")]
        [InlineData("fake64.zip", "small")]
        [InlineData("empty.zip", "empty")]
        [InlineData("appended.zip", "small")]
        [InlineData("prepended.zip", "small")]
        [InlineData("emptydir.zip", "emptydir")]
        [InlineData("small.zip", "small")]
        [InlineData("unicode.zip", "unicode")]
        public static async Task TestStreamingRead(string zipFile, string zipFolder)
        {
            using (var stream = await StreamHelpers.CreateTempCopyStream(zfile(zipFile)))
            {
                Stream wrapped = new WrappedStream(stream, true, false, false, null);
                IsZipSameAsDir(wrapped, zfolder(zipFolder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
                Assert.False(wrapped.CanRead, "Wrapped stream should be closed at this point"); //check that it was closed
            }
        }

        [Fact]
        public static async Task ReadStreamOps()
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(zfile("normal.zip")), ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry e in archive.Entries)
                {
                    using (Stream s = e.Open())
                    {
                        Assert.True(s.CanRead, "Can read to read archive");
                        Assert.False(s.CanWrite, "Can't write to read archive");
                        Assert.False(s.CanSeek, "Can't seek on archive");
                        Assert.Equal(LengthOfUnseekableStream(s), e.Length); //"Length is not correct on unseekable stream"
                    }
                }
            }
        }

        [Fact]
        public static async Task ReadInterleaved()
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"))))
            {
                ZipArchiveEntry e1 = archive.GetEntry("first.txt");
                ZipArchiveEntry e2 = archive.GetEntry("notempty/second.txt");

                //read all of e1 and e2's contents
                byte[] e1readnormal = new byte[e1.Length];
                byte[] e2readnormal = new byte[e2.Length];
                byte[] e1interleaved = new byte[e1.Length];
                byte[] e2interleaved = new byte[e2.Length];

                using (Stream e1s = e1.Open())
                {
                    ReadBytes(e1s, e1readnormal, e1.Length);
                }
                using (Stream e2s = e2.Open())
                {
                    ReadBytes(e2s, e2readnormal, e2.Length);
                }

                //now read interleaved, assume we are working with < 4gb files
                const int bytesAtATime = 15;

                using (Stream e1s = e1.Open(), e2s = e2.Open())
                {
                    int e1pos = 0;
                    int e2pos = 0;

                    while (e1pos < e1.Length || e2pos < e2.Length)
                    {
                        if (e1pos < e1.Length)
                        {
                            int e1bytesRead = e1s.Read(e1interleaved, e1pos,
                                bytesAtATime + e1pos > e1.Length ? (int)e1.Length - e1pos : bytesAtATime);
                            e1pos += e1bytesRead;
                        }

                        if (e2pos < e2.Length)
                        {
                            int e2bytesRead = e2s.Read(e2interleaved, e2pos,
                                bytesAtATime + e2pos > e2.Length ? (int)e2.Length - e2pos : bytesAtATime);
                            e2pos += e2bytesRead;
                        }
                    }
                }

                //now compare to original read
                ArraysEqual<byte>(e1readnormal, e1interleaved, e1readnormal.Length);
                ArraysEqual<byte>(e2readnormal, e2interleaved, e2readnormal.Length);

                //now read one entry interleaved
                byte[] e1selfInterleaved1 = new byte[e1.Length];
                byte[] e1selfInterleaved2 = new byte[e2.Length];


                using (Stream s1 = e1.Open(), s2 = e1.Open())
                {
                    int s1pos = 0;
                    int s2pos = 0;

                    while (s1pos < e1.Length || s2pos < e1.Length)
                    {
                        if (s1pos < e1.Length)
                        {
                            int s1bytesRead = s1.Read(e1interleaved, s1pos,
                                bytesAtATime + s1pos > e1.Length ? (int)e1.Length - s1pos : bytesAtATime);
                            s1pos += s1bytesRead;
                        }

                        if (s2pos < e1.Length)
                        {
                            int s2bytesRead = s2.Read(e2interleaved, s2pos,
                                bytesAtATime + s2pos > e1.Length ? (int)e1.Length - s2pos : bytesAtATime);
                            s2pos += s2bytesRead;
                        }
                    }
                }

                //now compare to original read
                ArraysEqual<byte>(e1readnormal, e1selfInterleaved1, e1readnormal.Length);
                ArraysEqual<byte>(e1readnormal, e1selfInterleaved2, e1readnormal.Length);
            }
        }
        [Fact]
        public static async Task ReadModeInvalidOpsTest()
        {
            ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(zfile("normal.zip")), ZipArchiveMode.Read);
            ZipArchiveEntry e = archive.GetEntry("first.txt");

            //should also do it on deflated stream

            //on archive
            Assert.Throws<NotSupportedException>(() => archive.CreateEntry("hi there")); //"Should not be able to create entry"

            //on entry
            Assert.Throws<NotSupportedException>(() => e.Delete()); //"Should not be able to delete entry"
            //Throws<NotSupportedException>(() => e.MoveTo("dirka"));
            Assert.Throws<NotSupportedException>(() => e.LastWriteTime = new DateTimeOffset()); //"Should not be able to update time"

            //on stream
            Stream s = e.Open();
            Assert.Throws<NotSupportedException>(() => s.Flush()); //"Should not be able to flush on read stream"
            Assert.Throws<NotSupportedException>(() => s.WriteByte(25)); //"should not be able to write to read stream"
            Assert.Throws<NotSupportedException>(() => s.Position = 4); //"should not be able to seek on read stream"
            Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.Begin)); //"should not be able to seek on read stream"
            Assert.Throws<NotSupportedException>(() => s.SetLength(0)); //"should not be able to resize read stream"

            archive.Dispose();

            //after disposed
            Assert.Throws<ObjectDisposedException>(() => { var x = archive.Entries; }); //"Should not be able to get entries on disposed archive"
            Assert.Throws<NotSupportedException>(() => archive.CreateEntry("dirka")); //"should not be able to create on disposed archive"

            Assert.Throws<ObjectDisposedException>(() => e.Open()); //"should not be able to open on disposed archive"
            Assert.Throws<NotSupportedException>(() => e.Delete()); //"should not be able to delete on disposed archive"
            Assert.Throws<ObjectDisposedException>(() => { e.LastWriteTime = new DateTimeOffset(); }); //"Should not be able to update on disposed archive"

            Assert.Throws<NotSupportedException>(() => s.ReadByte()); //"should not be able to read on disposed archive"

            s.Dispose();
        }
    }
}
