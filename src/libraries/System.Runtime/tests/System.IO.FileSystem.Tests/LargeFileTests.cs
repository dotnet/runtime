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
        public async Task ReadAllBytesOverLimit()
        {
            using FileStream fs = new (GetTestFilePath(), FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.DeleteOnClose);

            foreach (long lengthOverLimit in new long[] { Array.MaxLength + 1L, int.MaxValue + 1L })
            {
                fs.SetLength(lengthOverLimit);

                Assert.Throws<IOException>(() => File.ReadAllBytes(fs.Name));
                await Assert.ThrowsAsync<IOException>(async () => await File.ReadAllBytesAsync(fs.Name));
            }
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

            using (FileStream stream = File.Create(filePath))
            {
                stream.Seek(position1, SeekOrigin.Begin);
                stream.Write(data1);

                stream.Seek(position2, SeekOrigin.Begin);
                stream.Write(data2);
            }

            using (FileStream stream = new (filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose))
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
