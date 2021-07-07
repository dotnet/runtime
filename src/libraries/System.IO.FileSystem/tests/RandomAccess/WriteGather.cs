// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_WriteGather : RandomAccess_Base<long>
    {
        protected override long MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.Write(handle, new ReadOnlyMemory<byte>[] { bytes }, fileOffset);

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ThrowsArgumentNullExceptionForNullBuffers(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: options))
            {
                AssertExtensions.Throws<ArgumentNullException>("buffers", () => RandomAccess.Write(handle, buffers: null, 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ThrowsOnReadAccess(FileOptions options)
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Read, options))
            {
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Write(handle, new ReadOnlyMemory<byte>[] { new byte[1] }, 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void WriteUsingEmptyBufferReturnsZero(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write, options: options))
            {
                Assert.Equal(0, RandomAccess.Write(handle, new ReadOnlyMemory<byte>[] { Array.Empty<byte>() }, fileOffset: 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void WritesBytesFromGivenBuffersToGivenFileAtGivenOffset(FileOptions options)
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, options))
            {
                long total = 0;
                long current = 0;

                while (total != fileSize)
                {
                    int firstBufferLength = (int)Math.Min(content.Length - total, fileSize / 4);
                    Memory<byte> buffer_1 = content.AsMemory((int)total, firstBufferLength);
                    Memory<byte> buffer_2 = content.AsMemory((int)total + firstBufferLength);

                    current = RandomAccess.Write(
                        handle,
                        new ReadOnlyMemory<byte>[]
                        {
                            buffer_1,
                            Array.Empty<byte>(),
                            buffer_2
                        },
                        fileOffset: total);

                    Assert.InRange(current, 0, buffer_1.Length + buffer_2.Length);

                    total += current;
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void DuplicatedBufferDuplicatesContent(FileOptions options)
        {
            const byte value = 1;
            const int repeatCount = 2;
            string filePath = GetTestFilePath();
            ReadOnlyMemory<byte> buffer = new byte[1] { value };
            List<ReadOnlyMemory<byte>> buffers = Enumerable.Repeat(buffer, repeatCount).ToList();

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Create, FileAccess.Write, options: options))
            {
                Assert.Equal(repeatCount, RandomAccess.Write(handle, buffers, fileOffset: 0));
            }

            byte[] actualContent = File.ReadAllBytes(filePath);
            Assert.Equal(repeatCount, actualContent.Length);
            Assert.All(actualContent, actual => Assert.Equal(value, actual));
        }
    }
}
