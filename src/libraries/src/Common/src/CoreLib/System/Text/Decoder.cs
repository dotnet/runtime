// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System;
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
    //
    public abstract class Decoder
    {
        internal DecoderFallback _fallback = null;

        internal DecoderFallbackBuffer _fallbackBuffer = null;

        protected Decoder()
        {
            // We don't call default reset because default reset probably isn't good if we aren't initialized.
        }

        public DecoderFallback Fallback
        {
            get
            {
                return _fallback;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                // Can't change fallback if buffer is wrong
                if (_fallbackBuffer != null && _fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(
                      SR.Argument_FallbackBufferNotEmpty, nameof(value));

                _fallback = value;
                _fallbackBuffer = null;
            }
        }

        // Note: we don't test for threading here because async access to Encoders and Decoders
        // doesn't work anyway.
        public DecoderFallbackBuffer FallbackBuffer
        {
            get
            {
                if (_fallbackBuffer == null)
                {
                    if (_fallback != null)
                        _fallbackBuffer = _fallback.CreateFallbackBuffer();
                    else
                        _fallbackBuffer = DecoderFallback.ReplacementFallback.CreateFallbackBuffer();
                }

                return _fallbackBuffer;
            }
        }

        internal bool InternalHasFallbackBuffer
        {
            get
            {
                return _fallbackBuffer != null;
            }
        }

        // Reset the Decoder
        //
        // Normally if we call GetChars() and an error is thrown we don't change the state of the Decoder.  This
        // would allow the caller to correct the error condition and try again (such as if they need a bigger buffer.)
        //
        // If the caller doesn't want to try again after GetChars() throws an error, then they need to call Reset().
        //
        // Virtual implementation has to call GetChars with flush and a big enough buffer to clear a 0 byte string
        // We avoid GetMaxCharCount() because a) we can't call the base encoder and b) it might be really big.
        public virtual void Reset()
        {
            byte[] byteTemp = Array.Empty<byte>();
            char[] charTemp = new char[GetCharCount(byteTemp, 0, 0, true)];
            GetChars(byteTemp, 0, 0, charTemp, 0, true);
            _fallbackBuffer?.Reset();
        }

        // Returns the number of characters the next call to GetChars will
        // produce if presented with the given range of bytes. The returned value
        // takes into account the state in which the decoder was left following the
        // last call to GetChars. The state of the decoder is not affected
        // by a call to this method.
        //
        public abstract int GetCharCount(byte[] bytes, int index, int count);

        public virtual int GetCharCount(byte[] bytes, int index, int count, bool flush)
        {
            return GetCharCount(bytes, index, count);
        }

        // We expect this to be the workhorse for NLS Encodings, but for existing
        // ones we need a working (if slow) default implementation)
        [CLSCompliant(false)]
        public virtual unsafe int GetCharCount(byte* bytes, int count, bool flush)
        {
            // Validate input parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes),
                      SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            byte[] arrbyte = new byte[count];
            int index;

            for (index = 0; index < count; index++)
                arrbyte[index] = bytes[index];

            return GetCharCount(arrbyte, 0, count);
        }

        public virtual unsafe int GetCharCount(ReadOnlySpan<byte> bytes, bool flush)
        {
            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            {
                return GetCharCount(bytesPtr, bytes.Length, flush);
            }
        }

        // Decodes a range of bytes in a byte array into a range of characters
        // in a character array. The method decodes byteCount bytes from
        // bytes starting at index byteIndex, storing the resulting
        // characters in chars starting at index charIndex. The
        // decoding takes into account the state in which the decoder was left
        // following the last call to this method.
        //
        // An exception occurs if the character array is not large enough to
        // hold the complete decoding of the bytes. The GetCharCount method
        // can be used to determine the exact number of characters that will be
        // produced for a given range of bytes. Alternatively, the
        // GetMaxCharCount method of the Encoding that produced this
        // decoder can be used to determine the maximum number of characters that
        // will be produced for a given number of bytes, regardless of the actual
        // byte values.
        //
        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                        char[] chars, int charIndex);

        public virtual int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                       char[] chars, int charIndex, bool flush)
        {
            return GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        // We expect this to be the workhorse for NLS Encodings, but for existing
        // ones we need a working (if slow) default implementation)
        //
        // WARNING WARNING WARNING
        //
        // WARNING: If this breaks it could be a security threat.  Obviously we
        // call this internally, so you need to make sure that your pointers, counts
        // and indexes are correct when you call this method.
        //
        // In addition, we have internal code, which will be marked as "safe" calling
        // this code.  However this code is dependent upon the implementation of an
        // external GetChars() method, which could be overridden by a third party and
        // the results of which cannot be guaranteed.  We use that result to copy
        // the char[] to our char* output buffer.  If the result count was wrong, we
        // could easily overflow our output buffer.  Therefore we do an extra test
        // when we copy the buffer so that we don't overflow charCount either.
        [CLSCompliant(false)]
        public virtual unsafe int GetChars(byte* bytes, int byteCount,
                                              char* chars, int charCount, bool flush)
        {
            // Validate input parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes),
                    SR.ArgumentNull_Array);

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? nameof(byteCount) : nameof(charCount)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            // Get the byte array to convert
            byte[] arrByte = new byte[byteCount];

            int index;
            for (index = 0; index < byteCount; index++)
                arrByte[index] = bytes[index];

            // Get the char array to fill
            char[] arrChar = new char[charCount];

            // Do the work
            int result = GetChars(arrByte, 0, byteCount, arrChar, 0, flush);

            Debug.Assert(result <= charCount, "Returned more chars than we have space for");

            // Copy the char array
            // WARNING: We MUST make sure that we don't copy too many chars.  We can't
            // rely on result because it could be a 3rd party implementation.  We need
            // to make sure we never copy more than charCount chars no matter the value
            // of result
            if (result < charCount)
                charCount = result;

            // We check both result and charCount so that we don't accidentally overrun
            // our pointer buffer just because of an issue in GetChars
            for (index = 0; index < charCount; index++)
                chars[index] = arrChar[index];

            return charCount;
        }

        public virtual unsafe int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool flush)
        {
            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            {
                return GetChars(bytesPtr, bytes.Length, charsPtr, chars.Length, flush);
            }
        }

        // This method is used when the output buffer might not be large enough.
        // It will decode until it runs out of bytes, and then it will return
        // true if it the entire input was converted.  In either case it
        // will also return the number of converted bytes and output characters used.
        // It will only throw a buffer overflow exception if the entire lenght of chars[] is
        // too small to store the next char. (like 0 or maybe 1 or 4 for some encodings)
        // We're done processing this buffer only if completed returns true.
        //
        // Might consider checking Max...Count to avoid the extra counting step.
        //
        // Note that if all of the input bytes are not consumed, then we'll do a /2, which means
        // that its likely that we didn't consume as many bytes as we could have.  For some
        // applications this could be slow.  (Like trying to exactly fill an output buffer from a bigger stream)
        public virtual void Convert(byte[] bytes, int byteIndex, int byteCount,
                                      char[] chars, int charIndex, int charCount, bool flush,
                                      out int bytesUsed, out int charsUsed, out bool completed)
        {
            // Validate parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException((bytes == null ? nameof(bytes) : nameof(chars)),
                      SR.ArgumentNull_Array);

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? nameof(charIndex) : nameof(charCount)),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars),
                      SR.ArgumentOutOfRange_IndexCountBuffer);

            bytesUsed = byteCount;

            // Its easy to do if it won't overrun our buffer.
            while (bytesUsed > 0)
            {
                if (GetCharCount(bytes, byteIndex, bytesUsed, flush) <= charCount)
                {
                    charsUsed = GetChars(bytes, byteIndex, bytesUsed, chars, charIndex, flush);
                    completed = (bytesUsed == byteCount &&
                        (_fallbackBuffer == null || _fallbackBuffer.Remaining == 0));
                    return;
                }

                // Try again with 1/2 the count, won't flush then 'cause won't read it all
                flush = false;
                bytesUsed /= 2;
            }

            // Oops, we didn't have anything, we'll have to throw an overflow
            throw new ArgumentException(SR.Argument_ConversionOverflow);
        }

        // This is the version that uses *.
        // We're done processing this buffer only if completed returns true.
        //
        // Might consider checking Max...Count to avoid the extra counting step.
        //
        // Note that if all of the input bytes are not consumed, then we'll do a /2, which means
        // that its likely that we didn't consume as many bytes as we could have.  For some
        // applications this could be slow.  (Like trying to exactly fill an output buffer from a bigger stream)
        [CLSCompliant(false)]
        public virtual unsafe void Convert(byte* bytes, int byteCount,
                                             char* chars, int charCount, bool flush,
                                             out int bytesUsed, out int charsUsed, out bool completed)
        {
            // Validate input parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes),
                    SR.ArgumentNull_Array);

            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? nameof(byteCount) : nameof(charCount)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            // Get ready to do it
            bytesUsed = byteCount;

            // Its easy to do if it won't overrun our buffer.
            while (bytesUsed > 0)
            {
                if (GetCharCount(bytes, bytesUsed, flush) <= charCount)
                {
                    charsUsed = GetChars(bytes, bytesUsed, chars, charCount, flush);
                    completed = (bytesUsed == byteCount &&
                        (_fallbackBuffer == null || _fallbackBuffer.Remaining == 0));
                    return;
                }

                // Try again with 1/2 the count, won't flush then 'cause won't read it all
                flush = false;
                bytesUsed /= 2;
            }

            // Oops, we didn't have anything, we'll have to throw an overflow
            throw new ArgumentException(SR.Argument_ConversionOverflow);
        }

        public virtual unsafe void Convert(ReadOnlySpan<byte> bytes, Span<char> chars, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        {
            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
            fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
            {
                Convert(bytesPtr, bytes.Length, charsPtr, chars.Length, flush, out bytesUsed, out charsUsed, out completed);
            }
        }
    }
}
