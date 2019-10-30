// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_ZipArchiveEntry_Extract : ZipFileTestBase
    {
        [Fact]
        public void ExtractToFileExtension()
        {
            using (ZipArchive archive = ZipFile.Open(zfile("normal.zip"), ZipArchiveMode.Read))
            {
                string file = GetTestFilePath();
                ZipArchiveEntry e = archive.GetEntry("first.txt");

                Assert.Throws<ArgumentNullException>(() => ((ZipArchiveEntry)null).ExtractToFile(file));
                Assert.Throws<ArgumentNullException>(() => e.ExtractToFile(null));

                //extract when there is nothing there
                e.ExtractToFile(file);

                using (Stream fs = File.Open(file, FileMode.Open), es = e.Open())
                {
                    StreamsEqual(fs, es);
                }

                Assert.Throws<IOException>(() => e.ExtractToFile(file, false));

                //truncate file
                using (Stream fs = File.Open(file, FileMode.Truncate))
                { }

                //now use overwrite mode
                e.ExtractToFile(file, true);

                using (Stream fs = File.Open(file, FileMode.Open), es = e.Open())
                {
                    StreamsEqual(fs, es);
                }
            }
        }
    }
}
