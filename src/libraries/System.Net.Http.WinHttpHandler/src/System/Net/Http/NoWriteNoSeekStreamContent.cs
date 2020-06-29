// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides an HttpContent for a Stream that is inherently read-only without support for writing or seeking.</summary>
    /// <remarks>Same as StreamContent, but specialized for no-write, no-seek, and without being constrained by its public API.</remarks>
    internal sealed class NoWriteNoSeekStreamContent : HttpContent
    {
        private readonly Stream _content;
        private bool _contentConsumed;

        internal NoWriteNoSeekStreamContent(Stream content)
        {
            Debug.Assert(content != null);
            Debug.Assert(content.CanRead);
            Debug.Assert(!content.CanWrite);
            Debug.Assert(!content.CanSeek);

            _content = content;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsyncCore(stream, context, async: true, CancellationToken.None);

#if NETCOREAPP
        protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
            SerializeToStreamAsyncCore(stream, context, async: true, cancellationToken);

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            SerializeToStreamAsyncCore(stream, context, async: false, cancellationToken).GetAwaiter().GetResult();
#endif

        private Task SerializeToStreamAsyncCore(Stream stream, TransportContext? context, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(stream != null);

            if (_contentConsumed)
            {
                throw new InvalidOperationException(SR.net_http_content_stream_already_read);
            }
            _contentConsumed = true;

            const int BufferSize = 8192;

            Task copyTask;
            if (async)
            {
                copyTask = _content.CopyToAsync(stream, BufferSize, cancellationToken);
            }
            else
            {
                try
                {
                    _content.CopyTo(stream, BufferSize);
                    copyTask = Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    copyTask = Task.FromException(ex);
                }
            }

            if (copyTask.IsCompleted)
            {
                try { _content.Dispose(); } catch { } // same as StreamToStreamCopy behavior
            }
            else
            {
                copyTask = copyTask.ContinueWith((t, s) =>
                {
                    try { ((Stream)s!).Dispose(); } catch { }
                    t.GetAwaiter().GetResult();
                }, _content, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            return copyTask;
        }
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult(_content);

#if NETCOREAPP
        protected override Stream CreateContentReadStream(CancellationToken cancellationToken) => _content;
#endif

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _content.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
