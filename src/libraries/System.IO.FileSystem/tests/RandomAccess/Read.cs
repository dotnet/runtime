// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_Read : RandomAccess_Base<int>
    {
        protected override int MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.Read(handle, bytes, fileOffset);

        protected override bool ShouldThrowForAsyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform sync IO using async handle

        [Fact]
        public void HappyPath()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] expected = new byte[fileSize];
            new Random().NextBytes(expected);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open))
            {
                byte[] actual = new byte[fileSize + 1];
                int current = 0;
                int total = 0;

                do
                {
                    current = RandomAccess.Read(
                        handle,
                        actual.AsSpan(total, Math.Min(actual.Length - total, fileSize / 4)),
                        fileOffset: total);

                    total += current;
                } while (current != 0);

                Assert.Equal(fileSize, total);
                Assert.Equal(expected, actual.Take(total).ToArray());
            }
        }
    }
}
