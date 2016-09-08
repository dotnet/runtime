// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.IO.Compression.Tests
{
    public class zip_CreateTests : ZipFileTestBase
    {
        [Fact]
        public static void CreateModeInvalidOperations()
        {
            MemoryStream ms = new MemoryStream();
            ZipArchive z = new ZipArchive(ms, ZipArchiveMode.Create);
            Assert.Throws<NotSupportedException>(() => { var x = z.Entries; }); //"Entries not applicable on Create"
            Assert.Throws<NotSupportedException>(() => z.GetEntry("dirka")); //"GetEntry not applicable on Create"

            ZipArchiveEntry e = z.CreateEntry("hey");
            Assert.Throws<NotSupportedException>(() => e.Delete()); //"Can't delete new entry"

            Stream s = e.Open();
            Assert.Throws<NotSupportedException>(() => s.ReadByte()); //"Can't read on new entry"
            Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.Begin)); //"Can't seek on new entry"
            Assert.Throws<NotSupportedException>(() => s.Position = 0); //"Can't set position on new entry"
            Assert.Throws<NotSupportedException>(() => { var x = s.Length; }); //"Can't get length on new entry"

            Assert.Throws<IOException>(() => e.LastWriteTime = new DateTimeOffset()); //"Can't get LastWriteTime on new entry"
            Assert.Throws<InvalidOperationException>(() => { var x = e.Length; }); //"Can't get length on new entry"
            Assert.Throws<InvalidOperationException>(() => { var x = e.CompressedLength; }); //"can't get CompressedLength on new entry"

            Assert.Throws<IOException>(() => z.CreateEntry("bad"));
            s.Dispose();

            Assert.Throws<ObjectDisposedException>(() => s.WriteByte(25)); //"Can't write to disposed entry"

            Assert.Throws<IOException>(() => e.Open());
            Assert.Throws<IOException>(() => e.LastWriteTime = new DateTimeOffset());
            Assert.Throws<InvalidOperationException>(() => { var x = e.Length; });
            Assert.Throws<InvalidOperationException>(() => { var x = e.CompressedLength; });

            ZipArchiveEntry e1 = z.CreateEntry("e1");
            ZipArchiveEntry e2 = z.CreateEntry("e2");

            Assert.Throws<IOException>(() => e1.Open()); //"Can't open previous entry after new entry created"

            z.Dispose();

            Assert.Throws<ObjectDisposedException>(() => z.CreateEntry("dirka")); //"Can't create after dispose"
        }

        [Theory]
        [InlineData("small", true)]
        [InlineData("small", false)]
        [InlineData("normal", true)]
        [InlineData("normal", false)]
        [InlineData("empty", true)]
        [InlineData("empty", false)]
        [InlineData("emptydir", true)]
        [InlineData("emptydir", false)]
        public static async Task CreateNormal(string folder, bool seekable)
        {
            using (var s = new MemoryStream())
            {
                var testStream = new WrappedStream(s, false, true, seekable, null);
                await CreateFromDir(zfolder(folder), testStream, ZipArchiveMode.Create);

                IsZipSameAsDir(s, zfolder(folder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
            }
        }


        [Theory]
        [InlineData("unicode", true)]
        [InlineData("unicode", false)]
        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Jenkins fails with unicode characters [JENKINS-12610]
        public static async Task CreateNormal_Unicode(string folder, bool seekable)
        {
            using (var s = new MemoryStream())
            {
                var testStream = new WrappedStream(s, false, true, seekable, null);
                await CreateFromDir(zfolder(folder), testStream, ZipArchiveMode.Create);

                IsZipSameAsDir(s, zfolder(folder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
            }
        }
    }
}

