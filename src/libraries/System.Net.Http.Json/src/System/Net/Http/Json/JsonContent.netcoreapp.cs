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
            if (JsonHelpers.GetEncoding(this) is Encoding targetEncoding && targetEncoding != Encoding.UTF8)
            {
                SerializeToStreamAsyncTranscoding(stream, async: false, targetEncoding, cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                JsonSerializer.Serialize(stream, Value, _typeInfo);
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => await SerializeToStreamAsyncCore(stream, cancellationToken).ConfigureAwait(false);
    }
}
