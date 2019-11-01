// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Seek : FileSystemTest
    {
        [Fact]
        public void InvalidSeekOriginThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentException>("origin", null, () => fs.Seek(0, ~SeekOrigin.Begin));
            }
        }

        [Fact]
        public void InvalidOffsetThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // Ensure our test is set up correctly
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);

                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Begin));
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Current));
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.End));

                Assert.Throws<IOException>(() => fs.Seek(int.MinValue, SeekOrigin.Begin));
                Assert.Throws<IOException>(() => fs.Seek(int.MinValue, SeekOrigin.Current));
                Assert.Throws<IOException>(() => fs.Seek(int.MinValue, SeekOrigin.End));
            }
        }

        [Fact]
        public void SeekDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Seek(1, SeekOrigin.Begin));
                // no fast path
                Assert.Throws<ObjectDisposedException>(() => fs.Seek(fs.Position, SeekOrigin.Begin));
                // parameter checking happens first
                AssertExtensions.Throws<ArgumentException>("origin", null, () => fs.Seek(0, ~SeekOrigin.Begin));
            }
        }

        [Fact]
        public void SeekUnseekableThrows()
        {
            using (FileStream fs = new UnseekableFileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<NotSupportedException>(() => fs.Seek(1, SeekOrigin.Begin));
                // no fast path
                Assert.Throws<NotSupportedException>(() => fs.Seek(fs.Position, SeekOrigin.Begin));
                // parameter checking happens first
                AssertExtensions.Throws<ArgumentException>("origin", null, () => fs.Seek(0, ~SeekOrigin.Begin));
                // dispose checking happens first
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Seek(fs.Position, SeekOrigin.Begin));
            }
        }

        [Fact]
        public void SeekAppendModifyThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Append))
            {
                long length = fs.Length;
                Assert.Throws<IOException>(() => fs.Seek(length - 1, SeekOrigin.Begin));
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Current));
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.End));
                Assert.Equal(length, fs.Position);

                Assert.Throws<IOException>(() => fs.Seek(0, SeekOrigin.Begin));
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Seek(-length, SeekOrigin.Current));
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Seek(-length, SeekOrigin.End));
                Assert.Equal(length, fs.Position);
            }
        }

        [Fact]
        public void SeekOriginBegin()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                // Ensure our test is set up correctly
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                // Beginning
                Assert.Equal(0, fs.Seek(0, SeekOrigin.Begin));
                Assert.Equal(0, fs.Position);

                // End
                Assert.Equal(fs.Length, fs.Seek(fs.Length, SeekOrigin.Begin));
                Assert.Equal(fs.Length, fs.Position);

                // Middle
                Assert.Equal(fs.Length / 2, fs.Seek(fs.Length / 2, SeekOrigin.Begin));
                Assert.Equal(fs.Length / 2, fs.Position);
            }
        }

        [Fact]
        public void SeekOriginCurrent()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                // Ensure our test is set up correctly
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                // Beginning
                Assert.Equal(0, fs.Seek(-fs.Length, SeekOrigin.Current));
                Assert.Equal(0, fs.Position);

                // End
                Assert.Equal(fs.Length, fs.Seek(fs.Length, SeekOrigin.Current));
                Assert.Equal(fs.Length, fs.Position);

                // Middle
                Assert.Equal(fs.Length / 2, fs.Seek(fs.Length / 2 - fs.Length, SeekOrigin.Current));
                Assert.Equal(fs.Length / 2, fs.Position);
            }
        }


        [Fact]
        public void SeekOriginEnd()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                // Ensure our test is set up correctly
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                // Beginning
                Assert.Equal(0, fs.Seek(-fs.Length, SeekOrigin.End));
                Assert.Equal(0, fs.Position);

                // End
                Assert.Equal(fs.Length, fs.Seek(0, SeekOrigin.End));
                Assert.Equal(fs.Length, fs.Position);

                // Middle
                Assert.Equal(fs.Length / 2, fs.Seek(fs.Length / 2 - fs.Length, SeekOrigin.End));
                Assert.Equal(fs.Length / 2, fs.Position);
            }
        }

        [Fact]
        public void NoopSeeksDoNotChangePosition()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                // Ensure our test is set up correctly
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                // end
                ValidateNoopSeeks(fs);

                fs.Seek(0, SeekOrigin.Begin);

                // beginning
                ValidateNoopSeeks(fs);

                fs.Seek(fs.Length / 2, SeekOrigin.Begin);

                // middle
                ValidateNoopSeeks(fs);
            }
        }

        private void ValidateNoopSeeks(Stream stream)
        {
            // validate seeks that don't change position
            long position = stream.Position;

            Assert.Equal(position, stream.Seek(position, SeekOrigin.Begin));
            Assert.Equal(position, stream.Position);

            Assert.Equal(position, stream.Seek(0, SeekOrigin.Current));
            Assert.Equal(position, stream.Position);

            Assert.Equal(position, stream.Seek(position - stream.Length, SeekOrigin.End));
            Assert.Equal(position, stream.Position);
        }

        [Fact]
        public void SeekPastEnd()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                // Ensure our test is set up correctly
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);
                long originalLength = TestBuffer.Length;

                // Move past end
                Assert.Equal(originalLength + 1, fs.Seek(fs.Length + 1, SeekOrigin.Begin));
                Assert.Equal(originalLength + 1, fs.Position);
                // Length is not updated until a write
                Assert.Equal(originalLength, fs.Length);

                // At end of stream
                Assert.Equal(-1, fs.ReadByte());
                // Read should not update position or length
                Assert.Equal(originalLength + 1, fs.Position);
                Assert.Equal(originalLength, fs.Length);

                // Move back one, still at end of stream since length hasn't changed
                Assert.Equal(originalLength, fs.Seek(-1, SeekOrigin.Current));
                Assert.Equal(-1, fs.ReadByte());
                // Read should not update position or length
                Assert.Equal(originalLength, fs.Position);
                Assert.Equal(originalLength, fs.Length);

                // Move past end
                Assert.Equal(originalLength + 1, fs.Seek(fs.Length + 1, SeekOrigin.Begin));
                fs.WriteByte(0x2A);
                // Writing a single byte should update length by 2 (filling gap with zero).
                Assert.Equal(originalLength + 2, fs.Position);
                Assert.Equal(originalLength + 2, fs.Length);
                // Validate zero fill
                Assert.Equal(originalLength, fs.Seek(-2, SeekOrigin.Current));
                Assert.Equal(0, fs.ReadByte());
                // Validate written value
                Assert.Equal(0x2A, fs.ReadByte());
            }
        }

        [Fact]
        public void RandomSeekReadConsistency()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                var rand = new Random(1); // fixed seed to enable repeatable runs
                const int Trials = 1000;
                const int FileLength = 0x4000;
                const int MaxBytesToRead = 21;

                // Write data to the file
                var buffer = new byte[FileLength];
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = unchecked((byte)i);
                fs.Write(buffer, 0, buffer.Length);
                fs.Position = 0;

                // Repeatedly jump around, reading, and making sure we get the right data back
                for (int trial = 0; trial < Trials; trial++)
                {
                    // Pick some number of bytes to read
                    int bytesToRead = rand.Next(1, MaxBytesToRead);

                    // Jump to a random position, seeking either from one of the possible origins
                    SeekOrigin origin = (SeekOrigin)rand.Next(3);
                    int offset = 0;
                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            offset = rand.Next(0, (int)fs.Length - bytesToRead);
                            break;
                        case SeekOrigin.Current:
                            offset = rand.Next(-(int)fs.Position + bytesToRead, (int)fs.Length - (int)fs.Position - bytesToRead);
                            break;
                        case SeekOrigin.End:
                            offset = -rand.Next(bytesToRead, (int)fs.Length);
                            break;
                    }
                    long pos = fs.Seek(offset, origin);
                    Assert.InRange(pos, 0, fs.Length - bytesToRead - 1);

                    // Read the requested number of bytes, and verify each is correct
                    for (int i = 0; i < bytesToRead; i++)
                    {
                        int byteRead = fs.ReadByte();
                        Assert.Equal(buffer[pos + i], byteRead);
                    }
                }
            }
        }

    }
}
