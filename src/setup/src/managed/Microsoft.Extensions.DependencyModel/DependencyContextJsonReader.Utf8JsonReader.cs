// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyModel
{
    public partial class DependencyContextJsonReader : IDependencyContextReader
    {
        public DependencyContext Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ArraySegment<byte> buffer = ReadToEnd(stream);
            try
            {
                return Read(new Utf8JsonReader(buffer, isFinalBlock: true, state: default));
            }
            finally
            {
                // Holds document content, clear it before returning it.
                buffer.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }

        private DependencyContext Read(Utf8JsonReader jsonReader)
        {
            var reader = new UnifiedJsonReader(jsonReader);
            return ReadCore(reader);
        }

        // Borrowed from https://github.com/dotnet/corefx/blob/ef23e3317ca6e83f1e959ab265a8e59fb8a6dcd9/src/System.Text.Json/src/System/Text/Json/Document/JsonDocument.Parse.cs#L176-L225
        private static ArraySegment<byte> ReadToEnd(Stream stream)
        {
            int written = 0;
            byte[] rented = null;

            try
            {
                if (stream.CanSeek)
                {
                    // Ask for 1 more than the length to avoid resizing later,
                    // which is unnecessary in the common case where the stream length doesn't change.
                    long expectedLength = Math.Max(0, stream.Length - stream.Position) + 1;
                    rented = ArrayPool<byte>.Shared.Rent(checked((int)expectedLength));
                }
                else
                {
                    rented = ArrayPool<byte>.Shared.Rent(UnseekableStreamInitialRentSize);
                }

                int lastRead;

                do
                {
                    if (rented.Length == written)
                    {
                        byte[] toReturn = rented;
                        rented = ArrayPool<byte>.Shared.Rent(checked(toReturn.Length * 2));
                        Buffer.BlockCopy(toReturn, 0, rented, 0, toReturn.Length);
                        // Holds document content, clear it.
                        ArrayPool<byte>.Shared.Return(toReturn, clearArray: true);
                    }

                    lastRead = stream.Read(rented, written, rented.Length - written);
                    written += lastRead;
                } while (lastRead > 0);

                return new ArraySegment<byte>(rented, 0, written);
            }
            catch
            {
                if (rented != null)
                {
                    // Holds document content, clear it before returning it.
                    rented.AsSpan(0, written).Clear();
                    ArrayPool<byte>.Shared.Return(rented);
                }

                throw;
            }
        }

        private const int UnseekableStreamInitialRentSize = 4096;
    }
}
