// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.IO.Compression;

internal static partial class ZipHelper
{
    /// <summary>
    /// Reads exactly bytesToRead out of stream, unless it is out of bytes
    /// </summary>
    internal static int ReadBytes(Stream stream, Span<byte> buffer, int bytesToRead)
    {
        int bytesRead = stream.ReadAtLeast(buffer, bytesToRead, throwOnEndOfStream: false);
        if (bytesRead < bytesToRead)
        {
            throw new IOException(SR.UnexpectedEndOfStream);
        }
        return bytesRead;
    }

    // Assumes all bytes of signatureToFind are non zero, looks backwards from current position in stream,
    // assumes maxBytesToRead is positive, ensures to not read beyond the provided max number of bytes,
    // if the signature is found then returns true and positions stream at first byte of signature
    // if the signature is not found, returns false
    internal static bool SeekBackwardsToSignature(Stream stream, ReadOnlySpan<byte> signatureToFind, int maxBytesToRead)
    {
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
        Span<byte> bufferSpan = buffer.AsSpan(0, BackwardsSeekingBufferSize);

        try
        {
            bool outOfBytes = false;
            bool signatureFound = false;

            int totalBytesRead = 0;
            int duplicateBytesRead = 0;

            while (!signatureFound && !outOfBytes && totalBytesRead <= maxBytesToRead)
            {
                int bytesRead = SeekBackwardsAndRead(stream, bufferSpan, signatureToFind.Length);

                outOfBytes = bytesRead < bufferSpan.Length;
                if (bytesRead < bufferSpan.Length)
                {
                    bufferSpan = bufferSpan.Slice(0, bytesRead);
                }

                bufferPointer = bufferSpan.LastIndexOf(signatureToFind);
                Debug.Assert(bufferPointer < bufferSpan.Length);

                totalBytesRead += (bufferSpan.Length - duplicateBytesRead);

                if (bufferPointer != -1)
                {
                    signatureFound = true;
                    break;
                }

                duplicateBytesRead = signatureToFind.Length;
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

    // Returns the number of bytes actually read.
    // Allows successive buffers to overlap by a number of bytes. This handles cases where
    // the value being searched for straddles buffers (i.e. where the first buffer ends with the
    // first X bytes being searched for, and the second buffer begins with the remaining bytes.)
    private static int SeekBackwardsAndRead(Stream stream, Span<byte> buffer, int overlap)
    {
        int bytesRead;

        if (stream.Position >= buffer.Length)
        {
            Debug.Assert(overlap <= buffer.Length);
            stream.Seek(-(buffer.Length - overlap), SeekOrigin.Current);
            bytesRead = ReadBytes(stream, buffer, buffer.Length);
            stream.Seek(-buffer.Length, SeekOrigin.Current);
        }
        else
        {
            int bytesToRead = (int)stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            bytesRead = ReadBytes(stream, buffer, bytesToRead);
            stream.Seek(0, SeekOrigin.Begin);
        }

        return bytesRead;
    }
}
