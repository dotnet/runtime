// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_GetLength : RandomAccess_Base<long>
    {
        protected override long MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.GetLength(handle);

        protected override bool UsesOffsets => false;

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReturnsZeroForEmptyFile(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: options))
            {
                Assert.Equal(0, RandomAccess.GetLength(handle));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReturnsExactSizeForNonEmptyFiles(FileOptions options)
        {
            const int fileSize = 123;
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[fileSize]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                Assert.Equal(fileSize, RandomAccess.GetLength(handle));
            }
        }
    }
}
