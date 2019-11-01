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
        private Stream _content;
        private int _bufferSize;
        private bool _contentConsumed;
        private long _start;

        public StreamContent(Stream content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            // Indicate that we should use default buffer size by setting size to 0.
            InitializeContent(content, 0);
        }

        public StreamContent(Stream content, int bufferSize)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            InitializeContent(content, bufferSize);
        }

        private void InitializeContent(Stream content, int bufferSize)
        {
            _content = content;
            _bufferSize = bufferSize;
            if (content.CanSeek)
            {
                _start = content.Position;
            }
            if (NetEventSource.IsEnabled) NetEventSource.Associate(this, content);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
            SerializeToStreamAsyncCore(stream, default);

        internal override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
            // Only skip the original protected virtual SerializeToStreamAsync if this
            // isn't a derived type that may have overridden the behavior.
            GetType() == typeof(StreamContent) ? SerializeToStreamAsyncCore(stream, cancellationToken) :
            base.SerializeToStreamAsync(stream, context, cancellationToken);

        private Task SerializeToStreamAsyncCore(Stream stream, CancellationToken cancellationToken)
        {
            Debug.Assert(stream != null);
            PrepareContent();
            return StreamToStreamCopy.CopyAsync(
                _content,
                stream,
                _bufferSize,
                !_content.CanSeek, // If the stream can't be re-read, make sure that it gets disposed once it is consumed.
                cancellationToken);
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

        internal override Stream TryCreateContentReadStream() =>
            GetType() == typeof(StreamContent) ? new ReadOnlyStream(_content) : // type check ensures we use possible derived type's CreateContentReadStreamAsync override
            null;

        internal override bool AllowDuplex => false;

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

        private sealed class ReadOnlyStream : DelegatingStream
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

            public override void Write(ReadOnlySpan<byte> buffer)
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

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException(SR.net_http_content_readonly_stream);
            }
        }
    }
}
