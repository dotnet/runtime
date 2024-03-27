// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Don't override IsAlwaysNormalized because it is just a Unicode Transformation and could be confused.
//

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Text
{
    // Encodes text into and out of UTF-32.  UTF-32 is a way of writing
    // Unicode characters with a single storage unit (32 bits) per character,
    //
    // The UTF-32 byte order mark is simply the Unicode byte order mark
    // (0x00FEFF) written in UTF-32 (0x0000FEFF or 0xFFFE0000).  The byte order
    // mark is used mostly to distinguish UTF-32 text from other encodings, and doesn't
    // switch the byte orderings.

    public sealed class UTF32Encoding : Encoding
    {
        /*
            words   bits    UTF-32 representation
            -----   ----    -----------------------------------
            1       16      00000000 00000000 xxxxxxxx xxxxxxxx
            2       21      00000000 000xxxxx hhhhhhll llllllll
            -----   ----    -----------------------------------

            Surrogate:
            Real Unicode value = (HighSurrogate - 0xD800) * 0x400 + (LowSurrogate - 0xDC00) + 0x10000
        */

        // Used by Encoding.UTF32/BigEndianUTF32 for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly UTF32Encoding s_default = new UTF32Encoding(bigEndian: false, byteOrderMark: true);
        internal static readonly UTF32Encoding s_bigEndianDefault = new UTF32Encoding(bigEndian: true, byteOrderMark: true);

        private readonly bool _emitUTF32ByteOrderMark;
        private readonly bool _isThrowException;
        private readonly bool _bigEndian;

        public UTF32Encoding() : this(false, true)
        {
        }

        public UTF32Encoding(bool bigEndian, bool byteOrderMark) :
            base(bigEndian ? 12001 : 12000)
        {
            _bigEndian = bigEndian;
            _emitUTF32ByteOrderMark = byteOrderMark;
        }

        public UTF32Encoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidCharacters) :
            this(bigEndian, byteOrderMark)
        {
            _isThrowException = throwOnInvalidCharacters;

            // Encoding constructor already did this, but it'll be wrong if we're throwing exceptions
            if (_isThrowException)
                SetDefaultFallbacks();
        }

        internal override void SetDefaultFallbacks()
        {
            // For UTF-X encodings, we use a replacement fallback with an empty string
            if (_isThrowException)
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

        // Returns the number of bytes required to encode a range of characters in
        // a character array.
        //
        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

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
            if (s is null)
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
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(count);

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
            ArgumentNullException.ThrowIfNull(s);
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);

            if (s.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(s), SR.ArgumentOutOfRange_IndexCount);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

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
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);

            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException(nameof(chars), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

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
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(charCount);
            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

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
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            // If no input just return 0, fixed doesn't like 0 length arrays.
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
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(count);

            return GetCharCount(bytes, count, null);
        }

        // All of our public Encodings that don't use EncodingNLS must have this (including EncodingNLS)
        // So if you fix this, fix the others.  Currently those include:
        // EncodingNLS, UTF7Encoding, UTF8Encoding, UTF32Encoding, ASCIIEncoding, UnicodeEncoding
        // parent method is safe

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                            char[] chars, int charIndex)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(byteIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException(nameof(bytes), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);

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
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(charCount);
            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

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
            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

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
        internal override unsafe int GetByteCount(char* chars, int count, EncoderNLS? encoder)
        {
            Debug.Assert(chars is not null, "[UTF32Encoding.GetByteCount]chars!=null");
            Debug.Assert(count >= 0, "[UTF32Encoding.GetByteCount]count >=0");

            char* end = chars + count;
            char* charStart = chars;
            int byteCount = 0;

            char highSurrogate = '\0';

            // For fallback we may need a fallback buffer
            EncoderFallbackBuffer? fallbackBuffer = null;
            char* charsForFallback;

            if (encoder is not null)
            {
                highSurrogate = encoder._charLeftOver;
                fallbackBuffer = encoder.FallbackBuffer;

                // We mustn't have left over fallback data when counting
                if (fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, this.EncodingName, encoder.Fallback?.GetType().ToString() ?? string.Empty));
            }
            else
            {
                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
            }

            // Set our internal fallback interesting things.
            fallbackBuffer.InternalInitialize(charStart, end, encoder, false);

            char ch;
        TryAgain:

            while (((ch = fallbackBuffer.InternalGetNextChar()) != 0) || chars < end)
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // Do we need a low surrogate?
                if (highSurrogate != '\0')
                {
                    //
                    // In previous char, we encounter a high surrogate, so we are expecting a low surrogate here.
                    //
                    if (char.IsLowSurrogate(ch))
                    {
                        // They're all legal
                        highSurrogate = '\0';

                        //
                        // One surrogate pair will be translated into 4 bytes UTF32.
                        //

                        byteCount += 4;
                        continue;
                    }

                    // We are missing our low surrogate, decrement chars and fallback the high surrogate
                    // The high surrogate may have come from the encoder, but nothing else did.
                    Debug.Assert(chars > charStart,
                        "[UTF32Encoding.GetByteCount]Expected chars to have advanced if no low surrogate");
                    chars--;

                    // Do the fallback
                    charsForFallback = chars;
                    fallbackBuffer.InternalFallback(highSurrogate, ref charsForFallback);
                    chars = charsForFallback;

                    // We're going to fallback the old high surrogate.
                    highSurrogate = '\0';
                    continue;
                }

                // Do we have another high surrogate?
                if (char.IsHighSurrogate(ch))
                {
                    //
                    // We'll have a high surrogate to check next time.
                    //
                    highSurrogate = ch;
                    continue;
                }

                // Check for illegal characters
                if (char.IsLowSurrogate(ch))
                {
                    // We have a leading low surrogate, do the fallback
                    charsForFallback = chars;
                    fallbackBuffer.InternalFallback(ch, ref charsForFallback);
                    chars = charsForFallback;

                    // Try again with fallback buffer
                    continue;
                }

                // We get to add the character (4 bytes UTF32)
                byteCount += 4;
            }

            // May have to do our last surrogate
            if ((encoder is null || encoder.MustFlush) && highSurrogate > 0)
            {
                // We have to do the fallback for the lonely high surrogate
                charsForFallback = chars;
                fallbackBuffer.InternalFallback(highSurrogate, ref charsForFallback);
                chars = charsForFallback;

                highSurrogate = (char)0;
                goto TryAgain;
            }

            // Check for overflows.
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_GetByteCountOverflow);

            // Shouldn't have anything in fallback buffer for GetByteCount
            // (don't have to check _throwOnOverflow for count)
            Debug.Assert(fallbackBuffer.Remaining == 0,
                "[UTF32Encoding.GetByteCount]Expected empty fallback buffer at end");

            // Return our count
            return byteCount;
        }

        internal override unsafe int GetBytes(char* chars, int charCount,
                                                 byte* bytes, int byteCount, EncoderNLS? encoder)
        {
            Debug.Assert(chars is not null, "[UTF32Encoding.GetBytes]chars!=null");
            Debug.Assert(bytes is not null, "[UTF32Encoding.GetBytes]bytes!=null");
            Debug.Assert(byteCount >= 0, "[UTF32Encoding.GetBytes]byteCount >=0");
            Debug.Assert(charCount >= 0, "[UTF32Encoding.GetBytes]charCount >=0");

            char* charStart = chars;
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            byte* byteEnd = bytes + byteCount;

            char highSurrogate = '\0';

            // For fallback we may need a fallback buffer
            EncoderFallbackBuffer? fallbackBuffer = null;
            char* charsForFallback;

            if (encoder is not null)
            {
                highSurrogate = encoder._charLeftOver;
                fallbackBuffer = encoder.FallbackBuffer;

                // We mustn't have left over fallback data when not converting
                if (encoder._throwOnOverflow && fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, this.EncodingName, encoder.Fallback?.GetType()));
            }
            else
            {
                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
            }

            // Set our internal fallback interesting things.
            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);

            char ch;
        TryAgain:

            while (((ch = fallbackBuffer.InternalGetNextChar()) != 0) || chars < charEnd)
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }

                // Do we need a low surrogate?
                if (highSurrogate != '\0')
                {
                    //
                    // In previous char, we encountered a high surrogate, so we are expecting a low surrogate here.
                    //
                    if (char.IsLowSurrogate(ch))
                    {
                        // Is it a legal one?
                        uint iTemp = GetSurrogate(highSurrogate, ch);
                        highSurrogate = '\0';

                        //
                        // One surrogate pair will be translated into 4 bytes UTF32.
                        //
                        if (bytes + 3 >= byteEnd)
                        {
                            // Don't have 4 bytes
                            if (fallbackBuffer.bFallingBack)
                            {
                                fallbackBuffer.MovePrevious();                  // Aren't using these 2 fallback chars
                                fallbackBuffer.MovePrevious();
                            }
                            else
                            {
                                // If we don't have enough room, then either we should've advanced a while
                                // or we should have bytes==byteStart and throw below
                                Debug.Assert(chars > charStart + 1 || bytes == byteStart,
                                    "[UnicodeEncoding.GetBytes]Expected chars to have when no room to add surrogate pair");
                                chars -= 2;                                       // Aren't using those 2 chars
                            }
                            ThrowBytesOverflow(encoder, bytes == byteStart);    // Throw maybe (if no bytes written)
                            highSurrogate = (char)0;                            // Nothing left over (we backed up to start of pair if supplimentary)
                            break;
                        }

                        if (_bigEndian)
                        {
                            *(bytes++) = (byte)(0x00);
                            *(bytes++) = (byte)(iTemp >> 16);       // Implies & 0xFF, which isn't needed cause high are all 0
                            *(bytes++) = (byte)(iTemp >> 8);        // Implies & 0xFF
                            *(bytes++) = (byte)(iTemp);             // Implies & 0xFF
                        }
                        else
                        {
                            *(bytes++) = (byte)(iTemp);             // Implies & 0xFF
                            *(bytes++) = (byte)(iTemp >> 8);        // Implies & 0xFF
                            *(bytes++) = (byte)(iTemp >> 16);       // Implies & 0xFF, which isn't needed cause high are all 0
                            *(bytes++) = (byte)(0x00);
                        }
                        continue;
                    }

                    // We are missing our low surrogate, decrement chars and fallback the high surrogate
                    // The high surrogate may have come from the encoder, but nothing else did.
                    Debug.Assert(chars > charStart,
                        "[UTF32Encoding.GetBytes]Expected chars to have advanced if no low surrogate");
                    chars--;

                    // Do the fallback
                    charsForFallback = chars;
                    fallbackBuffer.InternalFallback(highSurrogate, ref charsForFallback);
                    chars = charsForFallback;

                    // We're going to fallback the old high surrogate.
                    highSurrogate = '\0';
                    continue;
                }

                // Do we have another high surrogate?, if so remember it
                if (char.IsHighSurrogate(ch))
                {
                    //
                    // We'll have a high surrogate to check next time.
                    //
                    highSurrogate = ch;
                    continue;
                }

                // Check for illegal characters (low surrogate)
                if (char.IsLowSurrogate(ch))
                {
                    // We have a leading low surrogate, do the fallback
                    charsForFallback = chars;
                    fallbackBuffer.InternalFallback(ch, ref charsForFallback);
                    chars = charsForFallback;

                    // Try again with fallback buffer
                    continue;
                }

                // We get to add the character, yippee.
                if (bytes + 3 >= byteEnd)
                {
                    // Don't have 4 bytes
                    if (fallbackBuffer.bFallingBack)
                        fallbackBuffer.MovePrevious();                  // Aren't using this fallback char
                    else
                    {
                        // Must've advanced already
                        Debug.Assert(chars > charStart,
                            "[UTF32Encoding.GetBytes]Expected chars to have advanced if normal character");
                        chars--;                                        // Aren't using this char
                    }
                    ThrowBytesOverflow(encoder, bytes == byteStart);    // Throw maybe (if no bytes written)
                    break;                                              // Didn't throw, stop
                }

                if (_bigEndian)
                {
                    *(bytes++) = (byte)(0x00);
                    *(bytes++) = (byte)(0x00);
                    *(bytes++) = (byte)((uint)ch >> 8); // Implies & 0xFF
                    *(bytes++) = (byte)(ch);            // Implies & 0xFF
                }
                else
                {
                    *(bytes++) = (byte)(ch);            // Implies & 0xFF
                    *(bytes++) = (byte)((uint)ch >> 8); // Implies & 0xFF
                    *(bytes++) = (byte)(0x00);
                    *(bytes++) = (byte)(0x00);
                }
            }

            // May have to do our last surrogate
            if ((encoder is null || encoder.MustFlush) && highSurrogate > 0)
            {
                // We have to do the fallback for the lonely high surrogate
                charsForFallback = chars;
                fallbackBuffer.InternalFallback(highSurrogate, ref charsForFallback);
                chars = charsForFallback;

                highSurrogate = (char)0;
                goto TryAgain;
            }

            // Fix our encoder if we have one
            Debug.Assert(highSurrogate == 0 || (encoder is not null && !encoder.MustFlush),
                "[UTF32Encoding.GetBytes]Expected encoder to be flushed.");

            if (encoder is not null)
            {
                // Remember our left over surrogate (or 0 if flushing)
                encoder._charLeftOver = highSurrogate;

                // Need # chars used
                encoder._charsUsed = (int)(chars - charStart);
            }

            // return the new length
            return (int)(bytes - byteStart);
        }

        internal override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS? baseDecoder)
        {
            Debug.Assert(bytes is not null, "[UTF32Encoding.GetCharCount]bytes!=null");
            Debug.Assert(count >= 0, "[UTF32Encoding.GetCharCount]count >=0");

            UTF32Decoder? decoder = (UTF32Decoder?)baseDecoder;

            // None so far!
            int charCount = 0;
            byte* end = bytes + count;
            byte* byteStart = bytes;

            // Set up decoder
            int readCount = 0;
            uint iChar = 0;

            // For fallback we may need a fallback buffer
            DecoderFallbackBuffer? fallbackBuffer = null;

            // See if there's anything in our decoder
            if (decoder is not null)
            {
                readCount = decoder.readByteCount;
                iChar = (uint)decoder.iChar;
                fallbackBuffer = decoder.FallbackBuffer;

                // Shouldn't have anything in fallback buffer for GetCharCount
                // (don't have to check _throwOnOverflow for chars or count)
                Debug.Assert(fallbackBuffer.Remaining == 0,
                    "[UTF32Encoding.GetCharCount]Expected empty fallback buffer at start");
            }
            else
            {
                fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
            }

            // Set our internal fallback interesting things.
            fallbackBuffer.InternalInitialize(byteStart, null);

            // Loop through our input, 4 characters at a time!
            while (bytes < end && charCount >= 0)
            {
                // Get our next character
                if (_bigEndian)
                {
                    // Scoot left and add it to the bottom
                    iChar <<= 8;
                    iChar += *(bytes++);
                }
                else
                {
                    // Scoot right and add it to the top
                    iChar >>= 8;
                    iChar += (uint)(*(bytes++)) << 24;
                }

                readCount++;

                // See if we have all the bytes yet
                if (readCount < 4)
                    continue;

                // Have the bytes
                readCount = 0;

                // See if its valid to encode
                if (iChar > 0x10FFFF || (iChar >= 0xD800 && iChar <= 0xDFFF))
                {
                    // Need to fall back these 4 bytes
                    byte[] fallbackBytes;
                    if (_bigEndian)
                    {
                        fallbackBytes = new byte[] {
                            unchecked((byte)(iChar >> 24)), unchecked((byte)(iChar >> 16)),
                            unchecked((byte)(iChar >> 8)), unchecked((byte)(iChar)) };
                    }
                    else
                    {
                        fallbackBytes = new byte[] {
                            unchecked((byte)(iChar)), unchecked((byte)(iChar >> 8)),
                            unchecked((byte)(iChar >> 16)), unchecked((byte)(iChar >> 24)) };
                    }

                    charCount += fallbackBuffer.InternalFallback(fallbackBytes, bytes);

                    // Ignore the illegal character
                    iChar = 0;
                    continue;
                }

                // Ok, we have something we can add to our output
                if (iChar >= 0x10000)
                {
                    // Surrogates take 2
                    charCount++;
                }

                // Add the rest of the surrogate or our normal character
                charCount++;

                // iChar is back to 0
                iChar = 0;
            }

            // See if we have something left over that has to be decoded
            if (readCount > 0 && (decoder is null || decoder.MustFlush))
            {
                // Oops, there's something left over with no place to go.
                byte[] fallbackBytes = new byte[readCount];
                if (_bigEndian)
                {
                    while (readCount > 0)
                    {
                        fallbackBytes[--readCount] = unchecked((byte)iChar);
                        iChar >>= 8;
                    }
                }
                else
                {
                    while (readCount > 0)
                    {
                        fallbackBytes[--readCount] = unchecked((byte)(iChar >> 24));
                        iChar <<= 8;
                    }
                }

                charCount += fallbackBuffer.InternalFallback(fallbackBytes, bytes);
            }

            // Check for overflows.
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_GetByteCountOverflow);

            // Shouldn't have anything in fallback buffer for GetCharCount
            // (don't have to check _throwOnOverflow for chars or count)
            Debug.Assert(fallbackBuffer.Remaining == 0,
                "[UTF32Encoding.GetCharCount]Expected empty fallback buffer at end");

            // Return our count
            return charCount;
        }

        internal override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS? baseDecoder)
        {
            Debug.Assert(chars is not null, "[UTF32Encoding.GetChars]chars!=null");
            Debug.Assert(bytes is not null, "[UTF32Encoding.GetChars]bytes!=null");
            Debug.Assert(byteCount >= 0, "[UTF32Encoding.GetChars]byteCount >=0");
            Debug.Assert(charCount >= 0, "[UTF32Encoding.GetChars]charCount >=0");

            UTF32Decoder? decoder = (UTF32Decoder?)baseDecoder;

            // None so far!
            char* charStart = chars;
            char* charEnd = chars + charCount;

            byte* byteStart = bytes;
            byte* byteEnd = bytes + byteCount;

            // See if there's anything in our decoder (but don't clear it yet)
            int readCount = 0;
            uint iChar = 0;

            // For fallback we may need a fallback buffer
            DecoderFallbackBuffer? fallbackBuffer = null;
            char* charsForFallback;

            // See if there's anything in our decoder
            if (decoder is not null)
            {
                readCount = decoder.readByteCount;
                iChar = (uint)decoder.iChar;
                Debug.Assert(baseDecoder is not null);
                fallbackBuffer = baseDecoder.FallbackBuffer;

                // Shouldn't have anything in fallback buffer for GetChars
                // (don't have to check _throwOnOverflow for chars)
                Debug.Assert(fallbackBuffer.Remaining == 0,
                    "[UTF32Encoding.GetChars]Expected empty fallback buffer at start");
            }
            else
            {
                fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
            }

            // Set our internal fallback interesting things.
            fallbackBuffer.InternalInitialize(bytes, chars + charCount);

            // Loop through our input, 4 characters at a time!
            while (bytes < byteEnd)
            {
                // Get our next character
                if (_bigEndian)
                {
                    // Scoot left and add it to the bottom
                    iChar <<= 8;
                    iChar += *(bytes++);
                }
                else
                {
                    // Scoot right and add it to the top
                    iChar >>= 8;
                    iChar += (uint)(*(bytes++)) << 24;
                }

                readCount++;

                // See if we have all the bytes yet
                if (readCount < 4)
                    continue;

                // Have the bytes
                readCount = 0;

                // See if its valid to encode
                if (iChar > 0x10FFFF || (iChar >= 0xD800 && iChar <= 0xDFFF))
                {
                    // Need to fall back these 4 bytes
                    byte[] fallbackBytes;
                    if (_bigEndian)
                    {
                        fallbackBytes = new byte[] {
                            unchecked((byte)(iChar >> 24)), unchecked((byte)(iChar >> 16)),
                            unchecked((byte)(iChar >> 8)), unchecked((byte)(iChar)) };
                    }
                    else
                    {
                        fallbackBytes = new byte[] {
                            unchecked((byte)(iChar)), unchecked((byte)(iChar >> 8)),
                            unchecked((byte)(iChar >> 16)), unchecked((byte)(iChar >> 24)) };
                    }

                    // Chars won't be updated unless this works.
                    charsForFallback = chars;
                    bool fallbackResult = fallbackBuffer.InternalFallback(fallbackBytes, bytes, ref charsForFallback);
                    chars = charsForFallback;

                    if (!fallbackResult)
                    {
                        // Couldn't fallback, throw or wait til next time
                        // We either read enough bytes for bytes-=4 to work, or we're
                        // going to throw in ThrowCharsOverflow because chars == charStart
                        Debug.Assert(bytes >= byteStart + 4 || chars == charStart,
                            "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (bad surrogate)");
                        bytes -= 4;                                       // get back to where we were
                        iChar = 0;                                        // Remembering nothing
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                        break;                                           // Stop here, didn't throw
                    }

                    // Ignore the illegal character
                    iChar = 0;
                    continue;
                }

                // Ok, we have something we can add to our output
                if (iChar >= 0x10000)
                {
                    // Surrogates take 2
                    if (charEnd - chars < 2)
                    {
                        // Throwing or stopping
                        // We either read enough bytes for bytes-=4 to work, or we're
                        // going to throw in ThrowCharsOverflow because chars == charStart
                        Debug.Assert(bytes >= byteStart + 4 || chars == charStart,
                            "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (surrogate)");
                        bytes -= 4;                                       // get back to where we were
                        iChar = 0;                                        // Remembering nothing
                        ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                        break;                                           // Stop here, didn't throw
                    }

                    *(chars++) = GetHighSurrogate(iChar);
                    iChar = GetLowSurrogate(iChar);
                }
                // Bounds check for normal character
                else if (chars >= charEnd)
                {
                    // Throwing or stopping
                    // We either read enough bytes for bytes-=4 to work, or we're
                    // going to throw in ThrowCharsOverflow because chars == charStart
                    Debug.Assert(bytes >= byteStart + 4 || chars == charStart,
                        "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (normal char)");
                    bytes -= 4;                                       // get back to where we were
                    iChar = 0;                                        // Remembering nothing
                    ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                    break;                                           // Stop here, didn't throw
                }

                // Add the rest of the surrogate or our normal character
                *(chars++) = (char)iChar;

                // iChar is back to 0
                iChar = 0;
            }

            // See if we have something left over that has to be decoded
            if (readCount > 0 && (decoder is null || decoder.MustFlush))
            {
                // Oops, there's something left over with no place to go.
                byte[] fallbackBytes = new byte[readCount];
                int tempCount = readCount;
                if (_bigEndian)
                {
                    while (tempCount > 0)
                    {
                        fallbackBytes[--tempCount] = unchecked((byte)iChar);
                        iChar >>= 8;
                    }
                }
                else
                {
                    while (tempCount > 0)
                    {
                        fallbackBytes[--tempCount] = unchecked((byte)(iChar >> 24));
                        iChar <<= 8;
                    }
                }

                charsForFallback = chars;
                bool fallbackResult = fallbackBuffer.InternalFallback(fallbackBytes, bytes, ref charsForFallback);
                chars = charsForFallback;

                if (!fallbackResult)
                {
                    // Couldn't fallback.
                    fallbackBuffer.InternalReset();
                    ThrowCharsOverflow(decoder, chars == charStart); // Might throw, if no chars output
                    // Stop here, didn't throw, backed up, so still nothing in buffer
                }
                else
                {
                    // Don't clear our decoder unless we could fall it back.
                    // If we caught the if above, then we're a convert() and will catch this next time.
                    readCount = 0;
                    iChar = 0;
                }
            }

            // Remember any left over stuff, clearing buffer as well for MustFlush
            if (decoder is not null)
            {
                decoder.iChar = (int)iChar;
                decoder.readByteCount = readCount;
                decoder._bytesUsed = (int)(bytes - byteStart);
            }

            // Shouldn't have anything in fallback buffer for GetChars
            // (don't have to check _throwOnOverflow for chars)
            Debug.Assert(fallbackBuffer.Remaining == 0,
                "[UTF32Encoding.GetChars]Expected empty fallback buffer at end");

            // Return our count
            return (int)(chars - charStart);
        }

        private static uint GetSurrogate(char cHigh, char cLow)
        {
            return (((uint)cHigh - 0xD800) * 0x400) + ((uint)cLow - 0xDC00) + 0x10000;
        }

        private static char GetHighSurrogate(uint iChar)
        {
            return (char)((iChar - 0x10000) / 0x400 + 0xD800);
        }

        private static char GetLowSurrogate(uint iChar)
        {
            return (char)((iChar - 0x10000) % 0x400 + 0xDC00);
        }

        public override Decoder GetDecoder()
        {
            return new UTF32Decoder(this);
        }

        public override Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }

        public override int GetMaxByteCount(int charCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(charCount);

            // Characters would be # of characters + 1 in case left over high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // 4 bytes per char
            byteCount *= 4;

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(charCount), SR.ArgumentOutOfRange_GetByteCountOverflow);

            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

            // A supplementary character becomes 2 surrogate characters, so 4 input bytes becomes 2 chars,
            // plus we may have 1 surrogate char left over if the decoder has 3 bytes in it already for a non-bmp char.
            // Have to add another one because 1/2 == 0, but 3 bytes left over could be 2 char surrogate pair
            int charCount = (byteCount / 2) + 2;

            // Also consider fallback because our input bytes could be out of range of unicode.
            // Since fallback would fallback 4 bytes at a time, we'll only fall back 1/2 of MaxCharCount.
            if (DecoderFallback.MaxCharCount > 2)
            {
                // Multiply time fallback size
                charCount *= DecoderFallback.MaxCharCount;

                // We were already figuring 2 chars per 4 bytes, but fallback will be different #
                charCount /= 2;
            }

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException(nameof(byteCount), SR.ArgumentOutOfRange_GetCharCountOverflow);

            return (int)charCount;
        }

        public override byte[] GetPreamble()
        {
            if (_emitUTF32ByteOrderMark)
            {
                // Allocate new array to prevent users from modifying it.
                if (_bigEndian)
                {
                    return new byte[4] { 0x00, 0x00, 0xFE, 0xFF };
                }
                else
                {
                    return new byte[4] { 0xFF, 0xFE, 0x00, 0x00 }; // 00 00 FE FF
                }
            }
            else
                return Array.Empty<byte>();
        }

        public override ReadOnlySpan<byte> Preamble =>
            GetType() != typeof(UTF32Encoding) ? new ReadOnlySpan<byte>(GetPreamble()) : // in case a derived UTF32Encoding overrode GetPreamble
            !_emitUTF32ByteOrderMark ? default :
            _bigEndian ? [0x00, 0x00, 0xFE, 0xFF] :
            [0xFF, 0xFE, 0x00, 0x00];

        public override bool Equals([NotNullWhen(true)] object? value)
        {
            if (value is UTF32Encoding that)
            {
                return (_emitUTF32ByteOrderMark == that._emitUTF32ByteOrderMark) &&
                       (_bigEndian == that._bigEndian) &&
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            }

            return false;
        }

        public override int GetHashCode()
        {
            // Not great distribution, but this is relatively unlikely to be used as the key in a hashtable.
            return this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() +
                   CodePage + (_emitUTF32ByteOrderMark ? 4 : 0) + (_bigEndian ? 8 : 0);
        }

        private sealed class UTF32Decoder : DecoderNLS
        {
            // Need a place to store any extra bytes we may have picked up
            internal int iChar;
            internal int readByteCount;

            public UTF32Decoder(UTF32Encoding encoding) : base(encoding)
            {
                // base calls reset
            }

            public override void Reset()
            {
                this.iChar = 0;
                this.readByteCount = 0;
                _fallbackBuffer?.Reset();
            }

            // Anything left in our decoder?
            internal override bool HasState =>
                // ReadByteCount is our flag.  (iChar==0 doesn't mean much).
                this.readByteCount != 0;
        }
    }
}
