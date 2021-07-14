// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Don't override IsAlwaysNormalized because it is just a Unicode Transformation and could be confused.
//

// This define can be used to turn off the fast loops. Useful for finding whether
// the problem is fastloop-specific.
#define FASTLOOP

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerServices;

namespace System.Text
{
    public class UnicodeEncoding : Encoding
    {
        // Used by Encoding.BigEndianUnicode/Unicode for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly UnicodeEncoding s_bigEndianDefault = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
        internal static readonly UnicodeEncoding s_littleEndianDefault = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);

        private readonly bool isThrowException;

        private readonly bool bigEndian;
        private readonly bool byteOrderMark;

        // Unicode version 2.0 character size in bytes
        public const int CharSize = 2;

        public UnicodeEncoding()
            : this(false, true)
        {
        }

        public UnicodeEncoding(bool bigEndian, bool byteOrderMark)
            : base(bigEndian ? 1201 : 1200)  // Set the data item.
        {
            this.bigEndian = bigEndian;
            this.byteOrderMark = byteOrderMark;
        }

        public UnicodeEncoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes)
            : this(bigEndian, byteOrderMark)
        {
            this.isThrowException = throwOnInvalidBytes;

            // Encoding constructor already did this, but it'll be wrong if we're throwing exceptions
            if (this.isThrowException)
                SetDefaultFallbacks();
        }

        internal sealed override void SetDefaultFallbacks()
        {
            // For UTF-X encodings, we use a replacement fallback with an empty string
            if (this.isThrowException)
            {
                this.encoderFallback = EncoderFallback.ExceptionFallback;
                this.decoderFallback = DecoderFallback.ExceptionFallback;
            }
            else
            {
                this.encoderFallback = new EncoderReplacementFallback("\xFFFD");
                this.decoderFallback = new DecoderReplacementFallback("\xFFFD");
            }
        }

        // The following methods are copied from EncodingNLS.cs.
        // Unfortunately EncodingNLS.cs is internal and we're public, so we have to re-implement them here.
        // These should be kept in sync for the following classes:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        //

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            // Validate input parameters
            if (chars == null)
                throw new ArgumentNullException(nameof(chars), SR.ArgumentNull_Array);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input, return 0, avoid fixed empty array problem
            if (count == 0)
                return 0;

            // Just call the pointer version
            fixed (char* pChars = chars)
                return GetByteCount(pChars + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetByteCount(string s)
        {
            // Validate input
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            fixed (char* pChars = s)
                return GetByteCount(pChars, s.Length, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetByteCount(char* chars, int count)
        {
            // Validate Parameters
            if (chars == null)
                throw new ArgumentNullException(nameof(chars), SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            // Call it with empty encoder
            return GetByteCount(chars, count, null);
        }

        // Parent method is safe.
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        public override unsafe int GetBytes(string s, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            if (s == null || bytes == null)
                throw new ArgumentNullException(s == null ? nameof(s) : nameof(bytes), SR.ArgumentNull_Array);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (s.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(s), SR.ArgumentOutOfRange_IndexCount);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_Index);

            int byteCount = bytes.Length - byteIndex;

            fixed (char* pChars = s) fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, null);
        }

        // Encodes a range of characters in a character array into a range of bytes
        // in a byte array. An exception occurs if the byte array is not large
        // enough to hold the complete encoding of the characters. The
        // GetByteCount method can be used to determine the exact number of
        // bytes that will be produced for a given range of characters.
        // Alternatively, the GetMaxByteCount method can be used to
        // determine the maximum number of bytes that will be produced for a given
        // number of characters, regardless of the actual character values.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount,
                                               byte[] bytes, int byteIndex)
        {
            // Validate parameters
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? nameof(chars) : nameof(bytes), SR.ArgumentNull_Array);

            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException(charIndex < 0 ? nameof(charIndex) : nameof(charCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_Index);

            // If nothing to encode return 0, avoid fixed problem
            if (charCount == 0)
                return 0;

            // Just call pointer version
            int byteCount = bytes.Length - byteIndex;

            fixed (char* pChars = chars) fixed (byte* pBytes = &MemoryMarshal.GetReference((Span<byte>)bytes))
                // Remember that byteCount is # to decode, not size of array.
                return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars), SR.ArgumentNull_Array);

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(charCount < 0 ? nameof(charCount) : nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetBytes(chars, charCount, bytes, byteCount, null);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input just return 0, fixed doesn't like 0 length arrays
            if (count == 0)
                return 0;

            // Just call pointer version
            fixed (byte* pBytes = bytes)
                return GetCharCount(pBytes + index, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetCharCount(bytes, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars), SR.ArgumentNull_Array);

            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(byteIndex < 0 ? nameof(byteIndex) : nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ArgumentOutOfRange_Index);

            // If no input, return 0 & avoid fixed problem
            if (byteCount == 0)
                return 0;

            // Just call pointer version
            int charCount = chars.Length - charIndex;

            fixed (byte* pBytes = bytes) fixed (char* pChars = &MemoryMarshal.GetReference((Span<char>)chars))
                // Remember that charCount is # to decode, not size of array
                return GetChars(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding

        [CLSCompliant(false)]
        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            // Validate Parameters
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(chars), SR.ArgumentNull_Array);

            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException(charCount < 0 ? nameof(charCount) : nameof(byteCount), SR.ArgumentOutOfRange_NeedNonNegNum);

            return GetChars(bytes, byteCount, chars, charCount, null);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe string GetString(byte[] bytes, int index, int count)
        {
            // Validate Parameters
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), SR.ArgumentNull_Array);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // Avoid problems with empty input buffer
            if (count == 0) return string.Empty;

            fixed (byte* pBytes = bytes)
                return string.CreateStringFromEncoding(
                    pBytes + index, count, this);
        }

        //
        // End of standard methods copied from EncodingNLS.cs
        //
        internal sealed override unsafe int GetByteCount(char* chars, int count, EncoderNLS? encoder)
        {
            Debug.Assert(chars != null, "[UnicodeEncoding.GetByteCount]chars!=null");
            Debug.Assert(count >= 0, "[UnicodeEncoding.GetByteCount]count >=0");

            // Start by assuming each char gets 2 bytes
            int byteCount = count << 1;

            // Check for overflow in byteCount
            // (If they were all invalid chars, this would actually be wrong,
            // but that's a ridiculously large # so we're not concerned about that case)
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_GetByteCountOverflow);

            char* charStart = chars;
            char* charEnd = chars + count;
            char charLeftOver = (char)0;

            bool wasHereBefore = false;

            // For fallback we may need a fallback buffer
            EncoderFallbackBuffer? fallbackBuffer = null;
            char* charsForFallback;

            if (encoder != null)
            {
                charLeftOver = encoder._charLeftOver;

                // Assume extra bytes to encode charLeftOver if it existed
                if (charLeftOver > 0)
                    byteCount += 2;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0)
                        throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, this.EncodingName, encoder.Fallback?.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                }
            }

            char ch;
        TryAgain:

            while (((ch = (fallbackBuffer == null) ? (char)0 : fallbackBuffer.InternalGetNextChar()) != 0) || chars < charEnd)
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, maybe we can do it fast
#if FASTLOOP
                    // If endianess is backwards then each pair of bytes would be backwards.
                    if ((bigEndian ^ BitConverter.IsLittleEndian) &&
#if TARGET_64BIT
                        (unchecked((long)chars) & 7) == 0 &&
#else
                        (unchecked((int)chars) & 3) == 0 &&
#endif
                        charLeftOver == 0)
                    {
                        // Need -1 to check 2 at a time.  If we have an even #, longChars will go
                        // from longEnd - 1/2 long to longEnd + 1/2 long.  If we're odd, longChars
                        // will go from longEnd - 1 long to longEnd. (Might not get to use this)
                        ulong* longEnd = (ulong*)(charEnd - 3);

                        // Need new char* so we can check 4 at a time
                        ulong* longChars = (ulong*)chars;

                        while (longChars < longEnd)
                        {
                            // See if we potentially have surrogates (0x8000 bit set)
                            // (We're either big endian on a big endian machine or little endian on
                            // a little endian machine so that'll work)
                            if ((0x8000800080008000 & *longChars) != 0)
                            {
                                // See if any of these are high or low surrogates (0xd800 - 0xdfff).  If the high
                                // 5 bits looks like 11011, then its a high or low surrogate.
                                // We do the & f800 to filter the 5 bits, then ^ d800 to ensure the 0 isn't set.
                                // Note that we expect BMP characters to be more common than surrogates
                                // & each char with 11111... then ^ with 11011.  Zeroes then indicate surrogates
                                ulong uTemp = (0xf800f800f800f800 & *longChars) ^ 0xd800d800d800d800;

                                // Check each of the 4 chars.  0 for those 16 bits means it was a surrogate
                                // but no clue if they're high or low.
                                // If each of the 4 characters are non-zero, then none are surrogates.
                                if ((uTemp & 0xFFFF000000000000) == 0 ||
                                    (uTemp & 0x0000FFFF00000000) == 0 ||
                                    (uTemp & 0x00000000FFFF0000) == 0 ||
                                    (uTemp & 0x000000000000FFFF) == 0)
                                {
                                    // It has at least 1 surrogate, but we don't know if they're high or low surrogates,
                                    // or if there's 1 or 4 surrogates

                                    // If they happen to be high/low/high/low, we may as well continue.  Check the next
                                    // bit to see if its set (low) or not (high) in the right pattern
                                    if ((0xfc00fc00fc00fc00 & *longChars) !=
                                            (BitConverter.IsLittleEndian ? (ulong)0xdc00d800dc00d800 : (ulong)0xd800dc00d800dc00))
                                    {
                                        // Either there weren't 4 surrogates, or the 0x0400 bit was set when a high
                                        // was hoped for or the 0x0400 bit wasn't set where a low was hoped for.

                                        // Drop out to the slow loop to resolve the surrogates
                                        break;
                                    }
                                    // else they are all surrogates in High/Low/High/Low order, so we can use them.
                                }
                                // else none are surrogates, so we can use them.
                            }
                            // else all < 0x8000 so we can use them

                            // We already counted these four chars, go to next long.
                            longChars++;
                        }

                        chars = (char*)longChars;

                        if (chars >= charEnd)
                            break;
                    }
