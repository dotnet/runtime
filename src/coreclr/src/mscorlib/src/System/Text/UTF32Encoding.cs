// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Don't override IsAlwaysNormalized because it is just a Unicode Transformation and could be confused.
//

namespace System.Text
{

    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    // Encodes text into and out of UTF-32.  UTF-32 is a way of writing
    // Unicode characters with a single storage unit (32 bits) per character,
    //
    // The UTF-32 byte order mark is simply the Unicode byte order mark
    // (0x00FEFF) written in UTF-32 (0x0000FEFF or 0xFFFE0000).  The byte order
    // mark is used mostly to distinguish UTF-32 text from other encodings, and doesn't
    // switch the byte orderings.

    [Serializable]
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

        private bool emitUTF32ByteOrderMark = false;
        private bool isThrowException = false;
        private bool bigEndian = false;


        public UTF32Encoding(): this(false, true, false)
        {
        }


        public UTF32Encoding(bool bigEndian, bool byteOrderMark):
            this(bigEndian, byteOrderMark, false)
        {
        }


        public UTF32Encoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidCharacters):
            base(bigEndian ? 12001 : 12000)
        {
            this.bigEndian = bigEndian;
            this.emitUTF32ByteOrderMark = byteOrderMark;
            this.isThrowException = throwOnInvalidCharacters;

            // Encoding's constructor already did this, but it'll be wrong if we're throwing exceptions
            if (this.isThrowException)
                SetDefaultFallbacks();
        }

        internal override void SetDefaultFallbacks()
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

        // NOTE: Many methods in this class forward to EncodingForwarder for
        // validating arguments/wrapping the unsafe methods in this class 
        // which do the actual work. That class contains
        // shared logic for doing this which is used by
        // ASCIIEncoding, EncodingNLS, UnicodeEncoding, UTF32Encoding,
        // UTF7Encoding, and UTF8Encoding.
        // The reason the code is separated out into a static class, rather
        // than a base class which overrides all of these methods for us
        // (which is what EncodingNLS is for internal Encodings) is because
        // that's really more of an implementation detail so it's internal.
        // At the same time, C# doesn't allow a public class subclassing an
        // internal/private one, so we end up having to re-override these
        // methods in all of the public Encodings + EncodingNLS.

        // Returns the number of bytes required to encode a range of characters in
        // a character array.

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return EncodingForwarder.GetByteCount(this, chars, index, count);
        }

        public override int GetByteCount(String s)
        {
            return EncodingForwarder.GetByteCount(this, s);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public override unsafe int GetByteCount(char* chars, int count)
        {
            return EncodingForwarder.GetByteCount(this, chars, count);
        }

        public override int GetBytes(String s, int charIndex, int charCount,
                                              byte[] bytes, int byteIndex)
        {
            return EncodingForwarder.GetBytes(this, s, charIndex, charCount, bytes, byteIndex);
        }

        // Encodes a range of characters in a character array into a range of bytes
        // in a byte array. An exception occurs if the byte array is not large
        // enough to hold the complete encoding of the characters. The
        // GetByteCount method can be used to determine the exact number of
        // bytes that will be produced for a given range of characters.
        // Alternatively, the GetMaxByteCount method can be used to
        // determine the maximum number of bytes that will be produced for a given
        // number of characters, regardless of the actual character values.

        public override int GetBytes(char[] chars, int charIndex, int charCount,
                                               byte[] bytes, int byteIndex)
        {
            return EncodingForwarder.GetBytes(this, chars, charIndex, charCount, bytes, byteIndex);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            return EncodingForwarder.GetBytes(this, chars, charCount, bytes, byteCount);
        }

        // Returns the number of characters produced by decoding a range of bytes
        // in a byte array.

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return EncodingForwarder.GetCharCount(this, bytes, index, count);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            return EncodingForwarder.GetCharCount(this, bytes, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                              char[] chars, int charIndex)
        {
            return EncodingForwarder.GetChars(this, bytes, byteIndex, byteCount, chars, charIndex);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            return EncodingForwarder.GetChars(this, bytes, byteCount, chars, charCount);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.

        public override String GetString(byte[] bytes, int index, int count)
        {
            return EncodingForwarder.GetString(this, bytes, index, count);
        }
        
        // End of overridden methods which use EncodingForwarder

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetByteCount(char *chars, int count, EncoderNLS encoder)
        {
            Contract.Assert(chars!=null, "[UTF32Encoding.GetByteCount]chars!=null");
            Contract.Assert(count >=0, "[UTF32Encoding.GetByteCount]count >=0");

            char* end = chars + count;
            char* charStart = chars;
            int byteCount = 0;

            char highSurrogate = '\0';

            // For fallback we may need a fallback buffer
            EncoderFallbackBuffer fallbackBuffer = null;
            if (encoder != null)
            {
                highSurrogate = encoder.charLeftOver;
                fallbackBuffer = encoder.FallbackBuffer;

                // We mustn't have left over fallback data when counting
                if (fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                    this.EncodingName, encoder.Fallback.GetType()));
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
                    if (Char.IsLowSurrogate(ch))
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
                    Contract.Assert(chars > charStart, 
                        "[UTF32Encoding.GetByteCount]Expected chars to have advanced if no low surrogate");
                    chars--;

                    // Do the fallback
                    fallbackBuffer.InternalFallback(highSurrogate, ref chars);

                    // We're going to fallback the old high surrogate.
                    highSurrogate = '\0';
                    continue;

                }

                // Do we have another high surrogate?
                if (Char.IsHighSurrogate(ch))
                {
                    //
                    // We'll have a high surrogate to check next time.
                    //
                    highSurrogate = ch;
                    continue;
                }

                // Check for illegal characters
                if (Char.IsLowSurrogate(ch))
                {
                    // We have a leading low surrogate, do the fallback
                    fallbackBuffer.InternalFallback(ch, ref chars);

                    // Try again with fallback buffer
                    continue;
                }

                // We get to add the character (4 bytes UTF32)
                byteCount += 4;
            }

            // May have to do our last surrogate
            if ((encoder == null || encoder.MustFlush) && highSurrogate > 0)
            {
                // We have to do the fallback for the lonely high surrogate
                fallbackBuffer.InternalFallback(highSurrogate, ref chars);
                highSurrogate = (char)0;
                goto TryAgain;
            }

            // Check for overflows.
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString(
                    "ArgumentOutOfRange_GetByteCountOverflow"));

            // Shouldn't have anything in fallback buffer for GetByteCount
            // (don't have to check m_throwOnOverflow for count)
            Contract.Assert(fallbackBuffer.Remaining == 0,
                "[UTF32Encoding.GetByteCount]Expected empty fallback buffer at end");

            // Return our count
            return byteCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetBytes(char *chars, int charCount,
                                                 byte* bytes, int byteCount, EncoderNLS encoder)
        {
            Contract.Assert(chars!=null, "[UTF32Encoding.GetBytes]chars!=null");
            Contract.Assert(bytes!=null, "[UTF32Encoding.GetBytes]bytes!=null");
            Contract.Assert(byteCount >=0, "[UTF32Encoding.GetBytes]byteCount >=0");
            Contract.Assert(charCount >=0, "[UTF32Encoding.GetBytes]charCount >=0");

            char* charStart = chars;
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            byte* byteEnd = bytes + byteCount;

            char highSurrogate = '\0';

            // For fallback we may need a fallback buffer
            EncoderFallbackBuffer fallbackBuffer = null;
            if (encoder != null)
            {
                highSurrogate = encoder.charLeftOver;
                fallbackBuffer = encoder.FallbackBuffer;

                // We mustn't have left over fallback data when not converting
                if (encoder.m_throwOnOverflow && fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                    this.EncodingName, encoder.Fallback.GetType()));
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
                    if (Char.IsLowSurrogate(ch))
                    {
                        // Is it a legal one?
                        uint iTemp = GetSurrogate(highSurrogate, ch);
                        highSurrogate = '\0';

                        //
                        // One surrogate pair will be translated into 4 bytes UTF32.
                        //
                        if (bytes+3 >= byteEnd)
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
                                Contract.Assert(chars > charStart + 1 || bytes == byteStart, 
                                    "[UnicodeEncoding.GetBytes]Expected chars to have when no room to add surrogate pair");
                                chars-=2;                                       // Aren't using those 2 chars
                            }
                            ThrowBytesOverflow(encoder, bytes == byteStart);    // Throw maybe (if no bytes written)
                            highSurrogate = (char)0;                            // Nothing left over (we backed up to start of pair if supplimentary)
                            break;
                        }

                        if (bigEndian)
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
                    Contract.Assert(chars > charStart, 
                        "[UTF32Encoding.GetBytes]Expected chars to have advanced if no low surrogate");
                    chars--;

                    // Do the fallback
                    fallbackBuffer.InternalFallback(highSurrogate, ref chars);

                    // We're going to fallback the old high surrogate.
                    highSurrogate = '\0';
                    continue;
                }

                // Do we have another high surrogate?, if so remember it
                if (Char.IsHighSurrogate(ch))
                {
                    //
                    // We'll have a high surrogate to check next time.
                    //
                    highSurrogate = ch;
                    continue;
                }

                // Check for illegal characters (low surrogate)
                if (Char.IsLowSurrogate(ch))
                {
                    // We have a leading low surrogate, do the fallback
                    fallbackBuffer.InternalFallback(ch, ref chars);

                    // Try again with fallback buffer
                    continue;
                }

                // We get to add the character, yippee.
                if (bytes+3 >= byteEnd)
                {
                    // Don't have 4 bytes
                    if (fallbackBuffer.bFallingBack)
                        fallbackBuffer.MovePrevious();                  // Aren't using this fallback char
                    else
                    {
                        // Must've advanced already
                        Contract.Assert(chars > charStart,
                            "[UTF32Encoding.GetBytes]Expected chars to have advanced if normal character");
                        chars--;                                        // Aren't using this char
                    }
                    ThrowBytesOverflow(encoder, bytes == byteStart);    // Throw maybe (if no bytes written)
                    break;                                              // Didn't throw, stop
                }

                if (bigEndian)
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
            if ((encoder == null || encoder.MustFlush) && highSurrogate > 0)
            {
                // We have to do the fallback for the lonely high surrogate
                fallbackBuffer.InternalFallback(highSurrogate, ref chars);
                highSurrogate = (char)0;
                goto TryAgain;
            }

            // Fix our encoder if we have one
            Contract.Assert(highSurrogate == 0 || (encoder != null && !encoder.MustFlush),
                "[UTF32Encoding.GetBytes]Expected encoder to be flushed.");

            if (encoder != null)
            {
                // Remember our left over surrogate (or 0 if flushing)
                encoder.charLeftOver = highSurrogate;

                // Need # chars used
                encoder.m_charsUsed = (int)(chars-charStart);
            }

            // return the new length
            return (int)(bytes - byteStart);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS baseDecoder)
        {
            Contract.Assert(bytes!=null, "[UTF32Encoding.GetCharCount]bytes!=null");
            Contract.Assert(count >=0, "[UTF32Encoding.GetCharCount]count >=0");

            UTF32Decoder decoder = (UTF32Decoder)baseDecoder;

            // None so far!
            int charCount = 0;
            byte* end = bytes + count;
            byte* byteStart = bytes;

            // Set up decoder
            int readCount = 0;
            uint iChar = 0;

            // For fallback we may need a fallback buffer
            DecoderFallbackBuffer fallbackBuffer = null;

            // See if there's anything in our decoder
            if (decoder != null)
            {
                readCount = decoder.readByteCount;
                iChar = (uint)decoder.iChar;
                fallbackBuffer = decoder.FallbackBuffer;

                // Shouldn't have anything in fallback buffer for GetCharCount
                // (don't have to check m_throwOnOverflow for chars or count)
                Contract.Assert(fallbackBuffer.Remaining == 0,
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
                if(bigEndian)
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
                if ( iChar > 0x10FFFF || (iChar >= 0xD800 && iChar <= 0xDFFF))
                {
                    // Need to fall back these 4 bytes
                    byte[] fallbackBytes;
                    if (this.bigEndian)
                    {
                        fallbackBytes = new byte[] {
                            unchecked((byte)(iChar>>24)), unchecked((byte)(iChar>>16)),
                            unchecked((byte)(iChar>>8)), unchecked((byte)(iChar)) };
                    }
                    else
                    {
                        fallbackBytes = new byte[] {
                            unchecked((byte)(iChar)), unchecked((byte)(iChar>>8)),
                            unchecked((byte)(iChar>>16)), unchecked((byte)(iChar>>24)) };
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
            if (readCount > 0 && (decoder == null || decoder.MustFlush))
            {
                // Oops, there's something left over with no place to go.
                byte[] fallbackBytes = new byte[readCount];
                if (this.bigEndian)
                {
                    while(readCount > 0)
                    {
                        fallbackBytes[--readCount] = unchecked((byte)iChar);
                        iChar >>= 8;
                    }
                }
                else
                {
                    while (readCount > 0)
                    {
                        fallbackBytes[--readCount] = unchecked((byte)(iChar>>24));
                        iChar <<= 8;
                    }
                }

                charCount += fallbackBuffer.InternalFallback(fallbackBytes, bytes);
            }

            // Check for overflows.
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));

            // Shouldn't have anything in fallback buffer for GetCharCount
            // (don't have to check m_throwOnOverflow for chars or count)
            Contract.Assert(fallbackBuffer.Remaining == 0,
                "[UTF32Encoding.GetCharCount]Expected empty fallback buffer at end");

            // Return our count
            return charCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS baseDecoder)
        {
            Contract.Assert(chars!=null, "[UTF32Encoding.GetChars]chars!=null");
            Contract.Assert(bytes!=null, "[UTF32Encoding.GetChars]bytes!=null");
            Contract.Assert(byteCount >=0, "[UTF32Encoding.GetChars]byteCount >=0");
            Contract.Assert(charCount >=0, "[UTF32Encoding.GetChars]charCount >=0");

            UTF32Decoder decoder = (UTF32Decoder)baseDecoder;

            // None so far!
            char* charStart = chars;
            char* charEnd = chars + charCount;

            byte* byteStart = bytes;
            byte* byteEnd = bytes + byteCount;

            // See if there's anything in our decoder (but don't clear it yet)
            int readCount = 0;
            uint iChar = 0;

            // For fallback we may need a fallback buffer
            DecoderFallbackBuffer fallbackBuffer = null;

            // See if there's anything in our decoder
            if (decoder != null)
            {
                readCount = decoder.readByteCount;
                iChar = (uint)decoder.iChar;
                fallbackBuffer = baseDecoder.FallbackBuffer;

                // Shouldn't have anything in fallback buffer for GetChars
                // (don't have to check m_throwOnOverflow for chars)
                Contract.Assert(fallbackBuffer.Remaining == 0,
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
                if(bigEndian)
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
                if ( iChar > 0x10FFFF || (iChar >= 0xD800 && iChar <= 0xDFFF))
                {
                    // Need to fall back these 4 bytes
                    byte[] fallbackBytes;
                    if (this.bigEndian)
                    {
                        fallbackBytes = new byte[] {
                            unchecked((byte)(iChar>>24)), unchecked((byte)(iChar>>16)),
                            unchecked((byte)(iChar>>8)), unchecked((byte)(iChar)) };
                    }
                    else
                    {
                        fallbackBytes = new byte[] {
                            unchecked((byte)(iChar)), unchecked((byte)(iChar>>8)),
                            unchecked((byte)(iChar>>16)), unchecked((byte)(iChar>>24)) };
                    }

                    // Chars won't be updated unless this works.
                    if (!fallbackBuffer.InternalFallback(fallbackBytes, bytes, ref chars))
                    {
                        // Couldn't fallback, throw or wait til next time
                        // We either read enough bytes for bytes-=4 to work, or we're
                        // going to throw in ThrowCharsOverflow because chars == charStart
                        Contract.Assert(bytes >= byteStart + 4 || chars == charStart,
                            "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (bad surrogate)");
                        bytes-=4;                                       // get back to where we were
                        iChar=0;                                        // Remembering nothing
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                        break;                                          // Stop here, didn't throw
                    }

                    // Ignore the illegal character
                    iChar = 0;
                    continue;
                }


                // Ok, we have something we can add to our output
                if (iChar >= 0x10000)
                {
                    // Surrogates take 2
                    if (chars >= charEnd - 1)
                    {
                        // Throwing or stopping
                        // We either read enough bytes for bytes-=4 to work, or we're
                        // going to throw in ThrowCharsOverflow because chars == charStart
                        Contract.Assert(bytes >= byteStart + 4 || chars == charStart,
                            "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (surrogate)");
                        bytes-=4;                                       // get back to where we were
                        iChar=0;                                        // Remembering nothing
                        ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                        break;                                          // Stop here, didn't throw
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
                    Contract.Assert(bytes >= byteStart + 4 || chars == charStart,
                        "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (normal char)");
                    bytes-=4;                                       // get back to where we were
                    iChar=0;                                        // Remembering nothing                    
                    ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                    break;                                          // Stop here, didn't throw
                }

                // Add the rest of the surrogate or our normal character
                *(chars++) = (char)iChar;

                // iChar is back to 0
                iChar = 0;
            }

            // See if we have something left over that has to be decoded
            if (readCount > 0 && (decoder == null || decoder.MustFlush))
            {
                // Oops, there's something left over with no place to go.
                byte[] fallbackBytes = new byte[readCount];
                int tempCount = readCount;
                if (this.bigEndian)
                {
                    while(tempCount > 0)
                    {
                        fallbackBytes[--tempCount] = unchecked((byte)iChar);
                        iChar >>= 8;
                    }
                }
                else
                {
                    while (tempCount > 0)
                    {
                        fallbackBytes[--tempCount] = unchecked((byte)(iChar>>24));
                        iChar <<= 8;
                    }
                }

                if (!fallbackBuffer.InternalFallback(fallbackBytes, bytes, ref chars))
                {
                    // Couldn't fallback.
                    fallbackBuffer.InternalReset();
                    ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
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
            if (decoder != null)
            {
                decoder.iChar = (int)iChar;
                decoder.readByteCount = readCount;
                decoder.m_bytesUsed = (int)(bytes - byteStart);
            }

            // Shouldn't have anything in fallback buffer for GetChars
            // (don't have to check m_throwOnOverflow for chars)
            Contract.Assert(fallbackBuffer.Remaining == 0,
                "[UTF32Encoding.GetChars]Expected empty fallback buffer at end");

            // Return our count
            return (int)(chars - charStart);
        }


        private uint GetSurrogate(char cHigh, char cLow)
        {
            return (((uint)cHigh - 0xD800) * 0x400) + ((uint)cLow - 0xDC00) + 0x10000;
        }

        private char GetHighSurrogate(uint iChar)
        {
            return (char)((iChar - 0x10000) / 0x400 + 0xD800);
        }

        private char GetLowSurrogate(uint iChar)
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
            if (charCount < 0)
               throw new ArgumentOutOfRangeException("charCount",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

            // Characters would be # of characters + 1 in case left over high surrogate is ? * max fallback
            long byteCount = (long)charCount + 1;

            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;

            // 4 bytes per char
            byteCount *= 4;

            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));

            return (int)byteCount;
        }


        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
               throw new ArgumentOutOfRangeException("byteCount",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();

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
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));

            return (int)charCount;
        }


        public override byte[] GetPreamble()
        {
            if (emitUTF32ByteOrderMark)
            {
                // Allocate new array to prevent users from modifying it.
                if (bigEndian)
                {
                    return new byte[4] { 0x00, 0x00, 0xFE, 0xFF };
                }
                else
                {
                    return new byte[4] { 0xFF, 0xFE, 0x00, 0x00 }; // 00 00 FE FF
                }
            }
            else
                return EmptyArray<Byte>.Value;
        }


        public override bool Equals(Object value)
        {
            UTF32Encoding that = value as UTF32Encoding;
            if (that != null)
            {
                return (emitUTF32ByteOrderMark == that.emitUTF32ByteOrderMark) &&
                       (bigEndian == that.bigEndian) &&
//                       (isThrowException == that.isThrowException) && // same as encoder/decoderfallback being exceptions
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            }
            return (false);
        }


        public override int GetHashCode()
        {
            //Not great distribution, but this is relatively unlikely to be used as the key in a hashtable.
            return this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() +
                   CodePage + (emitUTF32ByteOrderMark?4:0) + (bigEndian?8:0);
        }

        [Serializable]
        internal class UTF32Decoder : DecoderNLS
        {
            // Need a place to store any extra bytes we may have picked up
            internal int iChar = 0;
            internal int readByteCount = 0;

            public UTF32Decoder(UTF32Encoding encoding) : base(encoding)
            {
                // base calls reset
            }

            public override void Reset()
            {
                this.iChar = 0;
                this.readByteCount = 0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            // Anything left in our decoder?
            internal override bool HasState
            {
                get
                {
                    // ReadByteCount is our flag.  (iChar==0 doesn't mean much).
                    return (this.readByteCount != 0);
                }
            }
        }
    }
}
