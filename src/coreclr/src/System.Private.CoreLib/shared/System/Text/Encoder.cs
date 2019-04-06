// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Text;
using System;
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
    public abstract class Encoder
    {
        internal EncoderFallback? _fallback = null;

        internal EncoderFallbackBuffer? _fallbackBuffer = null;

        protected Encoder()
        {
            // We don't call default reset because default reset probably isn't good if we aren't initialized.
        }

        public EncoderFallback? Fallback
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
        public EncoderFallbackBuffer FallbackBuffer
        {
            get
            {
                if (_fallbackBuffer == null)
                {
                    if (_fallback != null)
                        _fallbackBuffer = _fallback.CreateFallbackBuffer();
                    else
                        _fallbackBuffer = EncoderFallback.ReplacementFallback.CreateFallbackBuffer();
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

        // Reset the Encoder
        //
        // Normally if we call GetBytes() and an error is thrown we don't change the state of the encoder.  This
        // would allow the caller to correct the error condition and try again (such as if they need a bigger buffer.)
        //
        // If the caller doesn't want to try again after GetBytes() throws an error, then they need to call Reset().
        //
        // Virtual implementation has to call GetBytes with flush and a big enough buffer to clear a 0 char string
        // We avoid GetMaxByteCount() because a) we can't call the base encoder and b) it might be really big.
        public virtual void Reset()
        {
            char[] charTemp = { };
            byte[] byteTemp = new byte[GetByteCount(charTemp, 0, 0, true)];
            GetBytes(charTemp, 0, 0, byteTemp, 0, true);
            if (_fallbackBuffer != null)
                _fallbackBuffer.Reset();
        }

        // Returns the number of bytes the next call to GetBytes will
        // produce if presented with the given range of characters and the given
        // value of the flush parameter. The returned value takes into
        // account the state in which the encoder was left following the last call
        // to GetBytes. The state of the encoder is not affected by a call
        // to this method.
        //
        public abstract int GetByteCount(char[] chars, int index, int count, bool flush);

        // We expect this to be the workhorse for NLS encodings
        // unfortunately for existing overrides, it has to call the [] version,
        // which is really slow, so avoid this method if you might be calling external encodings.
        [CLSCompliant(false)]
        public virtual unsafe int GetByteCount(char* chars, int count, bool flush)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException(nameof(chars),
                      SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                      SR.ArgumentOutOfRange_NeedNonNegNum);

            char[] arrChar = new char[count];
            int index;

            for (index = 0; index < count; index++)
                arrChar[index] = chars[index];

            return GetByteCount(arrChar, 0, count, flush);
        }

        public virtual unsafe int GetByteCount(ReadOnlySpan<char> chars, bool flush)
        {
            fixed (char* charsPtr = &MemoryMarshal.GetNonNullPinnableReference(chars))
            {
                return GetByteCount(charsPtr, chars.Length, flush);
            }
        }

        // Encodes a range of characters in a character array into a range of bytes
        // in a byte array. The method encodes charCount characters from
        // chars starting at index charIndex, storing the resulting
        // bytes in bytes starting at index byteIndex. The encoding
        // takes into account the state in which the encoder was left following the
        // last call to this method. The flush parameter indicates whether
        // the encoder should flush any shift-states and partial characters at the
        // end of the conversion. To ensure correct termination of a sequence of
        // blocks of encoded bytes, the last call to GetBytes should specify
        // a value of true for the flush parameter.
        //
        // An exception occurs if the byte array is not large enough to hold the
        // complete encoding of the characters. The GetByteCount method can
        // be used to determine the exact number of bytes that will be produced for
        // a given range of characters. Alternatively, the GetMaxByteCount
        // method of the Encoding that produced this encoder can be used to
        // determine the maximum number of bytes that will be produced for a given
        // number of characters, regardless of the actual character values.
        //
        public abstract int GetBytes(char[] chars, int charIndex, int charCount,
                                        byte[] bytes, int byteIndex, bool flush);

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
        // external GetBytes() method, which could be overridden by a third party and
        // the results of which cannot be guaranteed.  We use that result to copy
        // the byte[] to our byte* output buffer.  If the result count was wrong, we
        // could easily overflow our output buffer.  Therefore we do an extra test
        // when we copy the buffer so that we don't overflow byteCount either.
        [CLSCompliant(false)]
        public virtual unsafe int GetBytes(char* chars, int charCount,
                                              byte* bytes, int byteCount, bool flush)
        {
            // Validate input parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars),
                    SR.ArgumentNull_Array);

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? nameof(charCount) : nameof(byteCount)),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            // Get the char array to convert
            char[] arrChar = new char[charCount];

            int index;
            for (index = 0; index < charCount; index++)
                arrChar[index] = chars[index];

            // Get the byte array to fill
            byte[] arrByte = new byte[byteCount];

            // Do the work
            int result = GetBytes(arrChar, 0, charCount, arrByte, 0, flush);

            Debug.Assert(result <= byteCount, "Returned more bytes than we have space for");

            // Copy the byte array
            // WARNING: We MUST make sure that we don't copy too many bytes.  We can't
            // rely on result because it could be a 3rd party implementation.  We need
            // to make sure we never copy more than byteCount bytes no matter the value
            // of result
            if (result < byteCount)
                byteCount = result;

            // Don't copy too many bytes!
            for (index = 0; index < byteCount; index++)
                bytes[index] = arrByte[index];

            return byteCount;
        }

        public virtual unsafe int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush)
        {
            fixed (char* charsPtr = &MemoryMarshal.GetNonNullPinnableReference(chars))
            fixed (byte* bytesPtr = &MemoryMarshal.GetNonNullPinnableReference(bytes))
            {
                return GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length, flush);
            }
        }

        // This method is used to avoid running out of output buffer space.
        // It will encode until it runs out of chars, and then it will return
        // true if it the entire input was converted.  In either case it
        // will also return the number of converted chars and output bytes used.
        // It will only throw a buffer overflow exception if the entire lenght of bytes[] is
        // too small to store the next byte. (like 0 or maybe 1 or 4 for some encodings)
        // We're done processing this buffer only if completed returns true.
        //
        // Might consider checking Max...Count to avoid the extra counting step.
        //
        // Note that if all of the input chars are not consumed, then we'll do a /2, which means
        // that its likely that we didn't consume as many chars as we could have.  For some
        // applications this could be slow.  (Like trying to exactly fill an output buffer from a bigger stream)
        public virtual void Convert(char[] chars, int charIndex, int charCount,
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

            charsUsed = charCount;

            // Its easy to do if it won't overrun our buffer.
            // Note: We don't want to call unsafe version because that might be an untrusted version
            // which could be really unsafe and we don't want to mix it up.
            while (charsUsed > 0)
            {
                if (GetByteCount(chars, charIndex, charsUsed, flush) <= byteCount)
                {
                    bytesUsed = GetBytes(chars, charIndex, charsUsed, bytes, byteIndex, flush);
                    completed = (charsUsed == charCount &&
                        (_fallbackBuffer == null || _fallbackBuffer.Remaining == 0));
                    return;
                }

                // Try again with 1/2 the count, won't flush then 'cause won't read it all
                flush = false;
                charsUsed /= 2;
            }

            // Oops, we didn't have anything, we'll have to throw an overflow
            throw new ArgumentException(SR.Argument_ConversionOverflow);
        }

        // Same thing, but using pointers
        //
        // Might consider checking Max...Count to avoid the extra counting step.
        //
        // Note that if all of the input chars are not consumed, then we'll do a /2, which means
        // that its likely that we didn't consume as many chars as we could have.  For some
        // applications this could be slow.  (Like trying to exactly fill an output buffer from a bigger stream)
        [CLSCompliant(false)]
        public virtual unsafe void Convert(char* chars, int charCount,
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

            // Get ready to do it
            charsUsed = charCount;

            // Its easy to do if it won't overrun our buffer.
            while (charsUsed > 0)
            {
                if (GetByteCount(chars, charsUsed, flush) <= byteCount)
                {
                    bytesUsed = GetBytes(chars, charsUsed, bytes, byteCount, flush);
                    completed = (charsUsed == charCount &&
                        (_fallbackBuffer == null || _fallbackBuffer.Remaining == 0));
                    return;
                }

                // Try again with 1/2 the count, won't flush then 'cause won't read it all
                flush = false;
                charsUsed /= 2;
            }

            // Oops, we didn't have anything, we'll have to throw an overflow
            throw new ArgumentException(SR.Argument_ConversionOverflow);
        }

        public virtual unsafe void Convert(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        {
            fixed (char* charsPtr = &MemoryMarshal.GetNonNullPinnableReference(chars))
            fixed (byte* bytesPtr = &MemoryMarshal.GetNonNullPinnableReference(bytes))
            {
                Convert(charsPtr, chars.Length, bytesPtr, bytes.Length, flush, out charsUsed, out bytesUsed, out completed);
            }
        }
    }
}

