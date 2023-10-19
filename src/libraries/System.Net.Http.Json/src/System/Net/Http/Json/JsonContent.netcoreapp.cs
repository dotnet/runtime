// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public sealed partial class JsonContent
    {
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            Encoding? targetEncoding = JsonHelpers.GetEncoding(this);
            if (targetEncoding != null && targetEncoding != Encoding.UTF8)
            {
                // Wrap provided stream into a transcoding stream that buffers the data transcoded from utf-8 to the targetEncoding.
                using Stream transcodingStream = Encoding.CreateTranscodingStream(stream, targetEncoding, Encoding.UTF8, leaveOpen: true);
                JsonSerializer.Serialize(transcodingStream, Value, _typeInfo);
                // Dispose will flush any partial write buffers. In practice our partial write
                // buffers should be empty as we expect JsonSerializer to emit only well-formed UTF-8 data.
            }
            else
            {
                JsonSerializer.Serialize(stream, Value, _typeInfo);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => SerializeToStreamAsyncCore(stream, cancellationToken);
    }
}
