// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_Write : RandomAccess_Base<int>
    {
        protected override int MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.Write(handle, bytes, fileOffset);

        protected override bool ShouldThrowForAsyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform sync IO using async handle

        [Fact]
        public void ThrowsOnReadAccess()
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Read))
            {
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Write(handle, new byte[1], 0));
            }
        }

        [Fact]
        public void WriteUsingEmptyBufferReturnsZero()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Equal(0, RandomAccess.Write(handle, Array.Empty<byte>(), fileOffset: 0));
            }
        }

        [Fact]
        public void WritesBytesFromGivenBufferToGivenFileAtGivenOffset()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                int total = 0;
                int current = 0;

                while (total != fileSize)
                {
                    Span<byte> buffer = content.AsSpan(total, Math.Min(content.Length - total, fileSize / 4));

                    current = RandomAccess.Write(handle, buffer, fileOffset: total);

                    Assert.InRange(current, 0, buffer.Length);

                    total += current;
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }
    }
}
