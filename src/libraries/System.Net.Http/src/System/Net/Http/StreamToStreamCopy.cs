// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Helper class is used to copy the content of a source stream to a destination stream,
    /// with optimizations based on expected usage within HttpClient and with the ability
    /// to dispose of the source stream when the copy has completed.
    /// </summary>
    internal static class StreamToStreamCopy
    {
        /// <summary>Copies the source stream from its current position to the destination stream at its current position.</summary>
        /// <param name="source">The source stream from which to copy.</param>
        /// <param name="destination">The destination stream to which to copy.</param>
        /// <param name="bufferSize">The size of the buffer to allocate if one needs to be allocated.</param>
        /// <param name="disposeSource">Whether to dispose of the source stream after the copy has finished successfully.</param>
        /// <param name="cancellationToken">CancellationToken used to cancel the copy operation.</param>
        public static Task CopyAsync(Stream source, Stream destination, int bufferSize, bool disposeSource, CancellationToken cancellationToken = default(CancellationToken))
        {
            Debug.Assert(source != null);
            Debug.Assert(destination != null);
            Debug.Assert(bufferSize > 0);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                // If the source is a MemoryStream from which we can extract the internal buffer,
                // then we can perform the copy in a single write operation.
                ArraySegment<byte> sourceBuffer;
                MemoryStream sourceMemoryStream = source as MemoryStream;
                if (sourceMemoryStream != null && sourceMemoryStream.TryGetBuffer(out sourceBuffer))
                {
                    // It's possible source derives from MemoryStream but has a bad override of Position.
                    // If we get back a value that doesn't make sense, don't take the optimized path.
                    long pos = source.CanSeek ? source.Position : -1;
                    if (pos >= 0 && pos < sourceBuffer.Count)
                    {
                        // We need to copy from the current position, so if necessary update the buffer
                        // based on the position of the stream.
                        if (pos != 0)
                        {
                            sourceBuffer = new ArraySegment<byte>(
                                sourceBuffer.Array, 
                                (int)checked(sourceBuffer.Offset + pos),
                                (int)checked(sourceBuffer.Count - pos));
                        }

                        // Update position to simulate reading all the data
                        source.Position += sourceBuffer.Count; 

                        // Now write the buffer to the destination stream. This will complete synchronously if the 
                        // destination's WriteAsync completes synchronously, such as if destination is also a MemoryStream.  
                        // Thus we don't need to special case it.
                        return WriteToAnyStreamAsync(sourceBuffer, source, destination, disposeSource, cancellationToken);
                    }
                }

                // The source is not a MemoryStream whose buffer we can use directly, but the destination might be our special 
                // LimitMemoryStream, in which case if it's been pre-sized with a capacity, we can optimize the copy 
                // by giving its buffer to the source directly, avoiding the need for an intermediate buffer.
                HttpContent.LimitMemoryStream destinationLimitStream = destination as HttpContent.LimitMemoryStream;
                if (destinationLimitStream != null)
                {
                    // We primarily care about the case where the stream has been presized based on a Content-Length.
                    // If capacity is 0, we have no buffer to copy to, so we skip.  If capacity is <= the max size allowed,
                    // then we can fill the capacity that's there, and if it's not enough (which should be very rare), 
                    // we'll just fall back to continuing with a read/write loop for any additional data.  If the capacity 
                    // is greater than the max size, we don't want to copy to it, as if we use all of the capacity, we'll 
                    // end up exceeding the max, so we skip the optimization for that case.  That case could happen if
                    // there's no Content-Length, and the stream is written to by something before it's given to us, in which 
                    // case the growth-algorithm inside the MemoryStream could cause its capacity to grow beyond the MaxSize; 
                    // that case should also be extremely rare, and we don't care about it from an optimization perspective.
                    int capacity = destinationLimitStream.Capacity;
                    if (capacity > 0 && capacity <= destinationLimitStream.MaxSize)
                    {
                        return CopyAsyncToPreSizedLimitMemoryStream(source, destinationLimitStream, bufferSize, disposeSource, cancellationToken);
                    }
                }

                // If the source is a MemoryStream, at this point we know we can't get its buffer.  But, since
                // we're about to allocate a new byte[] of length bufferSize, if the MemoryStream's Length is
                // no larger than that and we're at the beginning of the stream, we can just ask it for its array 
                // and still do a single write to the target.
                if (sourceMemoryStream != null && 
                    sourceMemoryStream.CanSeek &&
                    sourceMemoryStream.Position == 0 &&
                    sourceMemoryStream.Length <= bufferSize)
                {
                    var buffer = new ArraySegment<byte>(sourceMemoryStream.ToArray());
                    sourceMemoryStream.Position = buffer.Count; // Update position to simulate reading all the data
                    return WriteToAnyStreamAsync(buffer, sourceMemoryStream, destination, disposeSource, cancellationToken);
                }

                // No special-stream cases worked, so we need to fall back to doing a normal copy, involving
                // allocating a byte[] of length bufferSize. However, if we don't need to dispose of the source, 
                // then there's no work to be performed after the copy operation finishes, and we can simply delegate 
                // to the source's CopyToAsync implementation to provide the best implementation the source can muster. 
                // If we do need to dispose of the source, then using CopyToAsync would result in needing an extra
                // async method wrapper and its potential allocations, so we fall back to our own read/write loop that 
                // does the copy and the disposal in a single async method.
                return disposeSource ?
                    CopyAsyncAnyStreamToAnyStreamCore(source, destination, bufferSize, disposeSource, cancellationToken) :
                    source.CopyToAsync(destination, bufferSize, cancellationToken);
            }
            catch (Exception e)
            {
                // For compatibility with the previous implementation, catch everything (including arg exceptions) and
                // store errors into the task rather than letting them propagate to the synchronous caller.
                return Task.FromException(e);
            }
        }

        /// <summary>Writes the array segment to the destination stream.</summary>
        /// <param name="buffer">The array segment to write.</param>
        /// <param name="source">The source stream with which <paramref name="buffer"/> is associated.</param>
        /// <param name="destination">The destination stream to which to write.</param>
        /// <param name="disposeSource">Whether to dispose of the source stream after the copy has finished successfully.</param>
        private static async Task WriteToAnyStreamAsync(ArraySegment<byte> buffer, Stream source, Stream destination, bool disposeSource, CancellationToken cancellationToken)
        {
            await destination.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken).ConfigureAwait(false);
            DisposeSource(disposeSource, source);
        }

        /// <summary>Copies a source stream to a LimitMemoryStream by writing directly to the LimitMemoryStream's buffer.</summary>
        /// <param name="source">The source stream from which to copy.</param>
        /// <param name="destination">The destination LimitMemoryStream to write to.</param>
        /// <param name="bufferSize">The size of the buffer to allocate if one needs to be allocated.</param>
        /// <param name="disposeSource">Whether to dispose of the source stream after the copy has finished successfully.</param>
        private static async Task CopyAsyncToPreSizedLimitMemoryStream(Stream source, HttpContent.LimitMemoryStream destination, int bufferSize, bool disposeSource, CancellationToken cancellationToken)
        {
            // When a LimitMemoryStream is constructed to represent a response with a particular ContentLength, its
            // Capacity is set to that amount in order to pre-size it.  We can take advantage of that in this copy
            // by handing the destination's pre-sized underlying byte[] to the source stream for it to read into
            // rather than creating a temporary buffer, copying from the source into that, and then copying again
            // from the buffer into the LimitMemoryStream.
            long capacity = destination.Capacity;
            Debug.Assert(capacity > 0, "Caller should have checked that there's capacity");

            // Get the initial length of the stream.  When the length of a LimitMemoryStream is increased, the newly available
            // space is zero-filled, either due to allocating a new array or due to an explicit clear.  As a result, we can't
            // write into the array directly and then increase the length to the right size afterward, as doing so will overwrite
            // all of the data newly written.  Instead, we need to increase the length to the capacity, write in our data, and
            // then subsequently trim back the length to the end of the written data.
            long startingLength = destination.Length;
            if (startingLength < capacity)
            {
                destination.SetLength(capacity);
            }

            int bytesRead;
            try
            {
                // Get the LimitMemoryStream's buffer.
                ArraySegment<byte> entireBuffer;
                bool gotBuffer = destination.TryGetBuffer(out entireBuffer);
                Debug.Assert(gotBuffer, "Should only be in CopyAsyncToMemoryStream if we were able to get the buffer");
                Debug.Assert(entireBuffer.Offset == 0, "LimitMemoryStream's are only constructed with a 0-offset");
                Debug.Assert(entireBuffer.Count == entireBuffer.Array.Length, $"LimitMemoryStream's buffer count {entireBuffer.Count} should be the same as its length {entireBuffer.Array.Length}");

                // While there's space remaining in the destination buffer, do another read to try to fill it.
                // Each time we read successfully, we update the position of the destination stream to be
                // at the end of the data read.
                int spaceRemaining = (int)(entireBuffer.Array.Length - destination.Position);
                while (spaceRemaining > 0)
                {
                    // Read into the buffer
                    bytesRead = await source.ReadAsync(entireBuffer.Array, (int)destination.Position, spaceRemaining, cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        DisposeSource(disposeSource, source);
                        return;
                    }
                    destination.Position += bytesRead;
                    spaceRemaining -= bytesRead;
                }
            }
            finally
            {
                // Now that we're done reading directly into the buffer, if we previously increased the length
                // of the stream, set it be at the end of the data read.
                if (startingLength < capacity)
                {
                    destination.SetLength(destination.Position);
                }
            }

            // A typical case will be that we read exactly the amount requested.  This means that the next
            // read will likely return 0 bytes, but we need to try to do the read to know that, which means
            // we need a buffer to read into.  Use a cached single-byte array to do a read for 1-byte.
            // Ideally this read returns 0, and we're done.
            byte[] singleByteArray = RentCachedSingleByteArray();
            bytesRead = await source.ReadAsync(singleByteArray, 0, 1, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                ReturnCachedSingleByteArray(singleByteArray);
                DisposeSource(disposeSource, source);
                return;
            }

            // The read actually returned data, which means there was more data available then
            // the capacity of the LimitMemoryStream.  This is likely an error condition, but
            // regardless we need to finish the copy.  First, we write out the byte we read...
            await destination.WriteAsync(singleByteArray, 0, 1, cancellationToken).ConfigureAwait(false);
            ReturnCachedSingleByteArray(singleByteArray);

            // ...then we fall back to doing the normal read/write loop.
            await CopyAsyncAnyStreamToAnyStreamCore(source, destination, bufferSize, disposeSource, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Copies a source stream to a destination stream via a standard read/write loop.</summary>
        /// <param name="source">The source stream from which to copy.</param>
        /// <param name="destination">The destination stream to which to copy.</param>
        /// <param name="bufferSize">The size of the buffer to allocate if one needs to be allocated.</param>
        /// <param name="disposeSource">Whether to dispose of the source stream after the copy has finished successfully.</param>
        private static async Task CopyAsyncAnyStreamToAnyStreamCore(Stream source, Stream destination, int bufferSize, bool disposeSource, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];

            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, bufferSize, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }

            DisposeSource(disposeSource, source);
        }

        /// <summary>A cached byte[1] array.</summary>
        [ThreadStatic]
        private static byte[] t_singleByteArray;

        /// <summary>Gets a byte[1] array, either taking a cached one or allocating one a new.</summary>
        private static byte[] RentCachedSingleByteArray()
        {
            byte[] singleByteArray = t_singleByteArray;
            if (singleByteArray != null)
            {
                // This will be used across async points, so we need to ensure no one else can use
                // the array while it's in use.
                t_singleByteArray = null;
                return singleByteArray;
            }
            else
            {
                return new byte[1];
            }
        }

        private static void ReturnCachedSingleByteArray(byte[] singleByteArray)
        {
            t_singleByteArray = singleByteArray; // ok if we overwrite one already there
        }

        /// <summary>Disposes the source stream if <paramref name="disposeSource"/> is true.</summary>
        private static void DisposeSource(bool disposeSource, Stream source)
        {
            if (!disposeSource) return;

            try
            {
                source.Dispose();
            }
            catch (Exception e)
            {
                // Dispose() should never throw, but since we're on an async codepath, make sure to catch the exception.
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, e);
            }
        }
    }
}
