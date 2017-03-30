// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class StreamContent : HttpContent
    {
        private const int DefaultBufferSize = 4096;

        private Stream _content;
        private int _bufferSize;
        private CancellationToken _cancellationToken;
        private bool _contentConsumed;
        private long _start;

        public StreamContent(Stream content)
            : this(content, DefaultBufferSize)
        {
        }

        public StreamContent(Stream content, int bufferSize)
            : this(content, bufferSize, CancellationToken.None)
        {
        }

        internal StreamContent(Stream content, CancellationToken cancellationToken)
            : this(content, DefaultBufferSize, cancellationToken)
        {
        }

        private StreamContent(Stream content, int bufferSize, CancellationToken cancellationToken)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            _content = content;
            _bufferSize = bufferSize;
            _cancellationToken = cancellationToken;
            if (content.CanSeek)
            {
                _start = content.Position;
            }
            if (NetEventSource.IsEnabled) NetEventSource.Associate(this, content);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Debug.Assert(stream != null);

            PrepareContent();
            // If the stream can't be re-read, make sure that it gets disposed once it is consumed.
            return StreamToStreamCopy.CopyAsync(_content, stream, _bufferSize, !_content.CanSeek, _cancellationToken);
        }

        protected internal override bool TryComputeLength(out long length)
        {
            if (_content.CanSeek)
            {
                length = _content.Length - _start;
                return true;
            }
            else
            {
                length = 0;
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _content.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            // Wrap the stream with a read-only stream to prevent someone from writing to the stream.
            return Task.FromResult<Stream>(new ReadOnlyStream(_content));
        }

        private void PrepareContent()
        {
            if (_contentConsumed)
            {
                // If the content needs to be written to a target stream a 2nd time, then the stream must support
                // seeking (e.g. a FileStream), otherwise the stream can't be copied a second time to a target 
                // stream (e.g. a NetworkStream).
                if (_content.CanSeek)
                {
                    _content.Position = _start;
                }
                else
                {
                    throw new InvalidOperationException(SR.net_http_content_stream_already_read);
                }
            }

            _contentConsumed = true;
        }

        private class ReadOnlyStream : DelegatingStream
        {
            public override bool CanWrite
            {
                get { return false; }
            }

            public override int WriteTimeout
            {
                get { throw new NotSupportedException(SR.net_http_content_readonly_stream); }
                set { throw new NotSupportedException(SR.net_http_content_readonly_stream); }
            }

            public ReadOnlyStream(Stream innerStream)
                : base(innerStream)
            {
            }

            public override void Flush()
            {
                throw new NotSupportedException(SR.net_http_content_readonly_stream);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                throw new NotSupportedException(SR.net_http_content_readonly_stream);
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException(SR.net_http_content_readonly_stream);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException(SR.net_http_content_readonly_stream);
            }

            public override void WriteByte(byte value)
            {
                throw new NotSupportedException(SR.net_http_content_readonly_stream);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, Threading.CancellationToken cancellationToken)
            {
                throw new NotSupportedException(SR.net_http_content_readonly_stream);
            }
        }
    }
}
