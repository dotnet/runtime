// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides methods to help in the implementation of Stream-derived types.</summary>
    internal static partial class StreamHelpers
    {
        /// <summary>
        /// Provides an implementation usable as an override of Stream.CopyToAsync but that uses the shared
        /// ArrayPool for the intermediate buffer rather than allocating a new buffer each time.
        /// </summary>
        /// <param name="source">The source stream from which to read.</param>
        /// <param name="destination">The destination stream to which to write.</param>
        /// <param name="bufferSize">The buffer size to use.</param>
        /// <param name="cancellationToken">The cancellation token to use to cancel the operation.</param>
        /// <remarks>
        /// If/when the base CopyToAsync implementation is changed to use a pooled buffer, 
        /// this will no longer be necessary.
        /// </remarks>
        public static Task ArrayPoolCopyToAsync(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(source != null);
            ValidateCopyToArgs(source, destination, bufferSize);
            return ArrayPoolCopyToAsyncCore(source, destination, bufferSize, cancellationToken);
        }

        /// <summary>Standard read/write loop using ReadAsync on the source and WriteAsync on the destination.</summary>
        private static async Task ArrayPoolCopyToAsyncCore(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            bufferSize = 0; // reuse same field for high water mark to avoid needing another field in the state machine
            try
            {
                while (true)
                {
                    int bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    if (bytesRead > bufferSize)
                    {
                        bufferSize = bytesRead;
                    }
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                Array.Clear(buffer, 0, bufferSize); // clear only the most we used
                ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
            }
        }
    }
}
