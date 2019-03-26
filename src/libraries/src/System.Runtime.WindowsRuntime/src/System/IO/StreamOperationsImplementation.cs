// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.IO
{
    /// <summary>Depending on the concrete type of the stream managed by a <c>NetFxToWinRtStreamAdapter</c>,
    /// we want the <c>ReadAsync</c> / <c>WriteAsync</c> / <c>FlushAsync</c> / etc. operation to be implemented
    /// differently. This is for best performance as we can take advantage of the specifics of particular stream
    /// types. For instance, <c>ReadAsync</c> currently has a special implementation for memory streams.
    /// Moreover, knowledge about the actual runtime type of the <c>IBuffer</c> can also help choosing the optimal
    /// implementation. This type provides static methods that encapsulate the performance logic and can be used
    /// by <c>NetFxToWinRtStreamAdapter</c>.</summary>
    internal static class StreamOperationsImplementation
    {
        #region ReadAsync implementations

        internal static IAsyncOperationWithProgress<IBuffer, uint> ReadAsync_MemoryStream(Stream stream, IBuffer buffer, uint count)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream is MemoryStream);
            Debug.Assert(stream.CanRead);
            Debug.Assert(stream.CanSeek);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer is IBufferByteAccess);
            Debug.Assert(0 <= count);
            Debug.Assert(count <= int.MaxValue);
            Debug.Assert(count <= buffer.Capacity);

            // We will return a different buffer to the user backed directly by the memory stream (avoids memory copy).
            // This is permitted by the WinRT stream contract.
            // The user specified buffer will not have any data put into it:
            buffer.Length = 0;

            MemoryStream memStream = stream as MemoryStream;
            Debug.Assert(memStream != null);

            try
            {
                IBuffer dataBuffer = memStream.GetWindowsRuntimeBuffer((int)memStream.Position, (int)count);
                if (dataBuffer.Length > 0)
                    memStream.Seek(dataBuffer.Length, SeekOrigin.Current);

                return AsyncInfo.CreateCompletedOperation<IBuffer, UInt32>(dataBuffer);
            }
            catch (Exception ex)
            {
                return AsyncInfo.CreateFaultedOperation<IBuffer, UInt32>(ex);
            }
        }  // ReadAsync_MemoryStream


        internal static IAsyncOperationWithProgress<IBuffer, uint> ReadAsync_AbstractStream(Stream stream, IBuffer buffer, uint count,
                                                                                              InputStreamOptions options)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer is IBufferByteAccess);
            Debug.Assert(0 <= count);
            Debug.Assert(count <= int.MaxValue);
            Debug.Assert(count <= buffer.Capacity);
            Debug.Assert(options == InputStreamOptions.None || options == InputStreamOptions.Partial || options == InputStreamOptions.ReadAhead);

            int bytesRequested = (int)count;

            // Check if the buffer is our implementation.
            // IF YES: In that case, we can read directly into its data array.
            // IF NO:  The buffer is of unknown implementation. It's not backed by a managed array, but the wrapped stream can only
            //         read into a managed array. If we used the user-supplied buffer we would need to copy data into it after every read.
            //         The spec allows to return a buffer instance that is not the same as passed by the user. So, we will create an own
            //         buffer instance, read data *directly* into the array backing it and then return it to the user.
            //         Note: the allocation costs we are paying for the new buffer are unavoidable anyway, as we would need to create
            //         an array to read into either way.

            IBuffer dataBuffer = buffer as WindowsRuntimeBuffer;

            if (dataBuffer == null)
                dataBuffer = WindowsRuntimeBuffer.Create((int)Math.Min((uint)int.MaxValue, buffer.Capacity));

            // This operation delegate will we run inside of the returned IAsyncOperationWithProgress:
            Func<CancellationToken, IProgress<uint>, Task<IBuffer>> readOperation = async (cancelToken, progressListener) =>
            {
                // No bytes read yet:
                dataBuffer.Length = 0;

                // Get the buffer backing array:
                byte[] data;
                int offset;
                bool managedBufferAssert = dataBuffer.TryGetUnderlyingData(out data, out offset);
                Debug.Assert(managedBufferAssert);

                // Init tracking values:
                bool done = cancelToken.IsCancellationRequested;
                int bytesCompleted = 0;

                // Loop until EOS, cancelled or read enough data according to options:
                while (!done)
                {
                    int bytesRead = 0;

                    try
                    {
                        // Read asynchronously:
                        bytesRead = await stream.ReadAsync(data, offset + bytesCompleted, bytesRequested - bytesCompleted, cancelToken)
                                                .ConfigureAwait(continueOnCapturedContext: false);

                        // We will continue here on a different thread when read async completed:
                        bytesCompleted += bytesRead;
                        // We will handle a cancelation exception and re-throw all others:
                    }
                    catch (OperationCanceledException)
                    {
                        // We assume that cancelToken.IsCancellationRequested is has been set and simply proceed.
                        // (we check cancelToken.IsCancellationRequested later)
                        Debug.Assert(cancelToken.IsCancellationRequested);

                        // This is because if the cancellation came after we read some bytes we want to return the results we got instead
                        // of an empty cancelled task, so if we have not yet read anything at all, then we can throw cancellation:
                        if (bytesCompleted == 0 && bytesRead == 0)
                            throw;
                    }

                    // Update target buffer:
                    dataBuffer.Length = (uint)bytesCompleted;

                    Debug.Assert(bytesCompleted <= bytesRequested);

                    // Check if we are done:
                    done = options == InputStreamOptions.Partial  // If no complete read was requested, any amount of data is OK
                            || bytesRead == 0                         // this implies EndOfStream
                            || bytesCompleted == bytesRequested       // read all requested bytes
                            || cancelToken.IsCancellationRequested;   // operation was cancelled

                    // Call user Progress handler:
                    if (progressListener != null)
                        progressListener.Report(dataBuffer.Length);
                }  // while (!done)

                // If we got here, then no error was detected. Return the results buffer:
                return dataBuffer;
            };  // readOperation

            return AsyncInfo.Run<IBuffer, UInt32>(readOperation);
        }  // ReadAsync_AbstractStream

        #endregion ReadAsync implementations


        #region WriteAsync implementations

        internal static IAsyncOperationWithProgress<uint, uint> WriteAsync_AbstractStream(Stream stream, IBuffer buffer)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanWrite);
            Debug.Assert(buffer != null);

            // Choose the optimal writing strategy for the kind of buffer supplied:
            Func<CancellationToken, IProgress<uint>, Task<uint>> writeOperation;
            byte[] data;
            int offset;

            // If buffer is backed by a managed array:
            if (buffer.TryGetUnderlyingData(out data, out offset))
            {
                writeOperation = async (cancelToken, progressListener) =>
                {
                    if (cancelToken.IsCancellationRequested)  // CancellationToken is non-nullable
                        return 0;

                    Debug.Assert(buffer.Length <= int.MaxValue);

                    int bytesToWrite = (int)buffer.Length;

                    await stream.WriteAsync(data, offset, bytesToWrite, cancelToken).ConfigureAwait(continueOnCapturedContext: false);

                    if (progressListener != null)
                        progressListener.Report((uint)bytesToWrite);

                    return (uint)bytesToWrite;
                };
                // Otherwise buffer is of an unknown implementation:
            }
            else
            {
                writeOperation = async (cancelToken, progressListener) =>
                {
                    if (cancelToken.IsCancellationRequested)  // CancellationToken is non-nullable
                        return 0;

                    uint bytesToWrite = buffer.Length;
                    Stream dataStream = buffer.AsStream();

                    int buffSize = 0x4000;
                    if (bytesToWrite < buffSize)
                        buffSize = (int)bytesToWrite;

                    await dataStream.CopyToAsync(stream, buffSize, cancelToken).ConfigureAwait(continueOnCapturedContext: false);

                    if (progressListener != null)
                        progressListener.Report((uint)bytesToWrite);

                    return (uint)bytesToWrite;
                };
            }  // if-else

            // Construct and run the async operation:
            return AsyncInfo.Run<UInt32, UInt32>(writeOperation);
        }  // WriteAsync_AbstractStream

        #endregion WriteAsync implementations


        #region FlushAsync implementations

        internal static IAsyncOperation<bool> FlushAsync_AbstractStream(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanWrite);

            Func<CancellationToken, Task<bool>> flushOperation = async (cancelToken) =>
            {
                if (cancelToken.IsCancellationRequested)  // CancellationToken is non-nullable
                    return false;

                await stream.FlushAsync(cancelToken).ConfigureAwait(continueOnCapturedContext: false);
                return true;
            };

            // Construct and run the async operation:
            return AsyncInfo.Run<Boolean>(flushOperation);
        }
        #endregion FlushAsync implementations

    }  // class StreamOperationsImplementation
}  // namespace
