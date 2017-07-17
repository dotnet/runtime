// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.Net.Http.Functional.Tests
{
    public class MultiInterfaceReadOnlyStream : Stream, IRandomAccessStream
    {
        byte[] _bytes;
        long _position;

        public MultiInterfaceReadOnlyStream(byte[] bytes)
        {
            _bytes = bytes;
            _position = 0;
        }

        public MultiInterfaceReadOnlyStream(string content)
        {
            _bytes = Encoding.UTF8.GetBytes(content);
            _position = 0;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { return _bytes.Length; }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            byte[] dataBytes = ReadInternal(count);

            for (int i = 0; i < dataBytes.Length; i++)
            {
                buffer[offset + i] = dataBytes[i];
            }

            return dataBytes.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                _position = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                _position += offset;
            }
            else if (origin == SeekOrigin.End)
            {
                _position = _bytes.Length - offset;
            }
            else
            {
                throw new ArgumentException("Invalid seek origin.");
            }
            
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        bool IRandomAccessStream.CanRead
        {
            get { return true; }
        }

        bool IRandomAccessStream.CanWrite
        {
            get { return false; }
        }

        IRandomAccessStream IRandomAccessStream.CloneStream()
        {
            throw new NotImplementedException();
        }

        IInputStream IRandomAccessStream.GetInputStreamAt(ulong position)
        {
            _position = 0;
            return this;
        }

        IOutputStream IRandomAccessStream.GetOutputStreamAt(ulong position)
        {
            throw new NotImplementedException();
        }

        ulong IRandomAccessStream.Position
        {
            get
            {
                return (ulong)_position;
            }
        }

        void IRandomAccessStream.Seek(ulong position)
        {
            _position = (int)position;
        }

        ulong IRandomAccessStream.Size
        {
            get
            {
                return (ulong)_bytes.Length;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        void IDisposable.Dispose()
        {
        }

        byte[] ReadInternal(int count)
        {
            byte[] dataBytes;

            if (_position == _bytes.Length)
            {
                dataBytes = new byte[0];
            }
            else
            {
                long numBytes = Math.Min(_bytes.Length - _position, count);
                var stream = new MemoryStream(_bytes, (int)_position, (int)numBytes);
                _position += numBytes;
                dataBytes = stream.ToArray();
            }

            return dataBytes;
        }

        IAsyncOperationWithProgress<IBuffer, uint> IInputStream.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            byte[] dataBytes = ReadInternal((int)count);

            var ibuffer = dataBytes.AsBuffer();

            return AsyncInfo.Run(
                delegate (CancellationToken cancellationToken, IProgress<uint> progress)
                {
                    return Task.FromResult<IBuffer>(ibuffer);
                });
        }

        IAsyncOperation<bool> IOutputStream.FlushAsync()
        {
            throw new NotImplementedException();
        }

        IAsyncOperationWithProgress<uint, uint> IOutputStream.WriteAsync(IBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
