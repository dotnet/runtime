// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class zip_CreateTests : ZipFileTestBase
    {
        [Theory]
        [InlineData("folder/", "40755")]
        [InlineData("folder/file", "100644")]
        [InlineData("folder\\file", "100644")]
        public static void Verify_Default_Permissions_Are_Applied_For_Entries(string path, string mode)
        {
            using var archive = new ZipArchive(new MemoryStream(), ZipArchiveMode.Create, false);
            var newEntry = archive.CreateEntry(path);
            Assert.Equal(0, newEntry.ExternalAttributes & 0xffff);
            Assert.Equal(mode, Convert.ToString((uint)newEntry.ExternalAttributes >> 16, 8));
        }
    }
}
