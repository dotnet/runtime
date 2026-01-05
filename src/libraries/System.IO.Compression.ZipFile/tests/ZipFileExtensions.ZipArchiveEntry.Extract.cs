// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_ZipArchiveEntry_Extract : ZipFileTestBase
    {
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToFileExtension(bool async)
        {
            ZipArchive archive = await CallZipFileOpen(async, zfile("normal.zip"), ZipArchiveMode.Read);

            string file = GetTestFilePath();
            ZipArchiveEntry e = archive.GetEntry("first.txt");

            await Assert.ThrowsAsync<ArgumentNullException>(() => CallExtractToFile(async, (ZipArchiveEntry)null, file));
            await Assert.ThrowsAsync<ArgumentNullException>(() => CallExtractToFile(async, e, null));

            //extract when there is nothing there
            await CallExtractToFile(async, e, file);

            using (Stream fs = File.Open(file, FileMode.Open))
            {
                Stream es = await OpenEntryStream(async, e);
                StreamsEqual(fs, es);
                await DisposeStream(async, es);
            }

            await Assert.ThrowsAsync<IOException>(() => CallExtractToFile(async, e, file, false));

            //truncate file
            using (Stream fs = File.Open(file, FileMode.Truncate)) { }

            //now use overwrite mode
            await CallExtractToFile(async, e, file, true);

            using (Stream fs = File.Open(file, FileMode.Open))
            {
                Stream es = await OpenEntryStream(async, e);
                StreamsEqual(fs, es);
                await DisposeStream(async, es);
            }

            await DisposeZipArchive(async, archive);
        }
    }
}
