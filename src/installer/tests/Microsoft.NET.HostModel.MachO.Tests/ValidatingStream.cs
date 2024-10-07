using Microsoft.NET.HostModel.MachO.Streams;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.NET.HostModel.MachO.Tests
{
    public class ValidatingStream : Stream
    {
        private readonly Stream expectedStream;

        public ValidatingStream(Stream expectedStream)
        {
            this.expectedStream = expectedStream ?? throw new ArgumentNullException(nameof(expectedStream));
        }

        public override bool CanRead => false;

        public override bool CanSeek => expectedStream.CanSeek;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position
        {
            get => expectedStream.Position;
            set => expectedStream.Position = value;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.expectedStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var expected = new byte[count];
            this.expectedStream.ReadFully(expected);

            var actual = new byte[count];
            Array.Copy(buffer, offset, actual, 0, count);

            if (count < 32)
            {
                // Gives us a more concise error message.
                Assert.Equal(Convert.ToHexString(expected), Convert.ToHexString(actual));
            }
            else
            {
                // Assert.Equal for large byte arrays does not perform particularly well;
                // use SequenceEqual instead.
                Assert.True(expected.SequenceEqual(actual));
            }
        }
    }
}
