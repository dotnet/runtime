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
    internal static class StreamHelpers
    {
        /// <summary>
        /// Provides an implementation usable as an override of Stream.CopyToAsync but that uses the shared
        /// ArrayPool for the intermediate buffer rather than allocating a new buffer each time.
        /// </summary>
        /// <remarks>
        /// If/when the base CopyToAsync implementation is changed to use a pooled buffer, 
        /// this will no longer be necessary.
        /// </remarks>
        public static Task ArrayPoolCopyToAsync(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(source != null);

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (!source.CanRead)
            {
                throw source.CanWrite ?
                    (Exception)new NotSupportedException(SR.NotSupported_UnreadableStream) :
                    new ObjectDisposedException(null); // passing null as this is used as part of an instance Stream.CopyToAsync override
            }

            if (!destination.CanWrite)
            {
                throw destination.CanRead ?
                    (Exception)new NotSupportedException(SR.NotSupported_UnwritableStream) :
                    new ObjectDisposedException(nameof(destination));
            }

            return ArrayPoolCopyToAsyncInternal(source, destination, bufferSize, cancellationToken);
        }

        private static async Task ArrayPoolCopyToAsyncInternal(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, 0, bufferSize, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true); // TODO: When an overload is available, pass bufferSize so we only clear the used part of the array
            }
        }
    }
}
