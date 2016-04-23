// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Reflection.Metadata.Tests
{
    public class TestStream : Stream
    {
        private readonly bool _canRead, _canSeek, _canWrite;

        public TestStream(bool canRead, bool canSeek, bool canWrite)
        {
            _canRead = canRead;
            _canSeek = canSeek;
            _canWrite = canWrite;
        }

        public override bool CanRead => _canRead;
        public override bool CanSeek => _canSeek;
        public override bool CanWrite => _canWrite;

        public override long Length
        {
            get
            {
                return 0;
            }
        }

        public override long Position
        {
            get
            {
                return 0;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
