// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text
{
    public static class EncodingExtensions
    {
        /// <summary>
        /// The maximum number of input elements after which we'll begin to chunk the input.
        /// </summary>
        /// <remarks>
        /// The reason for this chunking is that the existing Encoding / Encoder / Decoder APIs
        /// like GetByteCount / GetCharCount will throw if an integer overflow occurs. Since
        /// we may be working with large inputs in these extension methods, we don't want to
        /// risk running into this issue. While it's technically possible even for 1 million
        /// input elements to result in an overflow condition, such a scenario is unrealistic,
        /// so we won't worry about it.
        /// </remarks>
        private const int MaxInputElementsPerIteration = 1 * 1024 * 1024;

        /// <summary>
        /// Encodes the specified <see cref="ReadOnlySpan{Char}"/> to <see langword="byte"/>s using the specified <see cref="Encoding"/>
        /// and writes the result to <paramref name="writer"/>.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> which represents how the data in <paramref name="chars"/> should be encoded.</param>
        /// <param name="chars">The <see cref="ReadOnlySequence{Char}"/> to encode to <see langword="byte"/>s.</param>
        /// <param name="writer">The buffer to which the encoded bytes will be written.</param>
        /// <exception cref="EncoderFallbackException">Thrown if <paramref name="chars"/> contains data that cannot be encoded and <paramref name="encoding"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static long GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, IBufferWriter<byte> writer)
        {
            ArgumentNullException.ThrowIfNull(encoding);
            ArgumentNullException.ThrowIfNull(writer);

            if (chars.Length <= MaxInputElementsPerIteration)
            {
                // The input span is small enough where we can one-shot this.

                int byteCount = encoding.GetByteCount(chars);
                Span<byte> scratchBuffer = writer.GetSpan(byteCount);

                int actualBytesWritten = encoding.GetBytes(chars, scratchBuffer);

                writer.Advance(actualBytesWritten);
                return actualBytesWritten;
            }
            else
            {
                // Allocate a stateful Encoder instance and chunk this.

                Convert(encoding.GetEncoder(), chars, writer, flush: true, out long totalBytesWritten, out _);
                return totalBytesWritten;
            }
        }

        /// <summary>
        /// Decodes the specified <see cref="ReadOnlySequence{Char}"/> to <see langword="byte"/>s using the specified <see cref="Encoding"/>
        /// and writes the result to <paramref name="writer"/>.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> which represents how the data in <paramref name="chars"/> should be encoded.</param>
        /// <param name="chars">The <see cref="ReadOnlySequence{Char}"/> whose contents should be encoded.</param>
        /// <param name="writer">The buffer to which the encoded bytes will be written.</param>
        /// <returns>The number of bytes written to <paramref name="writer"/>.</returns>
        /// <exception cref="EncoderFallbackException">Thrown if <paramref name="chars"/> contains data that cannot be encoded and <paramref name="encoding"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static long GetBytes(this Encoding encoding, in ReadOnlySequence<char> chars, IBufferWriter<byte> writer)
        {
            ArgumentNullException.ThrowIfNull(encoding);
            ArgumentNullException.ThrowIfNull(writer);

            // Delegate to the Span-based method if possible.
            // If that doesn't work, allocate the Encoder instance and run a loop.

            if (chars.IsSingleSegment)
            {
                return GetBytes(encoding, chars.FirstSpan, writer);
            }
            else
            {
                Convert(encoding.GetEncoder(), chars, writer, flush: true, out long bytesWritten, out _);
                return bytesWritten;
            }
        }

        /// <summary>
        /// Encodes the specified <see cref="ReadOnlySequence{Char}"/> to <see langword="byte"/>s using the specified <see cref="Encoding"/>
        /// and outputs the result to <paramref name="bytes"/>.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> which represents how the data in <paramref name="chars"/> should be encoded.</param>
        /// <param name="chars">The <see cref="ReadOnlySequence{Char}"/> to encode to <see langword="byte"/>s.</param>
        /// <param name="bytes">The destination buffer to which the encoded bytes will be written.</param>
        /// <returns>The number of bytes written to <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="bytes"/> is not large enough to contain the encoded form of <paramref name="chars"/>.</exception>
        /// <exception cref="EncoderFallbackException">Thrown if <paramref name="chars"/> contains data that cannot be encoded and <paramref name="encoding"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static int GetBytes(this Encoding encoding, in ReadOnlySequence<char> chars, Span<byte> bytes)
        {
            ArgumentNullException.ThrowIfNull(encoding);

            if (chars.IsSingleSegment)
            {
                // If the incoming sequence is single-segment, one-shot this.

                return encoding.GetBytes(chars.FirstSpan, bytes);
            }
            else
            {
                // If the incoming sequence is multi-segment, create a stateful Encoder
                // and use it as the workhorse. On the final iteration we'll pass flush=true.

                ReadOnlySequence<char> remainingChars = chars;
                int originalBytesLength = bytes.Length;
                Encoder encoder = encoding.GetEncoder();
                bool isFinalSegment;

                do
                {
                    remainingChars.GetFirstSpan(out ReadOnlySpan<char> firstSpan, out SequencePosition next);
                    isFinalSegment = remainingChars.IsSingleSegment;

                    int bytesWrittenJustNow = encoder.GetBytes(firstSpan, bytes, flush: isFinalSegment);
                    bytes = bytes.Slice(bytesWrittenJustNow);
                    remainingChars = remainingChars.Slice(next);
                } while (!isFinalSegment);

                return originalBytesLength - bytes.Length; // total number of bytes we wrote
            }
        }

        /// <summary>
        /// Encodes the specified <see cref="ReadOnlySequence{Char}"/> into a <see cref="byte"/> array using the specified <see cref="Encoding"/>.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> which represents how the data in <paramref name="chars"/> should be encoded.</param>
        /// <param name="chars">The <see cref="ReadOnlySequence{Char}"/> to encode to <see langword="byte"/>s.</param>
        /// <returns>A <see cref="byte"/> array which represents the encoded contents of <paramref name="chars"/>.</returns>
        /// <exception cref="EncoderFallbackException">Thrown if <paramref name="chars"/> contains data that cannot be encoded and <paramref name="encoding"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static byte[] GetBytes(this Encoding encoding, in ReadOnlySequence<char> chars)
        {
            ArgumentNullException.ThrowIfNull(encoding);

            if (chars.IsSingleSegment)
            {
                // If the incoming sequence is single-segment, one-shot this.

                ReadOnlySpan<char> span = chars.FirstSpan;

                byte[] retVal = new byte[encoding.GetByteCount(span)];
                encoding.GetBytes(span, retVal);
                return retVal;
            }
            else
            {
                // If the incoming sequence is multi-segment, create a stateful Encoder
                // and use it as the workhorse. On the final iteration we'll pass flush=true.

                Encoder encoder = encoding.GetEncoder();

                // Maintain a list of all the segments we'll need to concat together.
                // These will be released back to the pool at the end of the method.

                List<(byte[], int)> listOfSegments = new List<(byte[], int)>();
                int totalByteCount = 0;

                ReadOnlySequence<char> remainingChars = chars;
                bool isFinalSegment;

                do
                {
                    remainingChars.GetFirstSpan(out ReadOnlySpan<char> firstSpan, out SequencePosition next);
                    isFinalSegment = remainingChars.IsSingleSegment;

                    int byteCountThisIteration = encoder.GetByteCount(firstSpan, flush: isFinalSegment);
                    byte[] rentedArray = ArrayPool<byte>.Shared.Rent(byteCountThisIteration);
                    int actualBytesWrittenThisIteration = encoder.GetBytes(firstSpan, rentedArray, flush: isFinalSegment); // could throw ArgumentException if overflow would occur
                    listOfSegments.Add((rentedArray, actualBytesWrittenThisIteration));

                    totalByteCount += actualBytesWrittenThisIteration;
                    if (totalByteCount < 0)
                    {
                        // If we overflowed, call the array ctor, passing int.MaxValue.
                        // This will end up throwing the expected OutOfMemoryException
                        // since arrays are limited to under int.MaxValue elements in length.

                        totalByteCount = int.MaxValue;
                        break;
                    }

                    remainingChars = remainingChars.Slice(next);
                } while (!isFinalSegment);

                // Now build up the byte[] to return, then release all of our scratch buffers
                // back to the shared pool.

                byte[] retVal = new byte[totalByteCount];
                Span<byte> remainingBytes = retVal;

                foreach ((byte[] array, int length) in listOfSegments)
                {
                    array.AsSpan(0, length).CopyTo(remainingBytes);
                    ArrayPool<byte>.Shared.Return(array);
                    remainingBytes = remainingBytes.Slice(length);
                }

                Debug.Assert(remainingBytes.IsEmpty, "Over-allocated the byte[] instance?");

                return retVal;
            }
        }

        /// <summary>
        /// Decodes the specified <see cref="ReadOnlySpan{Byte}"/> to <see langword="char"/>s using the specified <see cref="Encoding"/>
        /// and writes the result to <paramref name="writer"/>.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> which represents how the data in <paramref name="bytes"/> should be decoded.</param>
        /// <param name="bytes">The <see cref="ReadOnlySpan{Byte}"/> whose bytes should be decoded.</param>
        /// <param name="writer">The buffer to which the decoded chars will be written.</param>
        /// <returns>The number of chars written to <paramref name="writer"/>.</returns>
        /// <exception cref="DecoderFallbackException">Thrown if <paramref name="bytes"/> contains data that cannot be decoded and <paramref name="encoding"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static long GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, IBufferWriter<char> writer)
        {
            ArgumentNullException.ThrowIfNull(encoding);
            ArgumentNullException.ThrowIfNull(writer);

            if (bytes.Length <= MaxInputElementsPerIteration)
            {
                // The input span is small enough where we can one-shot this.

                int charCount = encoding.GetCharCount(bytes);
                Span<char> scratchBuffer = writer.GetSpan(charCount);

                int actualCharsWritten = encoding.GetChars(bytes, scratchBuffer);

                writer.Advance(actualCharsWritten);
                return actualCharsWritten;
            }
            else
            {
                // Allocate a stateful Decoder instance and chunk this.

                Convert(encoding.GetDecoder(), bytes, writer, flush: true, out long totalCharsWritten, out _);
                return totalCharsWritten;
            }
        }

        /// <summary>
        /// Decodes the specified <see cref="ReadOnlySequence{Byte}"/> to <see langword="char"/>s using the specified <see cref="Encoding"/>
        /// and writes the result to <paramref name="writer"/>.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> which represents how the data in <paramref name="bytes"/> should be decoded.</param>
        /// <param name="bytes">The <see cref="ReadOnlySequence{Byte}"/> whose bytes should be decoded.</param>
        /// <param name="writer">The buffer to which the decoded chars will be written.</param>
        /// <returns>The number of chars written to <paramref name="writer"/>.</returns>
        /// <exception cref="DecoderFallbackException">Thrown if <paramref name="bytes"/> contains data that cannot be decoded and <paramref name="encoding"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static long GetChars(this Encoding encoding, in ReadOnlySequence<byte> bytes, IBufferWriter<char> writer)
        {
            ArgumentNullException.ThrowIfNull(encoding);
            ArgumentNullException.ThrowIfNull(writer);

            // Delegate to the Span-based method if possible.
            // If that doesn't work, allocate the Encoder instance and run a loop.

            if (bytes.IsSingleSegment)
            {
                return GetChars(encoding, bytes.FirstSpan, writer);
            }
            else
            {
                Convert(encoding.GetDecoder(), bytes, writer, flush: true, out long charsWritten, out _);
                return charsWritten;
            }
        }

        /// <summary>
        /// Decodes the specified <see cref="ReadOnlySequence{Byte}"/> to <see langword="char"/>s using the specified <see cref="Encoding"/>
        /// and outputs the result to <paramref name="chars"/>.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> which represents how the data in <paramref name="bytes"/> is encoded.</param>
        /// <param name="bytes">The <see cref="ReadOnlySequence{Byte}"/> to decode to characters.</param>
        /// <param name="chars">The destination buffer to which the decoded characters will be written.</param>
        /// <returns>The number of chars written to <paramref name="chars"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="chars"/> is not large enough to contain the encoded form of <paramref name="bytes"/>.</exception>
        /// <exception cref="DecoderFallbackException">Thrown if <paramref name="bytes"/> contains data that cannot be decoded and <paramref name="encoding"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static int GetChars(this Encoding encoding, in ReadOnlySequence<byte> bytes, Span<char> chars)
        {
            ArgumentNullException.ThrowIfNull(encoding);

            if (bytes.IsSingleSegment)
            {
                // If the incoming sequence is single-segment, one-shot this.

                return encoding.GetChars(bytes.FirstSpan, chars);
            }
            else
            {
                // If the incoming sequence is multi-segment, create a stateful Decoder
                // and use it as the workhorse. On the final iteration we'll pass flush=true.

                ReadOnlySequence<byte> remainingBytes = bytes;
                int originalCharsLength = chars.Length;
                Decoder decoder = encoding.GetDecoder();
                bool isFinalSegment;

                do
                {
                    remainingBytes.GetFirstSpan(out ReadOnlySpan<byte> firstSpan, out SequencePosition next);
                    isFinalSegment = remainingBytes.IsSingleSegment;

                    int charsWrittenJustNow = decoder.GetChars(firstSpan, chars, flush: isFinalSegment);
                    chars = chars.Slice(charsWrittenJustNow);
                    remainingBytes = remainingBytes.Slice(next);
                } while (!isFinalSegment);

                return originalCharsLength - chars.Length; // total number of chars we wrote
            }
        }

        /// <summary>
        /// Decodes the specified <see cref="ReadOnlySequence{Byte}"/> into a <see cref="string"/> using the specified <see cref="Encoding"/>.
        /// </summary>
        /// <param name="encoding">The <see cref="Encoding"/> which represents how the data in <paramref name="bytes"/> is encoded.</param>
        /// <param name="bytes">The <see cref="ReadOnlySequence{Byte}"/> to decode into characters.</param>
        /// <returns>A <see cref="string"/> which represents the decoded contents of <paramref name="bytes"/>.</returns>
        /// <exception cref="DecoderFallbackException">Thrown if <paramref name="bytes"/> contains data that cannot be decoded and <paramref name="encoding"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static string GetString(this Encoding encoding, in ReadOnlySequence<byte> bytes)
        {
            ArgumentNullException.ThrowIfNull(encoding);

            if (bytes.IsSingleSegment)
            {
                // If the incoming sequence is single-segment, one-shot this.

                return encoding.GetString(bytes.FirstSpan);
            }
            else
            {
                // If the incoming sequence is multi-segment, create a stateful Decoder
                // and use it as the workhorse. On the final iteration we'll pass flush=true.

                Decoder decoder = encoding.GetDecoder();

                // Maintain a list of all the segments we'll need to concat together.
                // These will be released back to the pool at the end of the method.

                List<(char[], int)> listOfSegments = new List<(char[], int)>();
                int totalCharCount = 0;

                ReadOnlySequence<byte> remainingBytes = bytes;
                bool isFinalSegment;

                do
                {
                    remainingBytes.GetFirstSpan(out ReadOnlySpan<byte> firstSpan, out SequencePosition next);
                    isFinalSegment = remainingBytes.IsSingleSegment;

                    int charCountThisIteration = decoder.GetCharCount(firstSpan, flush: isFinalSegment); // could throw ArgumentException if overflow would occur
                    char[] rentedArray = ArrayPool<char>.Shared.Rent(charCountThisIteration);
                    int actualCharsWrittenThisIteration = decoder.GetChars(firstSpan, rentedArray, flush: isFinalSegment);
                    listOfSegments.Add((rentedArray, actualCharsWrittenThisIteration));

                    totalCharCount += actualCharsWrittenThisIteration;
                    if (totalCharCount < 0)
                    {
                        // If we overflowed, call string.Create, passing int.MaxValue.
                        // This will end up throwing the expected OutOfMemoryException
                        // since strings are limited to under int.MaxValue elements in length.

                        totalCharCount = int.MaxValue;
                        break;
                    }

                    remainingBytes = remainingBytes.Slice(next);
                } while (!isFinalSegment);

                // Now build up the string to return, then release all of our scratch buffers
                // back to the shared pool.

                return string.Create(totalCharCount, listOfSegments, (span, listOfSegments) =>
                {
                    foreach ((char[] array, int length) in listOfSegments)
                    {
                        array.AsSpan(0, length).CopyTo(span);
                        ArrayPool<char>.Shared.Return(array);
                        span = span.Slice(length);
                    }

                    Debug.Assert(span.IsEmpty, "Over-allocated the string instance?");
                });
            }
        }

        /// <summary>
        /// Converts a <see cref="ReadOnlySpan{Char}"/> to bytes using <paramref name="encoder"/> and writes the result to <paramref name="writer"/>.
        /// </summary>
        /// <param name="encoder">The <see cref="Encoder"/> instance which can convert <see langword="char"/>s to <see langword="byte"/>s.</param>
        /// <param name="chars">A sequence of characters to encode.</param>
        /// <param name="writer">The buffer to which the encoded bytes will be written.</param>
        /// <param name="flush"><see langword="true"/> to indicate no further data is to be converted; otherwise <see langword="false"/>.</param>
        /// <param name="bytesUsed">When this method returns, contains the count of <see langword="byte"/>s which were written to <paramref name="writer"/>.</param>
        /// <param name="completed">
        /// When this method returns, contains <see langword="true"/> if <paramref name="encoder"/> contains no partial internal state; otherwise, <see langword="false"/>.
        /// If <paramref name="flush"/> is <see langword="true"/>, this will always be set to <see langword="true"/> when the method returns.
        /// </param>
        /// <exception cref="EncoderFallbackException">Thrown if <paramref name="chars"/> contains data that cannot be encoded and <paramref name="encoder"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static void Convert(this Encoder encoder, ReadOnlySpan<char> chars, IBufferWriter<byte> writer, bool flush, out long bytesUsed, out bool completed)
        {
            ArgumentNullException.ThrowIfNull(encoder);
            ArgumentNullException.ThrowIfNull(writer);

            // We need to perform at least one iteration of the loop since the encoder could have internal state.

            long totalBytesWritten = 0;

            do
            {
                // If our remaining input is very large, instead truncate it and tell the encoder
                // that there'll be more data after this call. This truncation is only for the
                // purposes of getting the required byte count. Since the writer may give us a span
                // larger than what we asked for, we'll pass the entirety of the remaining data
                // to the transcoding routine, since it may be able to make progress beyond what
                // was initially computed for the truncated input data.

                int byteCountForThisSlice = (chars.Length <= MaxInputElementsPerIteration)
                  ? encoder.GetByteCount(chars, flush)
                  : encoder.GetByteCount(chars.Slice(0, MaxInputElementsPerIteration), flush: false /* this isn't the end of the data */);

                Span<byte> scratchBuffer = writer.GetSpan(byteCountForThisSlice);

                encoder.Convert(chars, scratchBuffer, flush, out int charsUsedJustNow, out int bytesWrittenJustNow, out completed);

                chars = chars.Slice(charsUsedJustNow);
                writer.Advance(bytesWrittenJustNow);
                totalBytesWritten += bytesWrittenJustNow;
            } while (!chars.IsEmpty);

            bytesUsed = totalBytesWritten;
        }

        /// <summary>
        /// Converts a <see cref="ReadOnlySequence{Char}"/> to encoded bytes and writes the result to <paramref name="writer"/>.
        /// </summary>
        /// <param name="encoder">The <see cref="Encoder"/> instance which can convert <see langword="char"/>s to <see langword="byte"/>s.</param>
        /// <param name="chars">A sequence of characters to encode.</param>
        /// <param name="writer">The buffer to which the encoded bytes will be written.</param>
        /// <param name="flush"><see langword="true"/> to indicate no further data is to be converted; otherwise <see langword="false"/>.</param>
        /// <param name="bytesUsed">When this method returns, contains the count of <see langword="byte"/>s which were written to <paramref name="writer"/>.</param>
        /// <param name="completed">When this method returns, contains <see langword="true"/> if all input up until <paramref name="bytesUsed"/> was
        /// converted; otherwise, <see langword="false"/>. If <paramref name="flush"/> is <see langword="true"/>, this will always be set to
        /// <see langword="true"/> when the method returns.</param>
        /// <exception cref="EncoderFallbackException">Thrown if <paramref name="chars"/> contains data that cannot be encoded and <paramref name="encoder"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static void Convert(this Encoder encoder, in ReadOnlySequence<char> chars, IBufferWriter<byte> writer, bool flush, out long bytesUsed, out bool completed)
        {
            // Parameter null checks will be performed by the workhorse routine.

            ReadOnlySequence<char> remainingChars = chars;
            long totalBytesWritten = 0;
            bool isFinalSegment;

            do
            {
                // Process each segment individually. We need to run at least one iteration of the loop in case
                // the Encoder has internal state.

                remainingChars.GetFirstSpan(out ReadOnlySpan<char> firstSpan, out SequencePosition next);
                isFinalSegment = remainingChars.IsSingleSegment;

                Convert(encoder, firstSpan, writer, flush && isFinalSegment, out long bytesWrittenThisIteration, out completed);

                totalBytesWritten += bytesWrittenThisIteration;
                remainingChars = remainingChars.Slice(next);
            } while (!isFinalSegment);

            bytesUsed = totalBytesWritten;
        }

        /// <summary>
        /// Converts a <see cref="ReadOnlySpan{Byte}"/> to chars using <paramref name="decoder"/> and writes the result to <paramref name="writer"/>.
        /// </summary>
        /// <param name="decoder">The <see cref="Decoder"/> instance which can convert <see langword="byte"/>s to <see langword="char"/>s.</param>
        /// <param name="bytes">A sequence of bytes to decode.</param>
        /// <param name="writer">The buffer to which the decoded chars will be written.</param>
        /// <param name="flush"><see langword="true"/> to indicate no further data is to be converted; otherwise <see langword="false"/>.</param>
        /// <param name="charsUsed">When this method returns, contains the count of <see langword="char"/>s which were written to <paramref name="writer"/>.</param>
        /// <param name="completed">
        /// When this method returns, contains <see langword="true"/> if <paramref name="decoder"/> contains no partial internal state; otherwise, <see langword="false"/>.
        /// If <paramref name="flush"/> is <see langword="true"/>, this will always be set to <see langword="true"/> when the method returns.
        /// </param>
        /// <exception cref="DecoderFallbackException">Thrown if <paramref name="bytes"/> contains data that cannot be encoded and <paramref name="decoder"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static void Convert(this Decoder decoder, ReadOnlySpan<byte> bytes, IBufferWriter<char> writer, bool flush, out long charsUsed, out bool completed)
        {
            ArgumentNullException.ThrowIfNull(decoder);
            ArgumentNullException.ThrowIfNull(writer);

            // We need to perform at least one iteration of the loop since the decoder could have internal state.

            long totalCharsWritten = 0;

            do
            {
                // If our remaining input is very large, instead truncate it and tell the decoder
                // that there'll be more data after this call. This truncation is only for the
                // purposes of getting the required char count. Since the writer may give us a span
                // larger than what we asked for, we'll pass the entirety of the remaining data
                // to the transcoding routine, since it may be able to make progress beyond what
                // was initially computed for the truncated input data.

                int charCountForThisSlice = (bytes.Length <= MaxInputElementsPerIteration)
                    ? decoder.GetCharCount(bytes, flush)
                    : decoder.GetCharCount(bytes.Slice(0, MaxInputElementsPerIteration), flush: false /* this isn't the end of the data */);

                Span<char> scratchBuffer = writer.GetSpan(charCountForThisSlice);

                decoder.Convert(bytes, scratchBuffer, flush, out int bytesUsedJustNow, out int charsWrittenJustNow, out completed);

                bytes = bytes.Slice(bytesUsedJustNow);
                writer.Advance(charsWrittenJustNow);
                totalCharsWritten += charsWrittenJustNow;
            } while (!bytes.IsEmpty);

            charsUsed = totalCharsWritten;
        }

        /// <summary>
        /// Converts a <see cref="ReadOnlySequence{Byte}"/> to UTF-16 encoded characters and writes the result to <paramref name="writer"/>.
        /// </summary>
        /// <param name="decoder">The <see cref="Decoder"/> instance which can convert <see langword="byte"/>s to <see langword="char"/>s.</param>
        /// <param name="bytes">A sequence of bytes to decode.</param>
        /// <param name="writer">The buffer to which the decoded characters will be written.</param>
        /// <param name="flush"><see langword="true"/> to indicate no further data is to be converted; otherwise <see langword="false"/>.</param>
        /// <param name="charsUsed">When this method returns, contains the count of <see langword="char"/>s which were written to <paramref name="writer"/>.</param>
        /// <param name="completed">
        /// When this method returns, contains <see langword="true"/> if <paramref name="decoder"/> contains no partial internal state; otherwise, <see langword="false"/>.
        /// If <paramref name="flush"/> is <see langword="true"/>, this will always be set to <see langword="true"/> when the method returns.
        /// </param>
        /// <exception cref="DecoderFallbackException">Thrown if <paramref name="bytes"/> contains data that cannot be decoded and <paramref name="decoder"/> is configured
        /// to throw an exception when such data is seen.</exception>
        public static void Convert(this Decoder decoder, in ReadOnlySequence<byte> bytes, IBufferWriter<char> writer, bool flush, out long charsUsed, out bool completed)
        {
            // Parameter null checks will be performed by the workhorse routine.

            ReadOnlySequence<byte> remainingBytes = bytes;
            long totalCharsWritten = 0;
            bool isFinalSegment;

            do
            {
                // Process each segment individually. We need to run at least one iteration of the loop in case
                // the Decoder has internal state.

                remainingBytes.GetFirstSpan(out ReadOnlySpan<byte> firstSpan, out SequencePosition next);
                isFinalSegment = remainingBytes.IsSingleSegment;

                Convert(decoder, firstSpan, writer, flush && isFinalSegment, out long charsWrittenThisIteration, out completed);

                totalCharsWritten += charsWrittenThisIteration;
                remainingBytes = remainingBytes.Slice(next);
            } while (!isFinalSegment);

            charsUsed = totalCharsWritten;
        }
    }
}
