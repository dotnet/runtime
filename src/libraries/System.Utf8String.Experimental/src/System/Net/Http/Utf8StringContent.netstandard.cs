// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public sealed partial class Utf8StringContent : HttpContent
    {
        protected async override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            ReadOnlyMemory<byte> buffer = _content.AsMemoryBytes();
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                await stream.WriteAsync(array.Array, array.Offset, array.Count).ConfigureAwait(false);
            }
            else
            {
                byte[] localBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                try
                {
                    buffer.Span.CopyTo(localBuffer);
                    await stream.WriteAsync(localBuffer, 0, buffer.Length).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(localBuffer);
                }
            }
        }
    }
}
