// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Read : FileSystemTest
    {
        [Fact]
        public void NegativeReadRootThrows()
        {
            Assert.Throws<UnauthorizedAccessException>(() =>
                new FileStream(Path.GetPathRoot(Directory.GetCurrentDirectory()), FileMode.Open, FileAccess.Read));
        }

        [Fact]
        public void NoInt32OverflowInTheBufferingLogic()
        {
            const long positon1 = 10;
            const long positon2 = (1L << 32) + positon1;

            string filePath = GetTestFilePath();
            byte[] data1 = new byte[] { 1, 2, 3, 4, 5 };
            byte[] data2 = new byte[] { 6, 7, 8, 9, 10 };
            byte[] buffer = new byte[5];

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                stream.Seek(positon1, SeekOrigin.Begin);
                stream.Write(data1, 0, data1.Length);

                stream.Seek(positon2, SeekOrigin.Begin);
                stream.Write(data2, 0, data2.Length);
            }

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(positon1, SeekOrigin.Begin);
                Assert.Equal(buffer.Length, stream.Read(buffer));
                Assert.Equal(data1, buffer);

                Array.Clear(buffer);

                stream.Seek(positon2, SeekOrigin.Begin);
                Assert.Equal(buffer.Length, stream.Read(buffer));
                Assert.Equal(data2, buffer);
            }
        }
    }
}
