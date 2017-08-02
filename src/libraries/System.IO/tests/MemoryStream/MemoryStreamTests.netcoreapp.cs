// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Linq;

namespace System.IO.Tests
{
    public partial class MemoryStreamTests
    {
        [Fact]
        public void WriteSpan_DataWrittenAndPositionUpdated_Success()
        {
            const int Iters = 100;
            var rand = new Random();
            byte[] data = Enumerable.Range(0, (Iters * (Iters + 1)) / 2).Select(_ => (byte)rand.Next(256)).ToArray();
            var s = new MemoryStream();

            int expectedPos = 0;
            for (int i = 0; i <= Iters; i++)
            {
                s.Write(new ReadOnlySpan<byte>(data, expectedPos, i));
                expectedPos += i;
                Assert.Equal(expectedPos, s.Position);
            }

            Assert.Equal(data, s.ToArray());
        }

        [Fact]
        public void ReadSpan_DataReadAndPositionUpdated_Success()
        {
            const int Iters = 100;
            var rand = new Random();
            byte[] data = Enumerable.Range(0, (Iters * (Iters + 1)) / 2).Select(_ => (byte)rand.Next(256)).ToArray();
            var s = new MemoryStream(data);

            int expectedPos = 0;
            for (int i = 0; i <= Iters; i++)
            {
                var toRead = new Span<byte>(new byte[i * 3]); // enough room to read the data and have some offset and have slack at the end

                // Do the read and validate we read the expected number of bytes
                Assert.Equal(i, s.Read(toRead.Slice(i, i)));

                // The contents prior to and after the read should be empty.
                Assert.Equal<byte>(new byte[i], toRead.Slice(0, i).ToArray());
                Assert.Equal<byte>(new byte[i], toRead.Slice(i * 2, i).ToArray());

                // And the data read should match what was expected.
                Assert.Equal(new Span<byte>(data, expectedPos, i).ToArray(), toRead.Slice(i, i).ToArray());

                // Updated position should match
                expectedPos += i;
                Assert.Equal(expectedPos, s.Position);
            }

            // A final read should be empty
            Assert.Equal(0, s.Read(new Span<byte>(new byte[1])));
        }

        [Fact]
        public void DerivedMemoryStream_ReadWriteSpanCalled_ReadWriteArrayUsed()
        {
            var s = new ReadWriteOverridingMemoryStream();
            Assert.False(s.WriteInvoked);
            Assert.False(s.ReadInvoked);

            s.Write((ReadOnlySpan<byte>)new byte[1]);
            Assert.True(s.WriteInvoked);
            Assert.False(s.ReadInvoked);

            s.Position = 0;
            s.Read((Span<byte>)new byte[1]);
            Assert.True(s.WriteInvoked);
            Assert.True(s.ReadInvoked);
        }

        private class ReadWriteOverridingMemoryStream : MemoryStream
        {
            public bool ReadInvoked, WriteInvoked;

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadInvoked = true;
                return base.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteInvoked = true;
                base.Write(buffer, offset, count);
            }
        }
    }
}
