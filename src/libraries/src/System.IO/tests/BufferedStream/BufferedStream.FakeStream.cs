// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace BufferedStreamTests
{
    /// <summary>
    /// Provides a simple method of testing around a Stream with specific read/write/seek privileges.
    /// </summary>
    public class FakeStream : Stream
    {
        MemoryStream wrapped;
        bool canWrite;
        bool canRead;
        bool canSeek;

        public FakeStream(bool canRead, bool canWrite, bool canSeek)
        {
            wrapped = new MemoryStream();
            this.canWrite = canWrite;
            this.canRead = canRead;
            this.canSeek = canSeek;
        }

        public override bool CanRead
        {
            get
            {
                return canRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return canSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return canWrite;
            }
        }

        public override long Length
        {
            get
            {
                return wrapped.Length;
            }
        }

        public override long Position
        {
            get
            {
                return wrapped.Position;
            }

            set
            {
                wrapped.Position = value;
            }
        }

        public override void Flush()
        {
            wrapped.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return wrapped.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
           return  wrapped.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            wrapped.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            wrapped.Write(buffer, offset, count);
        }
    }
}
