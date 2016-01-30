// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Test.IO.Streams
{
    internal class PositionValueStream : Stream
    {
        private int _remaining;
        private byte _written;

        public PositionValueStream(int totalCount)
        {
            _remaining = totalCount;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_remaining < 0)
            {
                throw new ObjectDisposedException(typeof(PositionValueStream).Name);
            }

            if (_remaining == 0)
            {
                return 0;
            }

            int localLimit = _remaining / 2;

            if (localLimit == 0)
            {
                localLimit = _remaining;
            }

            if (localLimit > count)
            {
                localLimit = count;
            }
            
            for (int i = 0; i < localLimit; i++)
            {
                buffer[offset + i] = _written;

                unchecked
                {
                    _written++;
                }
            }

            _remaining -= localLimit;
            return localLimit;
        }

        protected override void Dispose(bool disposing)
        {
            _remaining = -1;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { throw new NotSupportedException(); } }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}
