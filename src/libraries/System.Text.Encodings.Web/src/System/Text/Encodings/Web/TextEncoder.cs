// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// An abstraction representing various text encoders.
    /// </summary>
    /// <remarks>
    /// TextEncoder subclasses can be used to do HTML encoding, URI encoding, and JavaScript encoding.
    /// Instances of such subclasses can be accessed using <see cref="HtmlEncoder.Default"/>, <see cref="UrlEncoder.Default"/>, and <see cref="JavaScriptEncoder.Default"/>.
    /// </remarks>
    public abstract class TextEncoder
    {
        private const int EncodeStartingOutputBufferSize = 1024; // bytes or chars, depending

        /// <summary>
        /// Encodes a Unicode scalar into a buffer.
        /// </summary>
        /// <param name="unicodeScalar">Unicode scalar.</param>
        /// <param name="buffer">The destination of the encoded text.</param>
        /// <param name="bufferLength">Length of the destination <paramref name="buffer"/> in chars.</param>
        /// <param name="numberOfCharactersWritten">Number of characters written to the <paramref name="buffer"/>.</param>
        /// <returns>Returns false if <paramref name="bufferLength"/> is too small to fit the encoded text, otherwise returns true.</returns>
        /// <remarks>This method is seldom called directly. One of the TextEncoder.Encode overloads should be used instead.
        /// Implementations of <see cref="TextEncoder"/> need to be thread safe and stateless.
        /// </remarks>
        [CLSCompliant(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool TryEncodeUnicodeScalar(uint unicodeScalar, Span<char> buffer, out int charsWritten)
        {
            fixed (char* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                return TryEncodeUnicodeScalar((int)unicodeScalar, pBuffer, buffer.Length, out charsWritten);
            }
        }

        private bool TryEncodeUnicodeScalarUtf8(uint unicodeScalar, Span<char> utf16ScratchBuffer, Span<byte> utf8Destination, out int bytesWritten)
        {
            if (!TryEncodeUnicodeScalar(unicodeScalar, utf16ScratchBuffer, out int charsWritten))
            {
                // We really don't expect any encoder to exceed 24 escaped chars per input scalar.
                // If this happens, throw an exception and we can figure out if we want to support it
                // in the future.
                ThrowArgumentException_MaxOutputCharsPerInputChar();
            }

            // Transcode chars -> bytes one at a time.

            utf16ScratchBuffer = utf16ScratchBuffer.Slice(0, charsWritten);
            int dstIdx = 0;

            while (!utf16ScratchBuffer.IsEmpty)
            {
                if (Rune.DecodeFromUtf16(utf16ScratchBuffer, out Rune nextScalarValue, out int scalarUtf16CodeUnitCount) != OperationStatus.Done)
                {
                    // Wrote bad UTF-16 data, we cannot transcode to UTF-8.
                    ThrowArgumentException_MaxOutputCharsPerInputChar();
                }

                uint utf8lsb = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)nextScalarValue.Value);
                do
                {
                    if (SpanUtility.IsValidIndex(utf8Destination, dstIdx))
                    {
                        utf8Destination[dstIdx++] = (byte)utf8lsb;
                    }
                    else
                    {
                        bytesWritten = 0; // ran out of space in the destination
                        return false;
                    }
                } while ((utf8lsb >>= 8) != 0);

                utf16ScratchBuffer = utf16ScratchBuffer.Slice(scalarUtf16CodeUnitCount);
            }

            bytesWritten = dstIdx;
            return true;
        }

        // all subclasses have the same implementation of this method.
        // but this cannot be made virtual, because it will cause a virtual call to Encodes, and it destroys perf, i.e. makes common scenario 2x slower

        /// <summary>
        /// Finds index of the first character that needs to be encoded.
        /// </summary>
        /// <param name="text">The text buffer to search.</param>
        /// <param name="textLength">The number of characters in the <paramref name="text"/>.</param>
        /// <returns></returns>
        /// <remarks>This method is seldom called directly. It's used by higher level helper APIs.</remarks>
        [CLSCompliant(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract unsafe int FindFirstCharacterToEncode(char* text, int textLength);

        /// <summary>
        /// Determines if a given Unicode scalar will be encoded.
        /// </summary>
        /// <param name="unicodeScalar">Unicode scalar.</param>
        /// <returns>Returns true if the <paramref name="unicodeScalar"/> will be encoded by this encoder, otherwise returns false.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract bool WillEncode(int unicodeScalar);

        // this could be a field, but I am trying to make the abstraction pure.

        /// <summary>
        /// Maximum number of characters that this encoder can generate for each input character.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract int MaxOutputCharactersPerInputCharacter { get; }

        /// <summary>
        /// Encodes the supplied string and returns the encoded text as a new string.
        /// </summary>
        /// <param name="value">String to encode.</param>
        /// <returns>Encoded string.</returns>
        public virtual string Encode(string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            int indexOfFirstCharToEncode = FindFirstCharacterToEncode(value.AsSpan());
            if (indexOfFirstCharToEncode < 0)
            {
                return value; // shortcut: there's no work to perform
            }

            // We optimize for the data having no "requires encoding" chars, so keep the
            // real encoding logic out of the fast path.

            return EncodeToNewString(value.AsSpan(), indexOfFirstCharToEncode);
        }

        private string EncodeToNewString(ReadOnlySpan<char> value, int indexOfFirstCharToEncode)
        {
            ReadOnlySpan<char> remainingInput = value.Slice(indexOfFirstCharToEncode);
            ValueStringBuilder stringBuilder = new ValueStringBuilder(stackalloc char[EncodeStartingOutputBufferSize]);

#if !NETCOREAPP
            // Can't call string.Concat later in the method, so memcpy now.
            stringBuilder.Append(value.Slice(0, indexOfFirstCharToEncode));
#endif

            // On each iteration of the main loop, we'll make sure we have at least this many chars left in the
            // destination buffer. This should prevent us from making very chatty calls where we only make progress
            // one char at a time.
            int minBufferBumpEachIteration = Math.Max(MaxOutputCharactersPerInputCharacter, EncodeStartingOutputBufferSize);

            do
            {
                // AppendSpan mutates the VSB length to include the newly-added span. This potentially overallocates.
                Span<char> destBuffer = stringBuilder.AppendSpan(Math.Max(remainingInput.Length, minBufferBumpEachIteration));
                EncodeCore(remainingInput, destBuffer, out int charsConsumedJustNow, out int charsWrittenJustNow, isFinalBlock: true);
                if (charsWrittenJustNow == 0 || (uint)charsWrittenJustNow > (uint)destBuffer.Length)
                {
                    ThrowArgumentException_MaxOutputCharsPerInputChar(); // couldn't make forward progress or returned bogus data
                }
                remainingInput = remainingInput.Slice(charsConsumedJustNow);
                // It's likely we didn't populate the entire span. If this is the case, adjust the VSB length
                // to reflect that there's unused buffer at the end of the VSB instance.
                stringBuilder.Length -= destBuffer.Length - charsWrittenJustNow;
            } while (!remainingInput.IsEmpty);

#if NETCOREAPP
            string retVal = string.Concat(value.Slice(0, indexOfFirstCharToEncode), stringBuilder.AsSpan());
            stringBuilder.Dispose();
            return retVal;
#else
            return stringBuilder.ToString();
#endif
        }

        /// <summary>
        /// Encodes the supplied string into a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output">Encoded text is written to this output.</param>
        /// <param name="value">String to be encoded.</param>
        public void Encode(TextWriter output, string value)
        {
            Encode(output, value, 0, value.Length);
        }

        /// <summary>
        ///  Encodes a substring into a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output">Encoded text is written to this output.</param>
        /// <param name="value">String whose substring is to be encoded.</param>
        /// <param name="startIndex">The index where the substring starts.</param>
        /// <param name="characterCount">Number of characters in the substring.</param>
        public virtual void Encode(TextWriter output, string value, int startIndex, int characterCount)
        {
            if (output is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.output);
            }
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            ValidateRanges(startIndex, characterCount, actualInputLength: value.Length);

            int indexOfFirstCharToEncode = FindFirstCharacterToEncode(value.AsSpan(startIndex, characterCount));
            if (indexOfFirstCharToEncode < 0)
            {
                indexOfFirstCharToEncode = characterCount;
            }

            // memcpy all characters that don't require encoding, then encode any remaining chars

            output.WritePartialString(value, startIndex, indexOfFirstCharToEncode);
            if (indexOfFirstCharToEncode != characterCount)
            {
                EncodeCore(output, value.AsSpan(startIndex + indexOfFirstCharToEncode, characterCount - indexOfFirstCharToEncode));
            }
        }

        /// <summary>
        ///  Encodes characters from an array into a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output">Encoded text is written to the output.</param>
        /// <param name="value">Array of characters to be encoded.</param>
        /// <param name="startIndex">The index where the substring starts.</param>
        /// <param name="characterCount">Number of characters in the substring.</param>
        public virtual void Encode(TextWriter output, char[] value, int startIndex, int characterCount)
        {
            if (output is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.output);
            }
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            ValidateRanges(startIndex, characterCount, actualInputLength: value.Length);

            int indexOfFirstCharToEncode = FindFirstCharacterToEncode(value.AsSpan(startIndex, characterCount));
            if (indexOfFirstCharToEncode < 0)
            {
                indexOfFirstCharToEncode = characterCount;
            }
            output.Write(value, startIndex, indexOfFirstCharToEncode);

            if (indexOfFirstCharToEncode != characterCount)
            {
                EncodeCore(output, value.AsSpan(startIndex + indexOfFirstCharToEncode, characterCount - indexOfFirstCharToEncode));
            }
        }

        /// <summary>
        /// Encodes the supplied UTF-8 text.
        /// </summary>
        /// <param name="utf8Source">A source buffer containing the UTF-8 text to encode.</param>
        /// <param name="utf8Destination">The destination buffer to which the encoded form of <paramref name="utf8Source"/>
        /// will be written.</param>
        /// <param name="bytesConsumed">The number of bytes consumed from the <paramref name="utf8Source"/> buffer.</param>
        /// <param name="bytesWritten">The number of bytes written to the <paramref name="utf8Destination"/> buffer.</param>
        /// <param name="isFinalBlock"><see langword="true"/> if there is further source data that needs to be encoded;
        /// <see langword="false"/> if there is no further source data that needs to be encoded.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the encoding operation.</returns>
        /// <remarks>The buffers <paramref name="utf8Source"/> and <paramref name="utf8Destination"/> must not overlap.</remarks>
        public virtual OperationStatus EncodeUtf8(
            ReadOnlySpan<byte> utf8Source,
            Span<byte> utf8Destination,
            out int bytesConsumed,
            out int bytesWritten,
            bool isFinalBlock = true)
        {
            // The Encode method is intended to be called in a loop, potentially where the source buffer
            // is much larger than the destination buffer. We don't want to walk the entire source buffer
            // on each invocation of this method, so we'll slice the source buffer to be no larger than
            // the destination buffer to avoid performing unnecessary work. The potential exists for us to
            // split the source in the middle of a UTF-8 multi-byte sequence. If this happens,
            // FindFirstCharacterToEncodeUtf8 will report the split bytes as "needs encoding", we'll fall
            // back down the slow path, and the slow path will handle the scenario appropriately.

            ReadOnlySpan<byte> sourceSearchSpace = utf8Source;
            if (utf8Destination.Length < utf8Source.Length)
            {
                sourceSearchSpace = utf8Source.Slice(0, utf8Destination.Length);
            }

            int idxOfFirstByteToEncode = FindFirstCharacterToEncodeUtf8(sourceSearchSpace);
            if (idxOfFirstByteToEncode < 0)
            {
                idxOfFirstByteToEncode = sourceSearchSpace.Length;
            }

            utf8Source.Slice(0, idxOfFirstByteToEncode).CopyTo(utf8Destination); // memcpy data that doesn't need to be encoded
            if (idxOfFirstByteToEncode == utf8Source.Length)
            {
                bytesConsumed = utf8Source.Length;
                bytesWritten = utf8Source.Length;
                return OperationStatus.Done; // memcopied all bytes, nothing more to do
            }

            // If we got to this point, we couldn't memcpy the entire source buffer into the destination.
            // Either the destination was too short or we found data that needs to be encoded.

            OperationStatus status = EncodeUtf8Core(utf8Source.Slice(idxOfFirstByteToEncode), utf8Destination.Slice(idxOfFirstByteToEncode), out int innerBytesConsumed, out int innerBytesWritten, isFinalBlock);
            bytesConsumed = idxOfFirstByteToEncode + innerBytesConsumed;
            bytesWritten = idxOfFirstByteToEncode + innerBytesWritten;
            return status;
        }

        // skips the call to FindFirstCharacterToEncodeUtf8
        private protected virtual OperationStatus EncodeUtf8Core(
            ReadOnlySpan<byte> utf8Source,
            Span<byte> utf8Destination,
            out int bytesConsumed,
            out int bytesWritten,
            bool isFinalBlock)
        {
            int originalUtf8SourceLength = utf8Source.Length;
            int originalUtf8DestinationLength = utf8Destination.Length;

            const int TempUtf16CharBufferLength = 24; // arbitrarily chosen, but sufficient for any reasonable implementation
            Span<char> utf16ScratchBuffer = stackalloc char[TempUtf16CharBufferLength];

            while (!utf8Source.IsEmpty)
            {
                OperationStatus opStatus = Rune.DecodeFromUtf8(utf8Source, out Rune scalarValue, out int bytesConsumedJustNow);
                if (opStatus != OperationStatus.Done)
                {
                    if (!isFinalBlock && opStatus == OperationStatus.NeedMoreData)
                    {
                        goto NeedMoreData;
                    }

                    Debug.Assert(scalarValue == Rune.ReplacementChar); // DecodeFromUtf8 should've performed substitution
                    goto MustEncode;
                }

                if (!WillEncode(scalarValue.Value))
                {
                    uint utf8lsb = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)scalarValue.Value);
                    int dstIdxTemp = 0;
                    do
                    {
                        if ((uint)dstIdxTemp >= (uint)utf8Destination.Length)
                        {
                            goto DestinationTooSmall;
                        }
                        utf8Destination[dstIdxTemp++] = (byte)utf8lsb;
                    } while ((utf8lsb >>= 8) != 0);
                    utf8Source = utf8Source.Slice(bytesConsumedJustNow);
                    utf8Destination = utf8Destination.Slice(dstIdxTemp);
                    continue;
                }

            MustEncode:

                if (!TryEncodeUnicodeScalarUtf8((uint)scalarValue.Value, utf16ScratchBuffer, utf8Destination, out int bytesWrittenJustNow))
                {
                    goto DestinationTooSmall;
                }

                utf8Source = utf8Source.Slice(bytesConsumedJustNow);
                utf8Destination = utf8Destination.Slice(bytesWrittenJustNow);
            }

            // And we're finished!

            OperationStatus retVal = OperationStatus.Done;

        ReturnCommon:
            bytesConsumed = originalUtf8SourceLength - utf8Source.Length;
            bytesWritten = originalUtf8DestinationLength - utf8Destination.Length;
            return retVal;

        NeedMoreData:
            retVal = OperationStatus.NeedMoreData;
            goto ReturnCommon;

        DestinationTooSmall:
            retVal = OperationStatus.DestinationTooSmall;
            goto ReturnCommon;
        }

        /// <summary>
        /// Encodes the supplied characters.
        /// </summary>
        /// <param name="source">A source buffer containing the characters to encode.</param>
        /// <param name="destination">The destination buffer to which the encoded form of <paramref name="source"/>
        /// will be written.</param>
        /// <param name="charsConsumed">The number of characters consumed from the <paramref name="source"/> buffer.</param>
        /// <param name="charsWritten">The number of characters written to the <paramref name="destination"/> buffer.</param>
        /// <param name="isFinalBlock"><see langword="true"/> if there is further source data that needs to be encoded;
        /// <see langword="false"/> if there is no further source data that needs to be encoded.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the encoding operation.</returns>
        /// <remarks>The buffers <paramref name="source"/> and <paramref name="destination"/> must not overlap.</remarks>
        public virtual OperationStatus Encode(
            ReadOnlySpan<char> source,
            Span<char> destination,
            out int charsConsumed,
            out int charsWritten,
            bool isFinalBlock = true)
        {
            // The Encode method is intended to be called in a loop, potentially where the source buffer
            // is much larger than the destination buffer. We don't want to walk the entire source buffer
            // on each invocation of this method, so we'll slice the source buffer to be no larger than
            // the destination buffer to avoid performing unnecessary work. The potential exists for us to
            // split the source in the middle of a UTF-16 surrogate pair. If this happens,
            // FindFirstCharacterToEncode will report the split surrogate as "needs encoding", we'll fall
            // back down the slow path, and the slow path will handle the surrogate appropriately.

            ReadOnlySpan<char> sourceSearchSpace = source;
            if (destination.Length < source.Length)
            {
                sourceSearchSpace = source.Slice(0, destination.Length);
            }

            int idxOfFirstCharToEncode = FindFirstCharacterToEncode(sourceSearchSpace);
            if (idxOfFirstCharToEncode < 0)
            {
                idxOfFirstCharToEncode = sourceSearchSpace.Length;
            }

            source.Slice(0, idxOfFirstCharToEncode).CopyTo(destination); // memcpy data that doesn't need to be encoded
            if (idxOfFirstCharToEncode == source.Length)
            {
                charsConsumed = source.Length;
                charsWritten = source.Length;
                return OperationStatus.Done; // memcopied all chars, nothing more to do
            }

            // If we got to this point, we couldn't memcpy the entire source buffer into the destination.
            // Either the destination was too short or we found data that needs to be encoded.

            OperationStatus status = EncodeCore(source.Slice(idxOfFirstCharToEncode), destination.Slice(idxOfFirstCharToEncode), out int innerCharsConsumed, out int innerCharsWritten, isFinalBlock);
            charsConsumed = idxOfFirstCharToEncode + innerCharsConsumed;
            charsWritten = idxOfFirstCharToEncode + innerCharsWritten;
            return status;
        }

        // skips the call to FindFirstCharacterToEncode
        private protected virtual OperationStatus EncodeCore(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten, bool isFinalBlock)
        {
            int originalSourceLength = source.Length;
            int originalDestinationLength = destination.Length;

            while (!source.IsEmpty)
            {
                OperationStatus status = Rune.DecodeFromUtf16(source, out Rune scalarValue, out int charsConsumedJustNow);
                if (status != OperationStatus.Done)
                {
                    if (!isFinalBlock && status == OperationStatus.NeedMoreData)
                    {
                        goto NeedMoreData;
                    }

                    Debug.Assert(scalarValue == Rune.ReplacementChar); // should be replacement char
                    goto MustEncode;
                }

                if (!WillEncode(scalarValue.Value))
                {
                    if (!scalarValue.TryEncodeToUtf16(destination, out _))
                    {
                        goto DestinationTooSmall;
                    }
                    source = source.Slice(charsConsumedJustNow);
                    destination = destination.Slice(charsConsumedJustNow); // reflecting input directly to the output, same # of chars written
                    continue;
                }

            MustEncode:

                if (!TryEncodeUnicodeScalar((uint)scalarValue.Value, destination, out int charsWrittenJustNow))
                {
                    goto DestinationTooSmall;
                }

                source = source.Slice(charsConsumedJustNow);
                destination = destination.Slice(charsWrittenJustNow);
            }

            // And we're finished!

            OperationStatus retVal = OperationStatus.Done;

        ReturnCommon:
            charsConsumed = originalSourceLength - source.Length;
            charsWritten = originalDestinationLength - destination.Length;
            return retVal;

        NeedMoreData:
            retVal = OperationStatus.NeedMoreData;
            goto ReturnCommon;

        DestinationTooSmall:
            retVal = OperationStatus.DestinationTooSmall;
            goto ReturnCommon;
        }

        // skips call to FindFirstCharacterToEncode
        private void EncodeCore(TextWriter output, ReadOnlySpan<char> value)
        {
            Debug.Assert(output != null);
            Debug.Assert(!value.IsEmpty, "Caller should've special-cased 'no encoding needed'.");

            // On each iteration of the main loop, we'll make sure we have at least this many chars left in the
            // destination buffer. This should prevent us from making very chatty calls where we only make progress
            // one char at a time.
            int minBufferBumpEachIteration = Math.Max(MaxOutputCharactersPerInputCharacter, EncodeStartingOutputBufferSize);
            char[] rentedArray = ArrayPool<char>.Shared.Rent(Math.Max(value.Length, minBufferBumpEachIteration));
            Span<char> scratchBuffer = rentedArray;

            do
            {
                EncodeCore(value, scratchBuffer, out int charsConsumedJustNow, out int charsWrittenJustNow, isFinalBlock: true);
                if (charsWrittenJustNow == 0 || (uint)charsWrittenJustNow > (uint)scratchBuffer.Length)
                {
                    ThrowArgumentException_MaxOutputCharsPerInputChar(); // couldn't make forward progress or returned bogus data
                }

                output.Write(rentedArray, 0, charsWrittenJustNow); // write char[], not Span<char>, for best compat & performance
                value = value.Slice(charsConsumedJustNow);
            } while (!value.IsEmpty);

            ArrayPool<char>.Shared.Return(rentedArray);
        }

        private protected virtual unsafe int FindFirstCharacterToEncode(ReadOnlySpan<char> text)
        {
            // Default implementation calls the unsafe overload

            fixed (char* pText = &MemoryMarshal.GetReference(text))
            {
                return FindFirstCharacterToEncode(pText, text.Length);
            }
        }

        /// <summary>
        /// Given a UTF-8 text input buffer, finds the first element in the input buffer which would be
        /// escaped by the current encoder instance.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text input buffer to search.</param>
        /// <returns>
        /// The index of the first element in <paramref name="utf8Text"/> which would be escaped by the
        /// current encoder instance, or -1 if no data in <paramref name="utf8Text"/> requires escaping.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual int FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte> utf8Text)
        {
            int utf8TextOriginalLength = utf8Text.Length;

            while (!utf8Text.IsEmpty)
            {
                OperationStatus opStatus = Rune.DecodeFromUtf8(utf8Text, out Rune scalarValue, out int bytesConsumed);
                if (opStatus != OperationStatus.Done || WillEncode(scalarValue.Value))
                {
                    break;
                }
                utf8Text = utf8Text.Slice(bytesConsumed);
            }

            return (utf8Text.IsEmpty) ? -1 : utf8TextOriginalLength - utf8Text.Length;
        }

        internal static bool TryCopyCharacters(string source, Span<char> destination, out int numberOfCharactersWritten)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));

            if (destination.Length < source.Length)
            {
                numberOfCharactersWritten = 0;
                return false;
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = source[i];
            }

            numberOfCharactersWritten = source.Length;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryWriteScalarAsChar(int unicodeScalar, Span<char> destination, out int numberOfCharactersWritten)
        {
            Debug.Assert(unicodeScalar < ushort.MaxValue);
            if (destination.IsEmpty)
            {
                numberOfCharactersWritten = 0;
                return false;
            }
            destination[0] = (char)unicodeScalar;
            numberOfCharactersWritten = 1;
            return true;
        }

        private static void ValidateRanges(int startIndex, int characterCount, int actualInputLength)
        {
            if (startIndex < 0 || startIndex > actualInputLength)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
            if (characterCount < 0 || characterCount > (actualInputLength - startIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(characterCount));
            }
        }

        [DoesNotReturn]
        private static void ThrowArgumentException_MaxOutputCharsPerInputChar()
        {
            throw new ArgumentException(SR.TextEncoderDoesNotImplementMaxOutputCharsPerInputChar);
        }
    }
}
