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
        {
            RandomAccess.Write(handle, bytes, fileOffset);
            return bytes?.Length ?? 0;
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ThrowsOnReadAccess(FileOptions options)
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Read, options))
            {
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Write(handle, new byte[1], 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void WriteUsingEmptyBufferReturns(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write, options: options))
            {
                RandomAccess.Write(handle, Array.Empty<byte>(), fileOffset: 0);
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void CanUseStackAllocatedMemory(FileOptions options)
        {
            string filePath = GetTestFilePath();
            Span<byte> stackAllocated = stackalloc byte[2] { 1, 2 };

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Create, FileAccess.Write, options: options))
            {
                RandomAccess.Write(handle, stackAllocated, fileOffset: 0);
            }

            Assert.Equal(stackAllocated.ToArray(), File.ReadAllBytes(filePath));
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void WriteBeyondEndOfFileExtendsTheFile(FileOptions options)
        {
            string filePath = GetTestFilePath();

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, options: options))
            {
                Assert.Equal(0, RandomAccess.GetLength(handle));
                RandomAccess.Write(handle, new byte[1] { 1 }, fileOffset: 1);
                Assert.Equal(2, RandomAccess.GetLength(handle));
            }

            Assert.Equal(new byte[] { 0, 1 }, File.ReadAllBytes(filePath));
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void WritesBytesFromGivenBufferToGivenFileAtGivenOffset(FileOptions options)
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, options))
            {
                int total = 0;

                while (total != fileSize)
                {
                    Span<byte> buffer = content.AsSpan(total, Math.Min(content.Length - total, fileSize / 4));

                    RandomAccess.Write(handle, buffer, fileOffset: total);

                    total += buffer.Length;
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }
    }
}
