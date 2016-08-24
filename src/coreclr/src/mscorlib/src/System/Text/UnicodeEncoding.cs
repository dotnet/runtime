// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Don't override IsAlwaysNormalized because it is just a Unicode Transformation and could be confused.
//

namespace System.Text
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;


    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class UnicodeEncoding : Encoding
    {
        // Used by Encoding.BigEndianUnicode/Unicode for lazy initialization
        // The initialization code will not be run until a static member of the class is referenced
        internal static readonly UnicodeEncoding s_bigEndianDefault = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
        internal static readonly UnicodeEncoding s_littleEndianDefault = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);

        [OptionalField(VersionAdded = 2)]
        internal bool isThrowException = false;
        
        internal bool bigEndian = false;
        internal bool byteOrderMark = true;

        // Unicode version 2.0 character size in bytes
        public const int CharSize = 2;


        public UnicodeEncoding()
            : this(false, true)
        {
        }


        public UnicodeEncoding(bool bigEndian, bool byteOrderMark)
            : this(bigEndian, byteOrderMark, false)
        {
        }


        public UnicodeEncoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes)
            : base(bigEndian ? 1201 : 1200)  //Set the data item.
        {
            this.isThrowException = throwOnInvalidBytes;
            this.bigEndian = bigEndian;
            this.byteOrderMark = byteOrderMark;

            // Encoding's constructor already did this, but it'll be wrong if we're throwing exceptions
            if (this.isThrowException)
                SetDefaultFallbacks();
        }

#region Serialization 
        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            // In Everett it is false. Whidbey will overwrite this value.
            isThrowException = false;
        }   
