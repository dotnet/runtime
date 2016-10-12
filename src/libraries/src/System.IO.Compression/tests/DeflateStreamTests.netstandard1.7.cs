// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class DeflateStreamTests_NS17 : DeflateStreamTests
    {

        [Fact]
        public static async void BeginEndReadTest()
        {
            byte[] buffer = new byte[32];
            string testFilePath = gzTestFile("GZTestDocument.pdf.gz");
            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(testFilePath, false, true))
            using (var unzip = new DeflateStream(readStream, CompressionMode.Decompress, true))
            {
                IAsyncResult result = unzip.BeginRead(new byte[1], 0, 1, (ar) => { }, new object());
                readStream.manualResetEvent.Set();
                unzip.EndRead(result);
            }
        }

        [Fact]
        public  static async void BeginEndWriteTest()
        {
            byte[] buffer = new byte[32];
            string testFilePath = gzTestFile("GZTestDocument.pdf.gz");
            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(testFilePath, false, true))
            using (var unzip = new DeflateStream(readStream, CompressionMode.Compress, true))
            {
                IAsyncResult result = unzip.BeginWrite(new byte[1], 0, 1, (ar) => { }, new object());
                readStream.manualResetEvent.Set();
                unzip.EndWrite(result);
            }
        }

    }
}