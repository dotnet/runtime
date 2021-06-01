// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void HappyPath()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = new byte[fileSize];
            new Random().NextBytes(content);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                int total = 0;

                while (total != fileSize)
                {
                    total += RandomAccess.Write(
                        handle,
                        content.AsSpan(total, Math.Min(content.Length - total, fileSize / 4)),
                        fileOffset: total);
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }
    }
}