#endif // FASTLOOP

                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }
                else
                {
                    // We weren't preallocating fallback space.
                    byteCount += 2;
                }

                // Check for high or low surrogates
                if (ch >= 0xd800 && ch <= 0xdfff)
                {
                    // Was it a high surrogate?
                    if (ch <= 0xdbff)
                    {
                        // Its a high surrogate, if we already had a high surrogate do its fallback
                        if (charLeftOver > 0)
                        {
                            // Unwind the current character, this should be safe because we
                            // don't have leftover data in the fallback, so chars must have
                            // advanced already.
                            Debug.Assert(chars > charStart,
                                "[UnicodeEncoding.GetByteCount]Expected chars to have advanced in unexpected high surrogate");
                            chars--;

                            // If previous high surrogate deallocate 2 bytes
                            byteCount -= 2;

                            // Fallback the previous surrogate
                            // Need to initialize fallback buffer?
                            if (fallbackBuffer == null)
                            {
                                if (encoder == null)
                                    fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                                else
                                    fallbackBuffer = encoder.FallbackBuffer;

                                // Set our internal fallback interesting things.
                                fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                            }

                            charsForFallback = chars; // Avoid passing chars by reference to allow it to be enregistered
                            fallbackBuffer.InternalFallback(charLeftOver, ref charsForFallback);
                            chars = charsForFallback;

                            // Now no high surrogate left over
                            charLeftOver = (char)0;
                            continue;
                        }

                        // Remember this high surrogate
                        charLeftOver = ch;
                        continue;
                    }

                    // Its a low surrogate
                    if (charLeftOver == 0)
                    {
                        // Expected a previous high surrogate.
                        // Don't count this one (we'll count its fallback if necessary)
                        byteCount -= 2;

                        // fallback this one
                        // Need to initialize fallback buffer?
                        if (fallbackBuffer == null)
                        {
                            if (encoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = encoder.FallbackBuffer;

                            // Set our internal fallback interesting things.
                            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                        }
                        charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                        fallbackBuffer.InternalFallback(ch, ref charsForFallback);
                        chars = charsForFallback;
                        continue;
                    }

                    // Valid surrogate pair, add our charLeftOver
                    charLeftOver = (char)0;
                    continue;
                }
                else if (charLeftOver > 0)
                {
                    // Expected a low surrogate, but this char is normal

                    // Rewind the current character, fallback previous character.
                    // this should be safe because we don't have leftover data in the
                    // fallback, so chars must have advanced already.
                    Debug.Assert(chars > charStart,
                        "[UnicodeEncoding.GetByteCount]Expected chars to have advanced when expected low surrogate");
                    chars--;

                    // fallback previous chars
                    // Need to initialize fallback buffer?
                    if (fallbackBuffer == null)
                    {
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                    }
                    charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                    fallbackBuffer.InternalFallback(charLeftOver, ref charsForFallback);
                    chars = charsForFallback;

                    // Ignore charLeftOver or throw
                    byteCount -= 2;
                    charLeftOver = (char)0;

                    continue;
                }

                // Ok we had something to add (already counted)
            }

            // Don't allocate space for left over char
            if (charLeftOver > 0)
            {
                byteCount -= 2;

                // If we have to flush, stick it in fallback and try again
                if (encoder == null || encoder.MustFlush)
                {
                    if (wasHereBefore)
                    {
                        // Throw it, using our complete character
                        throw new ArgumentException(
                            SR.Format(SR.Argument_RecursiveFallback, charLeftOver), nameof(chars));
                    }
                    else
                    {
                        // Need to initialize fallback buffer?
                        if (fallbackBuffer == null)
                        {
                            if (encoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = encoder.FallbackBuffer;

                            // Set our internal fallback interesting things.
                            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                        }
                        charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                        fallbackBuffer.InternalFallback(charLeftOver, ref charsForFallback);
                        chars = charsForFallback;
                        charLeftOver = (char)0;
                        wasHereBefore = true;
                        goto TryAgain;
                    }
                }
            }

            // Shouldn't have anything in fallback buffer for GetByteCount
            // (don't have to check _throwOnOverflow for count)
            Debug.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[UnicodeEncoding.GetByteCount]Expected empty fallback buffer at end");

            // Don't remember fallbackBuffer.encoder for counting
            return byteCount;
        }

        internal sealed override unsafe int GetBytes(
            char* chars, int charCount, byte* bytes, int byteCount, EncoderNLS? encoder)
        {
            Debug.Assert(chars != null, "[UnicodeEncoding.GetBytes]chars!=null");
            Debug.Assert(byteCount >= 0, "[UnicodeEncoding.GetBytes]byteCount >=0");
            Debug.Assert(charCount >= 0, "[UnicodeEncoding.GetBytes]charCount >=0");
            Debug.Assert(bytes != null, "[UnicodeEncoding.GetBytes]bytes!=null");

            char charLeftOver = (char)0;
            char ch;
            bool wasHereBefore = false;

            byte* byteEnd = bytes + byteCount;
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            // For fallback we may need a fallback buffer
            EncoderFallbackBuffer? fallbackBuffer = null;
            char* charsForFallback;

            // Get our encoder, but don't clear it yet.
            if (encoder != null)
            {
                charLeftOver = encoder._charLeftOver;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    // We always need the fallback buffer in get bytes so we can flush any remaining ones if necessary
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder._throwOnOverflow)
                        throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, this.EncodingName, encoder.Fallback?.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                }
            }

        TryAgain:
            while (((ch = (fallbackBuffer == null) ?
                        (char)0 : fallbackBuffer.InternalGetNextChar()) != 0) ||
                    chars < charEnd)
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, maybe we can do it fast
#if FASTLOOP
                    // If endianess is backwards then each pair of bytes would be backwards.
                    if ((bigEndian ^ BitConverter.IsLittleEndian) &&
#if TARGET_64BIT
                        (unchecked((long)chars) & 7) == 0 &&
#else
                        (unchecked((int)chars) & 3) == 0 &&
#endif
                        charLeftOver == 0)
                    {
                        // Need -1 to check 2 at a time.  If we have an even #, longChars will go
                        // from longEnd - 1/2 long to longEnd + 1/2 long.  If we're odd, longChars
                        // will go from longEnd - 1 long to longEnd. (Might not get to use this)
                        // We can only go iCount units (limited by shorter of char or byte buffers.
                        ulong* longEnd = (ulong*)(chars - 3 +
                                                  (((byteEnd - bytes) >> 1 < charEnd - chars) ?
                                                    (byteEnd - bytes) >> 1 : charEnd - chars));

                        // Need new char* so we can check 4 at a time
                        ulong* longChars = (ulong*)chars;
                        ulong* longBytes = (ulong*)bytes;

                        while (longChars < longEnd)
                        {
                            // See if we potentially have surrogates (0x8000 bit set)
                            // (We're either big endian on a big endian machine or little endian on
                            // a little endian machine so that'll work)
                            if ((0x8000800080008000 & *longChars) != 0)
                            {
                                // See if any of these are high or low surrogates (0xd800 - 0xdfff).  If the high
                                // 5 bits looks like 11011, then its a high or low surrogate.
                                // We do the & f800 to filter the 5 bits, then ^ d800 to ensure the 0 isn't set.
                                // Note that we expect BMP characters to be more common than surrogates
                                // & each char with 11111... then ^ with 11011.  Zeroes then indicate surrogates
                                ulong uTemp = (0xf800f800f800f800 & *longChars) ^ 0xd800d800d800d800;

                                // Check each of the 4 chars.  0 for those 16 bits means it was a surrogate
                                // but no clue if they're high or low.
                                // If each of the 4 characters are non-zero, then none are surrogates.
                                if ((uTemp & 0xFFFF000000000000) == 0 ||
                                    (uTemp & 0x0000FFFF00000000) == 0 ||
                                    (uTemp & 0x00000000FFFF0000) == 0 ||
                                    (uTemp & 0x000000000000FFFF) == 0)
                                {
                                    // It has at least 1 surrogate, but we don't know if they're high or low surrogates,
                                    // or if there's 1 or 4 surrogates

                                    // If they happen to be high/low/high/low, we may as well continue.  Check the next
                                    // bit to see if its set (low) or not (high) in the right pattern
                                    if ((0xfc00fc00fc00fc00 & *longChars) !=
                                            (BitConverter.IsLittleEndian ? (ulong)0xdc00d800dc00d800 : (ulong)0xd800dc00d800dc00))
                                    {
                                        // Either there weren't 4 surrogates, or the 0x0400 bit was set when a high
                                        // was hoped for or the 0x0400 bit wasn't set where a low was hoped for.

                                        // Drop out to the slow loop to resolve the surrogates
                                        break;
                                    }
                                    // else they are all surrogates in High/Low/High/Low order, so we can use them.
                                }
                                // else none are surrogates, so we can use them.
                            }
                            // else all < 0x8000 so we can use them

                            // We can use these 4 chars.
                            Unsafe.WriteUnaligned<ulong>(longBytes, *longChars);
                            longChars++;
                            longBytes++;
                        }

                        chars = (char*)longChars;
                        bytes = (byte*)longBytes;

                        if (chars >= charEnd)
                            break;
                    }
