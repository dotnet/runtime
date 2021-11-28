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
        [OuterLoop]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/45954", TestPlatforms.Browser)]
        public void NoInt32OverflowInTheBufferingLogic()
        {
            const long position1 = 10;
            const long position2 = (1L << 32) + position1;

            string filePath = GetTestFilePath();
            byte[] data1 = new byte[] { 1, 2, 3, 4, 5 };
            byte[] data2 = new byte[] { 6, 7, 8, 9, 10 };
            byte[] buffer = new byte[5];

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                stream.Seek(position1, SeekOrigin.Begin);
                stream.Write(data1);

                stream.Seek(position2, SeekOrigin.Begin);
                stream.Write(data2);
            }

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(position1, SeekOrigin.Begin);
                Assert.Equal(buffer.Length, stream.Read(buffer));
                Assert.Equal(data1, buffer);

                stream.Seek(position2, SeekOrigin.Begin);
                Assert.Equal(buffer.Length, stream.Read(buffer));
                Assert.Equal(data2, buffer);
            }
        }
    }
}
