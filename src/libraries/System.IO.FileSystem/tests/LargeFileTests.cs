// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    [OuterLoop]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/45954", TestPlatforms.Browser)]
    [Collection(nameof(DisableParallelization))] // don't create multiple large files at the same time
    public class LargeFileTests : FileSystemTest
    {
        [Fact]
        public void ReadFileOver2GB()
        {
            string path = GetTestFilePath();
            using (FileStream fs = File.Create(path))
            {
                fs.SetLength(int.MaxValue + 1L);
            }

            // File is too large for ReadAllBytes at once
            Assert.Throws<IOException>(() => File.ReadAllBytes(path));
        }

        [Fact]
        public void ReadFileOverMaxArrayLength()
        {
            string path = GetTestFilePath();
            using (FileStream fs = File.Create(path))
            {
                fs.SetLength(Array.MaxLength + 1L);
            }

            // File is too large for ReadAllBytes at once
            Assert.Throws<IOException>(() => File.ReadAllBytes(path));
        }

        [Fact]
        public async Task ReadFileOver2GBAsync()
        {
            string path = GetTestFilePath();
            using (FileStream fs = File.Create(path))
            {
                fs.SetLength(int.MaxValue + 1L);
            }

            // File is too large for ReadAllBytesAsync at once
            await Assert.ThrowsAsync<IOException>(async () => await File.ReadAllBytesAsync(path));
        }

        [Fact]
        public async Task ReadFileOverMaxArrayLengthAsync()
        {
            string path = GetTestFilePath();
            using (FileStream fs = File.Create(path))
            {
                fs.SetLength(Array.MaxLength + 1L);
            }

            // File is too large for ReadAllBytesAsync at once
            await Assert.ThrowsAsync<IOException>(async () => await File.ReadAllBytesAsync(path));
        }

        [Fact]
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
