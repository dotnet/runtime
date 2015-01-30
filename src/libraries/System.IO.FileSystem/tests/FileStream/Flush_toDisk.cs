// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_Flush_toDisk : FileSystemTest
    {
        [Fact]
        public void FlushThrowsForDisposedStream()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Flush(false));
                Assert.Throws<ObjectDisposedException>(() => fs.Flush(true));
            }
        }

        [Fact]
        public void BasicFlushFunctionality()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.WriteByte(0);
                fs.Flush(false);

                fs.WriteByte(0xFF);
                fs.Flush(true);
            }
        }

        [Fact]
        public void FlushOnReadOnlyFileDoesNotThrow()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                fs.Flush(false);
                fs.Flush(true);
            }
        }

        private class MyFileStream : FileStream
        {
            public MyFileStream(string path, FileMode mode)
                : base(path, mode)
            { }

            public Action<bool> FlushToDiskMethod { get; set; }

            public override void Flush(bool flushToDisk)
            {
                if (null != FlushToDiskMethod)
                {
                    FlushToDiskMethod(flushToDisk);
                }

                base.Flush(flushToDisk);
            }
        }

        [Fact]
        public void FlushCallsFlush_toDisk_false()
        {
            bool called = false;

            using (MyFileStream fs = new MyFileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.FlushToDiskMethod = (flushToDisk) =>
                {
                    Assert.False(flushToDisk);
                    called = true;
                };
                fs.Flush();
                Assert.True(called);
            }
        }
    }
}
