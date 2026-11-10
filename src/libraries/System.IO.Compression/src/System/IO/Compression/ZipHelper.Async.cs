// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

internal static partial class ZipHelper
{
    // Asynchronously assumes all bytes of signatureToFind are non zero, looks backwards from current position in stream,
    // assumes maxBytesToRead is positive, ensures to not read beyond the provided max number of bytes,
    // if the signature is found then returns true and positions stream at first byte of signature
    // if the signature is not found, returns false
    internal static async Task<bool> SeekBackwardsToSignatureAsync(Stream stream, ReadOnlyMemory<byte> signatureToFind, int maxBytesToRead, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Debug.Assert(signatureToFind.Length != 0);
        Debug.Assert(maxBytesToRead > 0);

        // This method reads blocks of BackwardsSeekingBufferSize bytes, searching each block for signatureToFind.
        // A simple LastIndexOf(signatureToFind) doesn't account for cases where signatureToFind is split, starting in
        // one block and ending in another.
        // To account for this, we read blocks of BackwardsSeekingBufferSize bytes, but seek backwards by
        // [BackwardsSeekingBufferSize - signatureToFind.Length] bytes. This guarantees that signatureToFind will not be
        // split between two consecutive blocks, at the cost of reading [signatureToFind.Length] duplicate bytes in each iteration.
        int bufferPointer = 0;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BackwardsSeekingBufferSize);
        Memory<byte> bufferMemory = buffer.AsMemory(0, BackwardsSeekingBufferSize);

        try
        {
            bool outOfBytes = false;
            bool signatureFound = false;

            int totalBytesRead = 0;

            while (!signatureFound && !outOfBytes && totalBytesRead < maxBytesToRead)
            {
                int overlap = totalBytesRead == 0 ? 0 : signatureToFind.Length;

                if (maxBytesToRead - totalBytesRead + overlap < bufferMemory.Length)
                {
                    // If we have less than a full buffer left to read, we adjust the buffer size.
                    bufferMemory = bufferMemory.Slice(0, maxBytesToRead - totalBytesRead + overlap);
                }

                int bytesRead = await SeekBackwardsAndReadAsync(stream, bufferMemory, overlap, cancellationToken).ConfigureAwait(false);

                outOfBytes = bytesRead < bufferMemory.Length;
                if (bytesRead < bufferMemory.Length)
                {
                    bufferMemory = bufferMemory.Slice(0, bytesRead);
                }

                bufferPointer = bufferMemory.Span.LastIndexOf(signatureToFind.Span);
                Debug.Assert(bufferPointer < bufferMemory.Length);

                totalBytesRead += bytesRead - overlap;

                if (bufferPointer != -1)
                {
                    signatureFound = true;
                    break;
                }
            }

            if (!signatureFound)
            {
                return false;
            }
            else
            {
                stream.Seek(bufferPointer, SeekOrigin.Current);
                return true;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // Asynchronously returns the number of bytes actually read.
    // Allows successive buffers to overlap by a number of bytes. This handles cases where
    // the value being searched for straddles buffers (i.e. where the first buffer ends with the
    // first X bytes being searched for, and the second buffer begins with the remaining bytes.)
    private static async Task<int> SeekBackwardsAndReadAsync(Stream stream, Memory<byte> buffer, int overlap, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int bytesRead;

        if (stream.Position >= buffer.Length)
        {
            Debug.Assert(overlap <= buffer.Length);
            stream.Seek(-(buffer.Length - overlap), SeekOrigin.Current);
            bytesRead = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: true, cancellationToken).ConfigureAwait(false);
            stream.Seek(-buffer.Length, SeekOrigin.Current);
        }
        else
        {
            int bytesToRead = (int)stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            bytesRead = await stream.ReadAtLeastAsync(buffer, bytesToRead, throwOnEndOfStream: true, cancellationToken).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);
        }

        return bytesRead;
    }
}
