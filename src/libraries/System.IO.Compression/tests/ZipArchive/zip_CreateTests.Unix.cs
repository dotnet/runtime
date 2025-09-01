// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class zip_CreateTests : ZipFileTestBase
    {
        public static IEnumerable<object[]> Get_Verify_Default_Permissions_Are_Applied_For_Entries_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { "folder/", "40755", async };
                yield return new object[] { "folder/file", "100644", async };
                yield return new object[] { "folder\\file", "100644", async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_Verify_Default_Permissions_Are_Applied_For_Entries_Data))]
        public static async Task Verify_Default_Permissions_Are_Applied_For_Entries(string path, string mode, bool async)
        {
            var archive = await CreateZipArchive(async, new MemoryStream(), ZipArchiveMode.Create, false);
            var newEntry = archive.CreateEntry(path);
            Assert.Equal(0, newEntry.ExternalAttributes & 0xffff);
            Assert.Equal(mode, Convert.ToString((uint)newEntry.ExternalAttributes >> 16, 8));
            await DisposeZipArchive(async, archive);
        }
    }
}
