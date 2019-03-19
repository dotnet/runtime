// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        public static ValueTask<TValue> ReadAsync<TValue>(Stream utf8Json, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            return ReadAsync<TValue>(utf8Json, typeof(TValue), options, cancellationToken);
        }

        public static ValueTask<object> ReadAsync(Stream utf8Json, Type returnType, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            return ReadAsync<object>(utf8Json, returnType, options, cancellationToken);
        }

        private static async ValueTask<TValue> ReadAsync<TValue>(Stream utf8Json, Type returnType, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultSettings;

            ReadStack state = default;
            JsonClassInfo classInfo = options.GetOrAddClass(returnType);
            state.Current.JsonClassInfo = classInfo;
            if (classInfo.ClassType != ClassType.Object)
            {
                state.Current.JsonPropertyInfo = classInfo.GetPolicyProperty();
            }

            var readerState = new JsonReaderState(options.ReaderOptions);

            // todo: switch to ArrayBuffer implementation to handle and simplify the allocs?
            byte[] buffer = ArrayPool<byte>.Shared.Rent(options.EffectiveBufferSize);
            int bytesInBuffer = 0;
            long totalBytesRead = 0;
            int clearMax = 0;

            try
            {
                while (true)
                {
                    // Read from the stream until either our buffer is filled or we hit EOF.
                    // Calling ReadCore is relatively expensive, so we minimize the number of times
                    // we need to call it.
                    bool isFinalBlock = false;
                    while (true)
                    {
                        int bytesRead = await utf8Json.ReadAsync(
#if BUILDING_INBOX_LIBRARY
                            buffer.AsMemory(bytesInBuffer),
#else
                            buffer, bytesInBuffer, buffer.Length - bytesInBuffer,
#endif
                            cancellationToken).ConfigureAwait(false);

                        if (bytesRead == 0)
                        {
                            isFinalBlock = true;
                            break;
                        }

                        totalBytesRead += bytesRead;
                        bytesInBuffer += bytesRead;

                        if (bytesInBuffer == buffer.Length)
                        {
                            break;
                        }
                    }

                    if (bytesInBuffer > clearMax)
                    {
                        clearMax = bytesInBuffer;
                    }

                    // Process the data available
                    ReadCore(
                        ref readerState,
                        isFinalBlock,
                        new Span<byte>(buffer, 0, bytesInBuffer),
                        options,
                        ref state);

                    Debug.Assert(readerState.BytesConsumed <= bytesInBuffer);
                    int bytesConsumed = (int)readerState.BytesConsumed;
                    bytesInBuffer -= bytesConsumed;

                    if (isFinalBlock)
                    {
                        break;
                    }

                    // Check if we need to shift or expand the buffer because there wasn't enough data to complete deserialization.
                    if ((uint)bytesInBuffer > ((uint)buffer.Length / 2))
                    {
                        // We have less than half the buffer available, double the buffer size.
                        byte[] dest = ArrayPool<byte>.Shared.Rent((buffer.Length < (int.MaxValue / 2)) ? buffer.Length * 2 : int.MaxValue);
                        
                        // Copy the unprocessed data to the new buffer while shifting the processed bytes.
                        Buffer.BlockCopy(buffer, bytesConsumed, dest, 0, bytesInBuffer);

                        new Span<byte>(buffer, 0, clearMax).Clear();
                        ArrayPool<byte>.Shared.Return(buffer);

                        clearMax = bytesInBuffer;
                        buffer = dest;
                    }
                    else if (bytesInBuffer != 0)
                    {
                        // Shift the processed bytes to the beginning of buffer to make more room.
                        Buffer.BlockCopy(buffer, bytesConsumed, buffer, 0, bytesInBuffer);
                    }
                }
            }
            finally
            {
                // Clear only what we used and return the buffer to the pool
                new Span<byte>(buffer, 0, clearMax).Clear();
                ArrayPool<byte>.Shared.Return(buffer);
            }

            if (bytesInBuffer != 0)
            {
                throw new JsonReaderException(
                    SR.Format(SR.DeserializeDataRemaining, totalBytesRead, bytesInBuffer),
                    readerState);
            }

            return (TValue)state.Current.ReturnValue;
        }

        private static void ReadCore(
            ref JsonReaderState readerState,
            bool isFinalBlock,
            Span<byte> buffer,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            var reader = new Utf8JsonReader(buffer, isFinalBlock, readerState);

            ReadCore(
                options,
                ref reader,
                ref state);

            readerState = reader.CurrentState;
        }
    }
}
