// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class ByteArrayContent : HttpContent
    {
        private readonly byte[] _content;
        private readonly int _offset;
        private readonly int _count;

        public ByteArrayContent(byte[] content)
        {
            ArgumentNullException.ThrowIfNull(content);

            _content = content;
            _count = content.Length;
        }

        public ByteArrayContent(byte[] content, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(content);

            if ((offset < 0) || (offset > content.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if ((count < 0) || (count > (content.Length - offset)))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _content = content;
            _offset = offset;
            _count = count;
        }

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            stream.Write(_content, _offset, _count);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsyncCore(stream, default);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            // Only skip the original protected virtual SerializeToStreamAsync if this
            // isn't a derived type that may have overridden the behavior.
            GetType() == typeof(ByteArrayContent) ? SerializeToStreamAsyncCore(stream, cancellationToken) :
            base.SerializeToStreamAsync(stream, context, cancellationToken);

        private protected Task SerializeToStreamAsyncCore(Stream stream, CancellationToken cancellationToken) =>
            stream.WriteAsync(_content, _offset, _count, cancellationToken);

        protected internal override bool TryComputeLength(out long length)
        {
            length = _count;
            return true;
        }

        protected override Stream CreateContentReadStream(CancellationToken cancellationToken) =>
            CreateMemoryStreamForByteArray();

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(CreateMemoryStreamForByteArray());

        internal override Stream? TryCreateContentReadStream() =>
            GetType() == typeof(ByteArrayContent) ? CreateMemoryStreamForByteArray() : // type check ensures we use possible derived type's CreateContentReadStreamAsync override
            null;

        internal MemoryStream CreateMemoryStreamForByteArray() => new MemoryStream(_content, _offset, _count, writable: false);

        internal override bool AllowDuplex => false;
    }
}
