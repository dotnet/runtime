// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed class WebSocketHttpListenerDuplexStream : Stream
    {
        private readonly HttpRequestStream _inputStream;
        private readonly HttpResponseStream _outputStream;
        
        public WebSocketHttpListenerDuplexStream(HttpRequestStream inputStream, HttpResponseStream outputStream)
        {
            Debug.Assert(inputStream != null, "'inputStream' MUST NOT be NULL.");
            Debug.Assert(outputStream != null, "'outputStream' MUST NOT be NULL.");
            Debug.Assert(inputStream.CanRead, "'inputStream' MUST support read operations.");
            Debug.Assert(outputStream.CanWrite, "'outputStream' MUST support write operations.");

            _inputStream = inputStream;
            _outputStream = outputStream;
        }

        public override bool CanRead
        {
            get
            {
                return _inputStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return _inputStream.CanTimeout && _outputStream.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _outputStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException(SR.net_noseek);
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException(SR.net_noseek);
            }
            set
            {
                throw new NotSupportedException(SR.net_noseek);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inputStream.Read(buffer, offset, count);
        }
        
        public override IAsyncResult BeginRead(byte[] buffer,
            int offset,
            int count,
            AsyncCallback callback,
            object state)
        {
            return _inputStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _inputStream.EndRead(asyncResult);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _outputStream.Write(buffer, offset, count);
        }
        
        public override IAsyncResult BeginWrite(byte[] buffer,
            int offset,
            int count,
            AsyncCallback callback,
            object state)
        {
            return _outputStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _outputStream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            _outputStream.Flush();
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.net_noseek);
        }
    }
}
