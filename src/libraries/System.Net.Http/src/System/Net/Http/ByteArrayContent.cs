// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class ByteArrayContent : HttpContent
    {
        private byte[] _content;
        private int _offset;
        private int _count;

        public ByteArrayContent(byte[] content)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            _content = content;
            _offset = 0;
            _count = content.Length;
        }

        public ByteArrayContent(byte[] content, int offset, int count)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if ((offset < 0) || (offset > content.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || (count > (content.Length - offset)))
            {
                throw new ArgumentOutOfRangeException("count");
            }

            _content = content;
            _offset = offset;
            _count = count;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Debug.Assert(stream != null);
            return stream.WriteAsync(_content, _offset, _count);
        }

        protected internal override bool TryComputeLength(out long length)
        {
            length = _count;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            TaskCompletionSource<Stream> tcs = new TaskCompletionSource<Stream>();
            tcs.TrySetResult(new MemoryStream(_content, _offset, _count, false));
            return tcs.Task;
        }
    }
}
