// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
    // Cache the request stream into a MemoryStream.  This is the
    // default behavior of Desktop HttpWebRequest.AllowWriteStreamBuffering (true).
    // Unfortunately, this property is not exposed in .NET Core, so it can't be changed
    // This will result in inefficient memory usage when sending (POST'ing) large
    // amounts of data to the server such as from a file stream.
    internal class RequestStream : Stream
    {
        private MemoryStream _buffer = new MemoryStream();

        public RequestStream()
        {
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override void Flush()
        {
            // Nothing to do.
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            _buffer.Write(buffer, offset, count);
        }

        public ArraySegment<byte> GetBuffer()
        {
            ArraySegment<byte> bytes;

            bool success = _buffer.TryGetBuffer(out bytes);

            if (!success)
            {
                // TODO: Need to figure out how to log this and throw a good exception.
                throw new Exception();
            }

            return bytes;
        }
    }
}
