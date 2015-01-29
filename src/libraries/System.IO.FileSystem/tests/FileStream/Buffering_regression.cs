// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_Buffering_regression: FileSystemTest
    {
        [Fact]
        public void FlushSetLengthAtEndOfBuffer()
        {
            // This is a regression test for a bug with FileStream’s Flush() 
            // and SetLength() methods.
            // The read-buffer was not flushed inside Flush and SetLength when
            // the buffer pointer was at the end. This causes subsequent Seek 
            // and Read calls to operate on stale/wrong data.


            // customer reported repro
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.SetLength(200);
                fs.Flush();

                // write 119 bytes starting from Pos = 28 
                fs.Seek(28, SeekOrigin.Begin);
                Byte[] buffer = new Byte[119];
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = Byte.MaxValue;
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush();

                // read 317 bytes starting from Pos = 84;
                fs.Seek(84, SeekOrigin.Begin);
                fs.Read(new byte[1024], 0, 317);

                fs.SetLength(135);
                fs.Flush();

                // read one byte at Pos = 97
                fs.Seek(97, SeekOrigin.Begin);
                Assert.Equal(fs.ReadByte(), (int)Byte.MaxValue);
            }
        }
    }
}
