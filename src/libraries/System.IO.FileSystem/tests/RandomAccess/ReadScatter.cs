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

        protected override bool ShouldThrowForAsyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform sync IO using async handle

        [Fact]
        public void ThrowsArgumentNullExceptionForNullBuffers()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write))
            {
                AssertExtensions.Throws<ArgumentNullException>("buffers", () => RandomAccess.Read(handle, buffers: null, 0));
            }
        }

        [Fact]
        public void ThrowsOnWriteAccess()
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Write))
            {
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Read(handle, new Memory<byte>[] { new byte[1] }, 0));
            }
        }

        [Fact]
        public void ReadToAnEmptyBufferReturnsZero()
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[1]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open))
            {
                Assert.Equal(0, RandomAccess.Read(handle, new Memory<byte>[] { Array.Empty<byte>() }, fileOffset: 0));
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

        [Fact]
        public void ReadToTheSameBufferOverwritesContent()
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[3] { 1, 2, 3 });

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open))
            {
                byte[] buffer = new byte[1];
                Assert.Equal(buffer.Length + buffer.Length, RandomAccess.Read(handle, Enumerable.Repeat(buffer.AsMemory(), 2).ToList(), fileOffset: 0));
                Assert.Equal(2, buffer[0]);
            }
        }
    }
}
