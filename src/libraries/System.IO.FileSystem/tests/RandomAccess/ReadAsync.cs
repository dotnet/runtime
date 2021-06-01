// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_ReadAsync : RandomAccess_Base<ValueTask<int>>
    {
        protected override ValueTask<int> MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.ReadAsync(handle, bytes, fileOffset);

        protected override bool ShouldThrowForSyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform async IO using sync handle

        [Fact]
        public async Task HappyPath()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] expected = new byte[fileSize];
            new Random().NextBytes(expected);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: FileOptions.Asynchronous))
            {
                byte[] actual = new byte[fileSize + 1];
                int current = 0;
                int total = 0;

                do
                {
                    current = await RandomAccess.ReadAsync(
                        handle,
                        actual.AsMemory(total, Math.Min(actual.Length - total, fileSize / 4)),
                        fileOffset: total);

                    total += current;
                } while (current != 0);

                Assert.Equal(fileSize, total);
                Assert.Equal(expected, actual.Take(total).ToArray());
            }
        }
    }
}
