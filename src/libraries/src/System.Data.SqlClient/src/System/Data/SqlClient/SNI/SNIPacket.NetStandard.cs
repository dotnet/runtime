// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient.SNI
{
    internal partial class SNIPacket
    {
        /// <summary>
        /// Read data from a stream asynchronously
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="callback">Completion callback</param>
        public void ReadFromStreamAsync(Stream stream, SNIAsyncCallback callback)
        {
            // Treat local function as a static and pass all params otherwise as async will allocate
            async Task ReadFromStreamAsync(SNIPacket packet, SNIAsyncCallback cb, Task<int> task)
            {
                bool error = false;
                try
                {
                    packet._dataLength = await task.ConfigureAwait(false);
                    if (packet._dataLength == 0)
                    {
                        SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.TCP_PROV, 0, SNICommon.ConnTerminatedError, string.Empty);
                        error = true;
                    }
                }
                catch (Exception ex)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.TCP_PROV, SNICommon.InternalExceptionError, ex);
                    error = true;
                }

                if (error)
                {
                    packet.Release();
                }

                cb(packet, error ? TdsEnums.SNI_ERROR : TdsEnums.SNI_SUCCESS);
            }

            Task<int> t = stream.ReadAsync(_data, _headerLength, _dataCapacity, CancellationToken.None);

            if ((t.Status & TaskStatus.RanToCompletion) != 0)
            {
                _dataLength = t.Result;
                // Zero length to go via async local function as is error condition
                if (_dataLength > 0)
                {
                    callback(this, TdsEnums.SNI_SUCCESS);

                    // Completed
                    return;
                }
            }

            // Not complete or error call the async local function to complete
            _ = ReadFromStreamAsync(this, callback, t);
        }

        /// <summary>
        /// Write data to a stream asynchronously
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        public void WriteToStreamAsync(Stream stream, SNIAsyncCallback callback, SNIProviders provider, bool disposeAfterWriteAsync = false)
        {
            // Treat local function as a static and pass all params otherwise as async will allocate
            async Task WriteToStreamAsync(SNIPacket packet, SNIAsyncCallback cb, SNIProviders providers, bool disposeAfter, Task task)
            {
                uint status = TdsEnums.SNI_SUCCESS;
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(providers, SNICommon.InternalExceptionError, e);
                    status = TdsEnums.SNI_ERROR;
                }

                cb(packet, status);

                if (disposeAfter)
                {
                    packet.Release();
                }
            }

            Task t = stream.WriteAsync(_data, _headerLength, _dataLength, CancellationToken.None);

            if ((t.Status & TaskStatus.RanToCompletion) != 0)
            {
                // Read the result to register as complete for the Task
                t.GetAwaiter().GetResult();

                callback(this, TdsEnums.SNI_SUCCESS);

                if (disposeAfterWriteAsync)
                {
					Release();
                }

                // Completed
                return;
            }

            // Not complete or error call the async local function to complete
            _ = WriteToStreamAsync(this, callback, provider, disposeAfterWriteAsync, t);
        }
    }
}
