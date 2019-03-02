// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private const int HalfMaxValue = int.MaxValue / 2;

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
            if (options == null)
                options = s_defaultSettings;

            ReadStack state = default;
            JsonClassInfo classInfo = options.GetOrAddClass(returnType);
            state.Current.JsonClassInfo = classInfo;
            if (classInfo.ClassType != ClassType.Object)
            {
                state.Current.JsonPropertyInfo = classInfo.GetPolicyProperty();
            }

            var readerState = new JsonReaderState(options: options.ReaderOptions);

            int bytesRemaining = 0;
            int bytesRead;

            // todo: switch to ArrayBuffer implementation to handle and simplify the allocs?
            byte[] buffer = ArrayPool<byte>.Shared.Rent(options.EffectiveBufferSize);
            int bufferSize = buffer.Length;
            int deserializeBufferSize;
            bool isFinalBlock;

            try
            {
                do
                {
                    int bytesToRead = bufferSize - bytesRemaining;
                    bytesRead = await utf8Json.ReadAsync(buffer, bytesRemaining, bytesToRead, cancellationToken).ConfigureAwait(false);

                    deserializeBufferSize = bytesRemaining + bytesRead;
                    isFinalBlock = (bytesRead == 0);

                    ReadCore(
                        ref readerState,
                        isFinalBlock,
                        buffer,
                        deserializeBufferSize,
                        options,
                        ref state);

                    int bytesConsumed = (int)readerState.BytesConsumed;
                    bytesRemaining = deserializeBufferSize - bytesConsumed;

                    if (isFinalBlock)
                    {
                        break;
                    }

                    // Check if we need to shift or expand the buffer because there wasn't enough data to complete deserialization.
                    if (bytesConsumed <= (bufferSize / 2))
                    {
                        // We have less than half the buffer available, double the buffer size.
                        bufferSize = (bufferSize < HalfMaxValue) ? bufferSize * 2 : int.MaxValue;

                        byte[] dest = ArrayPool<byte>.Shared.Rent(bufferSize);
                        bufferSize = dest.Length;
                        if (bytesRemaining > 0)
                        {
                            // Copy the unprocessed data to the new buffer while shifting the processed bytes.
                            Buffer.BlockCopy(buffer, bytesConsumed, dest, 0, bytesRemaining);
                        }

                        ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                        buffer = dest;
                    }
                    else if (bytesRemaining > 0)
                    {
                        // Shift the processed bytes to the beginning of buffer to make more room.
                        Buffer.BlockCopy(buffer, bytesConsumed, buffer, 0, bytesRemaining);
                    }
                } while (true);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }

            if (bytesRemaining != 0)
            {
                throw new JsonReaderException(SR.Format(SR.DeserializeDataRemaining,
                    deserializeBufferSize, bytesRemaining), readerState);
            }

            return (TValue)state.Current.ReturnValue;
        }

        private static void ReadCore(
            ref JsonReaderState readerState,
            bool isFinalBlock,
            byte[] buffer,
            int bytesToRead,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            Utf8JsonReader reader = new Utf8JsonReader(buffer.AsSpan(0, bytesToRead), isFinalBlock, readerState);

            ReadCore(
                options,
                ref reader,
                ref state);

            readerState = reader.CurrentState;
        }
    }
}
