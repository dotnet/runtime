// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text
{
    // A Decoder is used to decode a sequence of blocks of bytes into a
    // sequence of blocks of characters. Following instantiation of a decoder,
    // sequential blocks of bytes are converted into blocks of characters through
    // calls to the GetChars method. The decoder maintains state between the
    // conversions, allowing it to correctly decode byte sequences that span
    // adjacent blocks.
    //
    // Instances of specific implementations of the Decoder abstract base
    // class are typically obtained through calls to the GetDecoder method
    // of Encoding objects.

    internal class DecoderNLS : Decoder
    {
        // Remember our encoding
        private readonly Encoding _encoding;
        private bool _mustFlush;
        internal bool _throwOnOverflow;
        internal int _bytesUsed;
        private int _leftoverBytes; // leftover data from a previous invocation of GetChars (up to 4 bytes)
        private int _leftoverByteCount; // number of bytes of actual data in _leftoverBytes

        internal DecoderNLS(Encoding encoding)
        {
            _encoding = encoding;
            _fallback = this._encoding.DecoderFallback;
            this.Reset();
        }

        public override void Reset()
        {
            ClearLeftoverData();
            _fallbackBuffer?.Reset();
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetCharCount(bytes, index, count, false);
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count, bool flush)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                    SR.ArgumentOutOfRange_IndexCountBuffer);

            // Just call pointer version
            fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                return GetCharCount(pBytes + index, count, flush);
        }

        public override unsafe int GetCharCount(byte* bytes, int count, bool flush)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            // Remember the flush
            _mustFlush = flush;
            _throwOnOverflow = true;

            // By default just call the encoding version, no flush by default
            Debug.Assert(_encoding is not null);
            return _encoding.GetCharCount(bytes, count, this);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                             char[] chars, int charIndex)
        {
            return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                            char[] chars, int charIndex, bool flush)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentNullException.ThrowIfNull(chars);

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                    SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex),
                    SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

            int charCount = chars.Length - charIndex;

            // Just call pointer version
            fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
            fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
                // Remember that charCount is # to decode, not size of array
                return GetChars(pBytes + byteIndex, byteCount,
                                pChars + charIndex, charCount, flush);
        }

        public override unsafe int GetChars(byte* bytes, int byteCount,
                                            char* chars, int charCount, bool flush)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentNullException.ThrowIfNull(chars);

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(byteCount < 0 ? nameof(byteCount) : nameof(charCount),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            // Remember our flush
            _mustFlush = flush;
            _throwOnOverflow = true;

            // By default just call the encodings version
            Debug.Assert(_encoding is not null);
            return _encoding.GetChars(bytes, byteCount, chars, charCount, this);
        }

        // This method is used when the output buffer might not be big enough.
        // Just call the pointer version.  (This gets chars)
        public override unsafe void Convert(byte[] bytes, int byteIndex, int byteCount,
                                            char[] chars, int charIndex, int charCount, bool flush,
                                            out int bytesUsed, out int charsUsed, out bool completed)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentNullException.ThrowIfNull(chars);

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            // Just call the pointer version (public overrides can't do this)
            fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
            {
                fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
                {
                    Convert(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, flush,
                        out bytesUsed, out charsUsed, out completed);
                }
            }
        }

        // This is the version that used pointers.  We call the base encoding worker function
        // after setting our appropriate internal variables.  This is getting chars
        public override unsafe void Convert(byte* bytes, int byteCount,
                                              char* chars, int charCount, bool flush,
                                              out int bytesUsed, out int charsUsed, out bool completed)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentNullException.ThrowIfNull(chars);

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(byteCount < 0 ? nameof(byteCount) : nameof(charCount),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            // We don't want to throw
            _mustFlush = flush;
            _throwOnOverflow = false;
            _bytesUsed = 0;

            // Do conversion
            Debug.Assert(_encoding is not null);
            charsUsed = _encoding.GetChars(bytes, byteCount, chars, charCount, this);
            bytesUsed = _bytesUsed;

            // See comment in EncoderNLS.Convert for the details of the logic below.

            completed = (bytesUsed == byteCount)
                && (!flush || !this.HasState)
                && (_fallbackBuffer is null || _fallbackBuffer.Remaining == 0);
        }

        public bool MustFlush => _mustFlush;

        // Anything left in our decoder?
        internal virtual bool HasState => _leftoverByteCount != 0;

        // Allow encoding to clear our must flush instead of throwing (in ThrowCharsOverflow)
        internal void ClearMustFlush()
        {
            _mustFlush = false;
        }

        internal ReadOnlySpan<byte> GetLeftoverData() =>
            MemoryMarshal.AsBytes(new ReadOnlySpan<int>(in _leftoverBytes)).Slice(0, _leftoverByteCount);

        internal void SetLeftoverData(ReadOnlySpan<byte> bytes)
        {
            bytes.CopyTo(MemoryMarshal.AsBytes(new Span<int>(ref _leftoverBytes)));
            _leftoverByteCount = bytes.Length;
        }

        internal bool HasLeftoverData => _leftoverByteCount != 0;

        internal void ClearLeftoverData()
        {
            _leftoverByteCount = 0;
        }

        internal int DrainLeftoverDataForGetCharCount(ReadOnlySpan<byte> bytes, out int bytesConsumed)
        {
            // Quick check: we _should not_ have leftover fallback data from a previous invocation,
            // as we'd end up consuming any such data and would corrupt whatever Convert call happens
            // to be in progress. Unlike EncoderNLS, this is simply a Debug.Assert. No exception is thrown.

            Debug.Assert(_fallbackBuffer is null || _fallbackBuffer.Remaining == 0, "Should have no data remaining in the fallback buffer.");
            Debug.Assert(HasLeftoverData, "Caller shouldn't invoke this routine unless there's leftover data in the decoder.");

            // Copy the existing leftover data plus as many bytes as possible of the new incoming data
            // into a temporary concated buffer, then get its char count by decoding it.

            Span<byte> combinedBuffer = stackalloc byte[4];
            combinedBuffer = combinedBuffer.Slice(0, ConcatInto(GetLeftoverData(), bytes, combinedBuffer));
            int charCount = 0;

            Debug.Assert(_encoding is not null);
            switch (_encoding.DecodeFirstRune(combinedBuffer, out Rune value, out int combinedBufferBytesConsumed))
            {
                case OperationStatus.Done:
                    charCount = value.Utf16SequenceLength;
                    goto Finish; // successfully transcoded bytes -> chars

                case OperationStatus.NeedMoreData:
                    if (MustFlush)
                    {
                        goto case OperationStatus.InvalidData; // treat as equivalent to bad data
                    }
                    else
                    {
                        goto Finish; // consumed some bytes, output 0 chars
                    }

                case OperationStatus.InvalidData:
                    break;

                default:
                    Debug.Fail("Unexpected OperationStatus return value.");
                    break;
            }

            // Couldn't decode the buffer. Fallback the buffer instead. See comment in DrainLeftoverDataForGetChars
            // for more information on why a negative index is provided.

            if (FallbackBuffer.Fallback(combinedBuffer.Slice(0, combinedBufferBytesConsumed).ToArray(), index: -_leftoverByteCount))
            {
                charCount = _fallbackBuffer!.DrainRemainingDataForGetCharCount();
                Debug.Assert(charCount >= 0, "Fallback buffer shouldn't have returned a negative char count.");
            }

        Finish:

            bytesConsumed = combinedBufferBytesConsumed - _leftoverByteCount; // amount of 'bytes' buffer consumed just now
            return charCount;
        }

        internal int DrainLeftoverDataForGetChars(ReadOnlySpan<byte> bytes, Span<char> chars, out int bytesConsumed)
        {
            // Quick check: we _should not_ have leftover fallback data from a previous invocation,
            // as we'd end up consuming any such data and would corrupt whatever Convert call happens
            // to be in progress. Unlike EncoderNLS, this is simply a Debug.Assert. No exception is thrown.

            Debug.Assert(_fallbackBuffer is null || _fallbackBuffer.Remaining == 0, "Should have no data remaining in the fallback buffer.");
            Debug.Assert(HasLeftoverData, "Caller shouldn't invoke this routine unless there's leftover data in the decoder.");

            // Copy the existing leftover data plus as many bytes as possible of the new incoming data
            // into a temporary concated buffer, then transcode it from bytes to chars.

            Span<byte> combinedBuffer = stackalloc byte[4];
            combinedBuffer = combinedBuffer.Slice(0, ConcatInto(GetLeftoverData(), bytes, combinedBuffer));
            int charsWritten = 0;

            bool persistNewCombinedBuffer = false;

            Debug.Assert(_encoding is not null);
            switch (_encoding.DecodeFirstRune(combinedBuffer, out Rune value, out int combinedBufferBytesConsumed))
            {
                case OperationStatus.Done:
                    if (value.TryEncodeToUtf16(chars, out charsWritten))
                    {
                        goto Finish; // successfully transcoded bytes -> chars
                    }
                    else
                    {
                        goto DestinationTooSmall;
                    }

                case OperationStatus.NeedMoreData:
                    if (MustFlush)
                    {
                        goto case OperationStatus.InvalidData; // treat as equivalent to bad data
                    }
                    else
                    {
                        persistNewCombinedBuffer = true;
                        goto Finish; // successfully consumed some bytes, output no chars
                    }

                case OperationStatus.InvalidData:
                    break;

                default:
                    Debug.Fail("Unexpected OperationStatus return value.");
                    break;
            }

            // Couldn't decode the buffer. Fallback the buffer instead. The fallback mechanism relies
            // on a negative index to convey "the start of the invalid sequence was some number of
            // bytes back before the current buffer." Since we know the invalid sequence must have
            // started at the beginning of our leftover byte buffer, we can signal to our caller that
            // they must backtrack that many bytes to find the real start of the invalid sequence.

            if (FallbackBuffer.Fallback(combinedBuffer.Slice(0, combinedBufferBytesConsumed).ToArray(), index: -_leftoverByteCount)
                && !_fallbackBuffer!.TryDrainRemainingDataForGetChars(chars, out charsWritten))
            {
                goto DestinationTooSmall;
            }

        Finish:

            // Report back the number of bytes (from the new incoming span) we consumed just now.
            // This calculation is simple: it's the difference between the original leftover byte
            // count and the number of bytes from the combined buffer we needed to decode the first
            // scalar value. We need to report this before the call to SetLeftoverData /
            // ClearLeftoverData because those methods will overwrite the _leftoverByteCount field.

            bytesConsumed = combinedBufferBytesConsumed - _leftoverByteCount;

            if (persistNewCombinedBuffer)
            {
                Debug.Assert(combinedBufferBytesConsumed == combinedBuffer.Length, "We should be asked to persist the entire combined buffer.");
                SetLeftoverData(combinedBuffer); // the buffer still only contains partial data; a future call to Convert will need it
            }
            else
            {
                ClearLeftoverData(); // the buffer contains no partial data; we'll go down the normal paths
            }

            return charsWritten;

        DestinationTooSmall:

            // If we got to this point, we're trying to write chars to the output buffer, but we're unable to do
            // so. Unlike EncoderNLS, this type does not allow partial writes to the output buffer. Since we know
            // draining leftover data is the first operation performed by any DecoderNLS API, there was no
            // opportunity for any code before us to make forward progress, so we must fail immediately.

            _encoding.ThrowCharsOverflow(this, nothingDecoded: true);
            throw null!; // will never reach this point
        }

        /// <summary>
        /// Given a byte buffer <paramref name="dest"/>, concatenates as much of <paramref name="srcLeft"/> followed
        /// by <paramref name="srcRight"/> into it as will fit, then returns the total number of bytes copied.
        /// </summary>
        private static int ConcatInto(ReadOnlySpan<byte> srcLeft, ReadOnlySpan<byte> srcRight, Span<byte> dest)
        {
            int total = 0;

            for (int i = 0; i < srcLeft.Length; i++)
            {
                if ((uint)total >= (uint)dest.Length)
                {
                    goto Finish;
                }
                else
                {
                    dest[total++] = srcLeft[i];
                }
            }

            for (int i = 0; i < srcRight.Length; i++)
            {
                if ((uint)total >= (uint)dest.Length)
                {
                    goto Finish;
                }
                else
                {
                    dest[total++] = srcRight[i];
                }
            }

        Finish:

            return total;
        }
    }
}
