// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    internal sealed partial class JsonContent<TValue>
    {
        /// <summary>
        /// This method is overriden to avoid accidentally rooting <see cref="JsonContent.SerializeToStreamAsyncCore(Stream, bool, CancellationToken)"/> post ILLinker trimming.
        /// See <see cref="JsonContent{TValue}.SerializeToStreamAsyncCore(Stream, bool, CancellationToken)"/> for more info.
        /// </summary>
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => SerializeToStreamAsyncCore(stream, async: false, cancellationToken).GetAwaiter().GetResult();

        /// <summary>
        /// This method is overriden to avoid accidentally rooting <see cref="JsonContent.SerializeToStreamAsyncCore(Stream, bool, CancellationToken)"/> post ILLinker trimming.
        /// See <see cref="JsonContent{TValue}.SerializeToStreamAsyncCore(Stream, bool, CancellationToken)"/> for more info.
        /// </summary>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => SerializeToStreamAsyncCore(stream, async: true, cancellationToken);
    }
}