#endif // FASTLOOP

                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // Check for high or low surrogates
                if (ch >= 0xd800 && ch <= 0xdfff)
                {
                    // Was it a high surrogate?
                    if (ch <= 0xdbff)
                    {
                        // Its a high surrogate, see if we already had a high surrogate
                        if (charLeftOver > 0)
                        {
                            // Unwind the current character, this should be safe because we
                            // don't have leftover data in the fallback, so chars must have
                            // advanced already.
                            Debug.Assert(chars > charStart,
                                "[UnicodeEncoding.GetBytes]Expected chars to have advanced in unexpected high surrogate");
                            chars--;

                            // Fallback the previous surrogate
                            // Might need to create our fallback buffer
                            if (fallbackBuffer == null)
                            {
                                if (encoder == null)
                                    fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                                else
                                    fallbackBuffer = encoder.FallbackBuffer;

                                // Set our internal fallback interesting things.
                                fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                            }

                            charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                            fallbackBuffer.InternalFallback(charLeftOver, ref charsForFallback);
                            chars = charsForFallback;

                            charLeftOver = (char)0;
                            continue;
                        }

                        // Remember this high surrogate
                        charLeftOver = ch;
                        continue;
                    }

                    // Its a low surrogate
                    if (charLeftOver == 0)
                    {
                        // We'll fall back this one
                        // Might need to create our fallback buffer
                        if (fallbackBuffer == null)
                        {
                            if (encoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = encoder.FallbackBuffer;

                            // Set our internal fallback interesting things.
                            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                        }

                        charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                        fallbackBuffer.InternalFallback(ch, ref charsForFallback);
                        chars = charsForFallback;
                        continue;
                    }

                    // Valid surrogate pair, add our charLeftOver
                    if (bytes + 3 >= byteEnd)
                    {
                        // Not enough room to add this surrogate pair
                        if (fallbackBuffer != null && fallbackBuffer.bFallingBack)
                        {
                            // These must have both been from the fallbacks.
                            // Both of these MUST have been from a fallback because if the 1st wasn't
                            // from a fallback, then a high surrogate followed by an illegal char
                            // would've caused the high surrogate to fall back.  If a high surrogate
                            // fell back, then it was consumed and both chars came from the fallback.
                            fallbackBuffer.MovePrevious();                     // Didn't use either fallback surrogate
                            fallbackBuffer.MovePrevious();
                        }
                        else
                        {
                            // If we don't have enough room, then either we should've advanced a while
                            // or we should have bytes==byteStart and throw below
                            Debug.Assert(chars > charStart + 1 || bytes == byteStart,
                                "[UnicodeEncoding.GetBytes]Expected chars to have when no room to add surrogate pair");
                            chars -= 2;                                        // Didn't use either surrogate
                        }
                        ThrowBytesOverflow(encoder, bytes == byteStart);    // Throw maybe (if no bytes written)
                        charLeftOver = (char)0;                             // we'll retry it later
                        break;                                               // Didn't throw, but stop 'til next time.
                    }

                    if (bigEndian)
                    {
                        *(bytes++) = (byte)(charLeftOver >> 8);
                        *(bytes++) = (byte)charLeftOver;
                    }
                    else
                    {
                        *(bytes++) = (byte)charLeftOver;
                        *(bytes++) = (byte)(charLeftOver >> 8);
                    }

                    charLeftOver = (char)0;
                }
                else if (charLeftOver > 0)
                {
                    // Expected a low surrogate, but this char is normal

                    // Rewind the current character, fallback previous character.
                    // this should be safe because we don't have leftover data in the
                    // fallback, so chars must have advanced already.
                    Debug.Assert(chars > charStart,
                        "[UnicodeEncoding.GetBytes]Expected chars to have advanced after expecting low surrogate");
                    chars--;

                    // fallback previous chars
                    // Might need to create our fallback buffer
                    if (fallbackBuffer == null)
                    {
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                    }

                    charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                    fallbackBuffer.InternalFallback(charLeftOver, ref charsForFallback);
                    chars = charsForFallback;

                    // Ignore charLeftOver or throw
                    charLeftOver = (char)0;
                    continue;
                }

                // Ok, we have a char to add
                if (bytes + 1 >= byteEnd)
                {
                    // Couldn't add this char
                    if (fallbackBuffer != null && fallbackBuffer.bFallingBack)
                        fallbackBuffer.MovePrevious();                     // Not using this fallback char
                    else
                    {
                        // Lonely charLeftOver (from previous call) would've been caught up above,
                        // so this must be a case where we've already read an input char.
                        Debug.Assert(chars > charStart,
                            "[UnicodeEncoding.GetBytes]Expected chars to have advanced for failed fallback");
                        chars--;                                         // Not using this char
                    }
                    ThrowBytesOverflow(encoder, bytes == byteStart);    // Throw maybe (if no bytes written)
                    break;                                               // didn't throw, just stop
                }

                if (bigEndian)
                {
                    *(bytes++) = (byte)(ch >> 8);
                    *(bytes++) = (byte)ch;
                }
                else
                {
                    *(bytes++) = (byte)ch;
                    *(bytes++) = (byte)(ch >> 8);
                }
            }

            // Don't allocate space for left over char
            if (charLeftOver > 0)
            {
                // If we aren't flushing we need to fall this back
                if (encoder == null || encoder.MustFlush)
                {
                    if (wasHereBefore)
                    {
                        // Throw it, using our complete character
                        throw new ArgumentException(
                            SR.Format(SR.Argument_RecursiveFallback, charLeftOver), nameof(chars));
                    }
                    else
                    {
                        // If we have to flush, stick it in fallback and try again
                        // Might need to create our fallback buffer
                        if (fallbackBuffer == null)
                        {
                            if (encoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = encoder.FallbackBuffer;

                            // Set our internal fallback interesting things.
                            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                        }

                        // If we're not flushing, that'll remember the left over character.
                        charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                        fallbackBuffer.InternalFallback(charLeftOver, ref charsForFallback);
                        chars = charsForFallback;

                        charLeftOver = (char)0;
                        wasHereBefore = true;
                        goto TryAgain;
                    }
                }
            }

            // Not flushing, remember it in the encoder
            if (encoder != null)
            {
                encoder._charLeftOver = charLeftOver;
                encoder._charsUsed = (int)(chars - charStart);
            }

            // Remember charLeftOver if we must, or clear it if we're flushing
            // (charLeftOver should be 0 if we're flushing)
            Debug.Assert((encoder != null && !encoder.MustFlush) || charLeftOver == (char)0,
                "[UnicodeEncoding.GetBytes] Expected no left over characters if flushing");

            Debug.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0 ||
                encoder == null || !encoder._throwOnOverflow,
                "[UnicodeEncoding.GetBytes]Expected empty fallback buffer if not converting");

            return (int)(bytes - byteStart);
        }

        internal sealed override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS? baseDecoder)
        {
            Debug.Assert(bytes != null, "[UnicodeEncoding.GetCharCount]bytes!=null");
            Debug.Assert(count >= 0, "[UnicodeEncoding.GetCharCount]count >=0");

            UnicodeEncoding.Decoder? decoder = (UnicodeEncoding.Decoder?)baseDecoder;

            byte* byteEnd = bytes + count;
            byte* byteStart = bytes;

            // Need last vars
            int lastByte = -1;
            char lastChar = (char)0;

            // Start by assuming same # of chars as bytes
            int charCount = count >> 1;

            // For fallback we may need a fallback buffer
            DecoderFallbackBuffer? fallbackBuffer = null;

            if (decoder != null)
            {
                lastByte = decoder.lastByte;
                lastChar = decoder.lastChar;

                // Assume extra char if last char was around
                if (lastChar > 0)
                    charCount++;

                // Assume extra char if extra last byte makes up odd # of input bytes
                if (lastByte >= 0 && (count & 1) == 1)
                {
                    charCount++;
                }

                // Shouldn't have anything in fallback buffer for GetCharCount
                // (don't have to check _throwOnOverflow for count)
                Debug.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0,
                    "[UnicodeEncoding.GetCharCount]Expected empty fallback buffer at start");
            }

            while (bytes < byteEnd)
            {
                // If we're aligned then maybe we can do it fast
                // That'll hurt if we're unaligned because we'll always test but never be aligned
#if FASTLOOP
                if ((bigEndian ^ BitConverter.IsLittleEndian) &&
#if TARGET_64BIT
                    (unchecked((long)bytes) & 7) == 0 &&
#else
                    (unchecked((int)bytes) & 3) == 0 &&
#endif // TARGET_64BIT
                    lastByte == -1 && lastChar == 0)
                {
                    // Need -1 to check 2 at a time.  If we have an even #, longBytes will go
                    // from longEnd - 1/2 long to longEnd + 1/2 long.  If we're odd, longBytes
                    // will go from longEnd - 1 long to longEnd. (Might not get to use this)
                    ulong* longEnd = (ulong*)(byteEnd - 7);

                    // Need new char* so we can check 4 at a time
                    ulong* longBytes = (ulong*)bytes;

                    while (longBytes < longEnd)
                    {
                        // See if we potentially have surrogates (0x8000 bit set)
                        // (We're either big endian on a big endian machine or little endian on
                        // a little endian machine so that'll work)
                        if ((0x8000800080008000 & *longBytes) != 0)
                        {
                            // See if any of these are high or low surrogates (0xd800 - 0xdfff).  If the high
                            // 5 bits looks like 11011, then its a high or low surrogate.
                            // We do the & f800 to filter the 5 bits, then ^ d800 to ensure the 0 isn't set.
                            // Note that we expect BMP characters to be more common than surrogates
                            // & each char with 11111... then ^ with 11011.  Zeroes then indicate surrogates
                            ulong uTemp = (0xf800f800f800f800 & *longBytes) ^ 0xd800d800d800d800;

                            // Check each of the 4 chars.  0 for those 16 bits means it was a surrogate
                            // but no clue if they're high or low.
                            // If each of the 4 characters are non-zero, then none are surrogates.
                            if ((uTemp & 0xFFFF000000000000) == 0 ||
                                (uTemp & 0x0000FFFF00000000) == 0 ||
                                (uTemp & 0x00000000FFFF0000) == 0 ||
                                (uTemp & 0x000000000000FFFF) == 0)
                            {
                                // It has at least 1 surrogate, but we don't know if they're high or low surrogates,
                                // or if there's 1 or 4 surrogates

                                // If they happen to be high/low/high/low, we may as well continue.  Check the next
                                // bit to see if its set (low) or not (high) in the right pattern
                                if ((0xfc00fc00fc00fc00 & *longBytes) !=
                                        (BitConverter.IsLittleEndian ? (ulong)0xdc00d800dc00d800 : (ulong)0xd800dc00d800dc00))
                                {
                                    // Either there weren't 4 surrogates, or the 0x0400 bit was set when a high
                                    // was hoped for or the 0x0400 bit wasn't set where a low was hoped for.

                                    // Drop out to the slow loop to resolve the surrogates
                                    break;
                                }
                                // else they are all surrogates in High/Low/High/Low order, so we can use them.
                            }
                            // else none are surrogates, so we can use them.
                        }
                        // else all < 0x8000 so we can use them

                        // We can use these 4 chars.
                        longBytes++;
                    }

                    bytes = (byte*)longBytes;

                    if (bytes >= byteEnd)
                        break;
                }
#endif // FASTLOOP

                // Get 1st byte
                if (lastByte < 0)
                {
                    lastByte = *bytes++;
                    if (bytes >= byteEnd) break;
                }

                // Get full char
                char ch;
                if (bigEndian)
                {
                    ch = (char)(lastByte << 8 | *(bytes++));
                }
                else
                {
                    ch = (char)(*(bytes++) << 8 | lastByte);
                }
                lastByte = -1;

                // See if the char's valid
                if (ch >= 0xd800 && ch <= 0xdfff)
                {
                    // Was it a high surrogate?
                    if (ch <= 0xdbff)
                    {
                        // Its a high surrogate, if we had one then do fallback for previous one
                        if (lastChar > 0)
                        {
                            // Ignore previous bad high surrogate
                            charCount--;

                            // Get fallback for previous high surrogate
                            // Note we have to reconstruct bytes because some may have been in decoder
                            byte[]? byteBuffer = null;
                            if (bigEndian)
                            {
                                byteBuffer = new byte[]
                                    { unchecked((byte)(lastChar >> 8)), unchecked((byte)lastChar) };
                            }
                            else
                            {
                                byteBuffer = new byte[]
                                    { unchecked((byte)lastChar), unchecked((byte)(lastChar >> 8)) };
                            }

                            if (fallbackBuffer == null)
                            {
                                if (decoder == null)
                                    fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                                else
                                    fallbackBuffer = decoder.FallbackBuffer;

                                // Set our internal fallback interesting things.
                                fallbackBuffer.InternalInitialize(byteStart, null);
                            }

                            // Get fallback.
                            charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                        }

                        // Ignore the last one which fell back already,
                        // and remember the new high surrogate
                        lastChar = ch;
                        continue;
                    }

                    // Its a low surrogate
                    if (lastChar == 0)
                    {
                        // Expected a previous high surrogate
                        charCount--;

                        // Get fallback for this low surrogate
                        // Note we have to reconstruct bytes because some may have been in decoder
                        byte[]? byteBuffer = null;
                        if (bigEndian)
                        {
                            byteBuffer = new byte[]
                                { unchecked((byte)(ch >> 8)), unchecked((byte)ch) };
                        }
                        else
                        {
                            byteBuffer = new byte[]
                                { unchecked((byte)ch), unchecked((byte)(ch >> 8)) };
                        }

                        if (fallbackBuffer == null)
                        {
                            if (decoder == null)
                                fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = decoder.FallbackBuffer;

                            // Set our internal fallback interesting things.
                            fallbackBuffer.InternalInitialize(byteStart, null);
                        }

                        charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);

                        // Ignore this one (we already did its fallback)
                        continue;
                    }

                    // Valid surrogate pair, already counted.
                    lastChar = (char)0;
                }
                else if (lastChar > 0)
                {
                    // Had a high surrogate, expected a low surrogate
                    // Un-count the last high surrogate
                    charCount--;

                    // fall back the high surrogate.
                    byte[]? byteBuffer = null;
                    if (bigEndian)
                    {
                        byteBuffer = new byte[]
                            { unchecked((byte)(lastChar >> 8)), unchecked((byte)lastChar) };
                    }
                    else
                    {
                        byteBuffer = new byte[]
                            { unchecked((byte)lastChar), unchecked((byte)(lastChar >> 8)) };
                    }

                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(byteStart, null);
                    }

                    // Already subtracted high surrogate
                    charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);

                    // Not left over now, clear previous high surrogate and continue to add current char
                    lastChar = (char)0;
                }

                // Valid char, already counted
            }

            // Extra space if we can't use decoder
            if (decoder == null || decoder.MustFlush)
            {
                if (lastChar > 0)
                {
                    // No hanging high surrogates allowed, do fallback and remove count for it
                    charCount--;
                    byte[]? byteBuffer = null;
                    if (bigEndian)
                    {
                        byteBuffer = new byte[]
                            { unchecked((byte)(lastChar >> 8)), unchecked((byte)lastChar) };
                    }
                    else
                    {
                        byteBuffer = new byte[]
                            { unchecked((byte)lastChar), unchecked((byte)(lastChar >> 8)) };
                    }

                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(byteStart, null);
                    }

                    charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);

                    lastChar = (char)0;
                }

                if (lastByte >= 0)
                {
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(byteStart, null);
                    }

                    // No hanging odd bytes allowed if must flush
                    charCount += fallbackBuffer.InternalFallback(new byte[] { unchecked((byte)lastByte) }, bytes);
                    lastByte = -1;
                }
            }

            // If we had a high surrogate left over, we can't count it
            if (lastChar > 0)
                charCount--;

            // Shouldn't have anything in fallback buffer for GetCharCount
            // (don't have to check _throwOnOverflow for count)
            Debug.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[UnicodeEncoding.GetCharCount]Expected empty fallback buffer at end");

            return charCount;
        }

        internal sealed override unsafe int GetChars(
            byte* bytes, int byteCount, char* chars, int charCount, DecoderNLS? baseDecoder)
        {
            Debug.Assert(chars != null, "[UnicodeEncoding.GetChars]chars!=null");
            Debug.Assert(byteCount >= 0, "[UnicodeEncoding.GetChars]byteCount >=0");
            Debug.Assert(charCount >= 0, "[UnicodeEncoding.GetChars]charCount >=0");
            Debug.Assert(bytes != null, "[UnicodeEncoding.GetChars]bytes!=null");

            UnicodeEncoding.Decoder? decoder = (UnicodeEncoding.Decoder?)baseDecoder;

            // Need last vars
            int lastByte = -1;
            char lastChar = (char)0;

            // Get our decoder (but don't clear it yet)
            if (decoder != null)
            {
                lastByte = decoder.lastByte;
                lastChar = decoder.lastChar;

                // Shouldn't have anything in fallback buffer for GetChars
                // (don't have to check _throwOnOverflow for chars)
                Debug.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0,
                    "[UnicodeEncoding.GetChars]Expected empty fallback buffer at start");
            }

            // For fallback we may need a fallback buffer
            DecoderFallbackBuffer? fallbackBuffer = null;
            char* charsForFallback;

            byte* byteEnd = bytes + byteCount;
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            while (bytes < byteEnd)
            {
                // If we're aligned then maybe we can do it fast
                // That'll hurt if we're unaligned because we'll always test but never be aligned
#if FASTLOOP
                if ((bigEndian ^ BitConverter.IsLittleEndian) &&
#if TARGET_64BIT
                    (unchecked((long)chars) & 7) == 0 &&
#else
                    (unchecked((int)chars) & 3) == 0 &&
#endif
                    lastByte == -1 && lastChar == 0)
                {
                    // Need -1 to check 2 at a time.  If we have an even #, longChars will go
                    // from longEnd - 1/2 long to longEnd + 1/2 long.  If we're odd, longChars
                    // will go from longEnd - 1 long to longEnd. (Might not get to use this)
                    // We can only go iCount units (limited by shorter of char or byte buffers.
                    ulong* longEnd = (ulong*)(bytes - 7 +
                                                (((byteEnd - bytes) >> 1 < charEnd - chars) ?
                                                  (byteEnd - bytes) : (charEnd - chars) << 1));

                    // Need new char* so we can check 4 at a time
                    ulong* longBytes = (ulong*)bytes;
                    ulong* longChars = (ulong*)chars;

                    while (longBytes < longEnd)
                    {
                        // See if we potentially have surrogates (0x8000 bit set)
                        // (We're either big endian on a big endian machine or little endian on
                        // a little endian machine so that'll work)
                        if ((0x8000800080008000 & *longBytes) != 0)
                        {
                            // See if any of these are high or low surrogates (0xd800 - 0xdfff).  If the high
                            // 5 bits looks like 11011, then its a high or low surrogate.
                            // We do the & f800 to filter the 5 bits, then ^ d800 to ensure the 0 isn't set.
                            // Note that we expect BMP characters to be more common than surrogates
                            // & each char with 11111... then ^ with 11011.  Zeroes then indicate surrogates
                            ulong uTemp = (0xf800f800f800f800 & *longBytes) ^ 0xd800d800d800d800;

                            // Check each of the 4 chars.  0 for those 16 bits means it was a surrogate
                            // but no clue if they're high or low.
                            // If each of the 4 characters are non-zero, then none are surrogates.
                            if ((uTemp & 0xFFFF000000000000) == 0 ||
                                (uTemp & 0x0000FFFF00000000) == 0 ||
                                (uTemp & 0x00000000FFFF0000) == 0 ||
                                (uTemp & 0x000000000000FFFF) == 0)
                            {
                                // It has at least 1 surrogate, but we don't know if they're high or low surrogates,
                                // or if there's 1 or 4 surrogates

                                // If they happen to be high/low/high/low, we may as well continue.  Check the next
                                // bit to see if its set (low) or not (high) in the right pattern
                                if ((0xfc00fc00fc00fc00 & *longBytes) !=
                                        (BitConverter.IsLittleEndian ? (ulong)0xdc00d800dc00d800 : (ulong)0xd800dc00d800dc00))
                                {
                                    // Either there weren't 4 surrogates, or the 0x0400 bit was set when a high
                                    // was hoped for or the 0x0400 bit wasn't set where a low was hoped for.

                                    // Drop out to the slow loop to resolve the surrogates
                                    break;
                                }
                                // else they are all surrogates in High/Low/High/Low order, so we can use them.
                            }
                            // else none are surrogates, so we can use them.
                        }
                        // else all < 0x8000 so we can use them

                        // We can use these 4 chars.
                        Unsafe.WriteUnaligned<ulong>(longChars, *longBytes);
                        longBytes++;
                        longChars++;
                    }

                    chars = (char*)longChars;
                    bytes = (byte*)longBytes;

                    if (bytes >= byteEnd)
                        break;
                }
#endif // FASTLOOP

                // Get 1st byte
                if (lastByte < 0)
                {
                    lastByte = *bytes++;
                    continue;
                }

                // Get full char
                char ch;
                if (bigEndian)
                {
                    ch = (char)(lastByte << 8 | *(bytes++));
                }
                else
                {
                    ch = (char)(*(bytes++) << 8 | lastByte);
                }
                lastByte = -1;

                // See if the char's valid
                if (ch >= 0xd800 && ch <= 0xdfff)
                {
                    // Was it a high surrogate?
                    if (ch <= 0xdbff)
                    {
                        // Its a high surrogate, if we had one then do fallback for previous one
                        if (lastChar > 0)
                        {
                            // Get fallback for previous high surrogate
                            // Note we have to reconstruct bytes because some may have been in decoder
                            byte[]? byteBuffer = null;
                            if (bigEndian)
                            {
                                byteBuffer = new byte[]
                                    { unchecked((byte)(lastChar >> 8)), unchecked((byte)lastChar) };
                            }
                            else
                            {
                                byteBuffer = new byte[]
                                    { unchecked((byte)lastChar), unchecked((byte)(lastChar >> 8)) };
                            }

                            if (fallbackBuffer == null)
                            {
                                if (decoder == null)
                                    fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                                else
                                    fallbackBuffer = decoder.FallbackBuffer;

                                // Set our internal fallback interesting things.
                                fallbackBuffer.InternalInitialize(byteStart, charEnd);
                            }

                            charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                            bool fallbackResult = fallbackBuffer.InternalFallback(byteBuffer, bytes, ref charsForFallback);
                            chars = charsForFallback;

                            if (!fallbackResult)
                            {
                                // couldn't fall back lonely surrogate
                                // We either advanced bytes or chars should == charStart and throw below
                                Debug.Assert(bytes >= byteStart + 2 || chars == charStart,
                                    "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (bad surrogate)");
                                bytes -= 2;                                       // didn't use these 2 bytes
                                fallbackBuffer.InternalReset();
                                ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                                break;                                           // couldn't fallback but didn't throw
                            }
                        }

                        // Ignore the previous high surrogate which fell back already,
                        // yet remember the current high surrogate for next time.
                        lastChar = ch;
                        continue;
                    }

                    // Its a low surrogate
                    if (lastChar == 0)
                    {
                        // Expected a previous high surrogate
                        // Get fallback for this low surrogate
                        // Note we have to reconstruct bytes because some may have been in decoder
                        byte[]? byteBuffer = null;
                        if (bigEndian)
                        {
                            byteBuffer = new byte[]
                                { unchecked((byte)(ch >> 8)), unchecked((byte)ch) };
                        }
                        else
                        {
                            byteBuffer = new byte[]
                                { unchecked((byte)ch), unchecked((byte)(ch >> 8)) };
                        }

                        if (fallbackBuffer == null)
                        {
                            if (decoder == null)
                                fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = decoder.FallbackBuffer;

                            // Set our internal fallback interesting things.
                            fallbackBuffer.InternalInitialize(byteStart, charEnd);
                        }

                        charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                        bool fallbackResult = fallbackBuffer.InternalFallback(byteBuffer, bytes, ref charsForFallback);
                        chars = charsForFallback;

                        if (!fallbackResult)
                        {
                            // couldn't fall back lonely surrogate
                            // We either advanced bytes or chars should == charStart and throw below
                            Debug.Assert(bytes >= byteStart + 2 || chars == charStart,
                                "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (lonely surrogate)");
                            bytes -= 2;                                       // didn't use these 2 bytes
                            fallbackBuffer.InternalReset();
                            ThrowCharsOverflow(decoder, chars == charStart);  // Might throw, if no chars output
                            break;                                            // couldn't fallback but didn't throw
                        }

                        // Didn't throw, ignore this one (we already did its fallback)
                        continue;
                    }

                    // Valid surrogate pair, add our lastChar (will need 2 chars)
                    if (chars >= charEnd - 1)
                    {
                        // couldn't find room for this surrogate pair
                        // We either advanced bytes or chars should == charStart and throw below
                        Debug.Assert(bytes >= byteStart + 2 || chars == charStart,
                            "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (surrogate pair)");
                        bytes -= 2;                                       // didn't use these 2 bytes
                        ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                        // Leave lastChar for next call to Convert()
                        break;                                           // couldn't fallback but didn't throw
                    }

                    *chars++ = lastChar;
                    lastChar = (char)0;
                }
                else if (lastChar > 0)
                {
                    // Had a high surrogate, expected a low surrogate, fall back the high surrogate.
                    byte[]? byteBuffer = null;
                    if (bigEndian)
                    {
                        byteBuffer = new byte[]
                            { unchecked((byte)(lastChar >> 8)), unchecked((byte)lastChar) };
                    }
                    else
                    {
                        byteBuffer = new byte[]
                            { unchecked((byte)lastChar), unchecked((byte)(lastChar >> 8)) };
                    }

                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(byteStart, charEnd);
                    }

                    charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                    bool fallbackResult = fallbackBuffer.InternalFallback(byteBuffer, bytes, ref charsForFallback);
                    chars = charsForFallback;

                    if (!fallbackResult)
                    {
                        // couldn't fall back high surrogate, or char that would be next
                        // We either advanced bytes or chars should == charStart and throw below
                        Debug.Assert(bytes >= byteStart + 2 || chars == charStart,
                            "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (no low surrogate)");
                        bytes -= 2;                                       // didn't use these 2 bytes
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                        break;                                           // couldn't fallback but didn't throw
                    }

                    // Not left over now, clear previous high surrogate and continue to add current char
                    lastChar = (char)0;
                }

                // Valid char, room for it?
                if (chars >= charEnd)
                {
                    // 2 bytes couldn't fall back
                    // We either advanced bytes or chars should == charStart and throw below
                    Debug.Assert(bytes >= byteStart + 2 || chars == charStart,
                        "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (normal)");
                    bytes -= 2;                                       // didn't use these bytes
                    ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                    break;                                           // couldn't fallback but didn't throw
                }

                // add it
                *chars++ = ch;
            }

            // Remember our decoder if we must
            if (decoder == null || decoder.MustFlush)
            {
                if (lastChar > 0)
                {
                    // No hanging high surrogates allowed, do fallback and remove count for it
                    byte[]? byteBuffer = null;
                    if (bigEndian)
                    {
                        byteBuffer = new byte[]
                            { unchecked((byte)(lastChar >> 8)), unchecked((byte)lastChar) };
                    }
                    else
                    {
                        byteBuffer = new byte[]
                            { unchecked((byte)lastChar), unchecked((byte)(lastChar >> 8)) };
                    }

                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(byteStart, charEnd);
                    }

                    charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                    bool fallbackResult = fallbackBuffer.InternalFallback(byteBuffer, bytes, ref charsForFallback);
                    chars = charsForFallback;

                    if (!fallbackResult)
                    {
                        // 2 bytes couldn't fall back
                        // We either advanced bytes or chars should == charStart and throw below
                        Debug.Assert(bytes >= byteStart + 2 || chars == charStart,
                            "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (decoder)");
                        bytes -= 2;                                       // didn't use these bytes
                        if (lastByte >= 0)
                            bytes--;                                    // had an extra last byte hanging around
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                        // We'll remember these in our decoder though
                        bytes += 2;
                        if (lastByte >= 0)
                            bytes++;
                        goto End;
                    }

                    // done with this one
                    lastChar = (char)0;
                }

                if (lastByte >= 0)
                {
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;

                        // Set our internal fallback interesting things.
                        fallbackBuffer.InternalInitialize(byteStart, charEnd);
                    }

                    // No hanging odd bytes allowed if must flush
                    charsForFallback = chars; // Avoid passing chars by reference to allow it to be en-registered
                    bool fallbackResult = fallbackBuffer.InternalFallback(new byte[] { unchecked((byte)lastByte) }, bytes, ref charsForFallback);
                    chars = charsForFallback;

                    if (!fallbackResult)
                    {
                        // odd byte couldn't fall back
                        bytes--;                                        // didn't use this byte
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                        // didn't throw, but we'll remember it in the decoder
                        bytes++;
                        goto End;
                    }

                    // Didn't fail, clear buffer
                    lastByte = -1;
                }
            }

        End:

            // Remember our decoder if we must
            if (decoder != null)
            {
                Debug.Assert(!decoder.MustFlush || ((lastChar == (char)0) && (lastByte == -1)),
                    "[UnicodeEncoding.GetChars] Expected no left over chars or bytes if flushing");

                decoder._bytesUsed = (int)(bytes - byteStart);
                decoder.lastChar = lastChar;
                decoder.lastByte = lastByte;
            }

            // Shouldn't have anything in fallback buffer for GetChars
            // (don't have to check _throwOnOverflow for count or chars)
            Debug.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[UnicodeEncoding.GetChars]Expected empty fallback buffer at end");

            return (int)(chars - charStart);
        }

        public override System.Text.Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }

        public override System.Text.Decoder GetDecoder()
        {
            return new UnicodeEncoding.Decoder(this);
        }

        public override byte[] GetPreamble()
        {
            if (byteOrderMark)
            {
                // Note - we must allocate new byte[]'s here to prevent someone
                // from modifying a cached byte[].
                if (bigEndian)
                    return new byte[2] { 0xfe, 0xff };
                else
                    return new byte[2] { 0xff, 0xfe };
            }
            return Array.Empty<byte>();
        }

        public override ReadOnlySpan<byte> Preamble =>
            GetType() != typeof(UnicodeEncoding) ? new ReadOnlySpan<byte>(GetPreamble()) : // in case a derived UnicodeEncoding overrode GetPreamble
            !byteOrderMark ? default :
            bigEndian ? (ReadOnlySpan<byte>)new byte[2] { 0xfe, 0xff } : // uses C# compiler's optimization for static byte[] data
            (ReadOnlySpan<byte>)new byte[2] { 0xff, 0xfe };

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // Characters would be # of characters + 1 in case left over high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // 2 bytes per char
            byteCount <<= 1;

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);

            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount),
                     SR.ArgumentOutOfRange_NeedNonNegNum);

            // long because byteCount could be biggest int.
            // 1 char per 2 bytes.  Round up in case 1 left over in decoder.
            // Round up using &1 in case byteCount is max size
            // Might also need an extra 1 if there's a left over high surrogate in the decoder.
            long charCount = (long)(byteCount >> 1) + (byteCount & 1) + 1;

            // Don't forget fallback (in case they have a bunch of lonely surrogates or something bizarre like that)
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow);

            return (int)charCount;
        }

        public override bool Equals([NotNullWhen(true)] object? value)
        {
            if (value is UnicodeEncoding that)
            {
                //
                // Big Endian Unicode has different code page (1201) than small Endian one (1200),
                // so we still have to check _codePage here.
                //
                return (CodePage == that.CodePage) &&
                        byteOrderMark == that.byteOrderMark &&
                        // isThrowException == that.isThrowException &&  // Same as Encoder/Decoder being exception fallbacks
                        bigEndian == that.bigEndian &&
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return CodePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() +
                   (byteOrderMark ? 4 : 0) + (bigEndian ? 8 : 0);
        }

        private sealed class Decoder : System.Text.DecoderNLS
        {
            internal int lastByte = -1;
            internal char lastChar;

            public Decoder(UnicodeEncoding encoding) : base(encoding)
            {
                // base calls reset
            }

            public override void Reset()
            {
                lastByte = -1;
                lastChar = '\0';
                if (_fallbackBuffer != null)
                    _fallbackBuffer.Reset();
            }

            // Anything left in our decoder?
            internal override bool HasState => this.lastByte != -1 || this.lastChar != '\0';
        }
    }
}
