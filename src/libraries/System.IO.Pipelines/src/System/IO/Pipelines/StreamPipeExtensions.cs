// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    /// <summary>Provides extension methods for <see cref="System.IO.Stream" /> that support read and write operations directly into pipes.</summary>
    public static class StreamPipeExtensions
    {
        /// <summary>Asynchronously reads the bytes from the <see cref="System.IO.Stream" /> and writes them to the specified <see cref="System.IO.Pipelines.PipeWriter" />, using a cancellation token.</summary>
        /// <param name="source">The stream from which the contents of the current stream will be copied.</param>
        /// <param name="destination">The writer to which the contents of the source stream will be copied.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        public static Task CopyToAsync(this Stream source, PipeWriter destination, CancellationToken cancellationToken = default)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (destination is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destination);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return destination.CopyFromAsync(source, cancellationToken);
        }
    }
}
