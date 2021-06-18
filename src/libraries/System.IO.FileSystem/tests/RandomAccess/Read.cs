// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;
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
        public void ThrowsOnWriteAccess()
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Write))
            {
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Read(handle, new byte[1], 0));
            }
        }

        [Fact]
        public void ReadToAnEmptyBufferReturnsZero()
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[1]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open))
            {
                Assert.Equal(0, RandomAccess.Read(handle, Array.Empty<byte>(), fileOffset: 0));
            }
        }

        [Fact]
        public void ReadsBytesFromGivenFileAtGivenOffset()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] expected = RandomNumberGenerator.GetBytes(fileSize);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open))
            {
                byte[] actual = new byte[fileSize + 1];
                int current = 0;
                int total = 0;

                do
                {
                    Span<byte> buffer = actual.AsSpan(total, Math.Min(actual.Length - total, fileSize / 4));

                    current = RandomAccess.Read(handle, buffer, fileOffset: total);

                    Assert.InRange(current, 0, buffer.Length);

                    total += current;
                } while (current != 0);

                Assert.Equal(fileSize, total);
                Assert.Equal(expected, actual.Take(total).ToArray());
            }
        }
    }
}
