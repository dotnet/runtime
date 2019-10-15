// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_netcoreappTests : ZipFileTestBase
    {
        [Theory]
        [InlineData("sharpziplib.zip", 0)]
        [InlineData("Linux_RW_RW_R__.zip", 0x8000 + 0x0100 + 0x0080 + 0x0020 + 0x0010 + 0x0004)]
        [InlineData("Linux_RWXRW_R__.zip", 0x8000 + 0x01C0 + 0x0020 + 0x0010 + 0x0004)]
        [InlineData("OSX_RWXRW_R__.zip", 0x8000 + 0x01C0 + 0x0020 + 0x0010 + 0x0004)]
        public static async Task Read_UnixFilePermissions(string zipName, uint expectedAttr)
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(compat(zipName)), ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry e in archive.Entries)
                {
                    Assert.Equal(expectedAttr, ((uint)e.ExternalAttributes) >> 16);
                }
            }
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        [InlineData((0x8000 + 0x01C0 + 0x0020 + 0x0010 + 0x0004) << 16)]
        public static async Task RoundTrips_UnixFilePermissions(int expectedAttr)
        {
            using (var stream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip")))
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Update, true))
                {
                    foreach (ZipArchiveEntry e in archive.Entries)
                    {
                        e.ExternalAttributes = expectedAttr;
                        Assert.Equal(expectedAttr, e.ExternalAttributes);
                    }
                }
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry e in archive.Entries)
                    {
                        Assert.Equal(expectedAttr, e.ExternalAttributes);
                    }
                }
            }
        }
    }
}
