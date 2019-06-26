// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text
{
    // An Encoder is used to encode a sequence of blocks of characters into
    // a sequence of blocks of bytes. Following instantiation of an encoder,
    // sequential blocks of characters are converted into blocks of bytes through
    // calls to the GetBytes method. The encoder maintains state between the
    // conversions, allowing it to correctly encode character sequences that span
    // adjacent blocks.
    //
    // Instances of specific implementations of the Encoder abstract base
    // class are typically obtained through calls to the GetEncoder method
    // of Encoding objects.
    //

    internal class EncoderNLS : Encoder
    {
        // Need a place for the last left over character, most of our encodings use this
        internal char _charLeftOver;
        private Encoding _encoding;
        private bool _mustFlush;
        internal bool _throwOnOverflow;
        internal int _charsUsed;

        internal EncoderNLS(Encoding encoding)
        {
            _encoding = encoding;
            _fallback = _encoding.EncoderFallback;
            this.Reset();
        }

        public override void Reset()
        {
            _charLeftOver = (char)0;
            if (_fallbackBuffer != null)
                _fallbackBuffer.Reset();
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException(nameof(chars),
                      SR.ArgumentNull_Array);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(chars),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            // Just call the pointer version
            int result = -1;
            fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
            {
                result = GetByteCount(pChars + index, count, flush);
            }
            return result;
        }

        public unsafe override int GetByteCount(char* chars, int count, bool flush)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException(nameof(chars),
                      SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            _mustFlush = flush;
            _throwOnOverflow = true;
            Debug.Assert(_encoding != null);
            return _encoding.GetByteCount(chars, count, this);
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex, bool flush)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? nameof(chars) : nameof(bytes)),
                      SR.ArgumentNull_Array);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex),
                     SR.ArgumentOutOfRange_Index);

            int byteCount = bytes.Length - byteIndex;

            // Just call pointer version
            fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
            fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))

                // Remember that charCount is # to decode, not size of array.
                return GetBytes(pChars + charIndex, charCount,
                                pBytes + byteIndex, byteCount, flush);
        }

        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? nameof(chars) : nameof(bytes)),
                      SR.ArgumentNull_Array);

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? nameof(byteCount) : nameof(charCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            _mustFlush = flush;
            _throwOnOverflow = true;
            Debug.Assert(_encoding != null);
            return _encoding.GetBytes(chars, charCount, bytes, byteCount, this);
        }

        // This method is used when your output buffer might not be large enough for the entire result.
        // Just call the pointer version.  (This gets bytes)
        public override unsafe void Convert(char[] chars, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex, int byteCount, bool flush,
                                              out int charsUsed, out int bytesUsed, out bool completed)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? nameof(chars) : nameof(bytes)),
                      SR.ArgumentNull_Array);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            // Just call the pointer version (can't do this for non-msft encoders)
            fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
            {
                fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                {
                    Convert(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, flush,
                        out charsUsed, out bytesUsed, out completed);
                }
            }
        }

        // This is the version that uses pointers.  We call the base encoding worker function
        // after setting our appropriate internal variables.  This is getting bytes
        public override unsafe void Convert(char* chars, int charCount,
                                              byte* bytes, int byteCount, bool flush,
                                              out int charsUsed, out int bytesUsed, out bool completed)
        {
            // Validate input parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars),
                    SR.ArgumentNull_Array);
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount) : nameof(byteCount)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            // We don't want to throw
            _mustFlush = flush;
            _throwOnOverflow = false;
            _charsUsed = 0;

            // Do conversion
            Debug.Assert(_encoding != null);
            bytesUsed = _encoding.GetBytes(chars, charCount, bytes, byteCount, this);
            charsUsed = _charsUsed;

            // Per MSDN, "The completed output parameter indicates whether all the data in the input
            // buffer was converted and stored in the output buffer." That means we've successfully
            // consumed all the input _and_ there's no pending state or fallback data remaining to be output.

            completed = (charsUsed == charCount)
                && !this.HasState
                && (_fallbackBuffer is null || _fallbackBuffer.Remaining == 0);

            // Our data thingys are now full, we can return
        }

        public Encoding Encoding
        {
            get
            {
                Debug.Assert(_encoding != null);
                return _encoding;
            }
        }

        public bool MustFlush
        {
            get
            {
                return _mustFlush;
            }
        }

        /// <summary>
        /// States whether a call to <see cref="Encoding.GetBytes(char*, int, byte*, int, EncoderNLS)"/> must first drain data on this <see cref="EncoderNLS"/> instance.
        /// </summary>
        internal bool HasLeftoverData => _charLeftOver != default || (_fallbackBuffer != null && _fallbackBuffer.Remaining > 0);

        // Anything left in our encoder?
        internal virtual bool HasState
        {
            get
            {
                return (_charLeftOver != (char)0);
            }
        }

        // Allow encoding to clear our must flush instead of throwing (in ThrowBytesOverflow)
        internal void ClearMustFlush()
        {
            _mustFlush = false;
        }

        internal int DrainLeftoverDataForGetByteCount(ReadOnlySpan<char> chars, out int charsConsumed)
        {
            // Quick check: we _should not_ have leftover fallback data from a previous invocation,
            // as we'd end up consuming any such data and would corrupt whatever Convert call happens
            // to be in progress.

            if (_fallbackBuffer != null && _fallbackBuffer.Remaining > 0)
            {
                throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, Encoding.EncodingName, _fallbackBuffer.GetType()));
            }

            // If we have a leftover high surrogate from a previous operation, consume it now.
            // We won't clear the _charLeftOver field since GetByteCount is supposed to be
            // a non-mutating operation, and we need the field to retain its value for the
            // next call to Convert.

            charsConsumed = 0; // could be incorrect, will fix up later in the method

            if (_charLeftOver == default)
            {
                return 0; // no leftover high surrogate char - short-circuit and finish
            }
            else
            {
                char secondChar = default;

                if (chars.IsEmpty)
                {
                    // If the input buffer is empty and we're not being asked to flush, no-op and return
                    // success to our caller. If we're being asked to flush, the leftover high surrogate from
                    // the previous operation will go through the fallback mechanism by itself.

                    if (!MustFlush)
                    {
                        return 0; // no-op = success
                    }
                }
                else
                {
                    secondChar = chars[0];
                }

                // If we have to fallback the chars we're reading immediately below, populate the
                // fallback buffer with the invalid data. We'll just fall through to the "consume
                // fallback buffer" logic at the end of the method.

                bool didFallback;

                if (Rune.TryCreate(_charLeftOver, secondChar, out Rune rune))
                {
                    charsConsumed = 1; // consumed the leftover high surrogate + the first char in the input buffer

                    Debug.Assert(_encoding != null);
                    if (_encoding.TryGetByteCount(rune, out int byteCount))
                    {
                        Debug.Assert(byteCount >= 0, "Encoding shouldn't have returned a negative byte count.");
                        return byteCount;
                    }
                    else
                    {
                        // The fallback mechanism relies on a negative index to convey "the start of the invalid
                        // sequence was some number of chars back before the current buffer." In this block and
                        // in the block immediately thereafter, we know we have a single leftover high surrogate
                        // character from a previous operation, so we provide an index of -1 to convey that the
                        // char immediately before the current buffer was the start of the invalid sequence.

                        didFallback = FallbackBuffer.Fallback(_charLeftOver, secondChar, index: -1);
                    }
                }
                else
                {
                    didFallback = FallbackBuffer.Fallback(_charLeftOver, index: -1);
                }

                // Now tally the number of bytes that would've been emitted as part of fallback.
                Debug.Assert(_fallbackBuffer != null);
                return _fallbackBuffer.DrainRemainingDataForGetByteCount();
            }
        }

        internal bool TryDrainLeftoverDataForGetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsConsumed, out int bytesWritten)
        {
            // We may have a leftover high surrogate data from a previous invocation, or we may have leftover
            // data in the fallback buffer, or we may have neither, but we will never have both. Check for these
            // conditions and handle them now.

            charsConsumed = 0; // could be incorrect, will fix up later in the method
            bytesWritten = 0; // could be incorrect, will fix up later in the method

            if (_charLeftOver != default)
            {
                char secondChar = default;

                if (chars.IsEmpty)
                {
                    // If the input buffer is empty and we're not being asked to flush, no-op and return
                    // success to our caller. If we're being asked to flush, the leftover high surrogate from
                    // the previous operation will go through the fallback mechanism by itself.

                    if (!MustFlush)
                    {
                        charsConsumed = 0;
                        bytesWritten = 0;
                        return true; // no-op = success
                    }
                }
                else
                {
                    secondChar = chars[0];
                }

                // If we have to fallback the chars we're reading immediately below, populate the
                // fallback buffer with the invalid data. We'll just fall through to the "consume
                // fallback buffer" logic at the end of the method.

                if (Rune.TryCreate(_charLeftOver, secondChar, out Rune rune))
                {
                    charsConsumed = 1; // at the very least, we consumed 1 char from the input
                    Debug.Assert(_encoding != null);
                    switch (_encoding.EncodeRune(rune, bytes, out bytesWritten))
                    {
                        case OperationStatus.Done:
                            _charLeftOver = default; // we just consumed this char
                            return true; // that's all - we've handled the leftover data

                        case OperationStatus.DestinationTooSmall:
                            _charLeftOver = default; // we just consumed this char
                            _encoding.ThrowBytesOverflow(this, nothingEncoded: true); // will throw
                            break;

                        case OperationStatus.InvalidData:
                            FallbackBuffer.Fallback(_charLeftOver, secondChar, index: -1); // see comment in DrainLeftoverDataForGetByteCount
                            break;

                        default:
                            Debug.Fail("Unknown return value.");
                            break;
                    }
                }
                else
                {
                    FallbackBuffer.Fallback(_charLeftOver, index: -1); // see comment in DrainLeftoverDataForGetByteCount
                }
            }

            // Now check the fallback buffer for any remaining data.

            if (_fallbackBuffer != null && _fallbackBuffer.Remaining > 0)
            {
                return _fallbackBuffer.TryDrainRemainingDataForGetBytes(bytes, out bytesWritten);
            }

            // And we're done!

            return true; // success
        }
    }
}