#endregion Serialization

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
        [System.Runtime.InteropServices.ComVisible(false)]
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
        [System.Runtime.InteropServices.ComVisible(false)]
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
        [System.Runtime.InteropServices.ComVisible(false)]
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
        [System.Runtime.InteropServices.ComVisible(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            return EncodingForwarder.GetChars(this, bytes, byteCount, chars, charCount);
        }

        // Returns a string containing the decoded representation of a range of
        // bytes in a byte array.

        [System.Runtime.InteropServices.ComVisible(false)]
        public override String GetString(byte[] bytes, int index, int count)
        {
            return EncodingForwarder.GetString(this, bytes, index, count);
        }
        
        // End of overridden methods which use EncodingForwarder

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetByteCount(char* chars, int count, EncoderNLS encoder)
        {
            Contract.Assert(chars!=null, "[UnicodeEncoding.GetByteCount]chars!=null");
            Contract.Assert(count >= 0, "[UnicodeEncoding.GetByteCount]count >=0");

            // Start by assuming each char gets 2 bytes
            int byteCount = count << 1;

            // Check for overflow in byteCount
            // (If they were all invalid chars, this would actually be wrong,
            // but that's a ridiculously large # so we're not concerned about that case)
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));

            char* charStart = chars;
            char* charEnd = chars + count;
            char  charLeftOver = (char)0;

            bool wasHereBefore = false;

            // Need -1 to check 2 at a time.  If we have an even #, longChars will go
            // from longEnd - 1/2 long to longEnd + 1/2 long.  If we're odd, longChars
            // will go from longEnd - 1 long to longEnd. (Might not get to use this)
            ulong* longEnd = (ulong*)(charEnd - 3);

            // For fallback we may need a fallback buffer
            EncoderFallbackBuffer fallbackBuffer = null;

            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;

                // Assume extra bytes to encode charLeftOver if it existed
                if (charLeftOver > 0)
                    byteCount+=2;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                        this.EncodingName, encoder.Fallback.GetType()));

                    // Set our internal fallback interesting things.
                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                }
            }

            char ch;
            TryAgain:

            while (((ch = (fallbackBuffer == null) ? (char)0 :fallbackBuffer.InternalGetNextChar()) != 0) || chars < charEnd)
            {
                // First unwind any fallback
                if (ch == 0)
                {
                    // No fallback, maybe we can do it fast
#if !NO_FAST_UNICODE_LOOP
#if BIGENDIAN       // If endianess is backwards then each pair of bytes would be backwards.
                    if ( bigEndian &&
#else
                    if ( !bigEndian &&
#endif // BIGENDIAN

#if BIT64           // 64 bit CPU needs to be long aligned for this to work.
                          charLeftOver == 0 && (unchecked((long)chars) & 7) == 0)
#else
                          charLeftOver == 0 && (unchecked((int)chars) & 3) == 0)
#endif
                    {
                        // Need new char* so we can check 4 at a time
                        ulong* longChars = (ulong*)chars;

                        while (longChars < longEnd)
                        {
                            // See if we potentially have surrogates (0x8000 bit set)
                            // (We're either big endian on a big endian machine or little endian on 
                            // a little endian machine so this'll work)                            
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
#if BIGENDIAN
                                    if (((0xfc00fc00fc00fc00 & *longChars) ^ 0xd800dc00d800dc00) != 0)
#else
                                    if (((0xfc00fc00fc00fc00 & *longChars) ^ 0xdc00d800dc00d800) != 0)
#endif
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
#endif // !NO_FAST_UNICODE_LOOP

                    // No fallback, just get next char
                    ch = *chars;
                    chars++;
                }
                else
                {
                    // We weren't preallocating fallback space.
                    byteCount+=2;
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
                            Contract.Assert(chars > charStart, 
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

                            fallbackBuffer.InternalFallback(charLeftOver, ref chars);

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
                        fallbackBuffer.InternalFallback(ch, ref chars);
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
                    Contract.Assert(chars > charStart, 
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
                    fallbackBuffer.InternalFallback(charLeftOver, ref chars);

                    // Ignore charLeftOver or throw
                    byteCount-=2;
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
                                    Environment.GetResourceString("Argument_RecursiveFallback",
                                    charLeftOver), "chars");
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
                        fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                        charLeftOver = (char)0;
                        wasHereBefore = true;
                        goto TryAgain;
                    }
                }
            }

            // Shouldn't have anything in fallback buffer for GetByteCount
            // (don't have to check m_throwOnOverflow for count)
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[UnicodeEncoding.GetByteCount]Expected empty fallback buffer at end");

            // Don't remember fallbackBuffer.encoder for counting
            return byteCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetBytes(char* chars, int charCount,
                                                byte* bytes, int byteCount, EncoderNLS encoder)
        {
            Contract.Assert(chars!=null, "[UnicodeEncoding.GetBytes]chars!=null");
            Contract.Assert(byteCount >=0, "[UnicodeEncoding.GetBytes]byteCount >=0");
            Contract.Assert(charCount >=0, "[UnicodeEncoding.GetBytes]charCount >=0");
            Contract.Assert(bytes!=null, "[UnicodeEncoding.GetBytes]bytes!=null");

            char charLeftOver = (char)0;
            char ch;
            bool wasHereBefore = false;


            byte* byteEnd = bytes + byteCount;
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            // For fallback we may need a fallback buffer
            EncoderFallbackBuffer fallbackBuffer = null;

            // Get our encoder, but don't clear it yet.
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;

                // We mustn't have left over fallback data when counting
                if (encoder.InternalHasFallbackBuffer)
                {
                    // We always need the fallback buffer in get bytes so we can flush any remaining ones if necessary
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder.m_throwOnOverflow)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty",
                        this.EncodingName, encoder.Fallback.GetType()));

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
#if !NO_FAST_UNICODE_LOOP
#if BIGENDIAN           // If endianess is backwards then each pair of bytes would be backwards.
                    if ( bigEndian &&
#else
                    if ( !bigEndian &&
#endif // BIGENDIAN
#if BIT64           // 64 bit CPU needs to be long aligned for this to work, 32 bit CPU needs to be 32 bit aligned
                        (unchecked((long)chars) & 7) == 0 && (unchecked((long)bytes) & 7) == 0 &&
#else
                        (unchecked((int)chars) & 3) == 0 && (unchecked((int)bytes) & 3) == 0 &&
#endif // BIT64
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
                            // a little endian machine so this'll work)                            
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
#if BIGENDIAN
                                    if (((0xfc00fc00fc00fc00 & *longChars) ^ 0xd800dc00d800dc00) != 0)
#else
                                    if (((0xfc00fc00fc00fc00 & *longChars) ^ 0xdc00d800dc00d800) != 0)
#endif
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
                            *longBytes = *longChars;
                            longChars++;
                            longBytes++;
                        }

                        chars = (char*)longChars;
                        bytes = (byte*)longBytes;

                        if (chars >= charEnd)
                            break;
                    }
                    // Not aligned, but maybe we can still be somewhat faster
                    // Also somehow this optimizes the above loop?  It seems to cause something above
                    // to get enregistered, but I haven't figured out how to make that happen without this loop.
                    else if ((charLeftOver == 0) &&
#if BIGENDIAN
                        bigEndian &&
#else
                        !bigEndian &&
#endif // BIGENDIAN

#if BIT64
                        (unchecked((long)chars) & 7) != (unchecked((long)bytes) & 7) &&  // Only do this if chars & bytes are out of line, otherwise faster loop'll be faster next time
#else
                        (unchecked((int)chars) & 3) != (unchecked((int)bytes) & 3) &&  // Only do this if chars & bytes are out of line, otherwise faster loop'll be faster next time
#endif // BIT64
                        (unchecked((int)(bytes)) & 1) == 0 )
                    {
                        // # to use
                        long iCount = ((byteEnd - bytes) >> 1 < charEnd - chars) ?
                                       (byteEnd - bytes) >> 1 : charEnd - chars;

                        // Need new char*
                        char* charOut = ((char*)bytes);     // a char* for our output
                        char* tempEnd = chars + iCount - 1; // Our end pointer

                        while (chars < tempEnd)
                        {
                            if (*chars >= (char)0xd800 && *chars <= (char)0xdfff)
                            {
                                // break for fallback for low surrogate
                                if (*chars >= 0xdc00)
                                    break;

                                // break if next one's not a low surrogate (will do fallback)
                                if (*(chars+1) < 0xdc00 || *(chars+1) > 0xdfff)
                                    break;

                                // They both exist, use them
                            }
                            // If 2nd char is surrogate & this one isn't then only add one
                            else if (*(chars+1) >= (char)0xd800 && *(chars+1) <= 0xdfff)
                            {
                                *charOut = *chars;
                                charOut++;
                                chars++;
                                continue;
                            }

                            *charOut = *chars;
                            *(charOut+1) = *(chars+1);
                            charOut+=2;
                            chars+=2;

                        }

                        bytes=(byte*)charOut;

                        if (chars >= charEnd)
                            break;
                    }
#endif // !NO_FAST_UNICODE_LOOP

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
                            Contract.Assert(chars > charStart, 
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

                            fallbackBuffer.InternalFallback(charLeftOver, ref chars);

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

                        fallbackBuffer.InternalFallback(ch, ref chars);
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
                            Contract.Assert(chars > charStart + 1 || bytes == byteStart, 
                                "[UnicodeEncoding.GetBytes]Expected chars to have when no room to add surrogate pair");
                            chars-=2;                                        // Didn't use either surrogate
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
                    Contract.Assert(chars > charStart, 
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

                    fallbackBuffer.InternalFallback(charLeftOver, ref chars);

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
                        Contract.Assert(chars > charStart, 
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
                                    Environment.GetResourceString("Argument_RecursiveFallback",
                                    charLeftOver), "chars");
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
                        
                        // If we're not flushing, this'll remember the left over character.
                        fallbackBuffer.InternalFallback(charLeftOver, ref chars);

                        charLeftOver = (char)0;
                        wasHereBefore = true;
                        goto TryAgain;
                    }
                }

            }

            // Not flushing, remember it in the encoder
            if (encoder != null)
            {
                encoder.charLeftOver = charLeftOver;
                encoder.m_charsUsed = (int)(chars - charStart);
            }

            // Remember charLeftOver if we must, or clear it if we're flushing
            // (charLeftOver should be 0 if we're flushing)
            Contract.Assert((encoder != null && !encoder.MustFlush) || charLeftOver == (char)0,
                "[UnicodeEncoding.GetBytes] Expected no left over characters if flushing");

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0 ||
                encoder == null || !encoder.m_throwOnOverflow,
                "[UnicodeEncoding.GetBytes]Expected empty fallback buffer if not converting");

            // We used to copy it fast, but this doesn't check for surrogates
            // System.IO.__UnmanagedMemoryStream.memcpyimpl(bytes, (byte*)chars, usedByteCount);

            return (int)(bytes - byteStart);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetCharCount(byte* bytes, int count, DecoderNLS baseDecoder)
        {
            Contract.Assert(bytes!=null, "[UnicodeEncoding.GetCharCount]bytes!=null");
            Contract.Assert(count >= 0, "[UnicodeEncoding.GetCharCount]count >=0");

            UnicodeEncoding.Decoder decoder = (UnicodeEncoding.Decoder)baseDecoder;

            byte* byteEnd = bytes + count;
            byte* byteStart = bytes;

            // Need last vars
            int lastByte = -1;
            char lastChar = (char)0;

            // Start by assuming same # of chars as bytes
            int charCount = count >> 1;

            // Need -1 to check 2 at a time.  If we have an even #, longBytes will go
            // from longEnd - 1/2 long to longEnd + 1/2 long.  If we're odd, longBytes
            // will go from longEnd - 1 long to longEnd. (Might not get to use this)
            ulong* longEnd = (ulong*)(byteEnd - 7);

            // For fallback we may need a fallback buffer
            DecoderFallbackBuffer fallbackBuffer = null;

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
                // (don't have to check m_throwOnOverflow for count)
                Contract.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0,
                    "[UnicodeEncoding.GetCharCount]Expected empty fallback buffer at start");
            }

            while (bytes < byteEnd)
            {
                // If we're aligned then maybe we can do it fast
                // This'll hurt if we're unaligned because we'll always test but never be aligned
#if !NO_FAST_UNICODE_LOOP
#if BIGENDIAN
                if (bigEndian &&
#else // BIGENDIAN
                if (!bigEndian &&
#endif // BIGENDIAN
#if BIT64 // win64 has to be long aligned
                    (unchecked((long)bytes) & 7) == 0 &&
#else
                    (unchecked((int)bytes) & 3) == 0 &&
#endif // BIT64
                    lastByte == -1 && lastChar == 0)
                {
                    // Need new char* so we can check 4 at a time
                    ulong* longBytes = (ulong*)bytes;

                    while (longBytes < longEnd)
                    {
                        // See if we potentially have surrogates (0x8000 bit set)
                        // (We're either big endian on a big endian machine or little endian on 
                        // a little endian machine so this'll work)
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
#if BIGENDIAN
                                if (((0xfc00fc00fc00fc00 & *longBytes) ^ 0xd800dc00d800dc00) != 0)
#else
                                if (((0xfc00fc00fc00fc00 & *longBytes) ^ 0xdc00d800dc00d800) != 0)
#endif
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
#endif // !NO_FAST_UNICODE_LOOP

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
                            byte[] byteBuffer = null;
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
                        byte[] byteBuffer = null;
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
                    // Uncount the last high surrogate
                    charCount--;

                    // fall back the high surrogate.
                    byte[] byteBuffer = null;
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
                    byte[] byteBuffer = null;
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
                    charCount += fallbackBuffer.InternalFallback( new byte[] { unchecked((byte)lastByte) }, bytes);
                    lastByte = -1;
                }
            }

            // If we had a high surrogate left over, we can't count it
            if (lastChar > 0)
                charCount--;

            // Shouldn't have anything in fallback buffer for GetCharCount
            // (don't have to check m_throwOnOverflow for count)
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[UnicodeEncoding.GetCharCount]Expected empty fallback buffer at end");

            return charCount;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal override unsafe int GetChars(byte* bytes, int byteCount,
                                                char* chars, int charCount, DecoderNLS baseDecoder )
        {
            Contract.Assert(chars!=null, "[UnicodeEncoding.GetChars]chars!=null");
            Contract.Assert(byteCount >=0, "[UnicodeEncoding.GetChars]byteCount >=0");
            Contract.Assert(charCount >=0, "[UnicodeEncoding.GetChars]charCount >=0");
            Contract.Assert(bytes!=null, "[UnicodeEncoding.GetChars]bytes!=null");

            UnicodeEncoding.Decoder decoder = (UnicodeEncoding.Decoder)baseDecoder;

            // Need last vars
            int lastByte = -1;
            char lastChar = (char)0;

            // Get our decoder (but don't clear it yet)
            if (decoder != null)
            {
                lastByte = decoder.lastByte;
                lastChar = decoder.lastChar;

                // Shouldn't have anything in fallback buffer for GetChars
                // (don't have to check m_throwOnOverflow for chars)
                Contract.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0,
                    "[UnicodeEncoding.GetChars]Expected empty fallback buffer at start");
            }

            // For fallback we may need a fallback buffer
            DecoderFallbackBuffer fallbackBuffer = null;

            byte* byteEnd = bytes + byteCount;
            char* charEnd = chars + charCount;
            byte* byteStart = bytes;
            char* charStart = chars;

            while (bytes < byteEnd)
            {
                // If we're aligned then maybe we can do it fast
                // This'll hurt if we're unaligned because we'll always test but never be aligned
#if !NO_FAST_UNICODE_LOOP
#if BIGENDIAN
                if (bigEndian &&
#else // BIGENDIAN
                if (!bigEndian &&
#endif // BIGENDIAN
#if BIT64 // win64 has to be long aligned
                    (unchecked((long)chars) & 7) == 0 && (unchecked((long)bytes) & 7) == 0 &&
#else
                    (unchecked((int)chars) & 3) == 0 && (unchecked((int)bytes) & 3) == 0 &&
#endif // BIT64
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
                        // a little endian machine so this'll work)
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
#if BIGENDIAN
                                if (((0xfc00fc00fc00fc00 & *longBytes) ^ 0xd800dc00d800dc00) != 0)
#else
                                if (((0xfc00fc00fc00fc00 & *longBytes) ^ 0xdc00d800dc00d800) != 0)
#endif
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
                        *longChars = *longBytes;
                        longBytes++;
                        longChars++;
                    }

                    chars = (char*)longChars;
                    bytes = (byte*)longBytes;

                    if (bytes >= byteEnd)
                        break;
                }
#endif // !NO_FAST_UNICODE_LOOP

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
                            byte[] byteBuffer = null;
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

                            if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                            {
                                // couldn't fall back lonely surrogate
                                // We either advanced bytes or chars should == charStart and throw below
                                Contract.Assert(bytes >= byteStart + 2 || chars == charStart,
                                    "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (bad surrogate)");
                                bytes-=2;                                       // didn't use these 2 bytes
                                fallbackBuffer.InternalReset();
                                ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                                break;                                          // couldn't fallback but didn't throw
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
                        byte[] byteBuffer = null;
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

                        if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                        {
                            // couldn't fall back lonely surrogate
                            // We either advanced bytes or chars should == charStart and throw below
                            Contract.Assert(bytes >= byteStart + 2 || chars == charStart,
                                "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (lonely surrogate)");
                            bytes-=2;                                       // didn't use these 2 bytes
                            fallbackBuffer.InternalReset();
                            ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                            break;                                          // couldn't fallback but didn't throw
                        }

                        // Didn't throw, ignore this one (we already did its fallback)
                        continue;
                    }

                    // Valid surrogate pair, add our lastChar (will need 2 chars)
                    if (chars >= charEnd - 1)
                    {
                        // couldn't find room for this surrogate pair
                        // We either advanced bytes or chars should == charStart and throw below
                        Contract.Assert(bytes >= byteStart + 2 || chars == charStart,
                            "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (surrogate pair)");
                        bytes-=2;                                       // didn't use these 2 bytes
                        ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                        // Leave lastChar for next call to Convert()
                        break;                                          // couldn't fallback but didn't throw
                    }

                    *chars++ = lastChar;
                    lastChar = (char)0;
                }
                else if (lastChar > 0)
                {
                    // Had a high surrogate, expected a low surrogate, fall back the high surrogate.
                    byte[] byteBuffer = null;
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

                    if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                    {
                        // couldn't fall back high surrogate, or char that would be next
                        // We either advanced bytes or chars should == charStart and throw below
                        Contract.Assert(bytes >= byteStart + 2 || chars == charStart,
                            "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (no low surrogate)");
                        bytes-=2;                                       // didn't use these 2 bytes
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                        break;                                          // couldn't fallback but didn't throw
                    }

                    // Not left over now, clear previous high surrogate and continue to add current char
                    lastChar = (char)0;
                }

                // Valid char, room for it?
                if (chars >= charEnd)
                {
                    // 2 bytes couldn't fall back
                    // We either advanced bytes or chars should == charStart and throw below
                    Contract.Assert(bytes >= byteStart + 2 || chars == charStart,
                        "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (normal)");
                    bytes-=2;                                       // didn't use these bytes
                    ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                    break;                                          // couldn't fallback but didn't throw
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
                    byte[] byteBuffer = null;
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

                    if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                    {
                        // 2 bytes couldn't fall back
                        // We either advanced bytes or chars should == charStart and throw below
                        Contract.Assert(bytes >= byteStart + 2 || chars == charStart,
                            "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (decoder)");
                        bytes-=2;                                       // didn't use these bytes
                        if (lastByte >= 0)
                            bytes--;                                    // had an extra last byte hanging around
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
                        // We'll remember these in our decoder though
                        bytes+=2;
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
                    if (!fallbackBuffer.InternalFallback( new byte[] { unchecked((byte)lastByte) }, bytes, ref chars ))
                    {
                        // odd byte couldn't fall back
                        bytes--;                                        // didn't use this byte
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);// Might throw, if no chars output
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
                Contract.Assert((decoder.MustFlush == false) || ((lastChar == (char)0) && (lastByte == -1)),
                    "[UnicodeEncoding.GetChars] Expected no left over chars or bytes if flushing"
//                    + " " + ((int)lastChar).ToString("X4") + " " + lastByte.ToString("X2")
                    );

                decoder.m_bytesUsed = (int)(bytes - byteStart);
                decoder.lastChar = lastChar;
                decoder.lastByte = lastByte;
            }

            // Used to do this the old way
            // System.IO.__UnmanagedMemoryStream.memcpyimpl((byte*)chars, bytes, byteCount);

            // Shouldn't have anything in fallback buffer for GetChars
            // (don't have to check m_throwOnOverflow for count or chars)
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0,
                "[UnicodeEncoding.GetChars]Expected empty fallback buffer at end");

            return (int)(chars - charStart);
        }


        [System.Runtime.InteropServices.ComVisible(false)]
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
            return EmptyArray<Byte>.Value;
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

            // 2 bytes per char
            byteCount <<= 1;

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

            // long because byteCount could be biggest int.
            // 1 char per 2 bytes.  Round up in case 1 left over in decoder.
            // Round up using &1 in case byteCount is max size
            // Might also need an extra 1 if there's a left over high surrogate in the decoder.
            long charCount = (long)(byteCount >> 1) + (byteCount & 1) + 1;

            // Don't forget fallback (in case they have a bunch of lonely surrogates or something bizzare like that)
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));

            return (int)charCount;
        }


        public override bool Equals(Object value)
        {
            UnicodeEncoding that = value as UnicodeEncoding;
            if (that != null)
            {
                //
                // Big Endian Unicode has different code page (1201) than small Endian one (1200),
                // so we still have to check m_codePage here.
                //
                return (CodePage == that.CodePage) &&
                        byteOrderMark == that.byteOrderMark &&
//                        isThrowException == that.isThrowException &&  // Same as Encoder/Decoder being exception fallbacks
                        bigEndian == that.bigEndian &&
                       (EncoderFallback.Equals(that.EncoderFallback)) &&
                       (DecoderFallback.Equals(that.DecoderFallback));
            }
            return (false);
        }

        public override int GetHashCode()
        {
            return CodePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() +
                   (byteOrderMark?4:0) + (bigEndian?8:0);
        }

        [Serializable]
        private class Decoder : System.Text.DecoderNLS, ISerializable
        {
            internal int lastByte = -1;
            internal char lastChar = '\0';

            public Decoder(UnicodeEncoding encoding) : base(encoding)
            {
                // base calls reset
            }

            // Constructor called by serialization, have to handle deserializing from Everett
            internal Decoder(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // Get Common Info
                this.lastByte = (int)info.GetValue("lastByte", typeof(int));

                try
                {
                    // Try the encoding, which is only serialized in Whidbey
                    this.m_encoding = (Encoding)info.GetValue("m_encoding", typeof(Encoding));
                    this.lastChar = (char)info.GetValue("lastChar", typeof(char));
                    this.m_fallback = (DecoderFallback)info.GetValue("m_fallback", typeof(DecoderFallback));
                }
                catch (SerializationException)
                {
                    // Everett didn't serialize the UnicodeEncoding, get the default one
                    bool bigEndian = (bool)info.GetValue("bigEndian", typeof(bool));
                    this.m_encoding = new UnicodeEncoding(bigEndian, false);
                }
            }

            // ISerializable implementation, get data for this object
            [System.Security.SecurityCritical]  // auto-generated_required
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                // Save Whidbey data
                info.AddValue("m_encoding", this.m_encoding);
                info.AddValue("m_fallback", this.m_fallback);
                info.AddValue("lastChar", this.lastChar);       // Unused by everett so it'll probably get lost
                info.AddValue("lastByte", this.lastByte);

                // Everett Only
                info.AddValue("bigEndian", ((UnicodeEncoding)(this.m_encoding)).bigEndian);
            }

            public override void Reset()
            {
                lastByte = -1;
                lastChar = '\0';
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            // Anything left in our decoder?
            internal override bool HasState
            {
                get
                {
                    return (this.lastByte != -1 || this.lastChar != '\0');
                }
            }
        }
    }
}

