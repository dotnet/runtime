// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_ReadByte : FileSystemTest
    {

        [Fact]
        public void ReadDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.ReadByte());
            }
        }

        [Fact]
        public void WriteOnlyThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => fs.ReadByte());

                fs.Dispose();
                // Disposed checking happens first
                Assert.Throws<ObjectDisposedException>(() => fs.ReadByte());
            }
        }

        [Fact]
        public void EmptyFileReads()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Equal(-1, fs.ReadByte());
            }
        }


        [Fact]
        public void ReadEntireFile()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                for(int i = 0; i < TestBuffer.Length; i++)
                {
                    Assert.Equal(TestBuffer[i], fs.ReadByte());
                }

                // end of stream
                Assert.Equal(-1, fs.ReadByte());
            }
        }
    }
}
