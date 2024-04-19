// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_ReadScatter : RandomAccess_Base<long>
    {
        protected override long MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.Read(handle, new Memory<byte>[] { bytes }, fileOffset);

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ThrowsArgumentNullExceptionForNullBuffers(FileOptions options)
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Read, options))
            {
                AssertExtensions.Throws<ArgumentNullException>("buffers", () => RandomAccess.Read(handle, buffers: null, 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ThrowsOnWriteAccess(FileOptions options)
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Write, options))
            {
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Read(handle, new Memory<byte>[] { new byte[1] }, 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReadToAnEmptyBufferReturnsZero(FileOptions options)
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[1]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                Assert.Equal(0, RandomAccess.Read(handle, new Memory<byte>[] { Array.Empty<byte>() }, fileOffset: 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReadFromBeyondEndOfFileReturnsZero(FileOptions options)
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[100]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                long eof = RandomAccess.GetLength(handle);
                Assert.Equal(0, RandomAccess.Read(handle, new Memory<byte>[] { new byte[1] }, fileOffset: eof + 1));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReadsBytesFromGivenFileAtGivenOffset(FileOptions options)
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] expected = RandomNumberGenerator.GetBytes(fileSize);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                byte[] actual = new byte[fileSize + 1];
                long current = 0;
                long total = 0;

                do
                {
                    int firstBufferLength = (int)Math.Min(actual.Length - total, fileSize / 4);
                    Memory<byte> buffer_1 = actual.AsMemory((int)total, firstBufferLength);
                    Memory<byte> buffer_2 = actual.AsMemory((int)total + firstBufferLength);

                    current = RandomAccess.Read(
                        handle,
                        new Memory<byte>[]
                        {
                            buffer_1,
                            Array.Empty<byte>(),
                            buffer_2
                        },
                        fileOffset: total);

                    Assert.InRange(current, 0, buffer_1.Length + buffer_2.Length);

                    total += current;
                } while (current != 0);

                Assert.Equal(fileSize, total);
                Assert.Equal(expected, actual.Take((int)total).ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReadToTheSameBufferOverwritesContent(FileOptions options)
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[3] { 1, 2, 3 });

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                byte[] buffer = new byte[1];
                Assert.Equal(buffer.Length + buffer.Length, RandomAccess.Read(handle, Enumerable.Repeat(buffer.AsMemory(), 2).ToList(), fileOffset: 0));
                Assert.Equal(2, buffer[0]);
            }
        }
    }
}
