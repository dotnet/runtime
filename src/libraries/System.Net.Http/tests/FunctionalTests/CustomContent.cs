// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    internal partial class CustomContent : StreamContent
    {
        private long _length;

        public static CustomContent Create(string data, bool rewindable)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            Stream stream = new CustomStream(bytes, rewindable);
            
            return new CustomContent(stream, bytes.Length);
        }

        private CustomContent(Stream stream, long length) : base(stream)
        {
            _length = length;
        }

        public long Length
        {
            get
            {
                return _length;
            }
        }

        internal class CustomStream : Stream
        {
            private byte[] _buffer;
            private long _position;
            private bool _rewindable;

            public CustomStream(byte[] buffer, bool rewindable)
            {
                _buffer = buffer;
                _position = 0;
                _rewindable = rewindable;
            }
        
            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return _rewindable; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override void Flush()
            {
                throw new NotSupportedException("CustomStream.Flush");
            }

            public override long Length
            {
                get
                {
                    if (_rewindable)
                    {
                        return (long)_buffer.Length;
                    }
                    else
                    {
                        throw new NotSupportedException("CustomStream.Length");
                    }
                }
            }

            public override long Position
            {
                get
                {
                    return _position;
                }
                set
                {
                    if (_rewindable)
                    {
                        _position = value;
                    }
                    else
                    {
                        throw new NotSupportedException("CustomStream.Position");
                    }
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = 0;

                for (int i = 0; i < count; i++)
                {
                    if (_position >= _buffer.Length)
                    {
                        break;
                    }

                    buffer[offset] = _buffer[_position];
                    bytesRead++;
                    offset++;
                    _position++;
                }

                return bytesRead;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (_rewindable)
                {
                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            Position = offset;
                            break;
                        case SeekOrigin.Current:
                            Position += offset;
                            break;
                        case SeekOrigin.End:
                            Position = _buffer.Length + offset;
                            break;
                        default:
                            throw new NotImplementedException("CustomStream.Seek");
                    }
                    return Position;
                }
                else
                {
                    throw new NotImplementedException("CustomStream.Seek");
                }
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException("CustomStream.SetLength");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("CustomStream.Write");
            }
        }
    }
}
